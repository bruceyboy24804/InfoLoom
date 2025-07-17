import { getModule } from "cs2/modding"
import { InfoSectionProps } from "cs2/ui";

const path$ = "game-ui/game/components/selected-info-panel/shared-components/info-section/info-section.module.scss"

export const InfoSectionSCSS = {
	infoSection: getModule(path$, "classes").infoSection,
	content: getModule(path$, "classes").content,
	column: getModule(path$, "classes").column,
	divider: getModule(path$, "classes").divider,
	noMargin: getModule(path$, "classes").noMargin,
	disableFocusHighlight: getModule(path$, "classes").disableFocusHighlight,
	infoWrapBox: getModule(path$, "classes").infoWrapBox,
}

const InfoSectionModule = getModule(path$, "InfoSection");

export function InfoSectionFoldout(propsInfoSectionFoldout: InfoSectionProps): JSX.Element {
	return < InfoSectionModule {...propsInfoSectionFoldout} />
}