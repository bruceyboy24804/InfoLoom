
import {useValue} from "cs2/api";
import * as bindings from "../../bindings";
import React, {useCallback} from "react";
import {Button, Panel} from "cs2/ui";
import styles from "../ResidentialMenu/ResidentialMenu.module.scss";
import Residential from "mods/InfoLoomSections/ResidentialSection/ResidentialDemandUI/residential";
import ResidentialHousehold from '../../InfoLoomSections/ResidentialSection/ResidentialHouseholdUI/ResidentialHousehold';

interface SectionConfig {
  component: JSX.Element;
  openState: () => boolean;
  toggle: (state: boolean) => void;
}
const sections: Record<string, SectionConfig> = {
  "Demand": {
    component: < Residential/>,
    openState: () => useValue(bindings.ResidentialDemandOpen),
    toggle: bindings.SetResidentialDemandOpen
  },
  /*"Households": {
    component: <ResidentialHousehold/>,
    openState: () => useValue(bindings.ResidentialDataDebugOpen),
    toggle: bindings.SetResidentialDataDebugOpen
  },*/
};
export function ResidentialMenuButton(): JSX.Element {
  const residentialMenuOpen = useValue(bindings.ResidentialMenuOpen);
  const sectionStates = Object.fromEntries(
    Object.entries(sections).map(([name, config]) => [name, config.openState()])
  );

  const toggleSection = useCallback(
    (name: string) => sections[name]?.toggle(!sectionStates[name]),
    [sectionStates]
  );
  return (
    <div>
      {residentialMenuOpen && (
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

export default ResidentialMenuButton;