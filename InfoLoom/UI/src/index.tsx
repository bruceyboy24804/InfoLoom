import { ModRegistrar } from "cs2/modding";
import  InfoLoomConent from "./mods/InfoLoomContent/InfoLoomMenu";
import 'intl';
import 'intl/locale-data/jsonp/en-US'; 
const register: ModRegistrar = (moduleRegistry) => {

    moduleRegistry.append('GameTopLeft', InfoLoomConent);
}

export default register;