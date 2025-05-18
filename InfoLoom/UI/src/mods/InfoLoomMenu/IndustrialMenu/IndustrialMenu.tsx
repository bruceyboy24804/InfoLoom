
import {useValue} from "cs2/api";
import * as bindings from "../../bindings";
import React, {useCallback} from "react";
import {Button, Panel} from "cs2/ui";
import styles from "../IndustrialMenu/IndustrialMenu.module.scss";
import Industrial from "mods/InfoLoomSections/IndustrialSection/IndustrialDemandUI/IndustrialDemand";
import IndustrialProducts from "mods/InfoLoomSections/IndustrialSection/IndustrialProductsUI/IndustrialProducts";
import IndustryCompany from "mods/InfoLoomSections/IndustrialSection/IndustrialCompanyUI/IndustrialCompany";

interface SectionConfig {
    component: JSX.Element;
    openState: () => boolean;
    toggle: (state: boolean) => void;
}
const sections: Record<string, SectionConfig> = {
    "Demand": {
        component: <Industrial />,
        openState: () => useValue(bindings.IndustrialDemandOpen),
        toggle: bindings.SetIndustrialDemandOpen
    },
    "Products": {
        component: <IndustrialProducts />,
        openState: () => useValue(bindings.IndustrialProductsOpen),
        toggle: bindings.SetIndustrialProductsOpen
    },
    Companies: {
        component: <IndustryCompany />,
        openState: () => useValue(bindings.IndustrialCompanyDebugOpen),
        toggle: bindings.SetIndustrialCompanyDebugOpen
    }

};
export function IndustrialMenuButton(): JSX.Element {
    const industrialMenuOpen = useValue(bindings.IndustrialMenuOpen);
    const sectionStates = Object.fromEntries(
        Object.entries(sections).map(([name, config]) => [name, config.openState()])
    );

    const toggleSection = useCallback(
        (name: string) => sections[name]?.toggle(!sectionStates[name]),
        [sectionStates]
    );
    return (
        <div>
            {industrialMenuOpen && (
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

export default IndustrialMenuButton;