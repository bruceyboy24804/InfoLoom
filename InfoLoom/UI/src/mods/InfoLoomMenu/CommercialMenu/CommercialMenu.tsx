
import {useValue} from "cs2/api";
import * as bindings from "../../bindings";
import Commercial from "../../InfoLoomSections/CommercialSecction/CommercialDemandUI/CommercialDemand";
import CommercialProducts from "../../InfoLoomSections/CommercialSecction/CommercialProductsUI/CommercialProducts";
import CommercialCompanyDebugDataPanel
    from "../../InfoLoomSections/CommercialSecction/CommercialDebugDataUI/CommercialCompanyDebugData";
import React, {useCallback} from "react";
import {Button, Panel} from "cs2/ui";
import icon from "../../../images/infoloom.svg";
import styles from "../CommercialMenu/CommercialMenu.module.scss";


interface SectionConfig {
    component: JSX.Element;
    openState: () => boolean;
    toggle: (state: boolean) => void;
}
const sections: Record<string, SectionConfig> = {
    "Demand": {
        component: <Commercial />,
        openState: () => useValue(bindings.CommercialDemandOpen),
        toggle: bindings.SetCommercialDemandOpen
    },
    "Products": {
        component: <CommercialProducts />,
        openState: () => useValue(bindings.CommercialProductsOpen),
        toggle: bindings.SetCommercialProductsOpen
    },
    "Companies": {
        component: <CommercialCompanyDebugDataPanel />,
        openState: () => useValue(bindings.CommercialCompanyDebugOpen),
        toggle: bindings.SetCommercialCompanyDebugOpen
    },
};
export function CommercialMenuButton(): JSX.Element {
    const commercialMenuOpen = useValue(bindings.CommercialMenuOpen);
    const sectionStates = Object.fromEntries(
        Object.entries(sections).map(([name, config]) => [name, config.openState()])
    );

    const toggleSection = useCallback(
        (name: string) => sections[name]?.toggle(!sectionStates[name]),
        [sectionStates]
    );
    return (
        <div>
            {commercialMenuOpen && (
                <div className={styles.panel}>
                    <div className={styles.buttonRow}>
                        {Object.keys(sections).map(name => (
                            <Button
                                key={name}
                                variant="flat"
                                aria-label={name}
                                aria-expanded={sectionStates[name]}
                                className={`${styles.InfoLoomButton} ${
                                    sectionStates[name] ? styles.buttonSelected : ""
                                }`}
                                onClick={() => toggleSection(name)}
                            >
                                {name}
                            </Button>
                        ))}
                    </div>
                </div>
            )}

            {Object.entries(sections).map(([name, { component }]) =>
                    sectionStates[name] && (
                        <div key={name}>
                            {React.cloneElement(component, {
                                onClose: () => toggleSection(name)
                            })}
                        </div>
                    )
            )}
        </div>
    );
}

export default CommercialMenuButton;