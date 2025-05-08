import React, {FC} from "react";
import styles from "../DistrictSection/Districts.module.scss";

export function formatWords(text: string, forceUpper: boolean = false): string {
    text = text.replace(/([a-z])([A-Z])/g, '$1 $2');
    text = text.replace(/([a-zA-Z])(\d)/g, '$1 $2');
    text = text.replace(/(\d)([a-zA-Z])/g, '$1 $2');
    if (forceUpper) {
        // Capitalize first letter and letters after spaces
        text = text.replace(/(^[a-z])|(\ [a-z])/g, match => match.toUpperCase());
    }
    return text;
}

export const formatNumber = (number: number): string => {
    return new Intl.NumberFormat().format(number);
};
export const formatpercentage = (number: number): string => {
    return new Intl.NumberFormat(undefined, { style: 'percent', minimumFractionDigits: 0 }).format(number);
}