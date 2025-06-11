import {useValue} from "cs2/api";
import * as bindings from "../../bindings";
import React, {useCallback, useMemo} from "react";
import {Button, Panel} from "cs2/ui";
import styles from "../ResidentialMenu/ResidentialMenu.module.scss";
import Residential from "mods/InfoLoomSections/ResidentialSection/ResidentialDemandUI/residential";

// Define types for section configuration
interface SectionItem {
  component: JSX.Element;
  isOpen: boolean;
  toggle: (state: boolean) => void;
}

type SectionsType = Record<string, SectionItem>;

export const ResidentialMenuButton = (): JSX.Element => {
  const residentialMenuOpen = useValue(bindings.ResidentialMenuOpen);
  const residentialDemandOpen = useValue(bindings.ResidentialDemandOpen);

  // Define sections with memoization to prevent recreation on each render
  const sections = useMemo<SectionsType>(() => ({
    "Demand": {
      component: <Residential/>,
      isOpen: residentialDemandOpen,
      toggle: bindings.SetResidentialDemandOpen
    },
  }), [residentialDemandOpen]); // Add residentialDataDebugOpen to dependencies when uncommented

  const toggleSection = useCallback(
    (name: string) => {
      const section = sections[name];
      if (section) {
        section.toggle(!section.isOpen);
      }
    },
    [sections]
  );

  // Separate rendering of the menu and sections
  return (
    <>
      {/* Menu buttons - only render when menu is open */}
      {residentialMenuOpen && (
        <div className={styles.panel}>
          <div className={styles.buttonRow}>
            {Object.keys(sections).map(name => (
              <Button
                key={name}
                variant="flat"
                aria-label={name}
                aria-expanded={sections[name].isOpen}
                className={`${styles.InfoLoomButton} ${
                  sections[name].isOpen ? styles.buttonSelected : ""
                }`}
                onClick={() => toggleSection(name)}
              >
                {name}
              </Button>
            ))}
          </div>
        </div>
      )}

      {/* Always render sections based on their own open state, completely independent of menu state */}
      {Object.entries(sections).map(([name, section]) =>
        section.isOpen && (
          <div key={name}>
            {React.cloneElement(section.component, {
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
    </>
  );
}

export default ResidentialMenuButton;

