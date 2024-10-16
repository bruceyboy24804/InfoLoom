import React, { useCallback, useState, FC } from 'react';
import { Button, FloatingButton, Tooltip } from "cs2/ui";
import icon from "images/infoloom.svg";
import styles from "./InfoLoomMenu.module.scss";
import Demographics from "mods/InfoLoomSections/DemographicsSection/Demographics";
import Workforce from "mods/InfoLoomSections/WorkforceSection/Workforce";
import Workplaces from "mods/InfoLoomSections/WorkplacesSection/Workplaces";
// Import other sections as needed
// import Residential from "mods/InfoLoomSections/ResidentialSection/Residential";
// import Demand from "mods/InfoLoomSections/DemandSection/Demand";
// import Commercial from "mods/InfoLoomSections/CommercialSection/Commercial";
// import Industrial from "mods/InfoLoomSections/IndustrialSection/Industrial";

// Define the sections with their respective components
const sections: { name: Section; component: FC }[] = [
  { name: 'Demographics', component: Demographics },
  { name: 'Workforce', component: Workforce },
  { name: 'Workplaces', component: Workplaces },
  // Add other sections here
  // { name: 'Residential', component: Residential },
  // { name: 'Demand', component: Demand },
  // { name: 'Commercial', component: Commercial },
  // { name: 'Industrial', component: Industrial },
];

// Define the Section type
type Section = 'Demographics' | 'Workforce' | 'Workplaces' | 'Demand' | 'Residential' | 'Commercial' | 'Industrial';

const InfoLoomButton: FC = () => {
  const [mainMenuOpen, setMainMenuOpen] = useState<boolean>(false);
  const [openSections, setOpenSections] = useState<Record<Section, boolean>>({
    Demographics: false,
    Workforce: false,
    Workplaces: false,
    Demand: false,
    Residential: false,
    Commercial: false,
    Industrial: false,
  });

  const toggleMainMenu = useCallback(() => {
    setMainMenuOpen(prev => !prev);
  }, []);

  const toggleSection = useCallback((section: Section) => {
    setOpenSections(prev => ({
      ...prev,
      [section]: !prev[section],
    }));
  }, []);

  return (
    <div>
      <Tooltip tooltip="Info Loom">
        <FloatingButton onClick={toggleMainMenu} src={icon} aria-label="Toggle Info Loom Menu" />
      </Tooltip>

      {mainMenuOpen && (
        <div
          draggable={true}
          className={styles.panel}
        >
          <header className={styles.header}>
            <h2>Info Loom</h2>
          </header>
          <div className={styles.buttonRow}>
            {sections.map(({ name }) => (
              <Button
                key={name}
                variant='flat'
                aria-label={name}
                aria-expanded={openSections[name]}
                className={
                  openSections[name] ? styles.buttonSelected : styles.InfoLoomButton
                }
                onClick={() => toggleSection(name)}
                onMouseDown={(e) => e.preventDefault()} // Prevent button shrinking on click
              >
                {name}
              </Button>
            ))}
          </div>
        </div>
      )}

      {/* Render Components Conditionally */}
      {sections.map(({ name, component: Component }) => (
        openSections[name] && <Component key={name} />
      ))}
    </div>
  );
};

export default InfoLoomButton;
