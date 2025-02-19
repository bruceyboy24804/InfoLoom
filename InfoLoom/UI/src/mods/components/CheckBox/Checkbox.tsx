import { Icon } from "cs2/ui";
import styles from "./Checkbox.module.scss";

interface CheckboxProps {
    isChecked: boolean;
    onValueToggle: (newVal: boolean) => void;
}
 
export const Checkbox = ({ isChecked, onValueToggle }: CheckboxProps) => {
    //const checkmarkScr = "coui://uil/Standard/Checkmark.svg";
    const checkmarkScr = "Media/Glyphs/Checkmark.svg"
    return (
        <div className={styles.checkboxContainer} onClick={() => onValueToggle(!isChecked)}>
            {isChecked && <Icon
                src={checkmarkScr}
                className={styles.checkmarkIcon}
                tinted={true} />
            }
        </div>
    );
}