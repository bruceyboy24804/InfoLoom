import React from 'react';
import { useValue } from 'cs2/api';
import { DraggablePanelProps, PanelProps, Panel, Tooltip } from 'cs2/ui';
import { CommercialData, CommercialDataExRes } from '../../../bindings';
import styles from './CommercialDemand.module.scss';
import { useLocalization } from 'cs2/l10n';

interface Factor {
  factor: string;
  weight: number;
}

interface DemandSection2Props {
  title: string;
  value: number;
  factors: Factor[];
}

const DemandSection2 = ({ title, value, factors }: DemandSection2Props): JSX.Element => (
  <div
    className="infoview-panel-section_RXJ"
    style={{ width: '95%', paddingTop: '3rem', paddingBottom: '3rem' }}
  >
    <div className="labels_L7Q row_S2v uppercase_RJI">
      <div className="left_Lgw row_S2v">{title}</div>
      {value >= 0 && <div className="right_k30 row_S2v">{Math.round(value * 100)}</div>}
    </div>
    <div className="space_uKL" style={{ height: '3rem' }}></div>
    {factors.map((item, index) => (
      <div key={index} className="labels_L7Q row_S2v small_ExK" style={{ marginTop: '1rem' }}>
        <div className="left_Lgw row_S2v">{item.factor}</div>
        <div className="right_k30 row_S2v">
          {item.weight < 0 ? (
            <div className="negative_YWY">{item.weight}</div>
          ) : (
            <div className="positive_zrK">{item.weight}</div>
          )}
        </div>
      </div>
    ))}
  </div>
);

interface RowWithTwoColumnsProps {
  left: React.ReactNode;
  right: React.ReactNode;
  tooltip?: string | null;
}

const RowWithTwoColumns = ({ left, right, tooltip }: RowWithTwoColumnsProps): JSX.Element => {
  const content = (
    <div className="labels_L7Q row_S2v">
      <div className="row_S2v" style={{ width: '60%' }}>
        {left}
      </div>
      <div className="row_S2v" style={{ width: '40%', justifyContent: 'center' }}>
        {right}
      </div>
    </div>
  );

  return tooltip ? <Tooltip tooltip={tooltip}>{content}</Tooltip> : content;
};

interface RowWithThreeColumnsProps {
  left: string | null;
  leftSmall: string | null;
  right1: number;
  flag1: boolean;
  right2?: number;
  flag2?: boolean;
  tooltip?: string | null;
}

const RowWithThreeColumns = ({
  left,
  leftSmall,
  right1,
  flag1,
  right2,
  flag2,
  tooltip
}: RowWithThreeColumnsProps): JSX.Element => {
  const centerStyle = {
    width: right2 === undefined ? '40%' : '20%',
    justifyContent: 'center',
  };
  const right1text = `${right1} %`;
  const right2text = right2 !== undefined ? `${right2} %` : '';

  const content = (
    <div className="labels_L7Q row_S2v">
      <div className="row_S2v" style={{ width: '60%', flexDirection: 'column' }}>
        <p>{left}</p>
        <p style={{ fontSize: '80%' }}>{leftSmall}</p>
      </div>
      <div className={`row_S2v ${flag1 ? 'negative_YWY' : 'positive_zrK'}`} style={centerStyle}>
        {right1text}
      </div>
      {right2 !== undefined && (
        <div className={`row_S2v ${flag2 ? 'negative_YWY' : 'positive_zrK'}`} style={centerStyle}>
          {right2text}
        </div>
      )}
    </div>
  );

  return tooltip ? <Tooltip tooltip={tooltip}>{content}</Tooltip> : content;
};

const DataDivider = (): JSX.Element => (
  <div
    style={{ display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center' }}
  >
    <div style={{ borderBottom: '1px solid gray' }}></div>
  </div>
);

interface ColumnCommercialDataProps {
  data: number[];
  emptyBuildingsLabel: string | null;
  emptyBuildingsTooltip: string | null;
  propertylessCompaniesLabel: string | null;
  propertylessCompaniesTooltip: string | null;
  averageTaxRateLabel: string | null;
  averageTaxRateSmall: string | null;
  averageTaxRateTooltip: string | null;
  serviceUtilizationLabel: string | null;
  serviceUtilizationSmall: string | null;
  serviceUtilizationTooltip: string | null;
  productionCapacityLabel: string | null;
  productionCapacitySmall: string | null;
  productionCapacityTooltip: string  | null;
  employeeCapacityLabel: string | null;
  employeeCapacitySmall: string | null;
  employeeCapacityTooltip: string | null;
  availableWorkforceLabel: string | null;
  availableWorkforceTooltip: string | null;
  educatedLabel: string | null;
  educatedTooltip: string | null;
  uneducatedLabel: string | null;
  uneducatedTooltip: string | null;
}

const ColumnCommercialData = ({
  data,
  emptyBuildingsLabel,
  emptyBuildingsTooltip,
  propertylessCompaniesLabel,
  propertylessCompaniesTooltip,
  averageTaxRateLabel,
  averageTaxRateSmall,
  averageTaxRateTooltip,
  serviceUtilizationLabel,
  serviceUtilizationSmall,
  serviceUtilizationTooltip,
  productionCapacityLabel,
  productionCapacitySmall,
  productionCapacityTooltip,
  employeeCapacityLabel,
  employeeCapacitySmall,
  employeeCapacityTooltip,
  availableWorkforceLabel,
  availableWorkforceTooltip,
  educatedLabel,
  educatedTooltip,
  uneducatedLabel,
  uneducatedTooltip,
}: ColumnCommercialDataProps): JSX.Element => {
  return (
    <div style={{ width: '70%', boxSizing: 'border-box', border: '1px solid gray' }}>
      <RowWithTwoColumns 
        left={emptyBuildingsLabel} 
        right={data[0]} 
        tooltip={emptyBuildingsTooltip}
      />
      <RowWithTwoColumns 
        left={propertylessCompaniesLabel} 
        right={data[1]} 
        tooltip={propertylessCompaniesTooltip}
      />
      <DataDivider />
      <RowWithThreeColumns
        left={averageTaxRateLabel}
        leftSmall={averageTaxRateSmall}
        right1={data[2] / 10}
        flag1={data[2] > 100}
        tooltip={averageTaxRateTooltip}
      />
      <DataDivider />
      <div className="labels_L7Q row_S2v">
        <div className="row_S2v" style={{ width: '60%' }} />
        <div className="row_S2v" style={{ width: '20%', justifyContent: 'center' }}>
          Standard
        </div>
        <div className="row_S2v" style={{ width: '20%', justifyContent: 'center' }}>
          Leisure
        </div>
      </div>
      <RowWithThreeColumns
        left={serviceUtilizationLabel}
        leftSmall={serviceUtilizationSmall}
        right1={data[3]}
        flag1={data[3] < 30}
        right2={data[4]}
        flag2={data[4] < 30}
        tooltip={serviceUtilizationTooltip}
      />
      <RowWithThreeColumns
        left={productionCapacityLabel}
        leftSmall={productionCapacitySmall}
        right1={data[5]}
        flag1={data[5] > 100}
        right2={data[6]}
        flag2={data[6] > 100}
        tooltip={productionCapacityTooltip}
      />
      <DataDivider />
      <RowWithThreeColumns
        left={employeeCapacityLabel}
        leftSmall={employeeCapacitySmall}
        right1={data[7] / 10}
        flag1={data[7] < 750}
        tooltip={employeeCapacityTooltip}
      />
      <DataDivider />
      <Tooltip tooltip={availableWorkforceTooltip}>
        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '60%' }}>
            <p style={{ margin: 0 }}>{availableWorkforceLabel}</p>
          </div>
          <div className="row_S2v" style={{ width: '40%', flexDirection: 'column' }}>
            <RowWithTwoColumns 
              left={educatedLabel} 
              right={data[8]} 
              tooltip={educatedTooltip}
            />
            <RowWithTwoColumns 
              left={uneducatedLabel} 
              right={data[9]} 
              tooltip={uneducatedTooltip}
            />
          </div>
        </div>
      </Tooltip>
    </div>
  );
};

interface ColumnExcludedResourcesProps {
  resources: string[];
  noDemandForLabel: string | null;
  excludedResourcesTooltip: string | null;
}

const ColumnExcludedResources = ({ resources, noDemandForLabel, excludedResourcesTooltip }: ColumnExcludedResourcesProps): JSX.Element => {
  return (
    <div style={{ width: '30%', boxSizing: 'border-box', border: '1px solid gray' }}>
      <Tooltip tooltip={excludedResourcesTooltip}>
        <div className="labels_L7Q row_S2v">
          <div className="row_S2v" style={{ width: '100%' }}>
            <p style={{ margin: 0 }}>{noDemandForLabel}</p>
          </div>
        </div>
      </Tooltip>
      <ul style={{ listStyleType: 'none', padding: 0, margin: 0 }}>
        {resources.map((item, index) => (
          <li key={index}>
            <div className="row_S2v small_ExK">{item}</div>
          </li>
        ))}
      </ul>
    </div>
  );
};

const $Commercial = ({ onClose, initialPosition }: DraggablePanelProps): JSX.Element => {
  const { translate } = useLocalization();
  const commercialData = useValue(CommercialData);
  const commercialDataExRes = useValue(CommercialDataExRes);

  // All translations are done here
  const emptyBuildingsLabel = translate("InfoLoomTwo.CommercialDemandPanel[EmptyBuildings]", "EMPTY BUILDINGS");
  const emptyBuildingsTooltip = translate("InfoLoomTwo.CommercialDemandPanel[EmptyBuildingsTooltip]", "Number of commercial buildings available for rent. More empty buildings reduces demand for new commercial buildings.");
  const propertylessCompaniesLabel = translate("InfoLoomTwo.CommercialDemandPanel[PropertylessCompanies]", "PROPERTYLESS COMPANIES");
  const propertylessCompaniesTooltip = translate("InfoLoomTwo.CommercialDemandPanel[PropertylessCompaniesTooltip]", "Number of commercial companies without properties. More propertyless companies increases demand for new commercial buildings.");
  const averageTaxRateLabel = translate("InfoLoomTwo.CommercialDemandPanel[AverageTaxRate]", "AVERAGE TAX RATE");
  const averageTaxRateSmall = translate("InfoLoomTwo.CommercialDemandPanel[AverageTaxRateNeutral]", "10% is the neutral rate");
  const averageTaxRateTooltip = translate("InfoLoomTwo.CommercialDemandPanel[TaxRateTooltip]", "Average commercial tax rate across all resources. Higher tax rates reduce commercial demand. 10% is considered neutral.");
  const serviceUtilizationLabel = translate("InfoLoomTwo.CommercialDemandPanel[ServiceUtilization]", "SERVICE UTILIZATION");
  const serviceUtilizationSmall = translate("InfoLoomTwo.CommercialDemandPanel[ServiceUtilizationNeutral]", "30% is the neutral ratio");
  const serviceUtilizationTooltip = translate("InfoLoomTwo.CommercialDemandPanel[ServiceUtilizationTooltip]", "Percentage of service capacity currently utilized. Higher utilization increases demand for more commercial services. Below 30% is considered insufficient demand.");
  const productionCapacityLabel = translate("InfoLoomTwo.CommercialDemandPanel[ProductionCapacity]", "PRODUCTION CAPACITY");
  const productionCapacitySmall = translate("InfoLoomTwo.CommercialDemandPanel[ProductionCapacityNeutral]", "100% when production capacity = resources needs");
  const productionCapacityTooltip = translate("InfoLoomTwo.CommercialDemandPanel[ProductionCapacityTooltip]", "Current production capacity relative to resource needs. 100% means supply matches demand. Above 100% indicates oversupply which reduces demand for new commercial businesses.");
  const employeeCapacityLabel = translate("InfoLoomTwo.CommercialDemandPanel[EmployeeCapacity]", "EMPLOYEE CAPACITY RATIO");
  const employeeCapacitySmall = translate("InfoLoomTwo.CommercialDemandPanel[EmployeeCapacityNeutral]", "75% is the neutral ratio");
  const employeeCapacityTooltip = translate("InfoLoomTwo.CommercialDemandPanel[EmployeeCapacityTooltip]", "Percentage of maximum worker positions currently filled. Below 75% indicates labor shortage which reduces demand for new commercial businesses.");
  const availableWorkforceLabel = translate("InfoLoomTwo.CommercialDemandPanel[AvailableWorkforce]", "AVAILABLE WORKFORCE");
  const availableWorkforceTooltip = translate("InfoLoomTwo.CommercialDemandPanel[AvailableWorkforceTooltip]", "Number of citizens available for work in commercial businesses. More available workers increases demand for new businesses.");
  const educatedLabel = translate("InfoLoomTwo.CommercialDemandPanel[Educated]", "Educated");
  const educatedTooltip = translate("InfoLoomTwo.CommercialDemandPanel[EducatedWorkforceTooltip]", "Number of educated citizens (high school or higher) available for work.");
  const uneducatedLabel = translate("InfoLoomTwo.CommercialDemandPanel[Uneducated]", "Uneducated");
  const uneducatedTooltip = translate("InfoLoomTwo.CommercialDemandPanel[UneducatedWorkforceTooltip]", "Number of uneducated citizens available for work.");
  const noDemandForLabel = translate("InfoLoomTwo.CommercialDemandPanel[NoDemandFor]", "NO DEMAND FOR");
  const excludedResourcesTooltip = translate("InfoLoomTwo.CommercialDemandPanel[ExcludedResourcesTooltip]", "Resources that currently have no commercial demand in your city. This may be due to oversupply, lack of customers, or economic factors.");
  
  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={{ x: 0.2, y: 0.5 }}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>{translate("InfoLoomTwo.CommercialDemandPanel[Title]", "Commercial Demand")}</span>
        </div>
      }
    >
      {commercialData.length === 0 ? (
        <p>{translate("InfoLoomTwo.CommercialDemandPanel[Loading]", "Loading commercial data...")}</p>
      ) : (
        <div style={{ display: 'flex' }}>
          <ColumnCommercialData
            data={commercialData}
            emptyBuildingsLabel={emptyBuildingsLabel}
            emptyBuildingsTooltip={emptyBuildingsTooltip}
            propertylessCompaniesLabel={propertylessCompaniesLabel}
            propertylessCompaniesTooltip={propertylessCompaniesTooltip}
            averageTaxRateLabel={averageTaxRateLabel}
            averageTaxRateSmall={averageTaxRateSmall}
            averageTaxRateTooltip={averageTaxRateTooltip}
            serviceUtilizationLabel={serviceUtilizationLabel}
            serviceUtilizationSmall={serviceUtilizationSmall}
            serviceUtilizationTooltip={serviceUtilizationTooltip}
            productionCapacityLabel={productionCapacityLabel}
            productionCapacitySmall={productionCapacitySmall}
            productionCapacityTooltip={productionCapacityTooltip}
            employeeCapacityLabel={employeeCapacityLabel}
            employeeCapacitySmall={employeeCapacitySmall}
            employeeCapacityTooltip={employeeCapacityTooltip}
            availableWorkforceLabel={availableWorkforceLabel}
            availableWorkforceTooltip={availableWorkforceTooltip}
            educatedLabel={educatedLabel}
            educatedTooltip={educatedTooltip}
            uneducatedLabel={uneducatedLabel}
            uneducatedTooltip={uneducatedTooltip}
          />
          <ColumnExcludedResources
            resources={commercialDataExRes}
            noDemandForLabel={noDemandForLabel}
            excludedResourcesTooltip={excludedResourcesTooltip}
          />
        </div>
      )}
    </Panel>
  );
};

export default $Commercial;
