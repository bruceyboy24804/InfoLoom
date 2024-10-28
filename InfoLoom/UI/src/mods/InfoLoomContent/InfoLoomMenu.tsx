import React, { useCallback, useState, FC } from 'react';
import { Button, FloatingButton, Tooltip } from "cs2/ui";
import icon from "images/infoloom.svg";
import styles from "./InfoLoomMenu.module.scss";
import Demographics from "mods/InfoLoomSections/DemographicsSection/Demographics";
import Workforce from "mods/InfoLoomSections/WorkforceSection/Workforce";
import Workplaces from "mods/InfoLoomSections/WorkplacesSection/Workplaces";
import Residential from "mods/InfoLoomSections/ResidentialSection/residential";
import Demand from "mods/InfoLoomSections/DemandSection/Demand";
import Commercial from "mods/InfoLoomSections/CommercialSecction/Commercial";
//import Industrial from "mods/InfoLoomSections/IndustrialSection/Industrial";
//import CommercialD from 'mods/InfoLoomSections/CommercialSecction/CommercialD';


// Define the Section type
type Section = 'Demographics' | 'Workforce' | 'Workplaces' | 'Demand' | 'Residential' | 'Commercial' | 'Industrial' | 'CommercialDetails';

// Define a new type for components that accept an onClose prop
type SectionComponentProps = {
  onClose: () => void;
};

// Update the sections array type
const sections: { name: Section; component: FC<SectionComponentProps> }[] = [
  { name: 'Demographics', component: Demographics },
  { name: 'Workforce', component: Workforce },
  { name: 'Workplaces', component: Workplaces },
  // Add other sections here
  { name: 'Residential', component: Residential },
  { name: 'Demand', component: Demand },
  { name: 'Commercial', component: Commercial },
  //{ name: 'Industrial', component: Industrial },
  //{ name: 'CommercialDetails', component: CommercialD },
];

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
    CommercialDetails: false,
  });

  const toggleMainMenu = useCallback(() => {
    setMainMenuOpen(prev => !prev);
  }, []);

  const toggleSection = useCallback((section: Section, isOpen?: boolean) => {
    setOpenSections(prev => ({
      ...prev,
      [section]: isOpen !== undefined ? isOpen : !prev[section],
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
                onMouseDown={(e) => e.preventDefault()}
              >
                {name}
              </Button>
            ))}
          </div>
        </div>
      )}

      {sections.map(({ name, component: Component }) => (
        openSections[name] && (
          <Component key={name} onClose={() => toggleSection(name, false)} />
        )
      ))}
    </div>
  );
};

export default InfoLoomButton;
