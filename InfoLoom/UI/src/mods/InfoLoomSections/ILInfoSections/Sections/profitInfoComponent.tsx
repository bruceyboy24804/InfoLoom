import { SelectedInfoSectionBase, Theme } from 'cs2/bindings';
import { getModule } from 'cs2/modding';
import { PanelSectionRow, FOCUS_AUTO, PanelFoldout, Tooltip } from 'cs2/ui';
import { InfoRowSCSS } from 'mods/InfoLoomSections/ILInfoSections/Modules/info-Row/info-Row.module.scss';
import { InfoSectionFoldout } from 'mods/InfoLoomSections/ILInfoSections/Modules/info-Section/info-section-foldout';
import classNames from 'classnames';
import { formatPercentage1 } from 'mods/InfoLoomSections/utils/formatText';

export const InfoRowTheme: Theme | any = getModule(
  'game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss',
  'classes'
);

interface ResourceValueItem extends SelectedInfoSectionBase {
  resourceName: string;
  amount: number;
  unitPrice: number;
  totalValue: number;
}

interface ILProfitSection extends SelectedInfoSectionBase {
  CurrentWorth: number;
  PreviousWorth: number;
  ProfitChange: number;
  Profitability: number;
  ResourceValue: number;
  LoadedGoodsValue: number;
  OwnedVehicles: number;
  LoadedVehicles: number;
  IndustrialPrices: string;
  ProfitPerDay: number;
  MaxProfitPerDay: number;
  ProfitabilityTrend: number;
  ResourceValues: ResourceValueItem[];
  TotalResourceValue: number;
}

// Helper functions without logging

const formatProfitChange = (profit: number): string => {
  if (profit > 0) return `+${profit}`;
  if (profit < 0) return `${profit}`;
  return '0';
};

const formatProfitabilityWithColor = (profitabilityValue: number): JSX.Element => {
  const percentage = ((profitabilityValue - 127) / 127) * 100;
  const formattedPercentage = `${percentage >= 0 ? '+' : ''}${percentage.toFixed(1)}%`;

  let color = 'inherit';
  if (percentage > 10)
    color = '#4CAF50'; // Green for good profit
  else if (percentage > 0)
    color = '#8BC34A'; // Light green for small profit
  else if (percentage < -10)
    color = '#F44336'; // Red for significant loss
  else if (percentage < 0) color = '#FF9800'; // Orange for small loss

  return <span style={{ color }}>{formattedPercentage}</span>;
};

let PanelOpen: boolean = false;

export const ILCompanyProfitSection = (componentList: any): any => {
  componentList['InfoLoomTwo.Systems.Sections.ILCompanyProfitSection'] = (props: ILProfitSection) => {
    // Parse the industrial prices string into an object
    const parseIndustrialPrices = (pricesString: string): Record<string, number> => {
      if (!pricesString) return {};

      const pricesData: Record<string, number> = {};
      const pairs = pricesString.split(';');

      pairs.forEach(pair => {
        const [resource, priceStr] = pair.split(':');
        if (resource && priceStr) {
          pricesData[resource] = parseFloat(priceStr);
        }
      });

      return pricesData;
    };

    const industrialPricesData = parseIndustrialPrices(props.IndustrialPrices);

    return (
      <PanelFoldout
        header={
          <div className={InfoRowTheme.infoRow}>
            <div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>Profit</div>
          </div>
        }
        initialExpanded={PanelOpen}
        expandFromContent={false}
        focusKey={FOCUS_AUTO}
        onToggleExpanded={(value: boolean) => {
          PanelOpen = value;
        }}
      >
        <PanelSectionRow
          left="Company economy"
          right={props.CurrentWorth}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
          tooltip="The company economy is the company's total worth (inventory value(resource value) + loaded goods value)"
        />

        <PanelSectionRow
          left="Last total worth"
          right={props.PreviousWorth}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
          tooltip="Last total worth is the companies total worth at the last update (inventory value(resource value) + loaded goods value)"
        />

        <PanelSectionRow
          left="Worth Change"
          right={formatProfitChange(props.ProfitChange)}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
          tooltip="Worth change is the difference between the current and previous worth (shows profit or loss since last update)"
        />

        <PanelSectionRow
          left="Profitability"
          right={
            <span>
              {props.Profitability} ({formatProfitabilityWithColor(props.Profitability)})
            </span>
          }
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
          tooltip="Profitability is a measure of how profitable the company is based on its profit change. 127 is break even or neutral, above 127 is profit, below 127 is loss."
        />
        {/* Resource Values Foldout */}
        {props.ResourceValues && props.ResourceValues.length > 0 && (
          <PanelFoldout
            header={
              <div className={InfoRowTheme.infoRow}>
                <div className={InfoRowSCSS.left}>Resource Values</div>
                <div className={InfoRowSCSS.right}>{`Total value: ${props.TotalResourceValue}`}</div>
              </div>
            }
            initialExpanded={false}
            expandFromContent={false}
            focusKey={FOCUS_AUTO}
          >
            {props.ResourceValues.map((item: ResourceValueItem, index: number) => (
              <PanelSectionRow
                key={index}
                left={`${item.resourceName}(${item.amount})`}
                right={`Price: $${item.unitPrice.toFixed(2)} | Value: $${item.totalValue.toFixed(2)}`}
                disableFocus={true}
                subRow={true}
                className={InfoRowTheme.infoRow}
              />
            ))}
          </PanelFoldout>
        )}

        <PanelSectionRow
          left="Owned Vehicles"
          right={props.OwnedVehicles}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
          tooltip="Owned vehicles are the total number of vehicles owned by the company."
        />

        <PanelSectionRow
          left="Loaded Vehicles"
          right={props.LoadedVehicles}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
          tooltip="Loaded vehicles are the total number of vehicles currently loaded with resources."
        />
      </PanelFoldout>
    );
  };
  return componentList;
};
