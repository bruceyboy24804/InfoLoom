import { trigger        } from "cs2/api";

import   styles           from "infomodeCheckBox.module.scss";
import   mod              from "mod.json";
import { ModuleResolver } from "../ModuleResolver/moduleResolver";

// Props for InfomodeCheckBox.
export interface InfomodeCheckBoxProps
{
    onClickEventName: string;
    isChecked: boolean;
    label: string | null;
}

// Check box inside an infomode.
export const InfomodeCheckBox = ({ onClickEventName, isChecked, label }: InfomodeCheckBoxProps) =>
{
    // Handle check box click.
    function onCheckBoxClick()
    {
        trigger("audio", "playSound", ModuleResolver.instance.UISound.selectItem, 1);
        trigger(mod.id, onClickEventName);
    }

    // Function to join classes.
    function joinClasses(...classes: any) { return classes.join(" "); }

    // A check box with a label.
    // A click anywhere on the enclosing container is considered a click on the check box.
    return (
        <div className={styles.buildingUseInfomodeCheckBoxContainer}
            onClick={() => onCheckBoxClick()}>
            <div className={joinClasses(ModuleResolver.instance.CheckboxClasses.toggle,
                                        ModuleResolver.instance.InfomodeItemClasses.checkbox,
                                        styles.buildingUseInfomodeCheckBox,
                                        (isChecked ? "checked" : "unchecked"))}>
                <div className={joinClasses(ModuleResolver.instance.CheckboxClasses.checkmark,
                                            (isChecked ? "checked" : ""))}></div>
            </div>
            <div className={styles.buildingUseInfomodeCheckBoxLabel}>
                {label}
            </div>
        </div>
    );
}