import React, { FC } from "react";
import { Panel, Tooltip, Icon, Portal } from "cs2/ui";
import styles from "./TradeCost.module.scss";
import { useValue } from "cs2/api";
import { useLocalization } from 'cs2/l10n';
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
} from "../../bindings";
import { ResourceTradeCost } from "mods/domain/tradeCostData";
import {
  ResourceNameEnum,
  BuyCostEnum,
  SellCostEnum,
  ProfitEnum,
  ProfitMarginEnum,
  ImportAmountEnum,
  ExportAmountEnum,
} from "mods/domain/TradeCostEnums";

const DataDivider: FC = () => <div className={styles.dataDivider} />;

function formatAmountInTons(amount: number): string {
  const tons = amount / 1000;
  
  if (tons === 0) return "0 t";
  if (tons < 0.1) return (tons * 1000).toFixed(0) + " kg";
  if (tons < 1) return (tons * 1000).toFixed(0) + " kg";
  if (tons < 10) return tons.toFixed(2) + " t";
  if (tons < 100) return tons.toFixed(1) + " t";
  return Math.round(tons).toLocaleString() + " t";
}

function isImmaterialResource(data: ResourceTradeCost): boolean {
  return data.BuyCost === 0 && data.SellCost === 0 && (data.ImportAmount > 0 || data.ExportAmount > 0);
}

function formatImmaterialAmount(amount: number): string {
  if (amount === 0) return "0";
  if (amount < 1000) return amount.toFixed(0);
  if (amount < 1000000) return (amount / 1000).toFixed(1) + "K";
  return (amount / 1000000).toFixed(1) + "M";
}

function getProfitClass(value: number) {
  if (value < 0) return styles.negative;
  if (value > 0) return styles.positive;
  return styles.neutral;
}

function calculateProfit(data: ResourceTradeCost) {
  return data.SellCost - data.BuyCost;
}

function calculateProfitMargin(data: ResourceTradeCost) {
  return data.BuyCost !== 0 ? ((data.SellCost - data.BuyCost) / data.BuyCost) * 100 : 0;
}

interface SortableHeaderProps {
  label: string;
  tooltip?: string;
  sortState: number;
  onSort: (direction: "asc" | "desc" | "off") => void;
  className?: string;
}

const SortableHeader: FC<SortableHeaderProps> = ({ label, tooltip, sortState, onSort, className }) => {
  const handleClick = (e: React.MouseEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (sortState === 0) onSort("asc");
    else if (sortState === 1) onSort("desc");
    else onSort("off");
  };

  return (
    <Tooltip tooltip={tooltip}>
      <div
        className={`${styles.sortableHeader} ${styles.headerCell} ${className || ""}`}
        onClick={handleClick}
        style={{ cursor: "pointer" }}
      >
        <span>{label}</span>
        <div className={styles.sortArrows}>
          {sortState === 1 && (
            <Icon src="coui://uil/Standard/ArrowSortHighDown.svg" className={styles.sortIcon} />
          )}
          {sortState === 2 && (
            <Icon src="coui://uil/Standard/ArrowSortLowDown.svg" className={styles.sortIcon} />
          )}
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
    ResourceName: (direction: "asc" | "desc" | "off") => {
      if (direction === "asc") SetResourceNameSorting(ResourceNameEnum.Ascending);
      else if (direction === "desc") SetResourceNameSorting(ResourceNameEnum.Descending);
      else SetResourceNameSorting(ResourceNameEnum.Off);
    },
    BuyCost: (direction: "asc" | "desc" | "off") => {
      if (direction === "asc") SetBuyCostSorting(BuyCostEnum.Ascending);
      else if (direction === "desc") SetBuyCostSorting(BuyCostEnum.Descending);
      else SetBuyCostSorting(BuyCostEnum.Off);
    },
    SellCost: (direction: "asc" | "desc" | "off") => {
      if (direction === "asc") SetSellCostSorting(SellCostEnum.Ascending);
      else if (direction === "desc") SetSellCostSorting(SellCostEnum.Descending);
      else SetSellCostSorting(SellCostEnum.Off);
    },
    Profit: (direction: "asc" | "desc" | "off") => {
      if (direction === "asc") SetProfitSorting(ProfitEnum.Ascending);
      else if (direction === "desc") SetProfitSorting(ProfitEnum.Descending);
      else SetProfitSorting(ProfitEnum.Off);
    },
    ProfitMargin: (direction: "asc" | "desc" | "off") => {
      if (direction === "asc") SetProfitMarginSorting(ProfitMarginEnum.Ascending);
      else if (direction === "desc") SetProfitMarginSorting(ProfitMarginEnum.Descending);
      else SetProfitMarginSorting(ProfitMarginEnum.Off);
    },
    ImportAmount: (direction: "asc" | "desc" | "off") => {
      if (direction === "asc") SetImportAmountSorting(ImportAmountEnum.Ascending);
      else if (direction === "desc") SetImportAmountSorting(ImportAmountEnum.Descending);
      else SetImportAmountSorting(ImportAmountEnum.Off);
    },
    ExportAmount: (direction: "asc" | "desc" | "off") => {
      if (direction === "asc") SetExportAmountSorting(ExportAmountEnum.Ascending);
      else if (direction === "desc") SetExportAmountSorting(ExportAmountEnum.Descending);
      else SetExportAmountSorting(ExportAmountEnum.Off);
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
                {translate?.("InfoLoomTwo.TradeCostsPanel[Title]", "Trade Costs") || "Trade Costs"}
              </span>
            </div>
          }
        >
          <p className={styles.loadingText}>
            {translate?.("InfoLoomTwo.TradeCostsPanel[Loading]", "Loading Trade Costs...") || "Loading Trade Costs..."}
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
              {translate?.("InfoLoomTwo.TradeCostsPanel[Title]", "Trade Costs") || "Trade Costs"}
            </span>
          </div>
        }
      >
        <div className={styles.panelContent}>
          <div className={styles.tableHeader}>
            <div className={styles.headerRow}>
              <div className={`${styles.headerCell} ${styles.iconColumn}`}>
                <b>Icon</b>
              </div>

              <SortableHeader
                label="Resource"
                tooltip="Name of the resource"
                sortState={resourceNameSorting}
                onSort={onSort.ResourceName}
                className={styles.resourceColumn}
              />

              <SortableHeader
                label="Buy Cost"
                tooltip="Cost to buy from outside connections"
                sortState={buyCostSorting}
                onSort={onSort.BuyCost}
                className={styles.buyCostColumn}
              />

              <SortableHeader
                label="Sell Cost"
                tooltip="Sell price to outside connections"
                sortState={sellCostSorting}
                onSort={onSort.SellCost}
                className={styles.sellCostColumn}
              />

              <SortableHeader
                label="Profit"
                tooltip="Sell Cost - Buy Cost"
                sortState={profitSorting}
                onSort={onSort.Profit}
                className={styles.profitColumn}
              />

              <SortableHeader
                label="Profit Margin"
                tooltip="Profit as a percentage of Buy Cost"
                sortState={profitMarginSorting}
                onSort={onSort.ProfitMargin}
                className={styles.profitMarginColumn}
              />

              <SortableHeader
                label="Import Amount"
                tooltip="Amount imported from outside connections"
                sortState={importAmountSorting}
                onSort={onSort.ImportAmount}
                className={styles.importAmountColumn}
              />

              <SortableHeader
                label="Export Amount"
                tooltip="Amount exported to outside connections"
                sortState={exportAmountSorting}
                onSort={onSort.ExportAmount}
                className={styles.exportAmountColumn}
              />
            </div>
          </div>

          <DataDivider />

          <div className={styles.tableBody}>
            {tradeCosts.map((row) => {
              const isImmaterial = isImmaterialResource(row);
              
              return (
                <div className={styles.row} key={row.Resource}>
                  <div className={styles.iconColumn}>
                    <Icon src={row.ResourceIcon} className={styles.resourceIcon} />
                  </div>

                  <div className={styles.resourceColumn}>{row.Resource}</div>

                  <div className={styles.buyCostColumn}>
                    {isImmaterial ? "N/A" : row.BuyCost.toFixed(2)}
                  </div>

                  <div className={styles.sellCostColumn}>
                    {isImmaterial ? "N/A" : row.SellCost.toFixed(2)}
                  </div>

                  <div className={`${styles.profitColumn} ${isImmaterial ? '' : getProfitClass(calculateProfit(row))}`}>
                    {isImmaterial ? "N/A" : calculateProfit(row).toFixed(2)}
                  </div>

                  <div className={`${styles.profitMarginColumn} ${isImmaterial ? '' : getProfitClass(calculateProfitMargin(row))}`}>
                    {isImmaterial ? "N/A" : calculateProfitMargin(row).toFixed(2) + "%"}
                  </div>
                  
                  <div className={styles.importAmountColumn}>
                    {isImmaterial ? formatImmaterialAmount(row.ImportAmount) : formatAmountInTons(row.ImportAmount)}
                  </div>
                  
                  <div className={styles.exportAmountColumn}>
                    {isImmaterial ? formatImmaterialAmount(row.ExportAmount) : formatAmountInTons(row.ExportAmount)}
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
