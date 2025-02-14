import React, { useCallback, useState, useEffect } from "react";
import { Button, FloatingButton, Tooltip } from "cs2/ui";
import icon from "images/infoloom.svg";
import styles from "./InfoLoomMenu.module.scss";
import Demographics from "mods/InfoLoomSections/DemographicsSection/Demographics";
import Workforce from "mods/InfoLoomSections/WorkforceSection/Workforce";
import Workplaces from "mods/InfoLoomSections/WorkplacesSection/Workplaces";
import Residential from "mods/InfoLoomSections/ResidentialSection/residential";
import Demand from "mods/InfoLoomSections/DemandSection/Demand";
import Commercial from "mods/InfoLoomSections/CommercialSecction/CommercialDemandUI/CommercialDemand";
import Industrial from "mods/InfoLoomSections/IndustrialSection/IndustrialDemandUI/IndustrialDemand";
import CommercialProducts from "mods/InfoLoomSections/CommercialSecction/CommercialProductsUI/CommercialProducts";
import IndustrialProducts from "mods/InfoLoomSections/IndustrialSection/IndustrialProductsUI/IndustrialProducts";
import TradeCost from "mods/InfoLoomSections/TradeCostSection/TradeCost";
import { bindValue, useValue } from "cs2/api";
import mod from "mod.json";
import Districts from "../InfoLoomSections/DistrictSection/Districts";

const sectionComponents: Record<string, JSX.Element> = {
  Demographics: <Demographics />,
  Workforce: <Workforce />,
  Workplaces: <Workplaces />,
  Residential: <Residential />,
  Demand: <Demand />,
  Commercial: <Commercial />,
  "Commercial Products": <CommercialProducts />,
  Industrial: <Industrial />,
  "Industrial Products": <IndustrialProducts />,
  "Trade Cost": <TradeCost />,
  Districts: <Districts />,
};

const allSections = Object.keys(sectionComponents).map((name) => ({
  name,
  displayName: name,
  component: sectionComponents[name],
}));

function InfoLoomButton(): JSX.Element {
  const [mainMenuOpen, setMainMenuOpen] = useState<boolean>(false);
  const [openSections, setOpenSections] = useState<Record<string, boolean>>({
    Demographics: false,
    Workforce: false,
    Workplaces: false,
    Demand: false,
    Residential: false,
    Commercial: false,
    "Commercial Products": false,
    Industrial: false,
    "Industrial Products": false,
    "Trade Cost": false,
    Districts: false,
  });

  const toggleMainMenu = useCallback(() => {
    setMainMenuOpen((prev) => !prev);
  }, []);

  const toggleSection = useCallback((section: string, isOpen?: boolean) => {
    setOpenSections((prev) => ({
      ...prev,
      [section]: isOpen !== undefined ? isOpen : !prev[section],
    }));
  }, []);

  const visibleSections = allSections;

  return (
    <div>
      <Tooltip tooltip="Info Loom">
        <FloatingButton onClick={toggleMainMenu} src={icon} aria-label="Toggle Info Loom Menu" />
      </Tooltip>

      {mainMenuOpen && (
        <div draggable={true} className={styles.panel}>
          <header className={styles.header}>
            <div>Info Loom</div>
          </header>
          <div className={styles.buttonRow}>
            {visibleSections.map(({ name }) => (
              <Button
                key={name}
                variant="flat"
                aria-label={name}
                aria-expanded={openSections[name]}
                className={`${styles.InfoLoomButton} ${openSections[name] ? styles.buttonSelected : ''}`}
                onClick={() => toggleSection(name)}
                onMouseDown={(e) => e.preventDefault()}
              >
                {name}
              </Button>
            ))}
          </div>
        </div>
      )}

      {visibleSections.map(({ name }) => {
        return openSections[name] && React.cloneElement(sectionComponents[name], { key: name, onClose: () => toggleSection(name, false) });
      })}
    </div>
  );
}

export default InfoLoomButton;