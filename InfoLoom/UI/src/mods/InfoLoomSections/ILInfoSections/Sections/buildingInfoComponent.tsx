import { SelectedInfoSectionBase, Theme, ChartData } from "cs2/bindings";
import { getModule } from "cs2/modding";
import { PanelSection, PanelSectionRow, PanelFoldout, FOCUS_AUTO, Button, Tooltip } from "cs2/ui";
import { VanillaComponentResolver } from "mods/VanillaComponents/VanillaComponents";
import {Entity} from "cs2/utils";
import {Name, NameType} from "cs2/bindings";
import { InfoRowSCSS } from "mods/InfoLoomSections/ILInfoSections/Modules/info-Row/info-Row.module.scss";
import { InfoSectionFoldout } from "mods/InfoLoomSections/ILInfoSections/Modules/info-Section/info-section-foldout";
import { useLocalization } from "cs2/l10n";
import classNames from "classnames";
import { formatWords } from "../../utils/formatText";
import {trigger} from "cs2/api";
import styles from "./buildingInfoComponent.module.scss";

export const InfoRowTheme: Theme | any = getModule(
    "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss",
    "classes"
);

interface TradeCostData extends SelectedInfoSectionBase {
  Resource: string;
  BuyCost: number;
  SellCost: number;
}   
interface ILBuildingSection extends SelectedInfoSectionBase {
    TradePartnerName: Name;
    TradePartnerEntity: Entity; 
    ResourceAmount: number;
    TransportCost: number;
    MaxProfitPerDay: number;
    ProfitPerDay: number;
    PotentialProfitGain: number;
    EmployeeCount: number;
    MaxEmployees: number;
    EducationDataEmployees: ChartData;
    EducationDataWorkplaces: ChartData;
    OvereductedEmployees: number;
    CommuterEmployees: number;
    TradeCosts: TradeCostData[];
}

const getDisplayName = (
    name: Name,
    translate: (id: string, fallback?: string | null) => string | null
): string => {
    if (!name) return '';
    if (typeof name === 'string') return name;
    if ('name' in name) return name.name;
    if ('nameId' in name) {
        const translated = translate(name.nameId);
        return translated || name.nameId;
    }
    return String(name);
};

// Safe wrapper for formatWords to handle undefined/null values
const safeFormatWords = (text: string | undefined | null): string => {
    if (!text || typeof text !== 'string') return '';
    return formatWords(text);
};

// Education level labels and colors (based on your screenshot)
const educationLevels = [
    { label: "Uneducated", color: "rgba(128, 128, 128, 1)" },
    { label: "Poorly Educated", color: "rgba(176, 152, 104, 1)" },
    { label: "Educated", color: "rgba(54, 138, 46, 1)" },
    { label: "Well Educated", color: "rgba(185, 129, 192, 1)" },
    { label: "Highly Educated", color: "rgba(87, 150, 209, 1)" }
];

// Helper function to create education tooltip content (vanilla style)
const createEducationTooltip = (
    employeesData: ChartData,
    workplacesData: ChartData,
    maxEmployees: number,
    overeducatedEmployees: number,
    commuterEmployees: number,
    translate: (id: string, fallback?: string | null) => string | null
): JSX.Element => {
    // Open positions is at index 5 in the values array
    const openPositions = employeesData.values[5] || 0;

    return (
      <div className={styles.content}>
        <div className={styles.tooltipContent}>
            <div className={styles.chartBar}>
                {workplacesData.values.slice(0, 5).map((value, index) => {
                    if (value === 0) return null;
                    const percentage = maxEmployees > 0 ? (value / maxEmployees) * 100 : 0;
                    
                    return (
                        <div
                            key={index}
                            className={styles.barSegment}
                            style={{
                                width: `${percentage}%`,
                                backgroundColor: educationLevels[index].color,
                            }}
                        />
                    );
                })}
                
                {/* Open positions segment */}
                {openPositions > 0 && (
                    <div
                        className={styles.barSegment}
                        style={{
                            width: `${(openPositions / maxEmployees) * 100}%`,
                            backgroundColor: "#4f5153",
                        }}
                    />
                )}
            </div>
            
            {/* Manual legend items */}
            <div className={styles.legends}>
                {/* Uneducated */}
                {workplacesData.values[0] > 0 && (
                    <div className={styles.colorLegend}>
                        <div className={styles.symbol} style={{ backgroundColor: "rgba(128, 128, 128, 1)" }} />
                        <div className={styles.label} co-font-fit-mode="shrink">Uneducated</div>
                        <div className={styles.value}>{employeesData.values[0] || 0} / {workplacesData.values[0]}</div>
                    </div>
                )}
                
                {/* Poorly Educated */}
                {workplacesData.values[1] > 0 && (
                    <div className={styles.colorLegend}>
                        <div className={styles.symbol} style={{ backgroundColor: "rgba(176, 152, 104, 1)" }} />
                        <div className={styles.label} co-font-fit-mode="shrink">Poorly Educated</div>
                        <div className={styles.value}>{employeesData.values[1] || 0} / {workplacesData.values[1]}</div>
                    </div>
                )}
                
                {/* Educated */}
                {workplacesData.values[2] > 0 && (
                    <div className={styles.colorLegend}>
                        <div className={styles.symbol} style={{ backgroundColor: "rgba(54, 138, 46, 1)" }} />
                        <div className={styles.label} co-font-fit-mode="shrink">Educated</div>
                        <div className={styles.value}>{employeesData.values[2] || 0} / {workplacesData.values[2]}</div>
                    </div>
                )}
                
                {/* Well Educated */}
                {workplacesData.values[3] > 0 && (
                    <div className={styles.colorLegend}>
                        <div className={styles.symbol} style={{ backgroundColor: "rgba(185, 129, 192, 1)" }} />
                        <div className={styles.label} co-font-fit-mode="shrink">Well Educated</div>
                        <div className={styles.value}>{employeesData.values[3] || 0} / {workplacesData.values[3]}</div>
                    </div>
                )}
                
                {/* Highly Educated */}
                {workplacesData.values[4] > 0 && (
                    <div className={styles.colorLegend}>
                        <div className={styles.symbol} style={{ backgroundColor: "rgba(87, 150, 209, 1)" }} />
                        <div className={styles.label} co-font-fit-mode="shrink">Highly Educated</div>
                        <div className={styles.value}>{employeesData.values[4] || 0} / {workplacesData.values[4]}</div>
                    </div>
                )}
                
                {/* Open Positions - ALWAYS show this */}
                <div className={styles.colorLegend}>
                    <div className={styles.symbol} style={{ backgroundColor: "#4f5153" }} />
                    <div className={styles.label} co-font-fit-mode="shrink">Open Positions</div>
                    <div className={styles.value}>{employeesData.values[5]}</div>
                </div>
                <div className={styles.colorLegend}>
                    <div className={styles.label} co-font-fit-mode="shrink">Overeducated Employees</div>
                    <div className={styles.value}>{overeducatedEmployees}</div>
                </div>
                <div className={styles.colorLegend}>
                    <div className={styles.label} co-font-fit-mode="shrink">Commuter</div>
                    <div className={styles.value}>{commuterEmployees}</div>
                </div>
            </div>
            
            {/* Description text */}
            <p className={styles.p_CKd2} cohinline="cohinline">
                Overeducated workers are citizens with higher education working in lower-skilled jobs. If there jobs available at the level of the oveeducated citizen then the citizen will move to that job.
            </p>
            <p className={styles.p_CKd2} cohinline="cohinline">
                Commuter workers are citizens who travel from outside the city to work in your city.
            </p>
        </div>
      </div>
    );
};

var PanelOpen: boolean = false;
const focusEntity = (e: Entity) => {
    trigger('camera', 'focusEntity', e);
};

export const ILBuildingInfoSection = (componentList: any): any => {
componentList["InfoLoomTwo.Systems.Sections.ILBuildingSection"] = (props: ILBuildingSection & TradeCostData) => {        
  const { translate } = useLocalization();
        return (
            <InfoSectionFoldout
                header={
                    <div className={InfoRowTheme.infoRow}>
                        <div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>Info Loom</div>
                    </div>
                }
                initialExpanded={PanelOpen}
                expandFromContent={false}
                focusKey={FOCUS_AUTO}
                onToggleExpanded={(value: boolean) => { PanelOpen = value }}
            >
                <PanelSectionRow
                    left={"Buying from"}
                    right={<Button
                        className={styles.button}
                        onSelect={() => focusEntity(props.TradePartnerEntity)}
                    >
                        <img className={styles.icon_hE2} src="Media/Glyphs/ViewInfo.svg"/>
                        <div className={styles.ellipsis_C0N}>{getDisplayName(props.TradePartnerName, translate)}</div>
                    </Button>}
                    uppercase={false}
                    disableFocus={true}
                    className={InfoRowTheme.infoRow}
                />
                <PanelSectionRow
                    left={"Transport Cost"}
                    right={`${(props.TransportCost || 0).toFixed(2)}`}
                    uppercase={false}
                    disableFocus={true}
                    className={InfoRowTheme.infoRow}
                />
                
                {/* Display all trade costs from the array */}
                {props.TradeCosts && Array.isArray(props.TradeCosts) && props.TradeCosts.length > 0 && (
                    <>
                        {props.TradeCosts.map((tradeCost, index) => {
                            // Additional safety checks for each trade cost item
                            if (!tradeCost || typeof tradeCost !== 'object') return null;
                            
                            return (
                                <PanelSectionRow
                                    key={index}
                                    left={`${safeFormatWords(tradeCost.Resource)} Cost (${props.ResourceAmount.toFixed(2)}t)`}
                                    right={`Buy ${(tradeCost.BuyCost || 0).toFixed(2)} / Sell ${(tradeCost.SellCost || 0).toFixed(2)}`}
                                    uppercase={false}
                                    disableFocus={true}
                                    className={InfoRowTheme.infoRow}
                                />
                            );
                        })}
                    </>
                )}
                
                {/* Employees row with education tooltip */}
                {props.EducationDataEmployees && props.EducationDataWorkplaces ? (
                    <Tooltip
                        tooltip={createEducationTooltip(
                            props.EducationDataEmployees,
                            props.EducationDataWorkplaces,
                            props.MaxEmployees,
                            props.OvereductedEmployees,
                            props.CommuterEmployees,
                            translate
                        )}
                        direction="right"
                        alignment="start"
                    >
                        <PanelSectionRow
                            left={"Employees"}
                            right={`${props.EmployeeCount || 0} / ${props.MaxEmployees || 0}`}
                            uppercase={false}
                            disableFocus={true}
                            className={InfoRowTheme.infoRow}
                        />
                    </Tooltip>
                ) : (
                    <PanelSectionRow
                        left={"Employees"}
                        right={`${props.EmployeeCount || 0} / ${props.MaxEmployees || 0}`}
                        uppercase={false}
                        disableFocus={true}
                        className={InfoRowTheme.infoRow}
                    />
                )}
            </InfoSectionFoldout>
        );
    };
    return componentList as any;
}