import { SelectedInfoSectionBase, Theme } from 'cs2/bindings';
import { getModule } from 'cs2/modding';
import { Button, PanelSectionRow, FOCUS_AUTO, PanelFoldout } from 'cs2/ui';
import { InfoRowSCSS } from 'mods/InfoLoomSections/ILInfoSections/Modules/info-Row/info-Row.module.scss';
import { InfoSectionFoldout } from 'mods/InfoLoomSections/ILInfoSections/Modules/info-Section/info-section-foldout';
import classNames from 'classnames';
import { useValue } from 'cs2/api';
import { useCallback, useMemo } from 'react';
import { Color } from 'cs2/bindings';
import { VanillaComponentResolver } from 'mods/VanillaComponents/VanillaComponents';
import { OverlayEffects, ToggleOverlay, EffectColors, ChangeEffectColor } from '../../../bindings';
import { formatWords } from 'mods/InfoLoomSections/utils/formatText';

export const InfoRowTheme: Theme | any = getModule(
	'game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss',
	'classes'
);

interface LocalModifier {
	type: string;
	mode: string;
	radiusCombineMode: string;
	deltaMin: number;
	deltaMax: number;
	radiusMin: number;
	radiusMax: number;
}

interface CityModifier {
	type: string;
	mode: string;
	deltaMin: number;
	deltaMax: number;
}

interface ILEffectsSection extends SelectedInfoSectionBase {
	entityIndex: number;
	localModifiers: LocalModifier[];
	cityModifiers: CityModifier[];
}

const formatDelta = (min: number, max: number): string => {
	if (min === max) return min.toFixed(1);
	return `${min.toFixed(1)} ~ ${Math.abs(max).toFixed(1)}`;
};

const formatRadius = (min: number, max: number): string => {
	return `${min.toFixed(1)} ~ ${Math.abs(max).toFixed(0)}`;
};

const rgbaToColor = (rgba: number[]): Color => ({
	r: (rgba[0] ?? 255) / 255,
	g: (rgba[1] ?? 255) / 255,
	b: (rgba[2] ?? 255) / 255,
	a: (rgba[3] ?? 230) / 255,
});

const ColorSwatch = ({ effectType, rgba }: { effectType: string; rgba: number[] }) => {
	const ColorField = VanillaComponentResolver.instance.ColorField;
	const handleChange = useCallback((newColor: Color) => {
		const r = Math.round(newColor.r * 255);
		const g = Math.round(newColor.g * 255);
		const b = Math.round(newColor.b * 255);
		const a = Math.round(newColor.a * 255);
		ChangeEffectColor(`${effectType}:${r}:${g}:${b}:${a}`);
	}, [effectType]);

	return (
		<ColorField
			value={rgbaToColor(rgba)}
			focusKey={VanillaComponentResolver.instance.FOCUS_DISABLED}
			onChange={handleChange}
		/>
	);
};

let PanelOpen: boolean = false;

export const ILEffectsInfoSection = (componentList: any): any => {
	componentList['InfoLoomTwo.Systems.Sections.ILEffectsSection'] = (props: ILEffectsSection) => {
		const overlayEffects = useValue(OverlayEffects);
		const overlaySet = useMemo(() => new Set(overlayEffects), [overlayEffects]);
		const effectColorList = useValue(EffectColors);
		const colorMap = useMemo(() => {
			const map: Record<string, number[]> = {};
			for (const c of effectColorList) {
				map[c.Type] = [c.R, c.G, c.B, c.A];
			}
			return map;
		}, [effectColorList]);

		const hasLocal = props.localModifiers && props.localModifiers.length > 0;
		const hasCity = props.cityModifiers && props.cityModifiers.length > 0;

		return (
			<InfoSectionFoldout
				header={
					<div className={InfoRowTheme.infoRow}>
						<div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>Effects</div>
						<div className={classNames(InfoRowSCSS.right)}>
							{[
								hasLocal && `${props.localModifiers.length} Local`,
								hasCity && `${props.cityModifiers.length} City`
							].filter(Boolean).join(' / ')}
						</div>
					</div>
				}
				initialExpanded={PanelOpen}
				expandFromContent={false}
				focusKey={FOCUS_AUTO}
				onToggleExpanded={(value: boolean) => {
					PanelOpen = value;
				}}
			>
				{hasLocal && (
					<PanelFoldout header="Local Modifiers" initialExpanded={true}>
						{props.localModifiers.map((m, i) => {
							const key = `${props.entityIndex}:${m.type}`;
							const isActive = overlaySet.has(key);
							const rgba = colorMap[m.type];
							return (
								<div key={`local-${i}`}>
									<PanelSectionRow
										left={m.type}
										right={
											<Button
												variant="text"
												onSelect={() => ToggleOverlay(key)}
											>
												{isActive ? 'Show' : 'Hide'}
											</Button>
										}
										disableFocus={true}
										subRow={false}
										className={InfoRowTheme.infoRow}
									/>
									<PanelSectionRow
										left="Delta"
										right={formatDelta(m.deltaMin, m.deltaMax)}
										disableFocus={true}
										subRow={true}
										className={InfoRowTheme.infoRow}
									/>
									<PanelSectionRow
										left="Radius"
										right={formatRadius(m.radiusMin, m.radiusMax)}
										disableFocus={true}
										subRow={true}
										className={InfoRowTheme.infoRow}
									/>
									<PanelSectionRow
										left="Mode"
										right={formatWords(m.mode)}
										disableFocus={true}
										subRow={true}
										className={InfoRowTheme.infoRow}
									/>
									{rgba && (
										<PanelSectionRow
											left="Color"
											right={<ColorSwatch effectType={m.type} rgba={rgba} />}
											disableFocus={true}
											subRow={true}
											className={InfoRowTheme.infoRow}
										/>
									)}
								</div>
							);
						})}
					</PanelFoldout>
				)}
				{hasCity && (
					<PanelFoldout header="City Modifiers" initialExpanded={true}>
						{props.cityModifiers.map((m, i) => (
							<div key={`city-${i}`}>
								<PanelSectionRow
									left={formatWords(m.type)}
									right={formatWords(m.mode)}
									disableFocus={true}
									subRow={false}
									className={InfoRowTheme.infoRow}
								/>
								<PanelSectionRow
									left="Delta"
									right={formatDelta(m.deltaMin, m.deltaMax)}
									disableFocus={true}
									subRow={true}
									className={InfoRowTheme.infoRow}
								/>
							</div>
						))}
					</PanelFoldout>
				)}
			</InfoSectionFoldout>
		);
	};
	return componentList as any;
};
