import React, { FC, useRef } from "react";
import { Panel, Scrollable, DraggablePanelProps, Number2, Tooltip, TooltipProps } from "cs2/ui";
import { useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { Name } from "cs2/bindings";
import styles from "./Districts.module.scss";
import { DistrictData$ } from "mods/bindings";
import { District } from "mods/domain/District";


const DataDivider: FC = () => (
    <div style={{display: 'flex', height: '4rem', flexDirection: 'column', justifyContent: 'center'}}>
        <div style={{borderBottom: '1px solid gray', width: '100%'}}></div>
    </div>
);

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
const formatTooltip = (text: string, highlights: string[] = []): JSX.Element => {
    if (!highlights.length) return <>{text}</>;
    
    const parts = text.split(new RegExp(`\\b(${highlights.join('|')})\\b`, 'gi'));
    return (
        <span style={{ 
            whiteSpace: 'normal', 
            display: 'inline',
            maxWidth: '300px' 
        }}>
            {parts.map((part, i) => {
                if (highlights.some(h => part.toLowerCase() === h.toLowerCase())) {
                    return <strong key={i}>{part}</strong>;
                }
                return <span key={i}>{part}</span>;
            })}
        </span>
    );
};
interface DistrictLineProps {
    data: District;
}

const DistrictLine: FC<DistrictLineProps> = ({ data }) => {
    const { translate } = useLocalization();
    return (
        <div
            className="labels_L7Q row_S2v"
            style={{
                width: '100%',
                padding: '1rem 25rem',
                display: 'flex',
                alignItems: 'center',
                boxSizing: 'border-box',
            }}
        >
            <div style={{
                flex: '0 0 20%',
                paddingRight: '1rem',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap'
            }}>
                {getDisplayName(data.name, translate)}
            </div>
            <div style={{flex: '0 0 15%', textAlign: 'center', display: "flex"}}>{data.householdCount} / {data.maxHouseholds}</div>
            <div style={{flex: '0 0 15%', textAlign: 'center'}}>{data.residentCount}</div>
            <div style={{flex: '0 0 15%', textAlign: 'center'}}>{data.petCount}</div>
            <div style={{flex: '0 0 15%', textAlign: 'center'}}>{data.wealthKey}</div>
        </div>
    );
};

const TableHeader: FC = () => (
    <div style={{maxWidth: '1200px', margin: '0 auto', padding: '0 25rem'}}>
        <div
            className="labels_L7Q row_S2v"
            style={{
                width: '100%',
                padding: '1rem 0',
                display: 'flex',
                alignItems: 'center',
            }}
        >
            <div style={{flex: '0 0 20%'}}><div><b>Name</b></div></div>
            <Tooltip tooltip="The current and maximum number of households in a district. The size of a building is determined by the maximum number of households.">
                <div style={{flex: '0 0 15%', textAlign: 'center', display: "flex"}}><b>Households</b></div>
            </Tooltip>
            <Tooltip tooltip="The number of residents living in a district.">
                <div style={{flex: '0 0 15%', textAlign: 'center'}}><b>Residents</b></div>
            </Tooltip>    
            <div style={{flex: '0 0 15%', textAlign: 'center'}}><b>Pets</b></div>
            <Tooltip 
                tooltip={formatTooltip(
                    "The average financial situation of the residents in a district, taking into account their members' income and the households net worth.",
                    ["income", "net worth"]
                )}
            >
                <div style={{flex: '0 0 15%', textAlign: 'center'}}><b>Average wealth</b></div>
            </Tooltip>
        </div>
    </div>
);

const AllDistrictsPanel: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
    const districts: District[] = useValue(DistrictData$) ?? [];
    const defaultPos: Number2 = { x: 0.038, y: 0.15 };
    const panelRef = useRef<HTMLDivElement>(null);

    return (
        <Panel
            draggable
            onClose={onClose}
            initialPosition={initialPosition || defaultPos}
            className={styles.panel}
            header={<div className={styles.header}><span className={styles.headerText}>Districts</span></div>}
        >
            {districts.length === 0 ? (
                <p className={styles.loadingText}>No Districts Found</p>
            ) : (
                <div>
                    <TableHeader />
                    <DataDivider />
                    <div style={{padding: '1rem 0'}}>
                        <Scrollable smooth vertical trackVisibility="scrollable">
                            {districts.map((district) => (
                                <DistrictLine
                                    key={district.entity.index}
                                    data={district}
                                />
                            ))}
                        </Scrollable>
                    </div>
                    <DataDivider />
                </div>
            )}
        </Panel>
    );
};

export default AllDistrictsPanel;