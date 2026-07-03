import { useValue } from 'cs2/api';
import * as bindings from '../../bindings';
import React, { useCallback } from 'react';
import { Button, Panel } from 'cs2/ui';
import styles from '../IndustrialMenu/IndustrialMenu.module.scss';
import IndustrialCompany from 'mods/InfoLoomSections/IndustrialSection/IndustrialCompanyUI/IndustrialCompany';
import { useLocalization } from 'cs2/l10n';
import { Localekeys } from 'mods/locale';
import StorageCompanyComponent from '../../InfoLoomSections/IndustrialSection/StorageCompanyUI/StorageCompanyComponent';
import IndustrialDemandUI from "../../InfoLoomSections/IndustrialSection/IndustrialDemandUI/IndustrialDemandUI";

interface SectionConfig {
  component: JSX.Element;
  openState: () => boolean;
  toggle: (state: boolean) => void;
  displayName: string | null;
}

export function IndustrialMenuButton(): JSX.Element {
  const { translate } = useLocalization();

  const sections: Record<string, SectionConfig> = {
    Demand: {
      component: <IndustrialDemandUI />,
      openState: () => useValue(bindings.IndustrialDemandOpen),
      toggle: bindings.SetIndustrialDemandOpen,
      displayName: null, // Will be set at render time
    },
    Companies: {
      component: <IndustrialCompany />,
      openState: () => useValue(bindings.IndustrialCompanyDebugOpen),
      toggle: bindings.SetIndustrialCompanyDebugOpen,
      displayName: null, // Will be set at render time
    },
    Storages: {
      component: <StorageCompanyComponent />,
      openState: () => useValue(bindings.storagePanelVisibleBinding.binding),
      toggle: bindings.SetStoragePanelVisible,
      displayName: null, // Will be set at render time
    },
  };

  const industrialMenuOpen = useValue(bindings.IndustrialMenuOpen);
  const sectionStates = Object.fromEntries(
    Object.entries(sections).map(([name, config]) => [name, config.openState()])
  );

  const toggleSection = useCallback((name: string) => sections[name]?.toggle(!sectionStates[name]), [sectionStates]);

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
                  displayName = translate(Localekeys.Demand, 'Demand');
                  break;
                case 'Products':
                  displayName = translate(Localekeys.Products, 'Products');
                  break;
                case 'Storage':
                  displayName = translate(Localekeys.Storage, 'Storage');
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
                  className={`${styles.InfoLoomButton} ${sectionStates[name] ? styles.buttonSelected : ''}`}
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
      {Object.entries(sections).map(
        ([name, { component }]) =>
          sectionStates[name] && (
            <div key={name}>
              {React.cloneElement(component, {
                onClose: (e?: React.SyntheticEvent) => {
                  // Stop event propagation to prevent cascading close actions
                  if (e && typeof e.stopPropagation === 'function') {
                    e.stopPropagation();
                  }
                  toggleSection(name);
                },
              })}
            </div>
          )
      )}
    </div>
  );
}

export default IndustrialMenuButton;
