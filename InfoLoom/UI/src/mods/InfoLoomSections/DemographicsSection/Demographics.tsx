import React, { memo, useCallback, useState } from 'react';
import styles from './Demographics.module.scss';
import { useLocalization } from 'cs2/l10n';
import { bindValue, useValue, trigger } from 'cs2/api';
import { InfoCheckbox } from 'mods/components/InfoCheckbox/InfoCheckbox';
import { DraggablePanelProps, Dropdown, DropdownToggle, Panel, Scrollable, DropdownItem, Button } from 'cs2/ui';
import { DemographicsDataOldestCitizen, DemographicsDataTotals, DemoStatsToggledOn, SetDemoStatsToggledOn, DemographicsCensusCrossTab } from '../../bindings';
import { getModule } from 'cs2/modding';
import { useLegendLabels, useLifecycleLabels, useCensusLabels, useAgeGranularityLabels } from './hooks';
import {
  AgeGranularity,
  AGE_GRANULARITIES,
  COLUMN_SAFE_AGE_GRANULARITIES,
  CensusDimension,
  CENSUS_DIMENSIONS,
} from './census';
import { DistrictSelector } from 'mods/InfoloomInfoviewContents/DistrictSelector/districtSelector';
import { StatisticsPanel, DemographicsChart, ErrorBoundary, LoadingSpinner } from './components';
import mod from 'mod.json';
import { Localekeys } from 'mods/locale';

const demographics = bindValue<boolean>(mod.id, 'BINDING:demographics', false);
const updateDemographics = (value: boolean) => trigger(mod.id, 'TRIGGER:updateDemographics', value);
const setCensusDimensions = (row: CensusDimension, col: CensusDimension, ageGranularity: AgeGranularity) =>
  trigger(mod.id, 'TRIGGER:setCensusDimensions', row, col, ageGranularity);

const DropdownStyle = getModule('game-ui/menu/themes/dropdown.module.scss', 'classes');

// === Main Demographics Component ===
const Demographics = ({ onClose }: DraggablePanelProps): JSX.Element => {
  const { translate } = useLocalization();
  const censusCrossTab = useValue(DemographicsCensusCrossTab.binding);
  const demographicsDataStructureTotals = useValue(DemographicsDataTotals.binding);
  const demographicsDataOldestCitizen = useValue(DemographicsDataOldestCitizen);
  const demoStatsToggledOn = useValue(DemoStatsToggledOn);
  const demographicsValue = useValue(demographics);

  // Use custom hooks for translated labels
  const lifecycleLabels = useLifecycleLabels(translate);
  const legendLabels = useLegendLabels(translate);
  const censusLabels = useCensusLabels(translate, legendLabels, lifecycleLabels);
  const ageGranularityLabels = useAgeGranularityLabels(translate);

  // Census: user-selected axes (and age resolution) for the free-form cross-tab chart
  const [censusRowDim, setCensusRowDim] = useState<CensusDimension>(CensusDimension.Age);
  const [censusColDim, setCensusColDim] = useState<CensusDimension>(CensusDimension.Activity);
  const [censusAgeGranularity, setCensusAgeGranularity] = useState<AgeGranularity>(AgeGranularity.Lifecycle);
  const changeCensusRow = useCallback(
    (dim: CensusDimension) => {
      setCensusRowDim(dim);
      setCensusDimensions(dim, censusColDim, censusAgeGranularity);
    },
    [censusColDim, censusAgeGranularity]
  );
  const changeCensusCol = useCallback(
    (dim: CensusDimension) => {
      // Age becomes a stacked segment + legend entry per bucket when it's the Column
      // dimension, so FiveYear/Detailed (24/120 buckets) aren't usable there — drop back to a
      // column-safe granularity if the current one wouldn't fit.
      const granularity =
        dim === CensusDimension.Age && !COLUMN_SAFE_AGE_GRANULARITIES.includes(censusAgeGranularity)
          ? AgeGranularity.Lifecycle
          : censusAgeGranularity;
      setCensusColDim(dim);
      setCensusAgeGranularity(granularity);
      setCensusDimensions(censusRowDim, dim, granularity);
    },
    [censusRowDim, censusAgeGranularity]
  );
  const changeCensusAgeGranularity = useCallback(
    (granularity: AgeGranularity) => {
      setCensusAgeGranularity(granularity);
      setCensusDimensions(censusRowDim, censusColDim, granularity);
    },
    [censusRowDim, censusColDim]
  );
  const ageIsSelected = censusRowDim === CensusDimension.Age || censusColDim === CensusDimension.Age;
  const availableAgeGranularities =
    censusColDim === CensusDimension.Age ? COLUMN_SAFE_AGE_GRANULARITIES : AGE_GRANULARITIES;

  return (
    <Panel
      draggable
      onClose={onClose}
      className={styles.panel}
      initialPosition={{ x: 0.16, y: 0.15 }}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>{translate(Localekeys.Demographics, 'Demographics')}</span>
        </div>
      }
    >
      <div className={styles.container}>
        <div className={styles.toggleContainer}>
          <DistrictSelector />
          <InfoCheckbox
            label={translate(Localekeys.ShowStatistics, 'Show Statistics')}
            isChecked={demoStatsToggledOn}
            onToggle={SetDemoStatsToggledOn}
          />
          <Button
            onSelect={() => updateDemographics(true)}
            className={styles.refreshButton}
            variant="flat"
            type="button"
          >
            {translate(Localekeys.Refresh, 'Refresh')}
          </Button>
        </div>

        {demoStatsToggledOn && (
          <StatisticsPanel
            totals={demographicsDataStructureTotals}
            oldestCitizen={demographicsDataOldestCitizen}
            translate={translate}
          />
        )}
        <div className={styles.chartToggle} style={{ gap: '24px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span>{translate(Localekeys.CensusRowLabel, 'Row')}:</span>
            <Dropdown
              theme={DropdownStyle}
              content={CENSUS_DIMENSIONS.filter(dim => dim !== censusColDim).map(dim => (
                <DropdownItem
                  key={dim}
                  value={dim}
                  closeOnSelect={true}
                  onChange={() => changeCensusRow(dim)}
                  className={DropdownStyle.dropdownItem}
                  selected={censusRowDim === dim}
                >
                  <div className={styles.dropdownItem}>
                    <span>{censusLabels.dimensionNames[dim]}</span>
                  </div>
                </DropdownItem>
              ))}
            >
              <DropdownToggle disabled={false}>
                <div className={styles.dropdownName}>{censusLabels.dimensionNames[censusRowDim]}</div>
              </DropdownToggle>
            </Dropdown>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <span>{translate(Localekeys.CensusColumnLabel, 'Column')}:</span>
            <Dropdown
              theme={DropdownStyle}
              content={CENSUS_DIMENSIONS.filter(dim => dim !== censusRowDim).map(dim => (
                <DropdownItem
                  key={dim}
                  value={dim}
                  closeOnSelect={true}
                  onChange={() => changeCensusCol(dim)}
                  className={DropdownStyle.dropdownItem}
                  selected={censusColDim === dim}
                >
                  <div className={styles.dropdownItem}>
                    <span>{censusLabels.dimensionNames[dim]}</span>
                  </div>
                </DropdownItem>
              ))}
            >
              <DropdownToggle disabled={false}>
                <div className={styles.dropdownName}>{censusLabels.dimensionNames[censusColDim]}</div>
              </DropdownToggle>
            </Dropdown>
          </div>
          {ageIsSelected && (
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <span>{translate(Localekeys.AgeGroupLabel, 'Age Groups')}:</span>
              <Dropdown
                theme={DropdownStyle}
                content={availableAgeGranularities.map(granularity => (
                  <DropdownItem
                    key={granularity}
                    value={granularity}
                    closeOnSelect={true}
                    onChange={() => changeCensusAgeGranularity(granularity)}
                    className={DropdownStyle.dropdownItem}
                    selected={censusAgeGranularity === granularity}
                  >
                    <div className={styles.dropdownItem}>
                      <span>{ageGranularityLabels[granularity]}</span>
                    </div>
                  </DropdownItem>
                ))}
              >
                <DropdownToggle disabled={false}>
                  <div className={styles.dropdownName}>{ageGranularityLabels[censusAgeGranularity]}</div>
                </DropdownToggle>
              </Dropdown>
            </div>
          )}
        </div>
        <Scrollable vertical trackVisibility="always" style={{ flex: 1 }}>
          <div className={styles.chartContainer}>
            <ErrorBoundary>
              {!censusCrossTab || censusCrossTab.length === 0 ? (
                <LoadingSpinner message="Loading demographics data..." />
              ) : (
                <DemographicsChart
                  censusCounts={censusCrossTab}
                  censusRowDim={censusRowDim}
                  censusColDim={censusColDim}
                  censusAgeGranularity={censusAgeGranularity}
                  censusLabels={censusLabels}
                />
              )}
            </ErrorBoundary>
          </div>
        </Scrollable>
      </div>
    </Panel>
  );
};

// Single memoized export
export default memo(Demographics);
