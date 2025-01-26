import React, { useCallback, useState, FC, useEffect } from "react";
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
import ModdedCommercialDemand from "mods/InfoLoomSections/CommercialSecction/ModdedCommercialDemand/ModdedCommercialDemand";
import TradeCost from "mods/InfoLoomSections/TradeCostSection/TradeCost";
import { bindValue, useValue, trigger } from "cs2/api";
import mod from "mod.json";
import { useLocalization } from 'cs2/l10n';
import { getKey } from 'mods/localizationHelper';

// This reactive boolean controls whether the "Modded Commercial Demand" UI is visible
const ModdedCommercialDemandButton$ = bindValue<boolean>(mod.id, 'MCDButton', false);

// Define the Section type
type Section =
  | 'Demographics'
  | 'Workforce'
  | 'Workplaces'
  | 'Demand'
  | 'Residential'
  | 'Commercial'
  | 'Commercial Products'
  | 'Industrial'
  | 'Industrial Products'
  | 'Modded Commercial Demand'
  | 'Trade Cost';

// Components that accept an onClose prop
type SectionComponentProps = {
  onClose: () => void;
};

// All possible sections
const allSections: {
  name: Section;
  displayName: string;
  component: FC<SectionComponentProps>;
}[] = [
  { name: 'Demographics', displayName: 'Demographics', component: Demographics },
  { name: 'Workforce', displayName: 'Workforce', component: Workforce },
  { name: 'Workplaces', displayName: 'Workplaces', component: Workplaces },
  { name: 'Residential', displayName: 'Residential', component: Residential },
  { name: 'Demand', displayName: 'Demand', component: Demand },
  { name: 'Commercial', displayName: 'Commercial', component: Commercial },
  {
    name: 'Commercial Products',
    displayName: 'Commercial Products',
    component: CommercialProducts,
  },
  { name: 'Industrial', displayName: 'Industrial', component: Industrial },
  {
    name: 'Industrial Products',
    displayName: 'Industrial Products',
    component: IndustrialProducts,
  },
  {
    name: 'Modded Commercial Demand',
    displayName: 'Modded Commercial Demand',
    component: ModdedCommercialDemand,
  },
  { name: 'Trade Cost', displayName: 'Trade Cost', component: TradeCost },
];

const InfoLoomButton: FC = () => {
  // Reactively read the boolean from bindValue
  const showModdedCommercialDemand = useValue(ModdedCommercialDemandButton$);
  const { translate } = useLocalization();
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
    'Modded Commercial Demand': false,
    'Trade Cost': false,
  });

  // Force-close Modded Commercial Demand if turned off while open
  useEffect(() => {
    if (!showModdedCommercialDemand && openSections['Modded Commercial Demand']) {
      setOpenSections(prev => ({
        ...prev,
        'Modded Commercial Demand': false,
      }));
    }
  }, [showModdedCommercialDemand, openSections]);

  const toggleMainMenu = useCallback(() => {
    setMainMenuOpen(prev => !prev);
  }, []);

  const toggleSection = useCallback((section: Section, isOpen?: boolean) => {
    setOpenSections(prev => ({
      ...prev,
      [section]: isOpen !== undefined ? isOpen : !prev[section],
    }));
  }, []);

  // Only include sections that should be visible
  const visibleSections = allSections.filter(section => {
    if (section.name === 'Modded Commercial Demand') {
      return showModdedCommercialDemand; // Hide if false
    }
    return true; // Otherwise show
  });

  return (
    <div>
      <Tooltip tooltip={translate(getKey('modName', 'menu'), 'Info Loom')}>
        <FloatingButton onClick={toggleMainMenu} src={icon} aria-label="Toggle Info Loom Menu" />
      </Tooltip>

      {mainMenuOpen && (
        <div draggable={true} className={styles.panel}>
          <header className={styles.header}>
            <div>{translate(getKey('modName', 'menu'), 'Info Loom')}</div>
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
                onMouseDown={e => e.preventDefault()}
              >
                {translate(getKey(name, 'menu'), name)}
              </Button>
            ))}
          </div>
        </div>
      )}

      {visibleSections.map(({ name, component: Component }) => {
        return (
          openSections[name] && <Component key={name} onClose={() => toggleSection(name, false)} />
        );
      })}
    </div>
  );
};

export default InfoLoomButton;
