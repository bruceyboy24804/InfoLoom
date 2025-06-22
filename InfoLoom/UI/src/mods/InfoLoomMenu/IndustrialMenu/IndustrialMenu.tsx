import {useValue} from "cs2/api";
import * as bindings from "../../bindings";
import React, {useCallback} from "react";
import {Button, Panel} from "cs2/ui";
import styles from "../IndustrialMenu/IndustrialMenu.module.scss";
import Industrial from "mods/InfoLoomSections/IndustrialSection/IndustrialDemandUI/IndustrialDemand";
import IndustrialProducts from "mods/InfoLoomSections/IndustrialSection/IndustrialProductsUI/IndustrialProducts";
import IndustryCompany from "mods/InfoLoomSections/IndustrialSection/IndustrialCompanyUI/IndustrialCompany";
import {useLocalization} from "cs2/l10n";

interface SectionConfig {
  component: JSX.Element;
  openState: () => boolean;
  toggle: (state: boolean) => void;
  displayName: string | null;
}


export function IndustrialMenuButton(): JSX.Element {
  const {translate} = useLocalization();

const sections: Record<string, SectionConfig> = {
  "Demand": {
    component: <Industrial />,
    openState: () => useValue(bindings.IndustrialDemandOpen),
    toggle: bindings.SetIndustrialDemandOpen,
    displayName: null, // Will be set at render time
  },
  "Products": {
    component: <IndustrialProducts />,
    openState: () => useValue(bindings.IndustrialProductsOpen),
    toggle: bindings.SetIndustrialProductsOpen,
    displayName: null, // Will be set at render time
  },
  "Companies": {
    component: <IndustryCompany />,
    openState: () => useValue(bindings.IndustrialCompanyDebugOpen),
    toggle: bindings.SetIndustrialCompanyDebugOpen,
    displayName: null, // Will be set at render time
  }
};

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
        {/* Only show the menu panel when industrialMenuOpen is true */}
        {industrialMenuOpen && (
            <div className={styles.panel}>
              <div className={styles.buttonRow}>
                {Object.keys(sections).map(name => {
                  // Get translated display name at render time
                  let displayName: string | null;
                  switch (name) {
                    case 'Demand':
                      displayName = translate("InfoLoomTwo.IndustrialMenu[Button1]", "Demand");
                      break;
                    case 'Products':
                      displayName = translate("InfoLoomTwo.IndustrialMenu[Button2]", "Products");
                      break;
                    case 'Companies':
                      displayName = translate("InfoLoomTwo.IndustrialMenu[Button3]", "Companies");
                      break;
                    default:
                      displayName = name;
                  }

                  return (
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
                      {displayName}
                    </Button>
                  );
                })}
              </div>
            </div>
        )}

        {/* Always render sections based on their own open state, regardless of menu state */}
        {Object.entries(sections).map(([name, { component }]) =>
                sectionStates[name] && (
                    <div key={name}>
                      {React.cloneElement(component, {
                        onClose: (e?: React.SyntheticEvent) => {
                          // Stop event propagation to prevent cascading close actions
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

export default IndustrialMenuButton;