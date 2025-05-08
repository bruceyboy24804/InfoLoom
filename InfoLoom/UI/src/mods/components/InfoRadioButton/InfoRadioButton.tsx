// InfoRadioButton.tsx
import {CSSProperties} from "react";
import {RadioButton} from "../RadioButton/RadioButton";
import styles from "./InfoRadioButton.module.scss";
import {GroupingStrategy} from "mods/domain/GroupingStrategy";
import {DemoGroupingStrategy} from "../../bindings";

interface InfoRadioButtonProps {
    label?: string | JSX.Element;
    count?: number;
    groupingStrategy: GroupingStrategy;
    isChecked: boolean;
    onToggle: (newVal: GroupingStrategy) => void;
    className?: string;
    style?: CSSProperties;
}

export const InfoRadioButton = ({count, groupingStrategy, isChecked, onToggle, className, style, label}: InfoRadioButtonProps) => {
    return (
        <div
            className={styles.subPanel + " " + className}
            style={{ ...style, opacity: groupingStrategy ? 1 : 0.5 }}
            onClick={() => onToggle(groupingStrategy)}
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
                    groupingStrategy={groupingStrategy}
                    onValueChange={onToggle}
                />
            </div>
        </div>
    )
};