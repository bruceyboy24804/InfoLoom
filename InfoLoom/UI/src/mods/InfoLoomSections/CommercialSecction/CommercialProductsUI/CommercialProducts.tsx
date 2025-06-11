import React, { useState, useCallback, FC } from 'react';
import { Button, Dropdown, DropdownToggle, PanelProps, Panel, Icon } from 'cs2/ui';
import { InfoCheckbox } from 'mods/components//InfoCheckbox/InfoCheckbox';
import { getModule } from "cs2/modding";
import styles from './CommercialProducts.module.scss';
import { formatWords } from 'mods/InfoLoomSections/utils/formatText';
import { useValue } from "cs2/api";
import { CommercialProductsData } from "mods/bindings";
import { CommercialProductData } from "../../../domain/commercialProductData";
const DropdownStyle = getModule("game-ui/menu/themes/dropdown.module.scss", "classes");

interface CommercialProps extends PanelProps {}



interface ResourceLineProps {
  data: CommercialProductData;
}

const ResourceLine: React.FC<ResourceLineProps> = ({data}) => {
  const formattedResourceName = formatWords(data.ResourceName, true);

  return (
    <div className={styles.row_S2v}>
      <div className={styles.cell} style={{ width: '3%' }}></div>
      <div className={styles.cell} style={{ width: '15%', justifyContent: 'flex-start'}}>
        <Icon src={data.ResourceIcon}/>
        <span>{formattedResourceName}</span>
      </div>
          <div className={`${styles.cell} ${data.Demand < 0 ? styles.negative_YWY : ''}`} style={{ width: '6%', textAlign: 'left' }}>
            {data.Demand}
          </div>
          <div className={`${styles.cell} ${data.Building <= 0 ? styles.negative_YWY : ''}`} style={{ width: '4%' }}>
            {data.Building}
          </div>
          <div className={`${styles.cell} ${data.Free <= 0 ? styles.negative_YWY : ''}`} style={{ width: '10%' }}>
            {data.Free}
          </div>
          <div className={styles.cell} style={{ width: '10%' }}>
            {data.Companies}
          </div>
        <div className={`${styles.cell} ${data.SvcPercent > 50 ? styles.negative_YWY : ''}`} style={{ width: '12%' }}>
          {`${data.SvcPercent}%`}
        </div>
          <div className={styles.cell} style={{ width: '10%' }}>
            {data.CapPerCompany}
          </div>
          <div className={`${styles.cell} ${data.CapPercent > 200 ? styles.negative_YWY : ''}`} style={{ width: '10%' }}>
            {`${data.CapPercent}%`}
          </div>
          <div className={styles.cell} style={{ width: '10%' }}>
            {data.Workers}
          </div>
          <div className={`${styles.cell} ${data.WrkPercent < 90 ? styles.negative_YWY : styles.positive_zrK}`} style={{ width: '10%' }}>
            {`${data.WrkPercent}%`}
          </div>
    </div>
  );
};

const TableHeader = (): JSX.Element => {
  return (
    <div className={styles.headerRow}>
      <div className={styles.headerCell} style={{ width: '3%' }}></div>
      <div className={styles.headerCell} style={{ width: '15%' }}>
        Resource
      </div>
          <div className={styles.headerCell} style={{ width: '6%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <span>Resource</span>
            <span>Demand</span>
          </div>
          <div className={styles.headerCell} style={{ width: '4%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <span>Zone</span>
            <span>Demand</span>
          </div>
          <div className={styles.headerCell} style={{ width: '10%' }}>Free</div>
          <div className={styles.headerCell} style={{ width: '10%' }}>Num</div>
          <div className={styles.headerCell} style={{ width: '12%' }}>Service%</div>
          <div className={styles.headerCell} style={{ width: '10%' }}>Cap/Co</div>
          <div className={styles.headerCell} style={{ width: '10%' }}>Cap%</div>
          <div className={styles.headerCell} style={{ width: '10%' }}>Workers</div>
          <div className={styles.headerCell} style={{ width: '10%' }}>Work%</div>
        </div>
  );
}


const $CommercialProducts: FC<CommercialProps> = ({ onClose }) => {
  const commercialProductsData = useValue(CommercialProductsData);
  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={{ x: 0.50, y: 0.50 }}
      className={styles.panel}
      header={<div className={styles.header}><span className={styles.headerText}>Commercial Products</span></div>}
    >
      {commercialProductsData.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div className={styles.panelContent}>
          <TableHeader/>

          {commercialProductsData
            .map((item, index) => (
              <ResourceLine key={`${item.ResourceName}-${index}`} data={item}/>
            ))}
        </div>
      )}
    </Panel>
  );
};

export default $CommercialProducts;