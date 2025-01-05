import React, { useState, useCallback, FC } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';
import { Button, Dropdown, DropdownToggle } from 'cs2/ui';
import { InfoCheckbox } from '../../InfoCheckbox/InfoCheckbox';
import { getModule } from "cs2/modding";
import styles from './CommercialProducts.module.scss';
import { ResourceIcon } from './resourceIcons';
import { formatWords } from '../utils/formatText';

const DropdownStyle = getModule("game-ui/menu/themes/dropdown.module.scss", "classes");

interface ResourceData {
  resource: string;
  demand: number;
  building: number;
  free: number;
  companies: number;
  svcpercent: number;
  cappercompany: number;
  cappercent: number;
  workers: number;
  wrkpercent: number;
  taxfactor: number;
  details: string;
}

interface CommercialProps {
  onClose: () => void;
}

type ShowColumnsType = {
  demand: boolean;
  service: boolean;
  capacity: boolean;
  workers: boolean;
  tax: boolean;
};

interface ResourceLineProps {
  data: ResourceData;
  showColumns: ShowColumnsType;
}

const ResourceLine: React.FC<ResourceLineProps> = ({ data, showColumns }) => {
  // Use the display name mapping if available
  const displayName = data.resource === 'Ore' ? 'MetalOre' : 
                     data.resource === 'Oil' ? 'CrudeOil' : 
                     data.resource;
  const formattedResourceName = formatWords(displayName, true);
  
  return (
    <div className={styles.row_S2v}>
      <div className={styles.cell} style={{ width: '3%' }}></div>
      <div className={styles.cell} style={{ width: '15%', justifyContent: 'flex-start', gap: '8px' }}>
        <ResourceIcon resourceName={data.resource} />
        <span>{formattedResourceName}</span>
      </div>
      {showColumns.demand && (
        <>
          <div className={`${styles.cell} ${data.demand < 0 ? styles.negative_YWY : ''}`} style={{ width: '6%' }}>
            {data.demand}
          </div>
          <div className={`${styles.cell} ${data.building <= 0 ? styles.negative_YWY : ''}`} style={{ width: '4%' }}>
            {data.building}
          </div>
          <div className={`${styles.cell} ${data.free <= 0 ? styles.negative_YWY : ''}`} style={{ width: '4%' }}>
            {data.free}
          </div>
          <div className={styles.cell} style={{ width: '5%' }}>
            {data.companies}
          </div>
        </>
      )}
      {showColumns.service && (
        <div className={`${styles.cell} ${data.svcpercent > 50 ? styles.negative_YWY : ''}`} style={{ width: '12%' }}>
          {`${data.svcpercent}%`}
        </div>
      )}
      {showColumns.capacity && (
        <>
          <div className={styles.cell} style={{ width: '10%' }}>
            {data.cappercompany}
          </div>
          <div className={`${styles.cell} ${data.cappercent > 200 ? styles.negative_YWY : ''}`} style={{ width: '10%' }}>
            {`${data.cappercent}%`}
          </div>
        </>
      )}
      {showColumns.workers && (
        <>
          <div className={styles.cell} style={{ width: '9%' }}>
            {data.workers}
          </div>
          <div className={`${styles.cell} ${data.wrkpercent < 90 ? styles.negative_YWY : styles.positive_zrK}`} style={{ width: '9%' }}>
            {`${data.wrkpercent}%`}
          </div>
        </>
      )}
      {showColumns.tax && (
        <div className={`${styles.cell} ${data.taxfactor < 0 ? styles.negative_YWY : ''}`} style={{ width: '12%' }}>
          {data.taxfactor}
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
        <div className={styles.headerCell} style={{ width: '12%' }}>Service</div>
      )}
      {showColumns.capacity && (
        <>
          <div className={styles.headerCell} style={{ width: '20%' }}>Household Need</div>
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

const CommercialProducts: FC<CommercialProps> = ({ onClose }) => {
  const [demandData, setDemandData] = useState<ResourceData[]>([]);
  useDataUpdate('realEco.commercialDemand', setDemandData);

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

  const sortData = useCallback((a: ResourceData, b: ResourceData) => {
    switch (sortBy) {
      case 'name':
        return a.resource.localeCompare(b.resource);
      case 'demand':
        return b.demand - a.demand;
      case 'workers':
        return b.workers - a.workers;
      case 'tax':
        return b.taxfactor - a.taxfactor;
      default:
        return 0;
    }
  }, [sortBy]);

  const filterData = useCallback((item: ResourceData) => {
    if (filterDemand === 'positive' && item.demand <= 0) return false;
    if (filterDemand === 'negative' && item.demand >= 0) return false;
    return true;
  }, [filterDemand]);

  const handleClose = useCallback(() => {
    onClose();
  }, [onClose]);

  return (
    <$Panel
      id="infoloom-commercial-products"
      title="Commercial Products"
      onClose={handleClose}
      initialSize={{ width: window.innerWidth * 0.45, height: window.innerHeight * 0.32 }}
      initialPosition={{ top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 }}
    >
      {demandData.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div>
          <div style={{ marginBottom: '1rem', display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
            <Dropdown
              theme={DropdownStyle}
              content={
                <div style={{ padding: '0.5rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Sort by Name"
                      isChecked={sortBy === 'name'}
                      onToggle={() => setSortBy('name')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Sort by Demand"
                      isChecked={sortBy === 'demand'}
                      onToggle={() => setSortBy('demand')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Sort by Workers"
                      isChecked={sortBy === 'workers'}
                      onToggle={() => setSortBy('workers')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
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
                <div style={{ padding: '0.5rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Show Demand"
                      isChecked={showColumns.demand}
                      onToggle={() => toggleColumn('demand')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Show Service"
                      isChecked={showColumns.service}
                      onToggle={() => toggleColumn('service')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Show Capacity"
                      isChecked={showColumns.capacity}
                      onToggle={() => toggleColumn('capacity')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Show Workers"
                      isChecked={showColumns.workers}
                      onToggle={() => toggleColumn('workers')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
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
                <div style={{ padding: '0.5rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="All Demand"
                      isChecked={filterDemand === 'all'}
                      onToggle={() => setFilterDemand('all')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Positive Demand"
                      isChecked={filterDemand === 'positive'}
                      onToggle={() => setFilterDemand('positive')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
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
          
          {demandData
            .filter(item => item.resource !== 'NoResource')
            .filter(filterData)
            .sort(sortData)
            .map(item => (
              <ResourceLine key={item.resource} data={item} showColumns={showColumns} />
            ))}
        </div>
      )}
    </$Panel>
  );
};

export default CommercialProducts;
