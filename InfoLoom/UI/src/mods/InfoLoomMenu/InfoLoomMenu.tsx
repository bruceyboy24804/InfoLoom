import React, { useCallback, useMemo } from 'react';
import { useValue } from 'cs2/api';
import { useLocalization } from 'cs2/l10n';
import { Button, FloatingButton, Tooltip, Icon } from 'cs2/ui';
import { trigger } from 'cs2/api';
import icon from 'images/Statistics.svg';
import styles from './InfoLoomMenu.module.scss';
import Demographics from 'mods/InfoLoomSections/DemographicsSection/Demographics';
import Workforce from 'mods/InfoLoomSections/WorkforceSection/Workforce';
import Workplaces from 'mods/InfoLoomSections/WorkplacesSection/Workplaces';
import Demand from 'mods/InfoLoomSections/DemandSection/Demand';
import TradeCost from 'mods/InfoLoomSections/TradeCostSection/TradeCost';
import * as bindings from 'mods/bindings';
import { CommercialMenuButton } from 'mods/InfoLoomMenu/CommercialMenu/CommercialMenu';
import IndustrialMenuButton from './IndustrialMenu/IndustrialMenu';
import Residential from 'mods/InfoLoomSections/ResidentialSection/ResidentialDemandUI/residential';
import ResidentialMenuButton from './ResidentialMenu/ResidentialMenu';
import SankeyMenuButton from './SankeyMenu/SankeyMenu';
import { Entity } from 'cs2/utils';
import mod from "mod.json"
import { Localekeys } from 'mods/locale';
interface SectionItem {
  component: JSX.Element;
  isOpen: boolean;
  toggle: (state: boolean) => void;
  displayName: string | null; // Add display name property
  src?: string;
}

type SectionsType = Record<string, SectionItem>;
enum InfoLoomState {
  Open,
  Closed,
}
function InfoLoomButton(): JSX.Element {
  const { translate } = useLocalization();

  // Move all hook calls to the component level instead of inside functions
  const infoLoomMenuOpen = useValue(bindings.InfoLoomMenuOpen);
  const demographicsOpen = useValue(bindings.DemographicsOpen);
  const workforceOpen = useValue(bindings.WorkforceOpen);
  const workplacesOpen = useValue(bindings.WorkplacesOpen);
  const residentialMenuOpen = useValue(bindings.ResidentialMenuOpen);
  const buildingDemandOpen = useValue(bindings.BuildingDemandOpen);
  const industrialMenuOpen = useValue(bindings.IndustrialMenuOpen);
  const tradeCostsOpen = useValue(bindings.TradeCostsOpen);
  const commercialMenuOpen = useValue(bindings.CommercialMenuOpen);
  const showButton = useValue(bindings.ShowEffectsButton);
  const effectsOpen = useValue(bindings.EffectsOpen);
  const sankeyMenuOpen = useValue(bindings.SankeyMenuOpen);
  // Define sections without translations - move translations to render time
  const sections = useMemo<SectionsType>(
    () => ({
      Demographics: {
        component: <Demographics />,
        isOpen: demographicsOpen,
        toggle: bindings.SetDemographicsOpen,
        displayName: null, // Will be set at render time
      },
      Workforce: {
        component: <Workforce />,
        isOpen: workforceOpen,
        toggle: bindings.SetWorkforceOpen,
        displayName: null, // Will be set at render time
      },
      Workplaces: {
        component: <Workplaces />,
        isOpen: workplacesOpen,
        toggle: bindings.SetWorkplacesOpen,
        displayName: null, // Will be set at render time
      },
      'Residential Menu': {
        component: <ResidentialMenuButton />,
        isOpen: residentialMenuOpen,
        toggle: bindings.SetResidentialMenuOpen,
        displayName: null, // Will be set at render time
        src: 'Media/Glyphs/FilledArrowRight.svg',
      },
      'Commercial Menu': {
        component: <CommercialMenuButton />,
        isOpen: commercialMenuOpen,
        toggle: bindings.SetCommercialMenuOpen,
        displayName: null, // Will be set at render time
        src: 'Media/Glyphs/FilledArrowRight.svg',
      },
      'Industrial Menu': {
        component: <IndustrialMenuButton />,
        isOpen: industrialMenuOpen,
        toggle: bindings.SetIndustrialMenuOpen,
        displayName: null, // Will be set at render time
        src: 'Media/Glyphs/FilledArrowRight.svg',
      },
      'Sankey Menu': {
        component: <SankeyMenuButton />,
        isOpen: sankeyMenuOpen,
        toggle: bindings.SetSankeyMenuOpen,
        displayName: null,
        src: 'Media/Glyphs/FilledArrowRight.svg',
      },
      Demand: {
        component: <Demand />,
        isOpen: buildingDemandOpen,
        toggle: bindings.SetBuildingDemandOpen,
        displayName: null, // Will be set at render time
      },
      'Trade Cost': {
        component: <TradeCost />,
        isOpen: tradeCostsOpen,
        toggle: bindings.SetTradeCostsOpen,
        displayName: null, // Will be set at render time
      },
    }),
      [
      demographicsOpen,
      workforceOpen,
      workplacesOpen,
      residentialMenuOpen,
      buildingDemandOpen,
      industrialMenuOpen,
      tradeCostsOpen,
      commercialMenuOpen,
      effectsOpen,
      showButton,
      sankeyMenuOpen,
    ]
  );

  const toggleSection = useCallback(
    (name: string) => {
      const section = sections[name];
      if (section) {
        section.toggle(!section.isOpen);
      }
    },
    [sections]
  );

  const handleInfoLoomToggle = useCallback(() => {
    const isClosing = infoLoomMenuOpen;

    // First set the main menu state
    bindings.SetInfoLoomMenuOpen(!infoLoomMenuOpen);

    // If we're closing the main menu, only close the menu buttons
    // but do NOT close any child sections/components
    if (isClosing) {
    const menuSectionsToClose = ['Residential Menu', 'Industrial Menu', 'Commercial Menu', 'Sankey Menu'];

      // Only close the menu sections UIs, but explicitly preserve all child component states
      menuSectionsToClose.forEach(sectionName => {
        const section = sections[sectionName];
        if (section && section.isOpen) {
          // Instead of directly closing via toggle, we'll manually handle state closing
          // This ensures that child components don't close when their parent menu closes

          if (sectionName === 'Residential Menu') {
            // Close only the residential menu UI, not its child components
            bindings.SetResidentialMenuOpen(false);
          } else if (sectionName === 'Industrial Menu') {
            bindings.SetIndustrialMenuOpen(false);
          } else if (sectionName === 'District Menu') {
            bindings.SetDistrictMenuOpen(false);
          } else if (sectionName === 'Commercial Menu') {
            bindings.SetCommercialMenuOpen(false);
          } else if (sectionName === 'Sankey Menu') {
            bindings.SetSankeyMenuOpen(false);
          }
        }
      });
    }
  }, [infoLoomMenuOpen, sections]);

  return (
    <div>
      <Tooltip tooltip={translate(Localekeys.ModName, "Info Loom")}>
        <Button
          variant="floating"
          src={icon}
          selected={infoLoomMenuOpen}
          onSelect={() => handleInfoLoomToggle()}
        ></Button>
      </Tooltip>

      {infoLoomMenuOpen && (
        <div draggable className={styles.panel}>
          <header className={styles.header}>
            <div>{translate(Localekeys.ModName, "Info Loom")}</div>
          </header>
          <div className={styles.buttonRow}>
            {Object.keys(sections).map(name => {
              // Get translated display name at render time
              let displayName: string | null;
              switch (name) {
                case 'Demographics':
                  displayName = translate(Localekeys.Demographics, 'Demographics');
                  break;
                case 'Workforce':
                  displayName = translate(Localekeys.Workforce, 'Workforce');
                  break;
                case 'Workplaces':
                  displayName = translate(Localekeys.Workplaces, 'Workplaces');
                  break;
                case 'Residential Menu':
                  displayName = translate(Localekeys.MenuResidential, 'Residential Menu');
                  break;
                case 'Demand':
                  displayName = translate(Localekeys.Demand, 'Demand');
                  break;
                case 'Industrial Menu':
                  displayName = translate(Localekeys.MenuIndustrial, 'Industrial Menu');
                  break;
                case 'Trade Cost':
                  displayName = translate(Localekeys.TradeCosts, 'Trade Costs');
                  break;
                case 'Commercial Menu':
                  displayName = translate(Localekeys.MenuCommercial, 'Commercial Menu');
                  break;
                case 'Sankey Menu':
                  displayName = translate(Localekeys.MenuSankey, 'Sankey Menu');
                  break;
                case 'Effects':
                  displayName = 'Effects';
                  break;
                default:
                  displayName = name;
              }

              return (
                <Button
                  key={name}
                  variant="flat"
                  className={`${styles.InfoLoomButton} ${sections[name].isOpen ? styles.buttonSelected : ''}`}
                  onClick={() => toggleSection(name)}
                >
                  <div className={styles.buttonContent}>
                    <span>{displayName}</span>
                    {sections[name].src !== undefined && (
                      <Icon tinted src={sections[name].src as string} className={styles.buttonIcon} />
                    )}
                  </div>
                </Button>
              );
            })}
          </div>
        </div>
      )}

      {/* Render non-menu sections based on their own open state */}
      {Object.entries(sections).map(([name, section]) => {
        // Skip menu type sections from this regular rendering
        if (['Residential Menu', 'Industrial Menu', 'District Menu', 'Commercial Menu', 'Sankey Menu'].includes(name)) {
          return null;
        }

        return (
          section.isOpen && (
            <div key={name}>
              {React.cloneElement(section.component, {
                onClose: (e?: React.SyntheticEvent) => {
                  // Stop event propagation to prevent closing cascades
                  if (e && typeof e.stopPropagation === 'function') {
                    e.stopPropagation();
                  }
                  toggleSection(name);
                },
              })}
            </div>
          )
        );
      })}

      {/* Always render these components regardless of the menu state */}
      {/* This ensures the panels remain visible even when the main InfoLoom menu is closed */}
      <ResidentialMenuButton />
      <IndustrialMenuButton />
      <CommercialMenuButton />
      <SankeyMenuButton />
    </div>
  );
}

export default InfoLoomButton;
