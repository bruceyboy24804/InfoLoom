import {useValue} from "cs2/api";
import * as bindings from "../../bindings";
import Commercial from "../../InfoLoomSections/CommercialSecction/CommercialDemandUI/CommercialDemand";
import CommercialProducts from "../../InfoLoomSections/CommercialSecction/CommercialProductsUI/CommercialProducts";
import CommercialCompanyDebugDataPanel
  from "../../InfoLoomSections/CommercialSecction/CommercialDebugDataUI/CommercialCompanyDebugData";
import React, {useCallback} from "react";
import {Button, Panel} from "cs2/ui";
import styles from "../CommercialMenu/CommercialMenu.module.scss";
import {useLocalization} from "cs2/l10n";


interface SectionConfig {
  component: JSX.Element;
  openState: () => boolean;
  toggle: (state: boolean) => void;
  displayName: string | null;
}

export function CommercialMenuButton(): JSX.Element {
  const {translate} = useLocalization();

const sections: Record<string, SectionConfig> = {
  "Demand": {
    component: <Commercial />,
    openState: () => useValue(bindings.CommercialDemandOpen),
    toggle: bindings.SetCommercialDemandOpen,
    displayName: null, // Will be set at render time
  },
  "Products": {
    component: <CommercialProducts />,
    openState: () => useValue(bindings.CommercialProductsOpen),
    toggle: bindings.SetCommercialProductsOpen,
    displayName: null, // Will be set at render time
  },
  "Companies": {
    component: <CommercialCompanyDebugDataPanel />,
    openState: () => useValue(bindings.CommercialCompanyDebugOpen),
    toggle: bindings.SetCommercialCompanyDebugOpen,
    displayName: null, // Will be set at render time
  },
};

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
                {Object.keys(sections).map(name => {
                  // Get translated display name at render time
                  let displayName: string | null;
                  switch (name) {
                    case 'Demand':
                      displayName = translate("InfoLoomTwo.CommercialMenu[Button1]", "Demand");
                      break;
                    case 'Products':
                      displayName = translate("InfoLoomTwo.CommercialMenu[Button2]", "Products");
                      break;
                    case 'Companies':
                      displayName = translate("InfoLoomTwo.CommercialMenu[Button3]", "Companies");
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

export default CommercialMenuButton;