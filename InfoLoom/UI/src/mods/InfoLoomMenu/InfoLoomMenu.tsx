import React, { useCallback, useMemo } from "react";
import { useValue } from "cs2/api";
import { Button, FloatingButton, Tooltip, Icon } from "cs2/ui";
import icon from "images/infoloom.svg";
import styles from "./InfoLoomMenu.module.scss";
import Demographics from "mods/InfoLoomSections/DemographicsSection/Demographics";
import Workforce from "mods/InfoLoomSections/WorkforceSection/Workforce";
import Workplaces from "mods/InfoLoomSections/WorkplacesSection/Workplaces";
import Demand from "mods/InfoLoomSections/DemandSection/Demand";
import TradeCost from "mods/InfoLoomSections/TradeCostSection/TradeCost";
import * as bindings from "mods/bindings";
import { CommercialMenuButton } from "mods/InfoLoomMenu/CommercialMenu/CommercialMenu";
import IndustrialMenuButton from "./IndustrialMenu/IndustrialMenu";
import DistrictMenuButton from "./DistrictMenu/DistrictMenu";
import Residential from "mods/InfoLoomSections/ResidentialSection/ResidentialDemandUI/residential";
import ResidentialMenuButton from './ResidentialMenu/ResidentialMenu';

interface SectionItem {
  component: JSX.Element;
  isOpen: boolean;
  toggle: (state: boolean) => void;
  src?: string;
}

type SectionsType = Record<string, SectionItem>;

function InfoLoomButton(): JSX.Element {
  // Move all hook calls to the component level instead of inside functions
  const infoLoomMenuOpen = useValue(bindings.InfoLoomMenuOpen);
  const demographicsOpen = useValue(bindings.DemographicsOpen);
  const workforceOpen = useValue(bindings.WorkforceOpen);
  const workplacesOpen = useValue(bindings.WorkplacesOpen);
  const residentialMenuOpen = useValue(bindings.ResidentialMenuOpen);
  const buildingDemandOpen = useValue(bindings.BuildingDemandOpen);
  const industrialMenuOpen = useValue(bindings.IndustrialMenuOpen);
  const tradeCostsOpen = useValue(bindings.TradeCostsOpen);
  const districtMenuOpen = useValue(bindings.DistrictMenuOpen);
  const commercialMenuOpen = useValue(bindings.CommercialMenuOpen);
  const residentialDemandOpen = useValue(bindings.ResidentialDemandOpen);

  // Define sections with memoization to prevent recreation on each render
  const sections = useMemo<SectionsType>(() => ({
    Demographics: {
      component: <Demographics />,
      isOpen: demographicsOpen,
      toggle: bindings.SetDemographicsOpen
    },
    Workforce: {
      component: <Workforce />,
      isOpen: workforceOpen,
      toggle: bindings.SetWorkforceOpen
    },
    Workplaces: {
      component: <Workplaces />,
      isOpen: workplacesOpen,
      toggle: bindings.SetWorkplacesOpen
    },
    "Residential Menu": {
      component: <ResidentialMenuButton />,
      isOpen: residentialMenuOpen,
      toggle: bindings.SetResidentialMenuOpen,
      src: "Media/Glyphs/FilledArrowRight.svg"
    },
    Demand: {
      component: <Demand />,
      isOpen: buildingDemandOpen,
      toggle: bindings.SetBuildingDemandOpen
    },
    "Industrial Menu": {
      component: <IndustrialMenuButton/>,
      isOpen: industrialMenuOpen,
      toggle: bindings.SetIndustrialMenuOpen,
      src: "Media/Glyphs/FilledArrowRight.svg"
    },
    "Trade Cost": {
      component: <TradeCost />,
      isOpen: tradeCostsOpen,
      toggle: bindings.SetTradeCostsOpen
    },
    "District Menu": {
      component: <DistrictMenuButton/>,
      isOpen: districtMenuOpen,
      toggle: bindings.SetDistrictMenuOpen,
      src: "Media/Glyphs/FilledArrowRight.svg"
    },
    "Commercial Menu" : {
      component: <CommercialMenuButton />,
      isOpen: commercialMenuOpen,
      toggle: bindings.SetCommercialMenuOpen,
      src: "Media/Glyphs/FilledArrowRight.svg"
    }
  }), [demographicsOpen, workforceOpen, workplacesOpen, residentialMenuOpen,
       buildingDemandOpen, industrialMenuOpen, tradeCostsOpen, districtMenuOpen, commercialMenuOpen]);

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
      const menuSectionsToClose = [
        "Residential Menu",
        "Industrial Menu",
        "District Menu",
        "Commercial Menu"
      ];

      // Only close the menu sections UIs, but explicitly preserve all child component states
      menuSectionsToClose.forEach(sectionName => {
        const section = sections[sectionName];
        if (section && section.isOpen) {
          // Instead of directly closing via toggle, we'll manually handle state closing
          // This ensures that child components don't close when their parent menu closes

          if (sectionName === "Residential Menu") {
            // Close only the residential menu UI, not its child components
            bindings.SetResidentialMenuOpen(false);
          } else if (sectionName === "Industrial Menu") {
            bindings.SetIndustrialMenuOpen(false);
          } else if (sectionName === "District Menu") {
            bindings.SetDistrictMenuOpen(false);
          } else if (sectionName === "Commercial Menu") {
            bindings.SetCommercialMenuOpen(false);
          }
        }
      });
    }
  }, [infoLoomMenuOpen, sections]);

  return (
    <div>
      <Tooltip tooltip="Info Loom">
        <FloatingButton
          onClick={handleInfoLoomToggle}
          src={icon}
        />
      </Tooltip>

      {infoLoomMenuOpen && (
        <div draggable className={styles.panel}>
          <header className={styles.header}>
            <div>Info Loom</div>
          </header>
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
                  <div className={styles.buttonContent}>
                    <span>{name}</span>
                    {sections[name].src !== undefined &&
                        <Icon tinted src={sections[name].src as string} className={styles.buttonIcon} />
                    }
                  </div>
                </Button>
            ))}
          </div>
        </div>
      )}

      {/* Render non-menu sections based on their own open state */}
      {Object.entries(sections).map(([name, section]) => {
        // Skip menu type sections from this regular rendering
        if (['Residential Menu', 'Industrial Menu', 'District Menu', 'Commercial Menu'].includes(name)) {
          return null;
        }

        return section.isOpen && (
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
        );
      })}

      {/* Always render these components regardless of the menu state */}
      {/* This ensures the panels remain visible even when the main InfoLoom menu is closed */}
      <ResidentialMenuButton />
      <IndustrialMenuButton />
      <DistrictMenuButton />
      <CommercialMenuButton />
    </div>
  );
}

export default InfoLoomButton;
