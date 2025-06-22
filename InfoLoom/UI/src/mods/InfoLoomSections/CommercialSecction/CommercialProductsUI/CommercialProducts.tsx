import React, { useState, useCallback, FC } from 'react';
import { Button, Dropdown, DropdownToggle, PanelProps, Panel, Icon, Tooltip } from 'cs2/ui';
import { InfoCheckbox } from 'mods/components//InfoCheckbox/InfoCheckbox';
import { getModule } from 'cs2/modding';
import styles from './CommercialProducts.module.scss';
import { formatWords } from 'mods/InfoLoomSections/utils/formatText';
import { useValue } from 'cs2/api';
import { CommercialProductsData } from 'mods/bindings';
import { CommercialProductData } from '../../../domain/commercialProductData';
import { useLocalization } from 'cs2/l10n'; // <-- Add localization

const DropdownStyle = getModule('game-ui/menu/themes/dropdown.module.scss', 'classes');

interface CommercialProps extends PanelProps {}

interface ResourceLineProps {
  data: CommercialProductData;
}

const ResourceLine: React.FC<ResourceLineProps> = ({ data }) => {
  const formattedResourceName = formatWords(data.ResourceName, true);

  return (
    <div className={styles.row_S2v}>
      <div className={styles.cell} style={{ width: '3%' }}></div>
      <div className={styles.cell} style={{ width: '15%', justifyContent: 'flex-start' }}>
        <Icon src={data.ResourceIcon} />
        <span>{formattedResourceName}</span>
      </div>
      <div
        className={`${styles.cell} ${data.Demand < 0 ? styles.negative_YWY : ''}`}
        style={{ width: '6%', textAlign: 'left' }}
      >
        {data.Demand}
      </div>
      <div
        className={`${styles.cell} ${data.Free <= 0 ? styles.negative_YWY : ''}`}
        style={{ width: '10%' }}
      >
        {data.Free}
      </div>
      <div className={styles.cell} style={{ width: '10%' }}>
        {data.Companies}
      </div>
      <div
        className={`${styles.cell} ${data.SvcPercent > 50 ? styles.negative_YWY : ''}`}
        style={{ width: '12%' }}
      >
        {`${data.SvcPercent}%`}
      </div>
      <div className={styles.cell} style={{ width: '10%' }}>
        {data.CapPerCompany}
      </div>
      <div
        className={`${styles.cell} ${data.CapPercent > 200 ? styles.negative_YWY : ''}`}
        style={{ width: '10%' }}
      >
        {`${data.CapPercent}%`}
      </div>
      <div className={styles.cell} style={{ width: '10%' }}>
        {data.Workers}
      </div>
      <div
        className={`${styles.cell} ${data.WrkPercent < 90 ? styles.negative_YWY : styles.positive_zrK}`}
        style={{ width: '10%' }}
      >
        {`${data.WrkPercent}%`}
      </div>
    </div>
  );
};

const TableHeader: React.FC = () => {
  const { translate } = useLocalization();
  return (
    <div className={styles.headerRow}>
      <div className={styles.headerCell} style={{ width: '3%' }}></div>
      <div className={styles.headerCell} style={{ width: '15%' }}>
        <Tooltip tooltip={translate("InfoLoomTwo.CommercialDemandPanel[ResourceTooltip]", "Resource type being produced")}>
          <span>{translate("InfoLoomTwo.CommercialDemandPanel[Resource]", "Resource")}</span>
        </Tooltip>
      </div>
      <div
        className={styles.headerCell}
        style={{
          width: '6%',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          textAlign: 'center',
          lineHeight: 1.1
        }}
      >
        <Tooltip tooltip={translate("InfoLoomTwo.CommercialDemandPanel[ResourceDemandTooltip]", "Total demand for this resource from all sources (city services, population, companies)")}>
          <div>
            <span>{translate("InfoLoomTwo.CommercialDemandPanel[ResourceDemand1]", "Resource")}</span>
            <span>{translate("InfoLoomTwo.CommercialDemandPanel[ResourceDemand2]", "Demand")}</span>
          </div>
        </Tooltip>
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <Tooltip tooltip={translate("InfoLoomTwo.CommercialDemandPanel[FreeTooltip]", "Number of empty commercial buildings available for companies producing this resource")}>
          <span>{translate("InfoLoomTwo.CommercialDemandPanel[Free]", "Free")}</span>
        </Tooltip>
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <Tooltip tooltip={translate("InfoLoomTwo.CommercialDemandPanel[NumTooltip]", "Number of companies currently producing this resource")}>
          <span>{translate("InfoLoomTwo.CommercialDemandPanel[Num]", "Num")}</span>
        </Tooltip>
      </div>
      <div className={styles.headerCell} style={{ width: '12%' }}>
        <Tooltip tooltip={translate("InfoLoomTwo.CommercialDemandPanel[ServicePercentTooltip]", "Percentage of service capacity currently utilized")}>
          <span>{translate("InfoLoomTwo.CommercialDemandPanel[ServicePercent]", "Service%")}</span>
        </Tooltip>
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <Tooltip tooltip={translate("InfoLoomTwo.CommercialDemandPanel[CapPerCompanyTooltip]", "Production capacity per company")}>
          <span>{translate("InfoLoomTwo.CommercialDemandPanel[CapPerCompany]", "Cap/Co")}</span>
        </Tooltip>
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <Tooltip tooltip={translate("InfoLoomTwo.CommercialDemandPanel[CapPercentTooltip]", "Current production capacity relative to resource needs")}>
          <span>{translate("InfoLoomTwo.CommercialDemandPanel[CapPercent]", "Cap%")}</span>
        </Tooltip>
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <Tooltip tooltip={translate("InfoLoomTwo.CommercialDemandPanel[WorkersTooltip]", "Current number of workers employed in companies producing this resource")}>
          <span>{translate("InfoLoomTwo.CommercialDemandPanel[Workers]", "Workers")}</span>
        </Tooltip>
      </div>
      <div className={styles.headerCell} style={{ width: '10%' }}>
        <Tooltip tooltip={translate("InfoLoomTwo.CommercialDemandPanel[WorkPercentTooltip]", "Percentage of maximum worker capacity currently employed. Low values indicate workforce shortages.")}>
          <span>{translate("InfoLoomTwo.CommercialDemandPanel[WorkPercent]", "Work%")}</span>
        </Tooltip>
      </div>
    </div>
  );
};

const $CommercialProducts: FC<CommercialProps> = ({ onClose }) => {
  const commercialProductsData = useValue(CommercialProductsData);
  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={{ x: 0.5, y: 0.5 }}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>Commercial Products</span>
        </div>
      }
    >
      {commercialProductsData.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div className={styles.panelContent}>
          <TableHeader />

          {commercialProductsData.map((item, index) => (
            <ResourceLine key={`${item.ResourceName}-${index}`} data={item} />
          ))}
        </div>
      )}
    </Panel>
  );
};

export default $CommercialProducts;
