import { ModRegistrar } from 'cs2/modding';
import InfoLoomMenu from 'mods/InfoLoomMenu/InfoLoomMenu';
import 'intl';
import 'intl/locale-data/jsonp/en-US';
import { Chart } from 'chart.js';
import { VanillaComponentResolver } from './mods/VanillaComponents/VanillaComponents';
import { ILCitizenInfoSection } from 'mods/InfoLoomSections/ILInfoSections/Sections/citizenInfoComponent';
import { ILBuildingInfoSection } from './mods/InfoLoomSections/ILInfoSections/Sections/buildingInfoComponent';
import { ILRentInfoSection } from 'mods/InfoLoomSections/ILInfoSections/Sections/rentInfoComponent';
import { ILCompanyProfitSection } from 'mods/InfoLoomSections/ILInfoSections/Sections/profitInfoComponent';
import { ILPropertyInfoSection } from 'mods/InfoLoomSections/ILInfoSections/Sections/propertyCountSection';

const register: ModRegistrar = moduleRegistry => {
  VanillaComponentResolver.setRegistry(moduleRegistry);
  moduleRegistry.append('GameTopLeft', InfoLoomMenu);
  moduleRegistry.extend(
    'game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx',
    'selectedInfoSectionComponents',
    ILCitizenInfoSection
  );
  moduleRegistry.extend(
    'game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx',
    'selectedInfoSectionComponents',
    ILBuildingInfoSection
  );
  moduleRegistry.extend(
    'game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx',
    'selectedInfoSectionComponents',
    ILRentInfoSection
  );
  moduleRegistry.extend(
    'game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx',
    'selectedInfoSectionComponents',
    ILCompanyProfitSection
  );
  moduleRegistry.extend(
    'game-ui/game/components/selected-info-panel/selected-info-sections/selected-info-sections.tsx',
    'selectedInfoSectionComponents',
    ILPropertyInfoSection
  );
};

/*const oldResize = Chart.prototype['resize'];
Chart.prototype['resize'] = function (width, height) {
  width = width || 1;
  height = height || 1;
  oldResize.apply(this, [width, height]);
};*/

export default register;
