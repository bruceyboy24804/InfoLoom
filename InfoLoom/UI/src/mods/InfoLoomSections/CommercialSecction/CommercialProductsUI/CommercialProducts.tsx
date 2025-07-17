import React, { useState, useCallback, FC } from 'react';
import { Button, Dropdown, DropdownToggle, PanelProps, Panel, Icon, Tooltip } from 'cs2/ui';
import { InfoCheckbox } from 'mods/components//InfoCheckbox/InfoCheckbox';
import { getModule } from 'cs2/modding';
import styles from './CommercialProducts.module.scss';
import { formatWords } from 'mods/InfoLoomSections/utils/formatText';
import { useValue, bindLocalValue } from 'cs2/api';
import { CommercialProductsData } from 'mods/bindings';
import { CommercialProductData } from '../../../domain/commercialProductData';
import { useLocalization } from 'cs2/l10n';
import menuStyles from 'mods/InfoLoomMenu/InfoLoomMenu.module.scss';

const DropdownStyle = getModule('game-ui/menu/themes/dropdown.module.scss', 'classes');

interface CommercialProps extends PanelProps {}

interface ResourceLineProps {
  data: CommercialProductData;
}
const ResourceLine: React.FC<ResourceLineProps> = ({ data }) => {
  const formattedResourceName = formatWords(data.ResourceName, true);
  return (
    <div className={styles.row_S2v}>
      <div className={styles.col1}>
        <span>{formattedResourceName}</span>
      </div>
      <div className={styles.col2}>
        {data.Building}
      </div>
      <div className={styles.col3}>
        {data.Free}
      </div>
      <div className={styles.col4}>
        {data.Companies}
      </div>
      <div className={styles.col5}>
        {`${data.Workers}/${data.WrkPercent}%`}
      </div>
      <div className={styles.col6}>
        {`${data.SvcFactor}/${data.SvcPercent}%`}
      </div>
      <div className={styles.col7}>
        {`${data.ResourceNeedPercent}%`}
      </div>
      <div className={styles.col8}>
        {data.ResourceNeedPerCompany}
      </div>
      <div className={styles.col9}>
          {data.TaxFactor}
        </div>
    </div>
  );
};

const TableHeader: React.FC<{ }> = ({ }) => {
  return (
    <div className={styles.headerRow}>
      <div className={styles.col1}>
        <Tooltip tooltip={'The type/name of the resource'}>
        <span>{"Resource"}</span>
        </Tooltip>
      </div>
        <div className={styles.col2}>
          <Tooltip tooltip={' key number that decides what buildings will spawn; 0 means there is no demand'}>
        <span>{"Building Demand"}</span>
        </Tooltip>
      </div>
      <div className={styles.col3}>
        <Tooltip tooltip={'Free indicates the number of free properties available for the resource. This is the number of properties that are not currently occupied or used by any commercial buildings.'}>
          <span>{"Free"}</span>
        </Tooltip>
      </div>
      <div className={styles.col4}>
        <Tooltip tooltip={'Companies indicates the number of commercial companies that are currently operating for the resource.'}>
          <span>{"Companies"}</span>
        </Tooltip>
      </div>
      <div className={styles.col5}>
        <Tooltip tooltip={'Workers indicates the number of workers employed by the commercial companies for the resource and worker % indicates the percentage of workers employed compared to the total number of workers available in the city. 100% means all workers are employed.'}>
          <span>{`${"Workers"}/${"Worker"}%`}</span>
        </Tooltip>
      </div>
      <div className={styles.col6}>
        <Tooltip tooltip={'It measures how much more service (production) is needed for a resource, considering both demand and current capacity. A higher value means more unmet demand. Service % shows the percentage of available service capacity currently being used for this resource. If there is no available capacity, it displays 0%.'}>
          <span>{`${"Service"}/${"Service"}%`}</span>
        </Tooltip>
      </div>
      <div className={styles.col7}>
        <Tooltip tooltip={'ResourceNeedPercent: Indicates how much of the required production capacity for this resource is currently being met, capped so the denominator is at least 100 to avoid extreme values.'}>
        <span>{"Resource Need%"}</span>
        </Tooltip>
      </div>
      <div className={styles.col8}>
        <Tooltip tooltip={'ResourceNeedPerCompany: Shows the average amount of resource needed per service company for this resource. If there are no service companies, it displays 0.'}>
          <span>{"Resource Need/Company"}</span>
        </Tooltip>
      </div>
      <div className={styles.col9}>
        <Tooltip tooltip={'TaxFactor: Shows the effect of the current commercial tax rate on demand for this resource, scaled as a percentage. A higher value means taxes are reducing demand more.'}>
          <span>{"Tax Factor"}</span>
        </Tooltip>
      </div>
    </div>
  );
};

const LodgingHeader: React.FC = () => {
  return (
    <div className={styles.headerRow}>
      <div className={styles.col1}>
        
        <span>{`Resource`}</span>
      </div>
      <div className={styles.col2}>
          <span>{`Building Demand`}</span>
      </div>
      <div className={styles.col3}>
          <span>{`Free`}</span>
      </div>
      <div className={styles.col4}>
          <span>{`Companies`}</span>  
      </div>
      <div className={styles.col5}>
          <span>{`${"Workers"}/${"Worker"}%`}</span>
      </div>
      <div className={styles.col7}>
        <Tooltip tooltip={'CurrentTourists: Displays the number of tourists currently staying in lodging for this resource. Used to show current occupancy in hotels or similar accommodations.'}>
          <span>{`Current Tourists`}</span>
        </Tooltip>
      </div>
      <div className={styles.col8}>
        <Tooltip tooltip={'AvailableLodging: Displays the number of lodging rooms currently available for tourists for this resource. Used to show current hotel or accommodation capacity.'}>
          <span>{`Available Lodging`}</span>
        </Tooltip>
      </div>
      <div className={styles.col9}>
        <Tooltip tooltip={'RequiredRooms: Displays the number of lodging rooms needed to accommodate all current tourists for this resource, based on the required occupancy percentage. Used to show how many rooms are needed to meet demand.'}>
          <span>{`Required Rooms`}</span>
        </Tooltip>
      </div>
      <div className={styles.col9}>
          <span>{`Tax Factor`}</span>
      </div>
    </div>
  );
};

interface LodgingResourceLineProps {
  data: CommercialProductData;
}

// Update the LodgingResourceLine component to use the new interface
const LodgingResourceLine: React.FC<LodgingResourceLineProps> = ({ data }) => {
  const formattedResourceName = formatWords(data.ResourceName, true);

  return (
    <div className={styles.row_S2v}>
      <div className={styles.col1}>
          <span>{formattedResourceName}</span>
      </div>
      <div className={styles.col2}>
        {data.Building}
      </div>
      <div className={styles.col3}>
        {data.Free}
      </div>
      <div className={styles.col4}>
        {data.Companies}
      </div>
      <div className={styles.col5}>
        {`${data.Workers}/${data.WrkPercent}%`}
      </div>
      <div className={styles.col6}>
        {data.CurrentTourists}
      </div>
      <div className={styles.col8}>
        {data.AvailableLodging}
      </div>
      <div className={styles.col9}>
        {data.RequiredRooms}
      </div>
      <div className={styles.col9}>
        {data.TaxFactor}
      </div>
    </div>
  );
};
const OtherResourcesHeader: React.FC = () => {
  return (
    <div className={styles.headerRow}>
      <div className={styles.col1}>
        <span>Resource</span>
      </div>
      <div className={styles.col2}>
          <span>Building Demand</span>
      </div>
      <div className={styles.col3}>
          <span>Free</span>
      </div>
      <div className={styles.col4}>
          <span>Companies</span>
      </div>
      <div className={styles.col5}>
          <span>{`${"Workers"}/${"Worker"}%`}</span>
      </div>

      <div className={styles.col6}>
        <span>Resource Need/Company</span>
      </div>
      <div className={styles.col7}>
        <span>Tax Factor</span>
      </div>
    </div>
  );
};
const OtherResourcesLine: React.FC<ResourceLineProps> = ({ data }) => {
  const formattedResourceName = formatWords(data.ResourceName, true);
  return (
    <div className={styles.row_S2v}>
      <div className={styles.col1}>
        <span>{formattedResourceName}</span>
      </div>
        <div className={styles.col2}>
          {data.Building}
        </div>
        <div className={styles.col3}>
          {data.Free}
        </div>
        <div className={styles.col4}>
          {data.Companies}
        </div>
        <div className={styles.col5}>
          {`${data.Workers}/${data.WrkPercent}%`}
        </div>
        <div className={styles.col6}>
          {data.ResourceNeedPerCompany}
        </div>
        <div className={styles.col7}>
          {data.TaxFactor}
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
          <TableHeader/>
          {commercialProductsData.filter(item => !['Lodging', 'Meals', 'Entertainment', 'Recreation'].includes(item.ResourceName)).map((item, index) => (
            <ResourceLine key={`${item.ResourceName}-${index}`} data={item}/>
          ))}
          <LodgingHeader />
          {commercialProductsData.filter(item => item.ResourceName === 'Lodging').map((item, index) => (
            <LodgingResourceLine key={`${item.ResourceName}-${index}`} data={item} />
          ))}
          <OtherResourcesHeader />
          {commercialProductsData.filter(item => ['Meals', 'Entertainment', 'Recreation'].includes(item.ResourceName)).map((item, index) => (
            <OtherResourcesLine key={`${item.ResourceName}-${index}`} data={item} />
          ))}
        </div>
      )}
    </Panel>
  );
};

export default $CommercialProducts;