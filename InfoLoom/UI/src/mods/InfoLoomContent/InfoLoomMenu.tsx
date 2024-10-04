// InfoLoomButton.tsx

import React, { useCallback, useState } from 'react';
import { Button, FloatingButton, Tooltip, FormattedText, } from "cs2/ui";
import icon from "images/infoloom.svg"; // Assuming you are using the same icon for open/closed state
import styles from "./InfoLoomMenu.module.scss";
import Demographics from "mods/InfoLoomSections/DemographicsSection/Demographics";
import Workforce from "mods/InfoLoomSections/WorkforceSection/Workforce";
//import Workplaces from "mods/InfoLoomSections/WorkplacesSection/Workplaces";
//import Residential from "mods/InfoLoomSections/ResidentialSection/residential";

// Define the sections as a TypeScript type
type Section = 'Demographics' | 'Workforce' | 'Workplaces' | 'Demand' | 'Residential' | 'Commercial' | 'Industrial';

const InfoLoomButton: React.FC = () => {
  const [mainMenuOpen, setMainMenuOpen] = useState<boolean>(false);
  const Icon = icon; // Since both states use the same icon

  // Toggle function to show or hide the main menu
  const toggleMainMenu = useCallback(() => {
    setMainMenuOpen(prev => !prev);
  }, []);

  // State to track the currently selected section for styling
  const [selected, setSelected] = useState<Section | "">("");

  // Consolidated state for open sections
  const [openSections, setOpenSections] = useState<{
    Demographics: boolean;
    Workforce: boolean;
    Workplaces: boolean;
    Demand: boolean;
    Residential: boolean;
    Commercial: boolean;
    Industrial: boolean;
  }>({
    Demographics: false,
    Workforce: false,
    Workplaces: false,
    Demand: false,
    Residential: false,
    Commercial: false,
    Industrial: false,
  });

  // Generic toggle function for sections
  const toggleSection = (section: Section) => {
    setOpenSections(prev => ({ ...prev, [section]: !prev[section] }));
    setSelected(prev => (prev === section ? "" : section));
  };

  return (
    <div>
      <Tooltip tooltip="Info Loom">
        <FloatingButton onClick={toggleMainMenu} src={Icon} />
      </Tooltip>

      {/* Panel that appears based on the mainMenuOpen state */}
      <div
        draggable={true}
        className={styles.panel}
        style={{ display: mainMenuOpen ? "flex" : "none" }}
      >
        <header className={styles.header}>
          <h2>Info Loom</h2>
        </header>
        {/* Panel Content */}
        <div className={styles.buttonRow}>
          {/* Demographics Button */}
          <Button
            variant='flat'
            aria-label="Demographics"
            className={
              selected === "Demographics" ? styles.buttonSelected : styles.InfoLoomButton
            }
            onClick={() => toggleSection("Demographics")}
            
          >
            Demographics
          </Button>

          {/* Workforce Button */}
          <Button
            variant='flat'
            aria-label="Workforce"
            className={
              selected === "Workforce" ? styles.buttonSelected : styles.InfoLoomButton
            }
            onClick={() => toggleSection("Workforce")}
          >
            Workforce
          </Button>

          
        </div>
      </div>

      {/* Render Components Conditionally */}
      {openSections.Demographics && <Demographics />}
      {openSections.Workforce && <Workforce />}
      {/* Add conditional rendering for other sections as needed */}
      {/* Example:
          {openSections.Demand && <Demand />}
          {openSections.Residential && <Residential />}
          etc.
      */}
    </div>
  );
};

// Export the component properly
export default InfoLoomButton;
