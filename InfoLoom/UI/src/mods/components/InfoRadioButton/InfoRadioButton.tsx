// InfoRadioButton.tsx
import {CSSProperties} from "react";
import {RadioButton} from "../RadioButton/RadioButton";
import styles from "./InfoRadioButton.module.scss";
import {GroupingStrategy} from "mods/domain/GroupingStrategy";
import {DemoGroupingStrategy} from "../../bindings";

interface InfoRadioButtonProps {
    label?: string | JSX.Element;
    count?: number;
    isChecked: GroupingStrategy;
    onToggle: (newVal: GroupingStrategy) => void;
    className?: string;
    style?: CSSProperties;
}

export const InfoRadioButton = ({count, isChecked, onToggle, className, style, label}: InfoRadioButtonProps) => {
    return (
        <div
            className={styles.subPanel + " " + className}
            style={{ ...style, opacity: isChecked ? 1 : 0.5 }}
            onClick={() => onToggle(isChecked)}
        >
            <div className={styles.iconLabelSection}>
                <span className={styles.label}>{label}</span>
            </div>
            <div className={styles.labelCheckboxSection}>
                {count !== undefined && count !== null && (
                    <span className={styles.label}>{"Count" + ": " + count}</span>
                )}
                <RadioButton
                    isChecked={isChecked}
                
                    onValueChange={onToggle}
                />
            </div>
        </div>
    )
};