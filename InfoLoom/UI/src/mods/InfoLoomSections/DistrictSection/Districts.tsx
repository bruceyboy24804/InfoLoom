import React from "react";
import { Panel, Scrollable, DraggablePanelProps, Number2 } from "cs2/ui";
import { useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { Name } from "cs2/bindings";
import styles from "./Districts.module.scss";
import { DistrictData$ } from "mods/bindings";
import { District } from "mods/domain/District";

const getDisplayName = (name: Name, translate: (id: string, fallback?: string | null) => string | null): string => {
    if (!name) return "";
    if (typeof name === "string") return name;
    if ("name" in name) return name.name;
    if ("nameId" in name) {
        const translated = translate(name.nameId);
        return translated || name.nameId;
    }
    return String(name);
};

const AllDistrictsPanel = ({ onClose, initialPosition }: DraggablePanelProps): JSX.Element => {
    const { translate } = useLocalization();
    const districts: District[] = useValue(DistrictData$) || [];
    const defaultPos: Number2 = { x: 0.038, y: 0.15 };

    return (
        <Panel
            draggable
            onClose={onClose}
            initialPosition={initialPosition || defaultPos}
            className={styles.panel}
            header={<div className={styles.header}><span>Districts</span></div>}
        >
            {districts.length === 0 ? (
                <p>No Districts Found</p>
            ) : (
                <Scrollable smooth vertical trackVisibility="scrollable">
                    {districts.map((district, index) => (
                        <div key={index}>
                            {getDisplayName(district.name, translate)}
                        </div>
                    ))}
                </Scrollable>
            )}
        </Panel>
    );
};

export default AllDistrictsPanel;