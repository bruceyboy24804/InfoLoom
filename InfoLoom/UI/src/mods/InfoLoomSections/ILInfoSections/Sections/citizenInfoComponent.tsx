import { SelectedInfoSectionBase, Theme } from "cs2/bindings";
import { getModule } from "cs2/modding";
import { PanelSection, PanelSectionRow, PanelFoldout, FOCUS_AUTO } from "cs2/ui";
import { VanillaComponentResolver } from "mods/VanillaComponents/VanillaComponents";
import {Entity} from "cs2/utils";
import {Name, NameType} from "cs2/bindings";
import { InfoRowSCSS } from "mods/InfoLoomSections/ILInfoSections/Modules/info-Row/info-Row.module.scss";
import { InfoSectionFoldout } from "mods/InfoLoomSections/ILInfoSections/Modules/info-Section/info-section-foldout";
//import { InfoSectionSCSS } from "../../game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.module.scss";

import classNames from "classnames";
import { formatWords } from "../../utils/formatText";


export const InfoRowTheme: Theme | any = getModule(
    "game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss",
    "classes"
);   


export interface ILCitizenSection extends SelectedInfoSectionBase {
    Household: string;
    HouseholdMoney: number;
    HouseholdSpendableMoney: number;
    HouseholdNeedResources: string;
    HouseholdNeedResourcesAmount: number;
    Workplace: Name;
    Shift: string;
    WellBeing: string;
    Health: number;
    BirthDay: number;
    Purpose: string;
    ShoppingAmount: number;
    Resource: string;
    Rent: number;
    NumberOfCitizensInHousehold: number;
   
}


var PanelOpen: boolean = false;
export const ILCitizenInfoSection = (componentList: any): any => {
    componentList["InfoLoomTwo.Systems.Sections.ILCitizenSection"] = (props: ILCitizenSection) => {
        
        return (
            <InfoSectionFoldout
				header={
					<div className={InfoRowTheme.infoRow}>
						<div  className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>Info Loom</div>
					</div>
				}
                initialExpanded={PanelOpen}
				expandFromContent={false}
				focusKey={FOCUS_AUTO}
				onToggleExpanded={(value: boolean) => { PanelOpen = value }}
			>

                <PanelFoldout header="Household Info" initialExpanded={true}>
                        <PanelSectionRow
                            left={"Number of Citizens in Household"}
                            right={props.NumberOfCitizensInHousehold}
                            disableFocus={true}
                            subRow={false}
                            className={InfoRowTheme.infoRow}
                        />
                        <PanelSectionRow
                            left={"Household Money / Spendable Money"}
                            right={`${props.HouseholdMoney} / ${props.HouseholdSpendableMoney}`}
                            disableFocus={true}
                            subRow={false}
                            className={InfoRowTheme.infoRow}
                        />
                        <PanelSectionRow
                            left={"Household Need"}
                            right={`${props.HouseholdNeedResources} (${props.HouseholdNeedResourcesAmount})`}
                            disableFocus={true}
                            subRow={false}
                            className={InfoRowTheme.infoRow}
                        />
                    </PanelFoldout>
                    {["Day", "Evening", "Night"].includes(props.Shift) && (
                        <PanelFoldout header="Workplace Info" initialExpanded={true}>
                            <PanelSectionRow
                                left={"Shift"}
                                right={props.Shift}
                                disableFocus={true}
                                subRow={false}
                                className={InfoRowTheme.infoRow}
                            />
                        </PanelFoldout>
                    )}   
                    <PanelFoldout header="Personal Info" initialExpanded={true}>
                        <PanelSectionRow
                            left={"Wellbeing"}
                            right={props.WellBeing}
                            disableFocus={true}
                            subRow={false}
                            className={InfoRowTheme.infoRow}
                        />
                        <PanelSectionRow
                            left={"Health"}
                            right={props.Health}
                            disableFocus={true}
                            subRow={false}
                            className={InfoRowTheme.infoRow}
                        />
                        <PanelSectionRow
                            left={"Birth Day"}
                            right={props.BirthDay}
                            disableFocus={true}
                            subRow={false}
                            className={InfoRowTheme.infoRow}
                        />
                    </PanelFoldout>
                    <PanelFoldout header="Activity Info" initialExpanded={true}>
                        <PanelSectionRow
                            left={"Purpose"}
                            right={formatWords(props.Purpose)}
                            disableFocus={true}
                            subRow={false}
                            className={InfoRowTheme.infoRow}
                        />
                        {["Shopping"].includes(props.Purpose) && (
                            <PanelSectionRow
                                left={"Shopping"}
                                right={`${props.Resource} (${props.ShoppingAmount})`}
                                disableFocus={true}
                                subRow={false}
                                className={InfoRowTheme.infoRow}
                            />
                        )}
                    </PanelFoldout>
                    <PanelFoldout header="Housing Info" initialExpanded={true}>
                        <PanelSectionRow
                            left={"Rent"}
                            right={props.Rent}
                            disableFocus={true}
                            subRow={false}
                            className={InfoRowTheme.infoRow}
                        />
                    </PanelFoldout>
                
            </InfoSectionFoldout>
        );
    };

    return componentList as any;
};