import { bindValue, useValue, trigger           } from "cs2/api";
import { useLocalization                        } from "cs2/l10n";
import { Dropdown, DropdownItem, DropdownToggle } from "cs2/ui";
import { Entity                                 } from "cs2/utils";

import   styles                                   from "./districtSelector.module.scss";
import   mod                                      from "mod.json";
import { ModuleResolver                         } from "../ModuleResolver/moduleResolver";

// Define district info passed from C#.
type DistrictInfo =
    {
        entity: Entity;
        name: string;
    }

// Define district bindings.
const bindingDistrictInfos    = bindValue<DistrictInfo[]>(mod.id, "districtInfos");
const bindingSelectedDistrict = bindValue<Entity        >(mod.id, "selectedDistrict", { index: 0, version: 0 });

// Custom infomode item for selecting a district.
export const DistrictSelector = () =>
{
    // Translations.
    
    

    // Get district infos and selected district.
    const districtInfos: DistrictInfo[]  = useValue(bindingDistrictInfos);
    const selectedDistrictEntity: Entity = useValue(bindingSelectedDistrict)

    // Create a dropdown item for each district info and get the name of the selected district.
    let selectedDistrictName: string = "";
    const districtDropdownItems: JSX.Element[] = districtInfos.map
    (
        (districtInfo: DistrictInfo) =>
        {
            // Get district entity and name from the district info.
            const districtEntity: Entity = districtInfo.entity;
            const districtName:   string = districtInfo.name;

            // Check if this district info is for the selected district.
            const selected: boolean =
                districtEntity.index   === selectedDistrictEntity.index &&
                districtEntity.version === selectedDistrictEntity.version;

            // Get the name of the selected district.
            if (selected)
            {
                selectedDistrictName = districtName;
            }

            // Return a dropdown item.
            return (
                <DropdownItem
                    theme={ModuleResolver.instance.DropdownClasses}
                    value={districtName}
                    closeOnSelect={true}
                    selected={selected}
                    className={selected ? styles.selectedDistrictDropdownItem : ""}
                    onChange={() => trigger(mod.id, "selectedDistrictChanged", districtEntity) }
                >
                    {districtName}
                </DropdownItem>
            );
        }
    );

    // If there are no districts in the city besides the entire city entry, then do not display the district selector.
    // It is important to let the logic above execute before performing this check.
    // The logic above actually uses the values from the bindings.
    // Using the binding values prevents an unhandled React error which crashes the game
    // when the last district is deleted while the infoview is active.
    if (districtInfos.length <= 1)
    {
        return (<></>);
    }

    // Entire row is a single dropdown of districts.
    return (
        <ModuleResolver.instance.Tooltip
            direction="right"
            tooltip={<ModuleResolver.instance.FormattedParagraphs children={"Set whether to show for entire city or a specific district."} />}
            theme={ModuleResolver.instance.TooltipClasses}
            children=
            {
                <div className={styles.districtDropdownRow}>
                    <Dropdown
                        theme={ModuleResolver.instance.DropdownClasses}
                        content={districtDropdownItems}>
                        <DropdownToggle>
                            {selectedDistrictName}
                        </DropdownToggle>
                    </Dropdown>
                </div>
            }
        />
    );
}