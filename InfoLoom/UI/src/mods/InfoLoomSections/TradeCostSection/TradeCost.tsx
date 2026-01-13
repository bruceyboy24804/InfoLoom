import React, { FC } from 'react';
import { Panel, Tooltip, Icon, Portal } from 'cs2/ui';
import styles from './TradeCost.module.scss';
import { useValue } from 'cs2/api';
import { LocalizedNumber, useLocalization, Unit, LocalizedString } from 'cs2/l10n';
import {
  TradeCostsData,
  SetResourceNameSorting,
  SetBuyCostSorting,
  SetSellCostSorting,
  SetProfitSorting,
  SetProfitMarginSorting,
  ResourceNameSorting,
  BuyCostSorting,
  SellCostSorting,
  ProfitSorting,
  ProfitMarginSorting,
  ImportAmountSorting,
  ExportAmountSorting,
  SetImportAmountSorting,
  SetExportAmountSorting,
} from '../../bindings';
import { ResourceTradeCost } from 'mods/domain/tradeCostData';
import {
  SortingEnum,
  OutsideConnectionType,
} from 'mods/domain/TradeCostEnums';
import { formatWords } from '../utils/formatText';
import { OutsideConnectionSelector } from './Selectors/outsideConnectionSelector';
import { Localekeys } from 'mods/locale';

const DataDivider: FC = () => <div className={styles.dataDivider} />;

function getProfitClass(value: number) {
  if (value < 0) return styles.negative;
  if (value > 0) return styles.positive;
  return styles.neutral;
}

function calculateProfit(data: ResourceTradeCost) {
  return data.SellCost - data.BuyCost;
}

function calculateProfitMargin(data: ResourceTradeCost) {
  return data.BuyCost !== 0 ? (data.SellCost - data.BuyCost) / data.BuyCost : 0;
}

interface SortableHeaderProps {
  label: string | null
  tooltip?: string | null;
  sortState: number;
  onSort: (direction: 'asc' | 'desc' | 'off') => void;
  className?: string;
}

const SortableHeader: FC<SortableHeaderProps> = ({ label, tooltip, sortState, onSort, className }) => {
  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (sortState === 0) onSort('asc');
    else if (sortState === 1) onSort('desc');
    else onSort('off');
  };

  return (
    <Tooltip tooltip={tooltip}>
      <div
        className={`${styles.sortableHeader} ${styles.headerCell} ${className || ''}`}
        onClick={handleClick}
        style={{ cursor: 'pointer' }}
      >
        <span>{label}</span>
        <div className={styles.sortArrows}>
          {sortState === 1 && <Icon src="coui://uil/Standard/ArrowSortHighDown.svg" className={styles.sortIcon} />}
          {sortState === 2 && <Icon src="coui://uil/Standard/ArrowSortLowDown.svg" className={styles.sortIcon} />}
        </div>
      </div>
    </Tooltip>
  );
};

interface TradeCostPanelProps {
  onClose?: () => void;
}

const TradeCostPanel: FC<TradeCostPanelProps> = ({ onClose }) => {
  const { translate } = useLocalization();
  const tradeCosts = useValue(TradeCostsData);
  const resourceNameSorting = useValue(ResourceNameSorting);
  const buyCostSorting = useValue(BuyCostSorting);
  const sellCostSorting = useValue(SellCostSorting);
  const profitSorting = useValue(ProfitSorting);
  const profitMarginSorting = useValue(ProfitMarginSorting);
  const importAmountSorting = useValue(ImportAmountSorting);
  const exportAmountSorting = useValue(ExportAmountSorting);

  const onSort = {
    ResourceName: (direction: 'asc' | 'desc' | 'off') => {
      if (direction === 'asc') SetResourceNameSorting(SortingEnum.Ascending);
      else if (direction === 'desc') SetResourceNameSorting(SortingEnum.Descending);
      else SetResourceNameSorting(SortingEnum.Off);
    },
    BuyCost: (direction: 'asc' | 'desc' | 'off') => {
      if (direction === 'asc') SetBuyCostSorting(SortingEnum.Ascending);
      else if (direction === 'desc') SetBuyCostSorting(SortingEnum.Descending);
      else SetBuyCostSorting(SortingEnum.Off);
    },
    SellCost: (direction: 'asc' | 'desc' | 'off') => {
      if (direction === 'asc') SetSellCostSorting(SortingEnum.Ascending);
      else if (direction === 'desc') SetSellCostSorting(SortingEnum.Descending);
      else SetSellCostSorting(SortingEnum.Off);
    },
    Profit: (direction: 'asc' | 'desc' | 'off') => {
      if (direction === 'asc') SetProfitSorting(SortingEnum.Ascending);
      else if (direction === 'desc') SetProfitSorting(SortingEnum.Descending);
      else SetProfitSorting(SortingEnum.Off);
    },
    ProfitMargin: (direction: 'asc' | 'desc' | 'off') => {
      if (direction === 'asc') SetProfitMarginSorting(SortingEnum.Ascending);
      else if (direction === 'desc') SetProfitMarginSorting(SortingEnum.Descending);
      else SetProfitMarginSorting(SortingEnum.Off);
    },
    ImportAmount: (direction: 'asc' | 'desc' | 'off') => {
      if (direction === 'asc') SetImportAmountSorting(SortingEnum.Ascending);
      else if (direction === 'desc') SetImportAmountSorting(SortingEnum.Descending);
      else SetImportAmountSorting(SortingEnum.Off);
    },
    ExportAmount: (direction: 'asc' | 'desc' | 'off') => {
      if (direction === 'asc') SetExportAmountSorting(SortingEnum.Ascending);
      else if (direction === 'desc') SetExportAmountSorting(SortingEnum.Descending);
      else SetExportAmountSorting(SortingEnum.Off);
    },
  };

  // Early return for no data
  if (!tradeCosts || tradeCosts.length === 0) {
    return (
      <Portal>
        <Panel
          draggable
          onClose={onClose}
          initialPosition={{ x: 0.5, y: 0.5 }}
          className={styles.panel}
          header={
            <div className={styles.header}>
              <span className={styles.headerText}>
                {translate?.(Localekeys.TradeCosts, 'Trade Costs')}
              </span>
            </div>
          }
        >
          <p className={styles.loadingText}>
            {translate?.(Localekeys.Waiting, 'Waiting')}
          </p>
        </Panel>
      </Portal>
    );
  }

  return (
    <Portal>
      <Panel
        draggable
        onClose={onClose}
        initialPosition={{ x: 0.5, y: 0.5 }}
        className={styles.panel}
        header={
          <div className={styles.header}>
            <span className={styles.headerText}>
              {translate?.(Localekeys.TradeCosts, 'Trade Costs')}
            </span>
          </div>
        }
      >
        <div className={styles.panelContent}>
          <div className={styles.outsideConnectionContainer}>
          <OutsideConnectionSelector />
          </div>

          <div className={styles.tableHeader}>
            <div className={styles.headerRow}>
              <div className={`${styles.headerCell} ${styles.iconColumn}`}>
                <b>Icon</b>
              </div>

              <SortableHeader
                label={translate(Localekeys.Resources)}
                tooltip={translate(Localekeys.ResourceHeaderTooltip)}
                sortState={resourceNameSorting}
                onSort={onSort.ResourceName}
                className={styles.resourceColumn}
              />

              <SortableHeader
                label={translate(Localekeys.BuyCostHeader)}
                tooltip={translate(Localekeys.BuyCostHeaderTooltip)}
                sortState={buyCostSorting}
                onSort={onSort.BuyCost}
                className={styles.buyCostColumn}
              />

              <SortableHeader
                label={translate(Localekeys.SellCostHeader)}
                tooltip={translate(Localekeys.SellCostHeaderTooltip)}
                sortState={sellCostSorting}
                onSort={onSort.SellCost}
                className={styles.sellCostColumn}
              />

              <SortableHeader
                label={translate(Localekeys.ProfitHeader)}
                tooltip={translate(Localekeys.ProfitHeaderTooltip)}
                sortState={profitSorting}
                onSort={onSort.Profit}
                className={styles.profitColumn}
              />

              <SortableHeader
                label={translate(Localekeys.ProfitMarginHeader)}
                tooltip={translate(Localekeys.ProfitMarginHeaderTooltip)}
                sortState={profitMarginSorting}
                onSort={onSort.ProfitMargin}
                className={styles.profitMarginColumn}
              />

              <SortableHeader
                label={translate(Localekeys.ImportAmountHeader)}
                tooltip={translate(Localekeys.ImportAmountHeaderTooltip)}
                sortState={importAmountSorting}
                onSort={onSort.ImportAmount}
                className={styles.importAmountColumn}
              />

              <SortableHeader
                label={translate(Localekeys.ExportAmountHeader)}
                tooltip={translate(Localekeys.ExportAmountHeaderTooltip)}
                sortState={exportAmountSorting}
                onSort={onSort.ExportAmount}
                className={styles.exportAmountColumn}
              />
            </div>
          </div>

          <DataDivider />

          <div className={styles.tableBody}>
            {tradeCosts.map(row => {
              return (
                <div className={styles.row} key={row.Resource}>
                  <div className={styles.iconColumn}>
                    <Icon src={row.ResourceIcon} className={styles.resourceIcon} />
                  </div>

                  <div className={styles.resourceColumn}><LocalizedString id={formatWords(row.Resource)} showIdOnFail={true} /></div>

                  <div className={styles.buyCostColumn}>
                    <LocalizedNumber value={row.BuyCost} unit={Unit.FloatTwoFractions} />
                  </div>

                  <div className={styles.sellCostColumn}>
                    <LocalizedNumber value={row.SellCost} unit={Unit.FloatTwoFractions} />
                  </div>

                  <div className={`${styles.profitColumn} ${getProfitClass(calculateProfit(row))}`}>
                    <LocalizedNumber value={calculateProfit(row)} unit={Unit.FloatTwoFractions} />
                  </div>

                  <div className={`${styles.profitMarginColumn} ${getProfitClass(calculateProfitMargin(row))}`}>
                    <LocalizedNumber value={calculateProfitMargin(row)} unit={Unit.PercentageSingleFraction} />
                  </div>

                  <div className={styles.importAmountColumn}>
                    <LocalizedNumber value={row.ImportAmount} unit={Unit.Weight} />
                  </div>

                  <div className={styles.exportAmountColumn}>
                    <LocalizedNumber value={row.ExportAmount} unit={Unit.Weight} />
                  </div>
                </div>
              );
            })}
          </div>

          <DataDivider />
        </div>
      </Panel>
    </Portal>
  );
};

export default TradeCostPanel;
