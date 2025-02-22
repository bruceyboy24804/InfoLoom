import React, { FC, useRef } from "react";
import { Panel, Scrollable, DraggablePanelProps, Number2, Tooltip } from "cs2/ui";
import { useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { Name } from "cs2/bindings";
import styles from "./Districts.module.scss";
import { DistrictData$ } from "mods/bindings";
import { District, AgeData, EducationData } from "mods/domain/District";

const DataDivider: FC = () => (
    <div className={styles.dataDivider} />
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

interface DistrictLineProps{
    data: District;
}

const formatCombinedTooltip = (data: { ageData: AgeData; educationData: EducationData }): JSX.Element => {
    const getPercentage = (value: number, total: number) => ((value / total) * 100).toFixed(1);

    const ColorIndicator: FC<{ color: string }> = ({ color }) => (
        <div className={styles.colorIndicator} style={{ backgroundColor: color }} />
    );

    const ageColors = {
        children: '#808080',
        teens: '#B09868',
        adults: '#368A2E',
        seniors: '#5796D1'
    };

    const educationColors = {
        uneducated: '#808080',
        poorlyEducated: '#B09868',
        educated: '#368A2E',
        wellEducated: '#B981C0',
        highlyEducated: '#5796D1'
    };
    
    const AgeChart = () => {
        const ageData = [
            { label: 'Children', value: data.ageData.children, color: ageColors.children },
            { label: 'Teens', value: data.ageData.teens, color: ageColors.teens },
            { label: 'Adults', value: data.ageData.adults, color: ageColors.adults },
            { label: 'Seniors', value: data.ageData.elders, color: ageColors.seniors }
        ];

        return (
            <div className={styles.chartSection}>
                <div className={styles.chartBar}>
                    {ageData.map((item, index) => (
                        <div
                            key={index}
                            className={styles.chartBarSegment}
                            style={{
                                width: `${getPercentage(item.value, data.ageData.total)}%`,
                                backgroundColor: item.color
                            }}
                        />
                    ))}
                </div>
                {ageData.map((item, index) => (
                    <div key={index} className={styles.chartLegendItem}>
                        <span className={styles.legendLabel}>
                            <ColorIndicator color={item.color} />{item.label}
                        </span>
                        <span>{item.value}</span>
                    </div>
                ))}
            </div>
        );
    };

    const EducationChart = () => {
        const educationData = [
            { label: 'Uneducated', value: data.educationData.uneducated, color: educationColors.uneducated },
            { label: 'Poorly Educated', value: data.educationData.poorlyEducated, color: educationColors.poorlyEducated },
            { label: 'Educated', value: data.educationData.educated, color: educationColors.educated },
            { label: 'Well Educated', value: data.educationData.wellEducated, color: educationColors.wellEducated },
            { label: 'Highly Educated', value: data.educationData.highlyEducated, color: educationColors.highlyEducated }
        ];

        return (
            <div className={styles.chartSection}>
                <div className={styles.chartBar}>
                    {educationData.map((item, index) => (
                        <div
                            key={index}
                            className={styles.chartBarSegment}
                            style={{
                                width: `${getPercentage(item.value, data.educationData.total)}%`,
                                backgroundColor: item.color
                            }}
                        />
                    ))}
                </div>
                {educationData.map((item, index) => (
                    <div key={index} className={styles.chartLegendItem}>
                        <span className={styles.legendLabel}>
                            <ColorIndicator color={item.color} />{item.label}
                        </span>
                        <span>{item.value}</span>
                    </div>
                ))}
            </div>
        );
    };
    
    return (
        <div className={styles.tooltipContent}>
            <div className={styles.chartSection}>
                <div className={styles.chartTitle}>Age Distribution</div>
                <AgeChart />
            </div>
            
            <div className={styles.chartSection}>
                <div className={styles.chartTitle}>Education Levels</div>
                <EducationChart />
            </div>
        </div>
    );
};

const DistrictLine: FC<DistrictLineProps> = ({ data }) => {
    const { translate } = useLocalization();
    return (
        <div className={styles.row}>
            <div className={styles.nameColumn}>
                {getDisplayName(data.name, translate)}
            </div>
            <Tooltip tooltip={`Current households: ${data.householdCount}\nMaximum capacity: ${data.maxHouseholds}\nOccupancy rate: ${((data.householdCount / data.maxHouseholds) * 100).toFixed(1)}%`}>
                <div className={styles.column}>{data.householdCount} / {data.maxHouseholds}</div>
            </Tooltip>
            <Tooltip tooltip={formatCombinedTooltip({
                ageData: data.ageData,
                educationData: data.educationData
            })}>
                <div className={styles.column}>{data.residentCount}</div>
            </Tooltip>
            <Tooltip tooltip={`Number of pets in the district: ${data.petCount}\nAverage pets per household: ${(data.petCount / data.householdCount).toFixed(2)}`}>
                <div className={styles.column}>{data.petCount}</div>
            </Tooltip>
            <Tooltip tooltip={`Wealth Level: ${data.wealthKey}\nThis indicates the overall financial status of households in the district`}>
                <div className={styles.column}>{data.wealthKey}</div>
            </Tooltip>
        </div>
    );
};

const TableHeader: FC = () => (
    <div className={styles.tableHeader}>
        <div className={styles.headerRow}>
            <div className={styles.nameColumn}><b>Name</b></div>
            <Tooltip tooltip="Shows current occupancy vs maximum capacity of households in the district. Hover over the values to see detailed statistics.">
                <div className={styles.column}><b>Households</b></div>
            </Tooltip>
            <Tooltip tooltip="Total number of residents. Hover to see detailed age and education demographics.">
                <div className={styles.column}><b>Residents</b></div>
            </Tooltip>    
            <Tooltip tooltip="Number of pets in the district. Hover to see average pets per household.">
                <div className={styles.column}><b>Pets</b></div>
            </Tooltip>
            <Tooltip 
                tooltip={formatTooltip(
                    "Indicates the overall prosperity level of the district based on household income and net worth. Hover over values to see detailed information.",
                    ["income", "net worth"]
                )}
            >
                <div className={styles.column}><b>Average wealth</b></div>
            </Tooltip>
        </div>
    </div>
);

const AllDistrictsPanel: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
    const districts: District[] = useValue(DistrictData$) ?? [];
    const defaultPos: Number2 = { x: 0.038, y: 0.15 };

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
                    <div className={styles.scrollableContent}>
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