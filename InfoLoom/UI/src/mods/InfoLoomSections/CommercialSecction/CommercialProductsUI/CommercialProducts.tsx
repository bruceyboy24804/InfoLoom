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

type ShowColumnsType = {
  demand: boolean;
  service: boolean;
  capacity: boolean;
  workers: boolean;
  tax: boolean;
};

interface ResourceLineProps {
  data: CommercialProductData;
  showColumns: ShowColumnsType;
}

const ResourceLine: React.FC<ResourceLineProps> = ({ data, showColumns }) => {
  const formattedResourceName = formatWords(data.ResourceName, true);

  return (
    <div className={styles.row_S2v}>
      <div className={styles.cell} style={{ width: '3%' }}></div>
      <div className={styles.cell} style={{ width: '15%', display: 'flex', alignItems: 'center', gap: '8px' }}>
        <Icon src={data.ResourceIcon}/>
        <span>{formattedResourceName}</span>
      </div>
      {showColumns.demand && (
        <>
          <div className={`${styles.cell} ${data.Demand < 0 ? styles.negative_YWY : ''}`} style={{ width: '6%' }}>
            {data.Demand}
          </div>
          <div className={`${styles.cell} ${data.Building <= 0 ? styles.negative_YWY : ''}`} style={{ width: '4%' }}>
            {data.Building}
          </div>
          <div className={`${styles.cell} ${data.Free <= 0 ? styles.negative_YWY : ''}`} style={{ width: '4%' }}>
            {data.Free}
          </div>
          <div className={styles.cell} style={{ width: '5%' }}>
            {data.Companies}
          </div>
        </>
      )}
      {showColumns.service && (
        <div className={`${styles.cell} ${data.SvcPercent > 50 ? styles.negative_YWY : ''}`} style={{ width: '12%' }}>
          {`${data.SvcPercent}%`}
        </div>
      )}
      {showColumns.capacity && (
        <>
          <div className={styles.cell} style={{ width: '10%' }}>
            {data.CapPerCompany}
          </div>
          <div className={`${styles.cell} ${data.CapPercent > 200 ? styles.negative_YWY : ''}`} style={{ width: '10%' }}>
            {`${data.CapPercent}%`}
          </div>
        </>
      )}
      {showColumns.workers && (
        <>
          <div className={styles.cell} style={{ width: '9%' }}>
            {data.Workers}
          </div>
          <div className={`${styles.cell} ${data.WrkPercent < 90 ? styles.negative_YWY : styles.positive_zrK}`} style={{ width: '9%' }}>
            {`${data.WrkPercent}%`}
          </div>
        </>
      )}
      {showColumns.tax && (
        <div className={`${styles.cell}`} style={{ width: '12%' }}>
          {data.TaxFactor}%
        </div>
      )}
    </div>
  );
};

const TableHeader: React.FC<{ showColumns: ShowColumnsType }> = ({ showColumns }) => {
  return (
    <div className={styles.headerRow}>
      <div className={styles.headerCell} style={{ width: '3%' }}></div>
      <div className={styles.headerCell} style={{ width: '15%' }}>
        Resource
      </div>
      {showColumns.demand && (
        <>
          <div className={styles.headerCell} style={{ width: '6%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <span>Resource</span>
            <span>Demand</span>
          </div>
          <div className={styles.headerCell} style={{ width: '4%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <span>Zone</span>
            <span>Demand</span>
          </div>
          <div className={styles.headerCell} style={{ width: '4%' }}>Free</div>
          <div className={styles.headerCell} style={{ width: '5%' }}>Num</div>
        </>
      )}
      {showColumns.service && (
        <div className={styles.headerCell} style={{ width: '12%' }}>Service%</div>
      )}
      {showColumns.capacity && (
        <>
          <div className={styles.headerCell} style={{ width: '10%' }}>Cap/Co</div>
          <div className={styles.headerCell} style={{ width: '10%' }}>Cap%</div>
        </>
      )}
      {showColumns.workers && (
        <>
          <div className={styles.headerCell} style={{ width: '9%' }}>Workers</div>
          <div className={styles.headerCell} style={{ width: '9%' }}>Work%</div>
        </>
      )}
      {showColumns.tax && (
        <div className={styles.headerCell} style={{ width: '12%' }}>Tax</div>
      )}
    </div>
  );
};

const $CommercialProducts: FC<CommercialProps> = ({ onClose }) => {
  const commercialProductsData = useValue(CommercialProductsData);
  
  const [showColumns, setShowColumns] = useState<ShowColumnsType>({
    demand: true,
    service: true,
    capacity: true,
    workers: true,
    tax: true,
  });

  const [sortBy, setSortBy] = useState<'name' | 'demand' | 'workers' | 'tax'>('name');
  const [filterDemand, setFilterDemand] = useState<'all' | 'positive' | 'negative'>('all');

  const toggleColumn = useCallback((column: keyof ShowColumnsType) => {
    setShowColumns(prev => ({
      ...prev,
      [column]: !prev[column]
    }));
  }, []);

  const sortData = useCallback((a: CommercialProductData, b: CommercialProductData) => {
    switch (sortBy) {
      case 'name':
        return a.ResourceName.localeCompare(b.ResourceName);
      case 'demand':
        return b.Demand - a.Demand;
      case 'workers':
        return b.Workers - a.Workers;
      case 'tax':
        return b.TaxFactor - a.TaxFactor;
      default:
        return 0;
    }
  }, [sortBy]);

  const filterData = useCallback((item: CommercialProductData) => {
    if (filterDemand === 'positive' && item.Demand <= 0) return false;
    if (filterDemand === 'negative' && item.Demand >= 0) return false;
    return true;
  }, [filterDemand]);

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
          <div className={styles.controls}>
            <Dropdown
              theme={DropdownStyle}
              content={
                <div className={styles.dropdownContent}>
                  <div className={styles.dropdownItem}>
                    <InfoCheckbox
                      label="Sort by Name"
                      isChecked={sortBy === 'name'}
                      onToggle={() => setSortBy('name')}
                    />
                  </div>
                  <div className={styles.dropdownItem}>
                    <InfoCheckbox
                      label="Sort by Demand"
                      isChecked={sortBy === 'demand'}
                      onToggle={() => setSortBy('demand')}
                    />
                  </div>
                  <div className={styles.dropdownItem}>
                    <InfoCheckbox
                      label="Sort by Workers"
                      isChecked={sortBy === 'workers'}
                      onToggle={() => setSortBy('workers')}
                    />
                  </div>
                  <div className={styles.dropdownItem}>
                    <InfoCheckbox
                      label="Sort by Tax"
                      isChecked={sortBy === 'tax'}
                      onToggle={() => setSortBy('tax')}
                    />
                  </div>
                </div>
              }
            >
              <DropdownToggle style={{ marginRight: '5rem' }}>
                Sort Options
              </DropdownToggle>
            </Dropdown>

            <Dropdown
              theme={DropdownStyle}
              content={
                <div className={styles.dropdownContent}>
                  <div className={styles.dropdownItem}>
                    <InfoCheckbox
                      label="Show Demand"
                      isChecked={showColumns.demand}
                      onToggle={() => toggleColumn('demand')}
                    />
                  </div>
                  <div className={styles.dropdownItem}>
                    <InfoCheckbox
                      label="Show Service"
                      isChecked={showColumns.service}
                      onToggle={() => toggleColumn('service')}
                    />
                  </div>
                  <div className={styles.dropdownItem}>
                    <InfoCheckbox
                      label="Show Capacity"
                      isChecked={showColumns.capacity}
                      onToggle={() => toggleColumn('capacity')}
                    />
                  </div>
                  <div className={styles.dropdownItem}>
                    <InfoCheckbox
                      label="Show Workers"
                      isChecked={showColumns.workers}
                      onToggle={() => toggleColumn('workers')}
                    />
                  </div>
                  <div className={styles.dropdownItem}>
                    <InfoCheckbox
                      label="Show Tax"
                      isChecked={showColumns.tax}
                      onToggle={() => toggleColumn('tax')}
                    />
                  </div>
                </div>
              }
            >
              <DropdownToggle style={{ marginRight: '5rem' }}>
                Column Options
              </DropdownToggle>
            </Dropdown>

            <Dropdown
              theme={DropdownStyle}
              content={
                <div className={styles.dropdownContent}>
                  <div className={styles.dropdownItem}>
                    <InfoCheckbox
                      label="All Demand"
                      isChecked={filterDemand === 'all'}
                      onToggle={() => setFilterDemand('all')}
                    />
                  </div>
                  <div className={styles.dropdownItem}>
                    <InfoCheckbox
                      label="Positive Demand"
                      isChecked={filterDemand === 'positive'}
                      onToggle={() => setFilterDemand('positive')}
                    />
                  </div>
                  <div className={styles.dropdownItem}>
                    <InfoCheckbox
                      label="Negative Demand"
                      isChecked={filterDemand === 'negative'}
                      onToggle={() => setFilterDemand('negative')}
                    />
                  </div>
                </div>
              }
            >
              <DropdownToggle style={{ marginRight: '5rem' }}>
                Filter Options
              </DropdownToggle>
            </Dropdown>
          </div>

          <TableHeader showColumns={showColumns} />

          {commercialProductsData
            .filter(item => item.ResourceName !== 'NoResource')
            .filter(filterData)
            .sort(sortData)
            .map((item, index) => (
              <ResourceLine key={`${item.ResourceName}-${index}`} data={item} showColumns={showColumns} />
            ))}
        </div>
      )}
    </Panel>
  );
};

export default $CommercialProducts;