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
import Industrial from "mods/InfoLoomSections/IndustrialSection/Industrial";
import CommercialProducts from "mods/InfoLoomSections/CommercialSecction/CommercialD";
import IndustrialProducts from "mods/InfoLoomSections/IndustrialSection/IndustrialD";
import Districts from "mods/InfoLoomSections/DistrictsSection/Districts";




// Define the Section type
type Section = 'Demographics' | 'Workforce' | 'Workplaces' | 'Demand' | 'Residential' | 'Commercial' | 'Commercial Products' |  'Industrial' | 'Industrial Products' | 'Districts';

// Define a new type for components that accept an onClose prop
type SectionComponentProps = {
  onClose: () => void;
};

// Update the sections array type
const sections: { name: Section; displayName: string; component: FC<SectionComponentProps> }[] = [
  { name: 'Demographics', displayName: 'Demographics', component: Demographics },
  { name: 'Workforce', displayName: 'Workforce', component: Workforce },
  { name: 'Workplaces', displayName: 'Workplaces', component: Workplaces },
  { name: 'Residential', displayName: 'Residential', component: Residential },
  { name: 'Demand', displayName: 'Demand', component: Demand },
  { name: 'Commercial', displayName: 'Commercial', component: Commercial },
  { name: 'Commercial Products', displayName: 'Commercial Products', component: CommercialProducts },
  { name: 'Industrial', displayName: 'Industrial', component: Industrial },
  { name: 'Industrial Products', displayName: 'Industrial Products', component: IndustrialProducts },
  { name: 'Districts', displayName: 'Districts', component: Districts},
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
    'Commercial Products': false,
    Industrial: false,
    'Industrial Products': false,
    Districts: false
    
    
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
