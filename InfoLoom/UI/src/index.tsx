import  { ModRegistrar } from 'cs2/modding';
import InfoLoomMenu from 'mods/InfoLoomMenu/InfoLoomMenu';
import 'intl';
import 'intl/locale-data/jsonp/en-US';
import {Chart, CategoryScale} from 'chart.js';

// Register the category scale
Chart.register(CategoryScale);

const register: ModRegistrar = moduleRegistry => {
  moduleRegistry.append('GameTopLeft', InfoLoomMenu);
};

const oldResize = Chart.prototype['resize'];
Chart.prototype['resize'] = function (width, height) {
  width = width || 1;
  height = height || 1;
  oldResize.apply(this, [width, height]);
}

export default register;
