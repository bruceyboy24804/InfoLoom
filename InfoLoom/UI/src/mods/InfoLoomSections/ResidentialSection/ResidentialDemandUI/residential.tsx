import React, { useCallback, useState } from 'react';
import { bindValue, useValue } from 'cs2/api';
import { DraggablePanelProps, Panel, PanelProps, Tooltip } from 'cs2/ui';
import { useLocalization } from 'cs2/l10n';
import styles from './residential.module.scss';
import { ResidentialData } from '../../../bindings';
import { LocalizedFraction } from 'cs2/l10n';
import { Localekeys } from 'mods/locale';
interface RowWithTwoColumnsProps {
  left: React.ReactNode;
  right: React.ReactNode;
}

const RowWithTwoColumns = ({ left, right }: RowWithTwoColumnsProps): JSX.Element => {
  return (
    <div className="labels_L7Q row_S2v">
      <div className="row_S2v" style={{ width: '70%' }}>
        {left}
      </div>
      <div className="row_S2v" style={{ width: '30%', justifyContent: 'center' }}>
        {right}
      </div>
    </div>
  );
};

interface RowWithThreeColumnsProps {
  left: React.ReactNode;
  leftSmall?: React.ReactNode;
  right1: React.ReactNode;
  flag1: boolean;
  right2?: React.ReactNode;
  flag2?: boolean;
}

const RowWithThreeColumns = ({
  left,
  leftSmall,
  right1,
  flag1,
  right2,
  flag2,
}: RowWithThreeColumnsProps): JSX.Element => {
  const centerStyle: React.CSSProperties = {
    width: right2 === undefined ? '30%' : '15%',
    justifyContent: 'center',
  };
  const right1text = `${right1}`;
  const right2text = `${right2}`;
  return (
    <div className="labels_L7Q row_S2v">
      <div className="row_S2v" style={{ width: '70%', flexDirection: 'column' }}>
        <p>{left}</p>
        {leftSmall && <p style={{ fontSize: '80%' }}>{leftSmall}</p>}
      </div>
      {flag1 ? (
        <div className="row_S2v negative_YWY" style={centerStyle}>
          {right1text}
        </div>
      ) : (
        <div className="row_S2v positive_zrK" style={centerStyle}>
          {right1text}
        </div>
      )}
      {right2 !== undefined &&
        (flag2 ? (
          <div className="row_S2v negative_YWY" style={centerStyle}>
            {right2text}
          </div>
        ) : (
          <div className="row_S2v positive_zrK" style={centerStyle}>
            {right2text}
          </div>
        ))}
    </div>
  );
};

// Simple horizontal line
const DataDivider = (): JSX.Element => {
  return (
    <div style={{ display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center' }}>
      <div style={{ borderBottom: '1px solid gray' }}></div>
    </div>
  );
};

interface SingleValueProps {
  value: React.ReactNode;
  flag?: boolean;
  width?: string;
  small?: boolean;
}

// Centered value, if flag exists then uses colors for negative/positive
// Width is 20% by default
const SingleValue = ({ value, flag, width, small }: SingleValueProps): JSX.Element => {
  const rowClass = small ? 'row_S2v small_ExK' : 'row_S2v';
  const centerStyle: React.CSSProperties = {
    width: width === undefined ? '16%' : width,
    justifyContent: 'center',
  };
  return flag === undefined ? (
    <div className={rowClass} style={centerStyle}>
      {value}
    </div>
  ) : flag ? (
    <div className={`${rowClass} negative_YWY`} style={centerStyle}>
      {value}
    </div>
  ) : (
    <div className={`${rowClass} positive_zrK`} style={centerStyle}>
      {value}
    </div>
  );
};

const Residential = ({ onClose, initialPosition }: DraggablePanelProps): JSX.Element => {
  const { translate } = useLocalization();
  const ilResidential = useValue(ResidentialData);

  const homelessThreshold = ilResidential.length > 13 ? Math.round((ilResidential[12] * ilResidential[13]) / 1000) : 0;

  const BuildingDemandSectionWithTranslation = ({ data }: { data: number[] }) => {
    const freeL = data[0] - data[3];
    const freeM = data[1] - data[4];
    const freeH = data[2] - data[5];
    const ratioX = data[18] / 10;
    const ratioY = data[19] / 10;
    const ratioZ = data[20] / 10;
    const needL = Math.max(1, Math.floor((ratioX * data[0]) / 100));
    const needM = Math.max(1, Math.floor((ratioY * data[1]) / 100));
    const needH = Math.max(1, Math.floor((ratioZ * data[2]) / 100));
    const demandL = Math.floor((1 - freeL / needL) * 100);
    const demandM = Math.floor((1 - freeM / needM) * 100);
    const demandH = Math.floor((1 - freeH / needH) * 100);
    const totalRes = data[0] + data[1] + data[2];
    const totalOcc = data[3] + data[4] + data[5];
    const totalFree = totalRes - totalOcc;
    const freeRatio = totalRes > 0 ? Math.round((1000 * totalFree) / totalRes) / 10 : 0;

    return (
      <div style={{ boxSizing: 'border-box', border: '1px solid gray' }}>
        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '36%' }}></div>
          <SingleValue value={translate(Localekeys.DemandPanelsLow, 'LOW')} />
          <SingleValue value={translate(Localekeys.DemandPanelsMedium, 'MEDIUM')} />
          <SingleValue value={translate(Localekeys.DemandPanelsHigh, 'HIGH')} />
          <SingleValue value={translate(Localekeys.DemandPanelsTotalProperties, 'TOTAL')} />
        </div>
        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '2%' }}></div>
          <div className="row_S2v" style={{ width: '34%' }}>
            <Tooltip
              tooltip={translate(
                Localekeys.TotalPropertiesTooltip,
                'Total number of residential properties by density type'
              )}
            >
              <span>{translate(Localekeys.DemandPanelsTotalProperties, 'Total properties')}</span>
            </Tooltip>
          </div>
          <SingleValue value={data[0]} />
          <SingleValue value={data[1]} />
          <SingleValue value={data[2]} />
          <SingleValue value={totalRes} />
        </div>
        <div className="labels_L7Q row_S2v">
          <div className="row_S2v small_ExK" style={{ width: '2%' }}></div>
          <div className="row_S2v small_ExK" style={{ width: '34%' }}>
            <Tooltip
              tooltip={translate(
                Localekeys.OccupiedPropertiesTooltip,
                'Properties currently occupied by households'
              )}
            >
              <span>{translate(Localekeys.DemandPanelsOccupiedProperties, '- Occupied properties')}</span>
            </Tooltip>
          </div>
          <SingleValue value={data[3]} small={true} />
          <SingleValue value={data[4]} small={true} />
          <SingleValue value={data[5]} small={true} />
          <SingleValue value={totalOcc} small={true} />
        </div>
        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '2%' }}></div>
          <div className="row_S2v" style={{ width: '34%' }}>
            <Tooltip
              tooltip={translate(
                Localekeys.EmptyPropertiesTooltip,
                'Available properties ready for new households to move in. Green indicates sufficient supply, red indicates shortage.'
              )}
            >
              <span>{translate(Localekeys.DemandPanelsEmptyProperties, '= Empty properties')}</span>
            </Tooltip>
          </div>
          <SingleValue value={freeL} flag={freeL > needL} />
          <SingleValue value={freeM} flag={freeM > needM} />
          <SingleValue value={freeH} flag={freeH > needH} />
          <SingleValue value={totalFree} />
        </div>
        <div className="labels_L7Q row_S2v">
          <div className="row_S2v small_ExK" style={{ width: '2%' }}></div>
          <div className="row_S2v small_ExK" style={{ width: '34%' }}>
            <Tooltip
              tooltip={translate(
                Localekeys.NoDemandAtTooltip,
                'Target number of empty properties needed to maintain zero building demand. Based on free residential requirement percentages.'
              )}
            >
              <span>{translate(Localekeys.DemandPanelsNoDemandAt, 'No demand at')}</span>
            </Tooltip>
          </div>
          <SingleValue value={needL} small={true} />
          <SingleValue value={needM} small={true} />
          <SingleValue value={needH} small={true} />
          <div className="row_S2v" style={{ width: '16%' }}></div>
        </div>
        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '2%' }}></div>
          <div className="row_S2v" style={{ width: '34%' }}>
            <Tooltip
              tooltip={translate(
                Localekeys.BuildingDemandTooltip,
                'Demand for new residential buildings. Calculated as (1 - empty properties / target empty properties) × 100. Green = surplus, Red = shortage. Total column shows overall empty property percentage.'
              )}
            >
              <span>{translate(Localekeys.DemandPanelsBuildingDemand, 'BUILDING DEMAND')}</span>
            </Tooltip>
          </div>
          <SingleValue value={demandL} flag={demandL < 0} />
          <SingleValue value={demandM} flag={demandM < 0} />
          <SingleValue value={demandH} flag={demandH < 0} />
          <SingleValue value={`${freeRatio}%`} />
        </div>
        <div className="space_uKL" style={{ height: '3rem' }}></div>
      </div>
    );
  };

  return (
    <Panel
      draggable={true}
      onClose={onClose}
      initialPosition={{ x: 0.15, y: 0.02 }}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>{translate(Localekeys.Residential, 'Residential')}</span>
        </div>
      }
    >
      {ilResidential.length === 0 ? (
        <p style={{ color: 'white' }}>{translate(Localekeys.Waiting, 'Waiting...')}</p>
      ) : (
        <div>
          <BuildingDemandSectionWithTranslation data={ilResidential} />
          {/* OTHER DATA, two columns */}
          <div style={{ display: 'flex' }}>
            <div
              style={{
                width: '50%',
                boxSizing: 'border-box',
                border: '1px solid gray',
                paddingLeft: '10rem',
                paddingRight: '10rem',
              }}
            >
              <div className="space_uKL" style={{ height: '3rem' }}></div>
              <RowWithTwoColumns
                left={
                  <Tooltip
                    tooltip={translate(
                      Localekeys.StudyPositionsTooltip,
                      'Total available study positions across all education levels (elementary through university)'
                    )}
                  >
                    <span>{translate(Localekeys.DemandPanelsStudyPositions, 'STUDY POSITIONS')}</span>
                  </Tooltip>
                }
                right={ilResidential[14]}
              />
              <DataDivider />
              <RowWithThreeColumns
                left={
                  <Tooltip
                    tooltip={translate(
                      Localekeys.HappinessTooltip,
                      'Average happiness of all citizens. Affects residential demand - unhappy citizens create more demand for housing.'
                    )}
                  >
                    <span>{translate(Localekeys.Happiness, 'HAPPINESS')}</span>
                  </Tooltip>
                }
                right1={ilResidential[7]}
                flag1={ilResidential[7] < ilResidential[8]}
              />
              <DataDivider />
              <RowWithThreeColumns
                left={
                  <Tooltip
                    tooltip={translate(
                      Localekeys.UnemploymentTooltip,
                      'Percentage of working-age population that is unemployed. High unemployment increases residential demand as people seek cheaper housing.'
                    )}
                  >
                    <span>{translate(Localekeys.Unemployment, 'UNEMPLOYMENT')}</span>
                  </Tooltip>
                }
                right1={Number(ilResidential[9]).toFixed(1)}
                flag1={ilResidential[9] > ilResidential[10] / 10}
              />
              <DataDivider />
              <RowWithThreeColumns
                left={
                  <Tooltip
                    tooltip={translate(
                      Localekeys.HouseholdDemandTooltip,
                      'Overall demand for households to move into the city. Positive values indicate growing population, negative values indicate population decline.'
                    )}
                  >
                    <span>{translate(Localekeys.DemandPanelsHouseholdDemand, 'HOUSEHOLD DEMAND')}</span>
                  </Tooltip>
                }
                right1={ilResidential[16]}
                flag1={ilResidential[16] < 0}
              />
              <div className="space_uKL" style={{ height: '3rem' }}></div>
            </div>
            <div
              style={{
                width: '50%',
                boxSizing: 'border-box',
                border: '1rem solid gray',
                paddingLeft: '10rem',
                paddingRight: '10rem',
              }}
            >
              <div className="space_uKL" style={{ height: '3rem' }}></div>
              <RowWithTwoColumns
                left={
                  <Tooltip
                    tooltip={translate(
                      Localekeys.HouseholdsTooltip,
                      'Total number of households that have recently moved into the city'
                    )}
                  >
                    <span>{translate(Localekeys.Households, 'HOUSEHOLDS')}</span>
                  </Tooltip>
                }
                right={ilResidential[12]}
              />
              <DataDivider />
              <RowWithThreeColumns
                left={
                  <Tooltip
                    tooltip={translate(
                      Localekeys.HomelessTooltip,
                      'Number of households without homes. High homelessness increases residential demand and reduces city attractiveness.'
                    )}
                  >
                    <span>{translate(Localekeys.Homeless, 'HOMELESS')}</span>
                  </Tooltip>
                }
                right1={ilResidential[11]}
                flag1={ilResidential[11] > homelessThreshold}
              />
              <DataDivider />
              <RowWithThreeColumns
                left={
                  <Tooltip
                    tooltip={translate(
                      Localekeys.TaxRateTooltip,
                      'Weighted average residential tax rate across all density types. Higher taxes reduce residential demand as people seek more affordable cities.'
                    )}
                  >
                    <span>{translate(Localekeys.TaxRate, 'TAX RATE (weighted)')}</span>
                  </Tooltip>
                }
                right1={ilResidential[15] / 10}
                flag1={ilResidential[15] > 100}
              />
              <DataDivider />
              <RowWithTwoColumns
                left={
                  <Tooltip
                    tooltip={translate(
                      Localekeys.StudentChanceTooltip,
                      'Percentage chance that unemployed citizens will become students instead of seeking jobs. Based on student vs unemployment demand factors.'
                    )}
                  >
                    <span>{translate(Localekeys.DemandPanelsStudentChance, 'STUDENT CHANCE')}</span>
                  </Tooltip>
                }
                right={`${ilResidential[17]} %`}
              />
              <div className="space_uKL" style={{ height: '3rem' }}></div>
            </div>
          </div>
        </div>
      )}
    </Panel>
  );
};

export default Residential;
