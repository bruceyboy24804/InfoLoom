import React, { useState, useCallback, FC } from 'react';
import $Panel from 'mods/panel';
import {Button, Dropdown, DropdownToggle, PanelProps, Scrollable} from 'cs2/ui';
import { InfoCheckbox } from 'mods/InfoCheckbox/InfoCheckbox';
import { getModule } from "cs2/modding";
import styles from './IndustrialProducts.module.scss';
import { ResourceIcon } from './resourceIcons';
import { formatWords } from 'mods/InfoLoomSections/utils/formatText';
import {bindValue, useValue} from "cs2/api";
import mod from "mod.json";


const DropdownStyle = getModule("game-ui/menu/themes/dropdown.module.scss", "classes");

interface IndustrialProductData {
  ResourceName: string;
  Demand: number;
  Building: number;
  Free: number;
  Companies: number;
  SvcPercent: number;
  CapPerCompany: number;
  CapPercent: number;
  Workers: number;
  WrkPercent: number;
  TaxFactor: number;
  
}
const IndustrialProduct$ = bindValue<IndustrialProductData[]>(mod.id, "industrialProducts", []);

// Interface for RowWithTwoColumns props
interface RowWithTwoColumnsProps {
  left: React.ReactNode;
  right: React.ReactNode;
}

// Component: RowWithTwoColumns
const RowWithTwoColumns: React.FC<RowWithTwoColumnsProps> = ({ left, right }) => {
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

// Interface for RowWithThreeColumns props
interface RowWithThreeColumnsProps {
  left: React.ReactNode;
  leftSmall?: React.ReactNode;
  right1: React.ReactNode;
  flag1: boolean;
  right2?: React.ReactNode;
  flag2?: boolean;
}

// Component: RowWithThreeColumns
const RowWithThreeColumns: React.FC<RowWithThreeColumnsProps> = ({
  left,
  leftSmall,
  right1,
  flag1,
  right2,
  flag2,
}) => {
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
      {right2 !== undefined && (
        flag2 ? (
          <div className="row_S2v negative_YWY" style={centerStyle}>
            {right2text}
          </div>
        ) : (
          <div className="row_S2v positive_zrK" style={centerStyle}>
            {right2text}
          </div>
        )
      )}
    </div>
  );
};

// Component: DataDivider
const DataDivider: React.FC = () => {
  return (
    <div style={{ display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center' }}>
      <div style={{ borderBottom: '1px solid gray' }}></div>
    </div>
  );
};

// Interface for SingleValue props
interface SingleValueProps {
  value: React.ReactNode;
  flag?: boolean;
  width?: string;
  small?: boolean;
}

// Component: SingleValue
const SingleValue: React.FC<SingleValueProps> = ({ value, flag, width, small }) => {
  const rowClass = small ? 'row_S2v small_ExK' : 'row_S2v';
  const centerStyle: React.CSSProperties = {
    width: width === undefined ? '10%' : width, // Changed default width to '10%'
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

// Interface for ResourceLine props


interface ResourceLineProps {
  data: IndustrialProductData;
  showColumns: {
    demand: boolean;
    buildings: boolean;
    storage: boolean;
    production: boolean;
    workers: boolean;
    tax: boolean;
  };
}

// Component: ResourceLine
const ResourceLine: React.FC<ResourceLineProps> = ({ data, showColumns }) => {
  // Use the display name mapping if available
  const displayName = data.ResourceName === 'Ore' ? 'MetalOre' : 
                     data.ResourceName === 'Oil' ? 'CrudeOil' : 
                     data.ResourceName;
  const formattedResourceName = formatWords(displayName, true);
  
  return (
    <div className={styles.row_S2v}>
      <div className={styles.cell} style={{ width: '3%' }}></div>
      <div className={styles.cell} style={{ width: '15%', justifyContent: 'flex-start', gap: '8px' }}>
        <ResourceIcon resourceName={data.ResourceName} />
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
      {showColumns.storage && (
        <div className={styles.cell} style={{ width: '12%' }}>
          {data.SvcPercent}
        </div>
      )}
      {showColumns.production && (
        <>
          <div className={styles.cell} style={{ width: '10%' }}>
            {data.CapPerCompany}
          </div>
          <div className={styles.cell} style={{ width: '10%' }}>
            {data.CapPercent}
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
        <div className={`${styles.cell} ${data.TaxFactor < 0 ? styles.negative_YWY : ''}`} style={{ width: '12%' }}>
          {data.TaxFactor}
        </div>
      )}
    </div>
  );
};

// Header component for the resource table
const TableHeader: React.FC<{ showColumns: any }> = ({ showColumns }) => {
  return (
    <div className={styles.headerRow}>
      <div className={styles.headerCell} style={{ width: '3%' }}></div>
      <div className={styles.headerCell} style={{ width: '15%' }}>
        Resource
        <div className={styles.tooltip}>Resource type and icon</div>
      </div>
      {showColumns.demand && (
        <>
          <div className={styles.headerCell} style={{ width: '6%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <span>Resource</span>
            <span>Demand</span>
          </div>
          <div className={styles.headerCell} style={{ width: '4%', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <span>Building</span>
            <span>Demand</span>
          </div>
          <div className={styles.headerCell} style={{ width: '4%' }}>
            Free
            <div className={styles.tooltip}>Free</div>
          </div>
          <div className={styles.headerCell} style={{ width: '5%' }}>
            Comp
            <div className={styles.tooltip}>Number of companies</div>
          </div>
        </>
      )}
      {showColumns.storage && (
        <div className={styles.headerCell} style={{ width: '12%' }}>
          Storage
          <div className={styles.tooltip}>No. of storage buildings</div>
        </div>
      )}
      {showColumns.production && (
        <>
          <div className={styles.headerCell} style={{ width: '10%' }}>
            Production
            <div className={styles.tooltip}>Production</div>
          </div>
          <div className={styles.headerCell} style={{ width: '10%' }}>
            Demand
            <div className={styles.tooltip}>Demand</div>
          </div>
        </>
      )}
      {showColumns.workers && (
        <>
          <div className={styles.headerCell} style={{ width: '9%' }}>
            Count
            <div className={styles.tooltip}>Number of workers</div>
          </div>
          <div className={styles.headerCell} style={{ width: '9%' }}>
            Staffing
            <div className={styles.tooltip}>Worker staffing percentage</div>
          </div>
        </>
      )}
      {showColumns.tax && (
        <div className={styles.headerCell} style={{ width: '12%' }}>
          Tax Factor
          <div className={styles.tooltip}>Tax rate multiplier</div>
        </div>
      )}
    </div>
  );
};

// Interface for $Industrial props
interface IndustrialProps extends PanelProps {
}

// Component: $Commercial
const $IndustrialProducts: FC<IndustrialProps> = ({ onClose }) => {
  const industrialProducts = useValue(IndustrialProduct$);

  // State to control panel visibility
  const [isPanelVisible, setIsPanelVisible] = useState(true);

  
  
  // Column visibility toggles
  const [showColumns, setShowColumns] = useState({
    demand: true,
    buildings: true,
    storage: true,
    production: true,
    workers: true,
    tax: true,
  });

  // State for sorting and filtering
  const [sortBy, setSortBy] = useState<'name' | 'demand' | 'workers' | 'tax'>('name');
  const [filterDemand, setFilterDemand] = useState<'all' | 'positive' | 'negative'>('all');
  const [filterWorkers, setFilterWorkers] = useState<'all' | 'full' | 'partial' | 'none'>('all');

  // Toggle column visibility
  const toggleColumn = useCallback((column: keyof typeof showColumns) => {
    
    setShowColumns(prev => {
      const newState = {
        ...prev,
        [column]: !prev[column]
      };
      
      return newState;
    });
  }, []);

  // Sort and filter functions
  const sortData = useCallback((a: IndustrialProductData, b: IndustrialProductData) => {
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


  const applyDataFilter = useCallback((item: IndustrialProductData) => {
  if (filterDemand === 'positive' && item.Demand <= 0) return false;
  if (filterDemand === 'negative' && item.Demand >= 0) return false;
  if (filterWorkers === 'full' && item.WrkPercent < 100) return false;
  if (filterWorkers === 'partial' && (item.WrkPercent === 0 || item.WrkPercent === 100)) return false;
  if (filterWorkers === 'none' && item.WrkPercent > 0) return false;
  return true;
}, [filterDemand, filterWorkers]);

  
  

  if (!isPanelVisible) {
    return null;
  }

  return (
    <$Panel
      id="infoloom-industrial-products"
      
      title="Industrial and Office Products"
      onClose={onClose}
      initialSize={{ width: window.innerWidth * 0.50, height: window.innerHeight * 0.70 }}
     
      initialPosition={{ top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 }}
    >
      {industrialProducts.length === 0 ? (
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
                Filter by Demand
              </DropdownToggle>
            </Dropdown>

            <Dropdown
              theme={DropdownStyle}
              content={
                <div style={{ padding: '0.5rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="All Workers"
                      isChecked={filterWorkers === 'all'}
                      onToggle={() => setFilterWorkers('all')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Fully Staffed"
                      isChecked={filterWorkers === 'full'}
                      onToggle={() => setFilterWorkers('full')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Partially Staffed"
                      isChecked={filterWorkers === 'partial'}
                      onToggle={() => setFilterWorkers('partial')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="No Workers"
                      isChecked={filterWorkers === 'none'}
                      onToggle={() => setFilterWorkers('none')}
                    />
                  </div>
                </div>
              }
            >
              <DropdownToggle style={{ marginRight: '5rem' }}>
                Filter by Workers
              </DropdownToggle>
            </Dropdown>

            <Dropdown
              theme={DropdownStyle}
              content={
                <div style={{ padding: '0.5rem', display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Demand Info"
                      isChecked={showColumns.demand}
                      onToggle={() => toggleColumn('demand')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Buildings"
                      isChecked={showColumns.buildings}
                      onToggle={() => toggleColumn('buildings')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Storage"
                      isChecked={showColumns.storage}
                      onToggle={() => toggleColumn('storage')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Production"
                      isChecked={showColumns.production}
                      onToggle={() => toggleColumn('production')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Workers"
                      isChecked={showColumns.workers}
                      onToggle={() => toggleColumn('workers')}
                    />
                  </div>
                  <div style={{ padding: '4px 8px', cursor: 'pointer' }}>
                    <InfoCheckbox
                      label="Tax"
                      isChecked={showColumns.tax}
                      onToggle={() => toggleColumn('tax')}
                    />
                  </div>
                </div>
              }
            >
              <DropdownToggle style={{ marginRight: '5rem' }}>
                Column Visibility
              </DropdownToggle>
            </Dropdown>
          </div>

          <TableHeader showColumns={showColumns} />
            <Scrollable vertical={true} smooth={true} trackVisibility="scrollable">
              {industrialProducts
              .filter((item: IndustrialProductData) => item.ResourceName !== 'NoResource')
              .filter(applyDataFilter)
              .sort((a: IndustrialProductData, b: IndustrialProductData) => sortData(a, b))
              .map((item: IndustrialProductData) => (
                <ResourceLine
                  key={item.ResourceName}
                  data={item}
                  showColumns={showColumns}
                />
              ))}
            </Scrollable>
        </div>
      )}
    </$Panel>
  );
};

export default $IndustrialProducts;   