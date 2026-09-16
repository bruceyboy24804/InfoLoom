import React, { FC, useCallback, useMemo, useState } from 'react';
import { useValue } from 'cs2/api';
import {
  DraggablePanelProps,
  Panel,
  Scrollable,
  Button,
  Dropdown,
  DropdownItem,
  DropdownToggle,
  PanelFoldout,
  Tooltip,
} from 'cs2/ui';
import { useLocalization } from 'cs2/l10n';
import { Color } from 'cs2/bindings';
import { EffectsBinding, OverlayEffects, ToggleOverlay, EffectColors, ChangeEffectColor } from 'mods/bindings';
import { formatWords } from 'mods/InfoLoomSections/utils/formatText';
import { VanillaComponentResolver } from 'mods/VanillaComponents/VanillaComponents';
import { ModuleResolver } from 'mods/ModuleResolver/moduleResolver';
import styles from './AllEffects.module.scss';

const ALL_TYPES = '__all__';

/** Which slice of buildings the list is showing. Driven by the stat tiles. */
type Scope = 'all' | 'local' | 'city' | 'both' | 'overlays';

const formatDelta = (min: number, max: number): string => {
  const sign = (v: number) => (v > 0 ? `+${v.toFixed(1)}` : v.toFixed(1));
  if (min === max) return sign(min);
  return `${sign(min)} ~ ${sign(max)}`;
};

const formatRadius = (min: number, max: number): string =>
  min === max ? `${max.toFixed(0)}m` : `${min.toFixed(0)}–${max.toFixed(0)}m`;

const rgbaToColor = (rgba: number[]): Color => ({
  r: (rgba[0] ?? 255) / 255,
  g: (rgba[1] ?? 255) / 255,
  b: (rgba[2] ?? 255) / 255,
  a: (rgba[3] ?? 230) / 255,
});

const rgbaToCss = (rgba?: number[]): string =>
  rgba
    ? `rgba(${rgba[0] ?? 255}, ${rgba[1] ?? 255}, ${rgba[2] ?? 255}, ${(rgba[3] ?? 230) / 255})`
    : 'rgba(255, 255, 255, 0.5)';

const StatTile: FC<{
  label: string;
  value: number;
  active: boolean;
  tooltip: string;
  onSelect: () => void;
}> = ({ label, value, active, tooltip, onSelect }) => (
  <Tooltip tooltip={tooltip}>
    <div
      className={`${styles.statTile} ${active ? styles.statTileActive : ''}`}
      onClick={(e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        onSelect();
      }}
    >
      <div className={styles.statValue}>{value}</div>
      <div className={styles.statLabel}>{label}</div>
    </div>
  </Tooltip>
);

const ColorSwatch: FC<{ effectType: string; rgba: number[] }> = ({ effectType, rgba }) => {
  const ColorField = VanillaComponentResolver.instance.ColorField;
  const handleChange = useCallback(
    (newColor: Color) => {
      const r = Math.round(newColor.r * 255);
      const g = Math.round(newColor.g * 255);
      const b = Math.round(newColor.b * 255);
      const a = Math.round(newColor.a * 255);
      ChangeEffectColor(`${effectType}:${r}:${g}:${b}:${a}`);
    },
    [effectType]
  );

  return (
    <ColorField
      value={rgbaToColor(rgba)}
      focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
      onChange={handleChange}
    />
  );
};

const AllEffects: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const { translate } = useLocalization();
  const effects = useValue(EffectsBinding.binding);
  const overlayEffects = useValue(OverlayEffects);
  const effectColorList = useValue(EffectColors.binding);

  const [typeFilter, setTypeFilter] = useState<string>(ALL_TYPES);
  const [scope, setScope] = useState<Scope>('all');

  const overlaySet = useMemo(() => new Set(overlayEffects), [overlayEffects]);
  const colorMap = useMemo(() => {
    const map: Record<string, number[]> = {};
    for (const c of effectColorList) {
      map[c.Type] = [c.R, c.G, c.B, c.A];
    }
    return map;
  }, [effectColorList]);

  const hasOverlay = useCallback(
    (entity: { EntityIndex: number; Modifiers: { Type: string }[] }) =>
      entity.Modifiers.some(m => overlaySet.has(`${entity.EntityIndex}:${m.Type}`)),
    [overlaySet]
  );

  // Scope is judged on the entity's full modifier sets, not the type-filtered ones, so
  // changing the type filter never reclassifies a building out from under you.
  const matchesScope = useCallback(
    (e: (typeof effects)[number], s: Scope) => {
      const hasLocal = e.Modifiers.length > 0;
      const hasCity = e.CityModifiers.length > 0;
      switch (s) {
        case 'local':
          return hasLocal && !hasCity;
        case 'city':
          return hasCity && !hasLocal;
        case 'both':
          return hasLocal && hasCity;
        case 'overlays':
          return hasOverlay(e);
        default:
          return true;
      }
    },
    [hasOverlay]
  );

  // Effect types present within a given scope, so the dropdown never offers a type that
  // would yield an empty list once the tile filter is applied.
  const typesForScope = useCallback(
    (s: Scope) => {
      const set = new Set<string>();
      for (const e of effects) {
        if (!matchesScope(e, s)) continue;
        if (s === 'overlays') {
          // Only overlaid effects are listed in this scope, so only their types apply.
          for (const m of e.Modifiers) {
            if (overlaySet.has(`${e.EntityIndex}:${m.Type}`)) set.add(m.Type);
          }
        } else {
          for (const m of e.Modifiers) set.add(m.Type);
          for (const m of e.CityModifiers) set.add(m.Type);
        }
      }
      return Array.from(set).sort();
    },
    [effects, matchesScope, overlaySet]
  );

  const availableTypes = useMemo(() => typesForScope(scope), [typesForScope, scope]);

  // Derived from the same list the panel renders, so every tile count equals the number of
  // rows you get by clicking it. (The C# EffectCount binding counts provider entities that
  // may have no modifiers at all, which would not match.)
  const counts = useMemo(() => {
    let local = 0;
    let city = 0;
    let both = 0;
    let overlays = 0;
    for (const e of effects) {
      const hasLocal = e.Modifiers.length > 0;
      const hasCity = e.CityModifiers.length > 0;
      if (hasLocal && hasCity) both++;
      else if (hasLocal) local++;
      else if (hasCity) city++;
      if (hasOverlay(e)) overlays++;
    }
    return { all: local + city + both, local, city, both, overlays };
  }, [effects, hasOverlay]);

  const visibleEntities = useMemo(() => {
    const matchesType = (t: string) => typeFilter === ALL_TYPES || t === typeFilter;

    return effects
      .filter(e => matchesScope(e, scope))
      .map(entity => {
        const locals = entity.Modifiers.filter(
          m => matchesType(m.Type) && (scope !== 'overlays' || overlaySet.has(`${entity.EntityIndex}:${m.Type}`))
        );
        // City modifiers have no radius, so they are never drawn as overlays.
        const cities = scope === 'overlays' ? [] : entity.CityModifiers.filter(m => matchesType(m.Type));
        return { entity, locals, cities };
      })
      .filter(g => g.locals.length > 0 || g.cities.length > 0)
      .sort((a, b) => (a.entity.Name ?? '').localeCompare(b.entity.Name ?? ''));
  }, [effects, typeFilter, scope, overlaySet, matchesScope]);

  const activeOverlayCount = overlayEffects.length;

  // Clicking the active tile clears the filter, so the tiles double as a toggle. Drop the
  // type filter too if the new scope has no such type, which would strand an empty list.
  const selectScope = (next: Scope) => {
    const resolved = next !== 'all' && next === scope ? 'all' : next;
    setScope(resolved);
    if (typeFilter !== ALL_TYPES && !typesForScope(resolved).includes(typeFilter)) {
      setTypeFilter(ALL_TYPES);
    }
  };

  const typeItems = [ALL_TYPES, ...availableTypes].map(type => {
    const selected = type === typeFilter;
    const label = type === ALL_TYPES ? 'All Types' : formatWords(type);
    return (
      <DropdownItem
        key={type}
        theme={ModuleResolver.instance.DropdownClasses}
        value={type}
        closeOnSelect={true}
        selected={selected}
        onChange={() => setTypeFilter(type)}
      >
        {label}
      </DropdownItem>
    );
  });

  const renderModifierRow = (
    key: string,
    type: string,
    value: string,
    radius: string | null,
    overlayKey: string | null
  ) => {
    const isActive = overlayKey !== null && overlaySet.has(overlayKey);
    return (
      <div key={key} className={`${styles.modifierRow} ${isActive ? styles.modifierRowActive : ''}`}>
        <div className={styles.dot} style={{ backgroundColor: rgbaToCss(colorMap[type]) }} />
        <div className={styles.modifierType}>{formatWords(type)}</div>
        <div className={styles.modifierValue}>{value}</div>
        <div className={styles.modifierRadius}>{radius ?? <span className={styles.muted}>—</span>}</div>
        <div className={styles.modifierAction}>
          {overlayKey !== null ? (
            <Button
              variant="flat"
              className={`${styles.overlayButton} ${isActive ? styles.overlayButtonActive : ''}`}
              onSelect={() => ToggleOverlay(overlayKey)}
            >
              {isActive ? 'Shown' : 'Show'}
            </Button>
          ) : (
            <Tooltip tooltip="City-wide effects have no radius to draw.">
              <span className={styles.cityTag}>City</span>
            </Tooltip>
          )}
        </div>
      </div>
    );
  };

  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={initialPosition ?? { x: 0.5, y: 0.5 }}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>{translate('InfoLoomTwo.EffectsPanel[Title]', 'Effects')}</span>
        </div>
      }
    >
      <div className={styles.content}>
        <div className={styles.statRow}>
          <StatTile
            label="All"
            value={counts.all}
            active={scope === 'all'}
            tooltip="Every building with effects. Click to clear the filter."
            onSelect={() => selectScope('all')}
          />
          <StatTile
            label="Local"
            value={counts.local}
            active={scope === 'local'}
            tooltip="Buildings with local (radius) effects only. Click to filter."
            onSelect={() => selectScope('local')}
          />
          <StatTile
            label="City"
            value={counts.city}
            active={scope === 'city'}
            tooltip="Buildings with city-wide effects only. Click to filter."
            onSelect={() => selectScope('city')}
          />
          <StatTile
            label="Both"
            value={counts.both}
            active={scope === 'both'}
            tooltip="Buildings with both local and city-wide effects. Click to filter."
            onSelect={() => selectScope('both')}
          />
          <StatTile
            label="Overlays"
            value={counts.overlays}
            active={scope === 'overlays'}
            tooltip="Buildings with an overlay currently drawn. Click to filter."
            onSelect={() => selectScope('overlays')}
          />
        </div>

        <div className={styles.controls}>
          <Dropdown theme={ModuleResolver.instance.DropdownClasses} content={typeItems}>
            <DropdownToggle className={styles.dropdownToggle}>
              {typeFilter === ALL_TYPES ? 'All Types' : formatWords(typeFilter)}
            </DropdownToggle>
          </Dropdown>

          {activeOverlayCount > 0 && (
            <Button
              variant="flat"
              className={styles.clearButton}
              onSelect={() => overlayEffects.forEach(k => ToggleOverlay(k))}
            >
              Clear
            </Button>
          )}
        </div>

        <PanelFoldout header={<div className={styles.foldoutHeader}>Effect Colors</div>} initialExpanded={false}>
          <div className={styles.legend}>
            {effectColorList.map(c => (
              <div key={c.Type} className={styles.legendRow}>
                <span className={styles.legendLabel}>{formatWords(c.Type)}</span>
                <ColorSwatch effectType={c.Type} rgba={[c.R, c.G, c.B, c.A]} />
              </div>
            ))}
          </div>
        </PanelFoldout>

        <div className={styles.resultCount}>{`${visibleEntities.length} of ${effects.length} buildings`}</div>

        <Scrollable className={styles.scrollable} vertical smooth>
          {effects.length === 0 && <div className={styles.emptyState}>No buildings with effects in this city.</div>}
          {effects.length > 0 && visibleEntities.length === 0 && (
            <div className={styles.emptyState}>No effects match the current filter.</div>
          )}
          {visibleEntities.map(({ entity, locals, cities }) => (
            <div key={entity.EntityIndex} className={styles.entityRow}>
              <div className={styles.entityHeader}>
                <span className={styles.entityName}>{entity.Name}</span>
                <span className={styles.entityCount}>{locals.length + cities.length}</span>
              </div>
              {locals.map((m, i) =>
                renderModifierRow(
                  `local-${i}`,
                  m.Type,
                  formatDelta(m.DeltaMin, m.DeltaMax),
                  formatRadius(m.RadiusMin, m.RadiusMax),
                  `${entity.EntityIndex}:${m.Type}`
                )
              )}
              {cities.map((m, i) =>
                renderModifierRow(`city-${i}`, m.Type, formatDelta(m.DeltaMin, m.DeltaMax), null, null)
              )}
            </div>
          ))}
        </Scrollable>
      </div>
    </Panel>
  );
};

export default AllEffects;
