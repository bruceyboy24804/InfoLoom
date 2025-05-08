import React, { useCallback } from "react";
import { useValue } from "cs2/api";
import { Button, FloatingButton, Tooltip } from "cs2/ui";
import icon from "images/infoloom.svg";
import styles from "./InfoLoomMenu.module.scss";
import Demographics from "mods/InfoLoomSections/DemographicsSection/Demographics";
import Workforce from "mods/InfoLoomSections/WorkforceSection/Workforce";
import Workplaces from "mods/InfoLoomSections/WorkplacesSection/Workplaces";
import Residential from "mods/InfoLoomSections/ResidentialSection/residential";
import Demand from "mods/InfoLoomSections/DemandSection/Demand";
import Commercial from "mods/InfoLoomSections/CommercialSecction/CommercialDemandUI/CommercialDemand";
import CommercialProducts from "mods/InfoLoomSections/CommercialSecction/CommercialProductsUI/CommercialProducts";
import Industrial from "mods/InfoLoomSections/IndustrialSection/IndustrialDemandUI/IndustrialDemand";
import IndustrialProducts from "mods/InfoLoomSections/IndustrialSection/IndustrialProductsUI/IndustrialProducts";
import TradeCost from "mods/InfoLoomSections/TradeCostSection/TradeCost";
import Districts from "mods/InfoLoomSections/DistrictSection/Districts";
import CommercialCompanyDebugDataPanel from "mods/InfoLoomSections/CommercialSecction/CommercialDebugDataUI/CommercialCompanyDebugData";
import * as bindings from "mods/bindings";


interface SectionConfig {
  component: JSX.Element;
  openState: () => boolean;
  toggle: (state: boolean) => void;
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
  Residential: {
    component: <Residential />,
    openState: () => useValue(bindings.ResidentialDemandOpen),
    toggle: bindings.SetResidentialDemandOpen
  },
  Demand: {
    component: <Demand />,
    openState: () => useValue(bindings.BuildingDemandOpen),
    toggle: bindings.SetBuildingDemandOpen
  },
  Commercial: {
    component: <Commercial />,
    openState: () => useValue(bindings.CommercialDemandOpen),
    toggle: bindings.SetCommercialDemandOpen
  },
  "Commercial Products": {
    component: <CommercialProducts />,
    openState: () => useValue(bindings.CommercialProductsOpen),
    toggle: bindings.SetCommercialProductsOpen
  },
    CommercialCompanyDebugData: {
        component: <CommercialCompanyDebugDataPanel />,
        openState: () => useValue(bindings.CommercialCompanyDebugOpen),
        toggle: bindings.SetCommercialCompanyDebugOpen
    },
  Industrial: {
    component: <Industrial />,
    openState: () => useValue(bindings.IndustrialDemandOpen),
    toggle: bindings.SetIndustrialDemandOpen
  },
  "Industrial Products": {
    component: <IndustrialProducts />,
    openState: () => useValue(bindings.IndustrialProductsOpen),
    toggle: bindings.SetIndustrialProductsOpen
  },
  "Trade Cost": {
    component: <TradeCost />,
    openState: () => useValue(bindings.TradeCostsOpen),
    toggle: bindings.SetTradeCostsOpen
  },
  Districts: {
    component: <Districts />,
    openState: () => useValue(bindings.DistrictDataOpen),
    toggle: bindings.SetDistrictDataOpen
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

export default InfoLoomButton;