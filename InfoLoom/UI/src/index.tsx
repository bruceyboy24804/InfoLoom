import { ModRegistrar } from 'cs2/modding';
import InfoLoomMenu from 'mods/InfoLoomMenu/InfoLoomMenu';
import 'intl';
import 'intl/locale-data/jsonp/en-US';
import { Chart } from 'chart.js';
import { VanillaComponentResolver } from './mods/VanillaComponents/VanillaComponents';

const register: ModRegistrar = moduleRegistry => {
  VanillaComponentResolver.setRegistry(moduleRegistry);
  moduleRegistry.append('GameTopLeft', InfoLoomMenu);
};

const oldResize = Chart.prototype['resize'];
Chart.prototype['resize'] = function (width, height) {
  width = width || 1;
  height = height || 1;
  oldResize.apply(this, [width, height]);
};

export default register;
