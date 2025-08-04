import { SelectedInfoSectionBase, Theme, ChartData } from "cs2/bindings";
import { getModule } from "cs2/modding";
import { PanelSection, PanelSectionRow, PanelFoldout, FOCUS_AUTO, Button, Tooltip, InfoRow } from "cs2/ui";
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
interface OvereducatedLevelData extends SelectedInfoSectionBase {
    EducationLevel: number;
    Count: number;
}
interface OvereducatedEducationLevelData {
    EducationLevel: number;
    Count: number;
}
interface OvereducatedWorkplaceLevelData {
    WorkplaceLevel: number;
    EducationLevels: OvereducatedEducationLevelData[];
}
export interface CommuterLevelData extends SelectedInfoSectionBase {
    EducationLevel: number;
    Count: number;
}
interface CommuterEducationLevelData extends SelectedInfoSectionBase {
    EducationLevel: number;
    Count: number;
}
interface CommuterWorkplaceLevelData extends SelectedInfoSectionBase {
    WorkplaceLevel: number;
    EducationLevels: CommuterEducationLevelData[];
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
    OvereductedByEducationLevel: OvereducatedLevelData[];
    OvereductedByWorkplaceAndEducationLevel: OvereducatedWorkplaceLevelData[];
    CommuterEmployees: number;
    CommuterByEducationLevel: CommuterLevelData[];
    CommuterByWorkplaceAndEducationLevel: CommuterWorkplaceLevelData[];

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


var PanelOpen: boolean = false;
const focusEntity = (e: Entity) => {
    trigger('camera', 'focusEntity', e);
};

export const ILBuildingInfoSection = (componentList: any): any => {
componentList["InfoLoomTwo.Systems.Sections.ILBuildingSection"] = (props: ILBuildingSection & TradeCostData & CommuterLevelData & OvereducatedLevelData) => {
  const { translate } = useLocalization();
        return (
            <InfoSectionFoldout
                header={
                    <div className={InfoRowTheme.infoRow}>
                        <div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>Info Loom Company</div>
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
                {props.OvereductedEmployees ? (
                            <PanelFoldout
                                header={
                                    <div className={InfoRowTheme.infoRow} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", width: "100%" }}>
                                        <div className={InfoRowSCSS.left}>Overeducated Employees</div>
                                        <div className={InfoRowSCSS.right} style={{ marginLeft: 8 }}>{props.OvereductedEmployees}</div>
                                    </div>
                                }
                                tooltip="The Workplace Level is the education level of the workplace where the overeducated employee works. Education Level is the level of education of the overeducated employee. The number to the right of the education level is the number of overeducated employees with that education level working at that workplace level."
                                initialExpanded={false}
                                expandFromContent={false}
                                focusKey={FOCUS_AUTO}
                            >
                                {/* Column titles row */}
                                <div className={InfoRowTheme.infoRow} style={{ display: "flex", justifyContent: "space-between", fontWeight: "bold" }}>
                                    <div className={InfoRowSCSS.left}>Workplace Level</div>
                                    <div className={InfoRowSCSS.right}>Education Level</div>
                                </div>
                                {props.OvereductedByWorkplaceAndEducationLevel && props.OvereductedByWorkplaceAndEducationLevel.length > 0 ? (
                                    [...props.OvereductedByWorkplaceAndEducationLevel]
                                        .sort((a, b) => a.WorkplaceLevel - b.WorkplaceLevel)
                                        .map((workplace, idx) => (
                                            <PanelSectionRow
                                                className={styles.panelSectionRow}
                                                key={idx}
                                                left={
                                                    <span style={{
                                                        display: "flex",
                                                        alignItems: "center",
                                                        width: "100%",
                                                    }}>
                                                        <span className={styles.symbol} style={{background: educationLevels[workplace.WorkplaceLevel]?.color}}/>
                                                        {educationLevels[workplace.WorkplaceLevel]
                                                            ? educationLevels[workplace.WorkplaceLevel].label
                                                            : `Workplace Level ${workplace.WorkplaceLevel}`}
                                                    </span>
                                                }
                                                right={
                                                    <div style={{
                                                        display: "flex",
                                                        flexDirection: "column",
                                                        width: "100%",
                                                    }}>
                                                        {workplace.EducationLevels && workplace.EducationLevels.length > 0 ? (
                                                            [...workplace.EducationLevels]
                                                                .sort((a, b) => a.EducationLevel - b.EducationLevel)
                                                                .map((ed, i) => (
                                                                    <span
                                                                        key={i}
                                                                        style={{
                                                                            display: "flex",
                                                                            alignItems: "center",
                                                                            marginBottom: 2,
                                                                            width: "100%"
                                                                        }}
                                                                    >
                                                                        <span
                                                                            className={styles.symbol}
                                                                            style={{
                                                                                background: educationLevels[ed.EducationLevel]?.color,
                                                                                marginRight: 8
                                                                            }}
                                                                        />
                                                                        <span className={InfoRowSCSS.left} style={{ flex: 1 }}>
                                                                            {(educationLevels[ed.EducationLevel]?.label || `Level ${ed.EducationLevel}`) + ":"}
                                                                        </span>
                                                                        <span className={InfoRowSCSS.right} style={{ minWidth: 32 }}>
                                                                            {ed.Count}
                                                                        </span>
                                                                    </span>
                                                                ))
                                                        ) : (
                                                            <span>No data</span>
                                                        )}
                                                    </div>
                                                }
                                            />
                                        ))
                                ) : (
                                    <PanelSectionRow
                                        left="No data"
                                    />
                                )}
                            </PanelFoldout>
                        ) : null}
                    <PanelFoldout
                        
                        header={
                            <div className={InfoRowTheme.infoRow} style={{ display: "flex", justifyContent: "space-between", alignItems: "center", width: "100%" }}>
                                <div className={InfoRowSCSS.left}>Commuter Employees</div>
                                <div className={InfoRowSCSS.right} style={{ marginLeft: 8 }}>{props.CommuterEmployees}</div>
                            </div>
                        }
                        tooltip="The Workplace Level is the education level of the workplace where the commuter works. Education Level is the level of education of the commuter. The number to the right of the education level is the number of commuters with that education level working at that workplace level."
                        initialExpanded={false}
                        expandFromContent={false}
                        focusKey={FOCUS_AUTO}
                    >
                        {/* Column titles row */}
                        <div className={InfoRowTheme.infoRow} style={{ display: "flex", justifyContent: "space-between", fontWeight: "bold" }}>
                            <div className={InfoRowSCSS.left}>Workplace Level</div>
                            <div className={InfoRowSCSS.right}>Education Level</div>
                        </div>
                        {props.CommuterByWorkplaceAndEducationLevel && props.CommuterByWorkplaceAndEducationLevel.length > 0 ? (
                            [...props.CommuterByWorkplaceAndEducationLevel]
                                .sort((a, b) => a.WorkplaceLevel - b.WorkplaceLevel)
                                .map((workplace, idx) => (
                                    <PanelSectionRow
                                        key={idx}
                                        className={styles.panelSectionRow}
                                        left={
                                            <span style={{
                                                display: "flex",
                                                alignItems: "center",
                                                width: "100%",
                                            }}>
                                                <span className={styles.symbol} style={{background: educationLevels[workplace.WorkplaceLevel]?.color}}/>
                                                {educationLevels[workplace.WorkplaceLevel]
                                                    ? educationLevels[workplace.WorkplaceLevel].label
                                                    : `Workplace Level ${workplace.WorkplaceLevel}`}
                                            </span>
                                        }
                                        right={
                                            <div style={{
                                                display: "flex",
                                                flexDirection: "column",
                                                width: "100%",
                                            }}>
                                                {workplace.EducationLevels && workplace.EducationLevels.length > 0 ? (
                                                    [...workplace.EducationLevels]
                                                        .sort((a, b) => a.EducationLevel - b.EducationLevel)
                                                        .map((ed, i) => (
                                                            <span
                                                                key={i}
                                                                style={{
                                                                    display: "flex",
                                                                    alignItems: "center",
                                                                    marginBottom: 2,
                                                                    width: "100%"
                                                                }}
                                                            >
                                                                <span
                                                                    className={styles.symbol}
                                                                    style={{
                                                                        background: educationLevels[ed.EducationLevel]?.color,
                                                                        marginRight: 8
                                                                    }}
                                                                />
                                                                <span className={InfoRowSCSS.left} style={{ flex: 1 }}>
                                                                    {(educationLevels[ed.EducationLevel]?.label || `Level ${ed.EducationLevel}`) + ":"}
                                                                </span>
                                                                <span className={InfoRowSCSS.right} style={{ minWidth: 32 }}>
                                                                    {ed.Count}
                                                                </span>
                                                            </span>
                                                        ))
                                                ) : (
                                                    <span>No data</span>
                                                )}
                                            </div>
                                        }
                                    />
                                ))
                        ) : (
                            <PanelSectionRow
                                left="No data"
                            />
                        )}
                    </PanelFoldout>
                
            </InfoSectionFoldout>
        );
    };
    return componentList as any;
}