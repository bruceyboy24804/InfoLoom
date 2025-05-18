import React, { useCallback } from "react";
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


interface SectionConfig {
  component: JSX.Element;
  openState: () => boolean;
  toggle: (state: boolean) => void;
  src?: string;
}

const sections: Record<string, SectionConfig> = {
  Demographics: {
    component: <Demographics />,
    openState: () => useValue(bindings.DemographicsOpen),
    toggle: bindings.SetDemographicsOpen
  },
  Workforce: {
    component: <Workforce />,
    openState: () => useValue(bindings.WorkforceOpen),
    toggle: bindings.SetWorkforceOpen
  },
  Workplaces: {
    component: <Workplaces />,
    openState: () => useValue(bindings.WorkplacesOpen),
    toggle: bindings.SetWorkplacesOpen
  },
  "Residential Menu": {
    component: < ResidentialMenuButton />,
    openState: () => useValue(bindings.ResidentialMenuOpen),
    toggle: bindings.SetResidentialMenuOpen,
    src: "Media/Glyphs/FilledArrowRight.svg"

  },
  Demand: {
    component: <Demand />,
    openState: () => useValue(bindings.BuildingDemandOpen),
    toggle: bindings.SetBuildingDemandOpen
  },
  "Industrial Menu": {
    component: <IndustrialMenuButton/>,
    openState: () => useValue(bindings.IndustrialMenuOpen),
    toggle: bindings.SetIndustrialMenuOpen,
    src: "Media/Glyphs/FilledArrowRight.svg"

  },
  "Trade Cost": {
    component: <TradeCost />,
    openState: () => useValue(bindings.TradeCostsOpen),
    toggle: bindings.SetTradeCostsOpen
  },
  "District Menu": {
    component: < DistrictMenuButton/>,
    openState: () => useValue(bindings.DistrictMenuOpen),
    toggle: bindings.SetDistrictMenuOpen,
    src: "Media/Glyphs/FilledArrowRight.svg"
  },
  "Commercial Menu" : {
    component: <CommercialMenuButton />,
    openState: () => useValue(bindings.CommercialMenuOpen),
    toggle: bindings.SetCommercialMenuOpen,
    src: "Media/Glyphs/FilledArrowRight.svg"
  }
};

function InfoLoomButton(): JSX.Element {
  const infoLoomMenuOpen = useValue(bindings.InfoLoomMenuOpen);
  const sectionStates = Object.fromEntries(
    Object.entries(sections).map(([name, config]) => [name, config.openState()])
  );

  const toggleSection = useCallback(
    (name: string) => sections[name]?.toggle(!sectionStates[name]),
    [sectionStates]
  );

  return (
    <div>
      <Tooltip tooltip="Info Loom">
        <FloatingButton
          onClick={() => bindings.SetInfoLoomMenuOpen(!infoLoomMenuOpen)}
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
                    aria-expanded={sectionStates[name]}
                    className={`${styles.InfoLoomButton} ${
                        sectionStates[name] ? styles.buttonSelected : ""
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

export default InfoLoomButton;