import { ModRegistrar } from 'cs2/modding';
import InfoLoomMenu from './mods/InfoLoomContent/InfoLoomMenu';
import 'intl';
import 'intl/locale-data/jsonp/en-US';

const register: ModRegistrar = moduleRegistry => {
  moduleRegistry.append('GameTopLeft', InfoLoomMenu);
};

export default register;
