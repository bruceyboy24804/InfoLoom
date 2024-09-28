import { ModRegistrar } from "cs2/modding";
import { InfoLoomButton } from "./mods/InfoLoomButton/InfoLoomButton";

const register: ModRegistrar = (moduleRegistry) => {

    moduleRegistry.append('GameTopLeft', InfoLoomButton);
}

export default register;