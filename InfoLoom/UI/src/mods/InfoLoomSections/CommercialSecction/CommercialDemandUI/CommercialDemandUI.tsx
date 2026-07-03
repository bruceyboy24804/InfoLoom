import { PanelFoldout, PanelSectionRow, DraggablePanelProps, Panel } from 'cs2/ui';
import { CommercialData, CommercialDataExRes, Divider, resourceBox, storageBox } from '../../../bindings';
import styles from './CommercialDemand.module.scss';
import { LocalizedNumber, Unit, useLocalization } from 'cs2/l10n';
import { Localekeys } from 'mods/locale';
import React from 'react';
import { useValue } from 'cs2/api';
import classNames from 'classnames';
import { InfoRowSCSS } from '../../ILInfoSections/Modules/info-Row/info-Row.module.scss';
import { InfoRowTheme } from '../../../bindings';

const CommercialDemandUI = ({ onClose, initialPosition }: DraggablePanelProps): JSX.Element => {
  const { translate } = useLocalization();
  const data = useValue(CommercialData.binding);
  const dataExRes = useValue(CommercialDataExRes.binding);
  const emptyBuildingsLabel = translate(Localekeys.UnoccupiedBuildings, 'Empty Building');
  const emptyBuildingsTooltip = translate(
    Localekeys.EmptyBuildingsTooltip,
    'Number of commercial buildings available for rent. More empty buildings reduces demand for new commercial buildings.'
  );
  const propertylessCompaniesLabel = translate(Localekeys.PropertylessCompanies, 'Propertyless Companies');
  const propertylessCompaniesTooltip = translate(
    Localekeys.PropertylessCompaniesTooltip,
    'Number of commercial companies without properties. More propertyless companies increases demand for new commercial buildings.'
  );
  const averageTaxRateLabel = translate(Localekeys.TaxRate, 'Average Tax Rate');
  const averageTaxRateTooltip = translate(
    Localekeys.TaxRateTooltip,
    'Average commercial tax rate across all resources. Higher tax rates reduce commercial demand. 10% is considered neutral.'
  );
  const shopStockingLabel = translate(Localekeys.ShopStocking, 'Shop Stocking');
  const shopStockingTooltip = translate(
    Localekeys.ShopStockingTooltip,
    'How well-stocked shops are on average. Low stocking means shelves are empty and demand for new commercial businesses is high. Below 30% is considered understocked.'
  );
  const hotelOccupancyLabel = translate(Localekeys.HotelOccupancy, 'Hotel Occupancy');
  const hotelOccupancyTooltip = translate(
    Localekeys.HotelOccupancyTooltip,
    'Current hotel room occupancy relative to tourist demand. Above 100% means tourists cannot find rooms, increasing demand for more lodging.'
  );
  const employeeCapacityLabel = translate(Localekeys.EmployeeCapacity, 'Employee Capacity Ratio');
  const employeeCapacityTooltip = translate(
    Localekeys.EmployeeCapacityTooltip,
    'Percentage of maximum worker positions currently filled. Below 75% indicates labor shortage which reduces demand for new commercial businesses.'
  );
  const availableWorkforceLabel = translate(Localekeys.Workforce, 'Available Workforce');
  const availableWorkforceTooltip = translate(
    Localekeys.AvailableWorkforceTooltip,
    'Number of citizens available for work in commercial businesses. More available workers increases demand for new businesses.'
  );
  const educatedLabel = translate(Localekeys.Educated, 'Educated');
  const educatedTooltip = translate(
    Localekeys.EducatedWorkforceTooltip,
    'Number of educated citizens (high school or higher) available for work.'
  );
  const uneducatedLabel = translate(Localekeys.Uneducated, 'Uneducated');
  const uneducatedTooltip = translate(
    Localekeys.UneducatedWorkforceTooltip,
    'Number of uneducated citizens available for work.'
  );
  const DemandForLabel = translate(Localekeys.DemandFor, 'Demand For');
  const excludedResourcesTooltip = translate(
    Localekeys.IncludedResourcesTooltip,
    'Resources that currently have no demand in your city. This may be due to oversupply, lack of customers, or economic factors.'
  );

  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={{ x: 0.2, y: 0.5 }}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>{translate(Localekeys.CommercialDemand, 'Commercial Demand')}</span>
        </div>
      }
    >
      <PanelSectionRow left={emptyBuildingsLabel} right={data[0]} tooltip={emptyBuildingsTooltip} />
      <PanelSectionRow left={propertylessCompaniesLabel} right={data[1]} tooltip={propertylessCompaniesTooltip} />
      <Divider noMargin={1} />
      <PanelSectionRow
        left={averageTaxRateLabel}
        right={<div className={data[2] > 100 ? styles.negative : styles.positive}>{`${data[2] / 10} %`}</div>}
        tooltip={averageTaxRateTooltip}
      />
      <Divider noMargin={1} />
      <PanelFoldout
        header={
          <div className={InfoRowTheme.infoRow}>
            <div className={classNames(InfoRowSCSS.left)}>{shopStockingLabel}</div>
          </div>
        }
        tooltip={shopStockingTooltip}
        initialExpanded={false}
      >
        <PanelSectionRow
          left={'Standard'}
          right={<div className={data[3] < 30 ? styles.negative : styles.positive}>{`${data[3]} %`}</div>}
          subRow={true}
        />
        <PanelSectionRow
          left={'Leisure'}
          right={<div className={data[4] < 30 ? styles.negative : styles.positive}>{`${data[4]} %`}</div>}
          subRow={true}
        />
      </PanelFoldout>
      <Divider noMargin={1} />
      <PanelSectionRow
        left={hotelOccupancyLabel}
        right={<div className={data[5] > 100 ? styles.negative : styles.positive}>{`${data[5]} %`}</div>}
        tooltip={hotelOccupancyTooltip}
      />
      <Divider noMargin={1} />
      <PanelSectionRow
        left={employeeCapacityLabel}
        right={<div className={data[7] < 750 ? styles.negative : styles.positive}>{`${data[7] / 10} %`}</div>}
        tooltip={employeeCapacityTooltip}
      />
      <Divider noMargin={1} />
      <PanelFoldout
        header={
          <div className={InfoRowTheme.infoRow}>
            <div className={classNames(InfoRowSCSS.left)}>{availableWorkforceLabel}</div>
          </div>
        }
        tooltip={availableWorkforceTooltip}
        initialExpanded={true}
      >
        <PanelSectionRow left={educatedLabel} right={data[8]} tooltip={educatedTooltip} subRow={true} />
        <PanelSectionRow left={uneducatedLabel} right={data[9]} tooltip={uneducatedTooltip} subRow={true} />
      </PanelFoldout>
      <PanelFoldout
        header={
          <div className={InfoRowTheme.infoRow}>
            <div className={classNames(InfoRowSCSS.left)}>{DemandForLabel}</div>
          </div>
        }
        tooltip={excludedResourcesTooltip}
      >
        <div className={styles.resourceGrid}>
          {dataExRes.map(resourceName => (
            <div className={`${storageBox.resource} ${resourceBox.field}`} key={resourceName}>
              <img className={`${resourceBox.icon}`} src={`Media/Game/Resources/${resourceName}.svg`} />
              <div className={resourceBox.label}>{translate(`Resources.TITLE[${resourceName}]`)}</div>
            </div>
          ))}
        </div>
      </PanelFoldout>
    </Panel>
  );
};
export default CommercialDemandUI;
