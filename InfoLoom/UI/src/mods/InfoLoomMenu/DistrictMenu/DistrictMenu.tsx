import {useValue} from "cs2/api";
import * as bindings from "../../bindings";
import React, {useCallback} from "react";
import {Button, Panel} from "cs2/ui";
import styles from "../DistrictMenu/DistrictMenu.module.scss";
import Districts from "mods/InfoLoomSections/DistrictSection/Districts";


interface SectionConfig {
    component: JSX.Element;
    openState: () => boolean;
    toggle: (state: boolean) => void;
}
const sections: Record<string, SectionConfig> = {
    "Districts": {
        component: <Districts />,
        openState: () => useValue(bindings.DistrictDataOpen),
        toggle: bindings.SetDistrictDataOpen
    },

};
export function DistrictMenuButton(): JSX.Element {
    const districtMenuOpen = useValue(bindings.DistrictMenuOpen);
    const sectionStates = Object.fromEntries(
        Object.entries(sections).map(([name, config]) => [name, config.openState()])
    );

    const toggleSection = useCallback(
        (name: string) => sections[name]?.toggle(!sectionStates[name]),
        [sectionStates]
    );
    return (
        <div>
            {districtMenuOpen && (
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

            {/* Always render sections based on their own open state, regardless of menu state */}
            {Object.entries(sections).map(([name, { component }]) =>
                sectionStates[name] && (
                    <div key={name}>
                        {React.cloneElement(component, {
                            onClose: (e?: React.SyntheticEvent) => {
                                // Stop event propagation to prevent closing cascades
                                if (e && typeof e.stopPropagation === 'function') {
                                    e.stopPropagation();
                                }
                                toggleSection(name);
                            }
                        })}
                    </div>
                )
            )}
        </div>
    );
}

export default DistrictMenuButton;