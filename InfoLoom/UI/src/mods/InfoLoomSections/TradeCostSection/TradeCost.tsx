import React, { FC } from 'react';
import { Panel, Tooltip, Icon, Portal } from 'cs2/ui';
import styles from './TradeCost.module.scss';
import { useValue } from 'cs2/api';
import { LocalizedNumber, useLocalization, Unit, LocalizedString } from 'cs2/l10n';
import {
  TradeCostsData,
  TC
} from '../../bindings';
import { ResourceTradeCost } from 'mods/domain/tradeCostData';
import { TCSortingEnum, OutsideConnectionType } from 'mods/domain/TradeCostEnums';
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
  label: string | null;
  tooltip?: string | null;
  sortState: TCSortingEnum;
  onSort: (next: TCSortingEnum) => void;
  className?: string;
}

// Clicking a header cycles Off -> Ascending -> Descending -> Off.
const NEXT_SORT: Record<TCSortingEnum, TCSortingEnum> = {
  [TCSortingEnum.Off]: TCSortingEnum.Ascending,
  [TCSortingEnum.Ascending]: TCSortingEnum.Descending,
  [TCSortingEnum.Descending]: TCSortingEnum.Off,
};

const SORT_ICON: Record<TCSortingEnum, string | null> = {
  [TCSortingEnum.Off]: null,
  [TCSortingEnum.Ascending]: 'coui://il/Sorting/ArrowSortHighDown.svg',
  [TCSortingEnum.Descending]: 'coui://il/Sorting/ArrowSortLowDown.svg',
};

const SortableHeader: FC<SortableHeaderProps> = ({ label, tooltip, sortState, onSort, className }) => {
  const icon = SORT_ICON[sortState];

  return (
    <Tooltip tooltip={tooltip}>
      <div
        className={`${styles.sortableHeader} ${styles.headerCell} ${className || ''}`}
        onClick={(e: React.MouseEvent) => {
          e.preventDefault();
          e.stopPropagation();
          onSort(NEXT_SORT[sortState]);
        }}
        style={{ cursor: 'pointer' }}
      >
        <span>{label}</span>
        <div className={styles.sortArrows}>
          {icon && <Icon src={icon} className={styles.sortIcon} />}
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
  const tradeCosts = useValue(TradeCostsData.binding);
  const resourceNameSorting = useValue(TC.ResourceName.binding);
  const buyCostSorting = useValue(TC.BuyCost.binding);
  const sellCostSorting = useValue(TC.SellCost.binding);
  const profitSorting = useValue(TC.Profit.binding);
  const profitMarginSorting = useValue(TC.ProfitMargin.binding);
  const importAmountSorting = useValue(TC.ImportAmount.binding);
  const exportAmountSorting = useValue(TC.ExportAmount.binding);

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
              <span className={styles.headerText}>{translate?.(Localekeys.TradeCosts, 'Trade Costs')}</span>
            </div>
          }
        >
          <p className={styles.loadingText}>{translate?.(Localekeys.Waiting, 'Waiting')}</p>
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
            <span className={styles.headerText}>{translate?.(Localekeys.TradeCosts, 'Trade Costs')}</span>
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
                onSort={next => TC.ResourceName.set(next)}
                className={styles.resourceColumn}
              />

              <SortableHeader
                label={translate(Localekeys.BuyCostHeader)}
                tooltip={translate(Localekeys.BuyCostHeaderTooltip)}
                sortState={buyCostSorting}
                onSort={next => TC.BuyCost.set(next)}
                className={styles.buyCostColumn}
              />

              <SortableHeader
                label={translate(Localekeys.SellCostHeader)}
                tooltip={translate(Localekeys.SellCostHeaderTooltip)}
                sortState={sellCostSorting}
                onSort={next => TC.SellCost.set(next)}
                className={styles.sellCostColumn}
              />

              <SortableHeader
                label={translate(Localekeys.ProfitHeader)}
                tooltip={translate(Localekeys.ProfitHeaderTooltip)}
                sortState={profitSorting}
                onSort={next => TC.Profit.set(next)}
                className={styles.profitColumn}
              />

              <SortableHeader
                label={translate(Localekeys.ProfitMarginHeader)}
                tooltip={translate(Localekeys.ProfitMarginHeaderTooltip)}
                sortState={profitMarginSorting}
                onSort={next => TC.ProfitMargin.set(next)}
                className={styles.profitMarginColumn}
              />

              <SortableHeader
                label={translate(Localekeys.ImportAmountHeader)}
                tooltip={translate(Localekeys.ImportAmountHeaderTooltip)}
                sortState={importAmountSorting}
                onSort={next => TC.ImportAmount.set(next)}
                className={styles.importAmountColumn}
              />

              <SortableHeader
                label={translate(Localekeys.ExportAmountHeader)}
                tooltip={translate(Localekeys.ExportAmountHeaderTooltip)}
                sortState={exportAmountSorting}
                onSort={next => TC.ExportAmount.set(next)}
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

                  <div className={styles.resourceColumn}>
                    <LocalizedString id={formatWords(row.Resource)} showIdOnFail={true} />
                  </div>

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
        </div>
      </Panel>
    </Portal>
  );
};

export default TradeCostPanel;
