import React, { FC, useCallback, useState } from 'react';
import { SelectedInfoSectionBase, Theme } from 'cs2/bindings';
import { getModule } from 'cs2/modding';
import { bindValue, useValue } from 'cs2/api';
import mod from 'mod.json';
import { DraggablePanelProps, PanelProps, Panel, Tooltip, Scrollable, PanelSectionRow } from 'cs2/ui';
import { IndustrialData, IndustrialDataExRes } from '../../../bindings';
import { LocalizedNumber, Unit, useLocalization } from 'cs2/l10n';
import styles from './IndustrialDemand.module.scss';
import { InfoRowSCSS } from 'mods/InfoLoomSections/ILInfoSections/Modules/info-Row/info-Row.module.scss';
import classNames from 'classnames';
import { Localekeys } from 'mods/locale';
export const InfoRowTheme: Theme | any = getModule(
  'game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss',
  'classes'
);

// Define interfaces for props
interface RowWithTwoColumnsProps {
  left: React.ReactNode;
  right: React.ReactNode;
}
interface SingleValueProps {
  value: number | string;
  flag?: boolean;
  width?: string;
  small?: boolean;
}

const RowWithTwoColumns: React.FC<RowWithTwoColumnsProps> = ({ left, right }) => {
  return (
    <div className="labels_L7Q row_S2v">
      <div className={classNames('row_S2v', styles.twoColumnLeft)}>{left}</div>
      <div className={classNames('row_S2v', styles.twoColumnRight)}>{right}</div>
    </div>
  );
};

const DataDivider: React.FC = () => {
  return (
    <div className={styles.dataDivider}>
      <div></div>
    </div>
  );
};

const SingleValue: React.FC<SingleValueProps> = ({ value, flag, width, small }) => {
  const rowClass = small ? 'row_S2v small_ExK' : 'row_S2v';
  const styleClass = width === undefined ? styles.singleValueDefault : '';
  const widthStyle = width !== undefined ? { width } : undefined;

  return flag === undefined ? (
    <div className={classNames(rowClass, styleClass)} style={widthStyle}>
      {value}
    </div>
  ) : flag ? (
    <div className={classNames(rowClass, 'negative_YWY', styleClass)} style={widthStyle}>
      {value}
    </div>
  ) : (
    <div className={classNames(rowClass, 'positive_zrK', styleClass)} style={widthStyle}>
      {value}
    </div>
  );
};

// Move components outside to prevent re-creation on render
const IndustrialDataWithTranslation = React.memo(({ data, translate }: { data: number[]; translate: any }) => {
  return (
    <div className={styles.mainDataContainer}>
      <div className="labels_L7Q row_S2v">
        <div className={classNames('row_S2v', styles.columnHeaderRow)}></div>
        <SingleValue value={translate(Localekeys.Industrial, 'INDUSTRIAL')} />
        <SingleValue value={translate(Localekeys.Office, 'OFFICE')} />
      </div>

      <div className="labels_L7Q row_S2v">
        <div className={classNames('row_S2v', styles.dataRow)}>
          <Tooltip
            tooltip={translate(
              Localekeys.EmptyBuildingsTooltip,
              'Number of empty industrial/office buildings available for new companies to move in'
            )}
          >
            <span>{translate(Localekeys.EmptyBuildings, 'EMPTY BUILDINGS')}</span>
          </Tooltip>
        </div>
        <SingleValue value={data[0]} />
        <SingleValue value={data[10]} />
      </div>

      <div className="labels_L7Q row_S2v">
        <div className={classNames('row_S2v', styles.dataRow)}>
          <Tooltip
            tooltip={translate(
              Localekeys.PropertylessCompaniesTooltip,
              "Companies that exist but don't have a building to operate from. High numbers indicate need for more zoned land."
            )}
          >
            <span>{translate(Localekeys.PropertylessCompanies, 'PROPERTYLESS COMPANIES')}</span>
          </Tooltip>
        </div>
        <div className={classNames('row_S2v', styles.dataValueNormal)}>{data[1]}</div>
        <div className={classNames('row_S2v', styles.dataValueNormal)}>{data[11]}</div>
      </div>

      <DataDivider />

      <div className="labels_L7Q row_S2v">
        <div className={classNames('row_S2v', styles.dataRowColumn)}>
          <Tooltip
            tooltip={translate(
              Localekeys.TaxRateTooltip,
              'Average tax rate across all resource types. Higher taxes reduce company demand. 10% is neutral.'
            )}
          >
            <p>{translate(Localekeys.TaxRate, 'AVERAGE TAX RATE')}</p>
          </Tooltip>
        </div>
        <div className={classNames('row_S2v', styles.dataValueNormal, data[2] > 100 ? 'negative_YWY' : 'positive_zrK')}>
          {(data[2] / 10).toFixed(1)} %
        </div>
        <div
          className={classNames('row_S2v', styles.dataValueNormal, data[12] > 100 ? 'negative_YWY' : 'positive_zrK')}
        >
          {(data[12] / 10).toFixed(1)} %
        </div>
      </div>

      <DataDivider />

      <div className="labels_L7Q row_S2v">
        <div className={classNames('row_S2v', styles.dataRowColumn)}>
          <Tooltip
            tooltip={translate(
              Localekeys.LocalDemandTooltip,
              'Production capacity compared to local demand. 100% means production equals demand. Higher values indicate overproduction.'
            )}
          >
            <p>{translate(Localekeys.LocalDemand, 'LOCAL DEMAND (ind)')}</p>
          </Tooltip>
        </div>
        <div className={classNames('row_S2v', styles.dataValueWide, data[3] > 100 ? 'negative_YWY' : 'positive_zrK')}>
          {data[3]} %
        </div>
      </div>

      <div className="labels_L7Q row_S2v">
        <div className={classNames('row_S2v', styles.dataRowColumn)}>
          <Tooltip
            tooltip={translate(
              Localekeys.InputUtilizationTooltip,
              'How well input resources are being utilized by manufacturing. 110% is neutral, values capped at 400%.'
            )}
          >
            <p>{translate(Localekeys.InputUtilization, 'INPUT UTILIZATION (ind)')}</p>
          </Tooltip>
        </div>
        <div className={classNames('row_S2v', styles.dataValueWide, data[7] > 100 ? 'negative_YWY' : 'positive_zrK')}>
          {data[7]} %
        </div>
      </div>

      <DataDivider />

      <div className="labels_L7Q row_S2v">
        <div className={classNames('row_S2v', styles.dataRowColumn)}>
          <Tooltip
            tooltip={translate(
              Localekeys.EmployeeCapacityTooltip,
              'Ratio of current employees to maximum employee capacity. 72% industrial and 75% office are neutral ratios.'
            )}
          >
            <p>{translate(Localekeys.EmployeeCapacity, 'EMPLOYEE CAPACITY RATIO')}</p>
          </Tooltip>
        </div>
        <div className={classNames('row_S2v', styles.dataValueNormal, data[4] < 720 ? 'negative_YWY' : 'positive_zrK')}>
          {(data[4] / 10).toFixed(1)} %
        </div>
        <div
          className={classNames('row_S2v', styles.dataValueNormal, data[14] < 750 ? 'negative_YWY' : 'positive_zrK')}
        >
          {(data[14] / 10).toFixed(1)} %
        </div>
      </div>

      <DataDivider />

      <div className={styles.storageContainer}>
        <div className={styles.workforceHeader}>
          <Tooltip
            tooltip={translate(
              Localekeys.WorkforceTooltip,
              'Available educated and uneducated workers that can be employed by new companies'
            )}
          >
            <span>{translate(Localekeys.Workforce, 'AVAILABLE WORKFORCE')}</span>
          </Tooltip>
        </div>
        <div className={styles.workforceData}>
          <RowWithTwoColumns left={translate(Localekeys.Educated, 'Educated')} right={data[8]} />
          <RowWithTwoColumns
            left={translate(Localekeys.Uneducated, 'Uneducated')}
            right={data[9]}
          />
        </div>
      </div>

      <DataDivider />

      <div className={styles.storageContainer}>
        <div className={styles.storageLeft}>
          <p className={styles.storageTitle}>
            <Tooltip
              tooltip={translate(
                Localekeys.StorageTooltip,
                'Storage facilities for industrial goods. The game spawns warehouses when demand for storage exists.'
              )}
            >
              <span>{translate(Localekeys.Storage, 'STORAGE')}</span>
            </Tooltip>
          </p>
          <p className={styles.storageDescription}>
            {translate(
              Localekeys.StorageDescription,
              'The game will spawn warehouses when DEMANDED TYPES exist.'
            )}
          </p>
        </div>
        <div className={styles.storageRight}>
          <RowWithTwoColumns
            left={
              <Tooltip
                tooltip={translate(
                  Localekeys.StorageEmptyBuildingsTooltip,
                  'Empty warehouse buildings available for storage companies'
                )}
              >
                <span>{translate(Localekeys.EmptyBuildings, 'Empty buildings')}</span>
              </Tooltip>
            }
            right={data[5]}
          />
          <RowWithTwoColumns
            left={
              <Tooltip
                tooltip={translate(
                  Localekeys.StorageEmptyBuildingsTooltip,
                  'Storage companies without warehouse buildings'
                )}
              >
                <span>{translate(Localekeys.PropertylessCompanies, 'Propertyless companies')}</span>
              </Tooltip>
            }
            right={data[6]}
          />
          <RowWithTwoColumns
            left={
              <Tooltip
                tooltip={translate(
                  Localekeys.DemandedTypesTooltip,
                  'Number of resource types that need storage capacity based on demand vs storage capacity'
                )}
              >
                <span>{translate(Localekeys.DemandedTypes, 'DEMANDED TYPES')}</span>
              </Tooltip>
            }
            right={data[15]}
          />
        </div>
      </div>
    </div>
  );
});

const ExcludedResourcesWithTranslation = React.memo(
  ({ resources, translate }: { resources: string[]; translate: any }) => {
    return (
      <div className={styles.excludedResourcesContainer}>
        <div className="labels_L7Q row_S2v">
          <div className={classNames('row_S2v', styles.excludedResourcesHeader)}>
            <p className={styles.excludedResourcesTitle}>
              {translate(Localekeys.DemandFor, 'DEMAND FOR')}
            </p>
          </div>
        </div>
        <Scrollable vertical={true} className={styles.scrollableList}>
          {resources.map((item, index) => (
            <li key={index}>
              <div className="row_S2v small_ExK">{item}</div>
            </li>
          ))}
        </Scrollable>
      </div>
    );
  }
);

const $Industrial: React.FC<DraggablePanelProps> = ({ onClose, initialPosition, draggable }) => {
  const { translate } = useLocalization();
  const ilIndustrial = useValue(IndustrialData);
  const ilIndustrialExRes = useValue(IndustrialDataExRes);

  return (
    <Panel
    
      draggable
      onClose={onClose}
      initialPosition={initialPosition}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>
            {translate(Localekeys.IndustrialPanelTitle, 'Industrial and Office Demand')}
          </span>
        </div>
      }
    >
      {ilIndustrial.length === 0 ? (
        <p>{translate(Localekeys.Waiting, 'Waiting...')}</p>
      ) : (
        <div className={styles.mainLayout}>
          <IndustrialDataWithTranslation data={ilIndustrial} translate={translate} />
          <ExcludedResourcesWithTranslation resources={ilIndustrialExRes} translate={translate} />
        </div>
      )}
    </Panel>
  );
};

export default $Industrial;
