import { Icon } from "cs2/ui";
import styles from "./RadioButton.module.scss";
import {GroupingStrategy} from "../../domain/GroupingStrategy";

interface RadioButtonProps {
    isChecked: GroupingStrategy;
    onValueChange: (newVal: GroupingStrategy) => void;
}

export const RadioButton = ({ isChecked, onValueChange }: RadioButtonProps) => {
    const dotSrc = "Media/Glyphs/Circle.svg";
    return (
        <div className={styles.radioContainer} onClick={() => onValueChange(isChecked)}>
            {isChecked && <Icon
                src={dotSrc}
                className={styles.dotIcon}
                tinted={true} />
            }
        </div>
    );
}