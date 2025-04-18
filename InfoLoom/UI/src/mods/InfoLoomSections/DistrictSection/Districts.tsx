import React, { FC, useRef } from "react";
import { Panel, Scrollable, DraggablePanelProps, Number2, Tooltip, BalloonTheme, Icon } from "cs2/ui";
import { useValue,  } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { Name, selectedInfo } from "cs2/bindings";
import styles from "./Districts.module.scss";
import { DistrictData$ } from "mods/bindings";
import { District, AgeData, EducationData, EmploymentData, LocalServiceBuilding, DistrictPolicy} from "mods/domain/District";

const DataDivider: FC = () => (
    <div className={styles.dataDivider} />
);
const formatNumber = (number: number): string => {
    return new Intl.NumberFormat().format(number);
};
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
            whiteSpace: 'pre-wrap', 
            display: 'inline',
            maxWidth: '300px' 
        }}>
            {parts.map((part, i) => {
                if (highlights.some(h => part.toLowerCase() === h.toLowerCase())) {
                    return <strong key={i}> {part} </strong>;
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
                        <span>{formatNumber(item.value)}</span>
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
                        <span>{formatNumber(item.value)}</span>
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

const formatEmployeesTooltip = (data: { 
    educationDataEmployees: EmploymentData; 
    educationDataWorkplaces: EmploymentData;
    employeeCount: number;
    maxEmployees: number;
}): JSX.Element => {
    const getPercentage = (value: number, total: number) => total > 0 ? ((value / total) * 100).toFixed(1) : "0.0";

    const ColorIndicator: FC<{ color: string }> = ({ color }) => (
        <div className={styles.colorIndicator} style={{ backgroundColor: color }} />
    );

    const educationColors = {
        uneducated: '#808080',
        poorlyEducated: '#B09868',
        educated: '#368A2E',
        wellEducated: '#B981C0',
        highlyEducated: '#5796D1',
        openPositions: '#4f5153'
    };
    
    // Calculate open positions
    const openPositions = Math.max(0, data.maxEmployees - data.employeeCount);
    
    // Create combined data array for employees and workplaces
    const combinedData = [
        { label: 'Uneducated', employees: data.educationDataEmployees.uneducated, workplaces: data.educationDataWorkplaces.uneducated, color: educationColors.uneducated },
        { label: 'Poorly Educated', employees: data.educationDataEmployees.poorlyEducated, workplaces: data.educationDataWorkplaces.poorlyEducated, color: educationColors.poorlyEducated },
        { label: 'Educated', employees: data.educationDataEmployees.educated, workplaces: data.educationDataWorkplaces.educated, color: educationColors.educated },
        { label: 'Well Educated', employees: data.educationDataEmployees.wellEducated, workplaces: data.educationDataWorkplaces.wellEducated, color: educationColors.wellEducated },
        { label: 'Highly Educated', employees: data.educationDataEmployees.highlyEducated, workplaces: data.educationDataWorkplaces.highlyEducated, color: educationColors.highlyEducated }
    ];
    
    return (
        <div className={styles.tooltipContent}>
            <div className={styles.chartTitle}>Employees by Education Level</div>
            <div className={styles.chartBar}>
                {combinedData.map((item, index) => (
                    <div
                        key={index}
                        className={styles.chartBarSegment}
                        style={{
                            width: `${getPercentage(item.employees, data.maxEmployees)}%`,
                            backgroundColor: item.color
                        }}
                    />
                ))}
                {/* Add open positions segment */}
                {openPositions > 0 && (
                    <div
                        className={styles.chartBarSegment}
                        style={{
                            width: `${getPercentage(openPositions, data.maxEmployees)}%`,
                            backgroundColor: educationColors.openPositions
                        }}
                    />
                )}
            </div>
            {combinedData.map((item, index) => (
                <div key={index} className={styles.chartLegendItem}>
                    <span className={styles.legendLabel}>
                        <ColorIndicator color={item.color} />{item.label}
                    </span>
                    <span className={styles.employeeNumbers}>
                        {formatNumber(item.employees)} / {formatNumber(item.workplaces)}
                    </span>
                </div>
            ))}
            
            {/* Open positions legend item */}
            {openPositions > 0 && (
                <div className={styles.chartLegendItem}>
                    <span className={styles.legendLabel}>
                        <ColorIndicator color={educationColors.openPositions} />Open Positions
                    </span>
                    <span className={styles.employeeNumbers}>
                        {formatNumber(openPositions)}
                    </span>
                </div>
            )}

            {/* Add max employees information */}
            <div className={styles.chartLegendItem}>
                <span className={styles.legendLabel}><b>Max Employees:</b></span>
                <span>{formatNumber(data.maxEmployees)}</span>
            </div>
        </div>
    );
};

// Service Buildings Component
interface ServiceBuildingsProps {
    serviceBuildings: LocalServiceBuilding[];
}

const ServiceBuildingsComponent: FC<ServiceBuildingsProps> = ({ serviceBuildings }) => {
    const { translate } = useLocalization();
    
    if (!serviceBuildings || serviceBuildings.length === 0) {
        return <div className={styles.noServices}>N/A</div>;
    }

    return (
        <div className={styles.serviceBuildings}>
            {serviceBuildings.map((service, index) => (
                <Tooltip 
                    key={service.entity.index} 
                    tooltip={getDisplayName(service.name, translate)}
                >
                    <div className={styles.serviceItem}>
                        <img 
                            src={service.serviceIcon}
                            className={styles.serviceIcon}
                            alt={getDisplayName(service.name, translate)}
                        />
                    </div>
                </Tooltip>
            ))}
        </div>
    );
};
interface PoliciesProps {
    policies: DistrictPolicy[];
}

const PoliciesComponent: FC<PoliciesProps> = ({ policies }) => {
    const { translate } = useLocalization();
    
    if (!policies || policies.length === 0) {
        return <div className={styles.noServices}>N/A</div>;
    }

    return (
        <div className={styles.policies}>
            {policies.map((policy, index) => (
                <Tooltip 
                    key={policy.entity.index} 
                    tooltip={getDisplayName(policy.name, translate)}
                >
                    <div className={styles.policyItem}>
                        <img 
                            src={policy.icon}
                            className={styles.policyIcon}
                            alt={getDisplayName(policy.name, translate)}
                        />
                    </div>
                </Tooltip>
            ))}
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
            <Tooltip tooltip={'max households: ' + formatNumber(data.maxHouseholds)}>
            <div className={styles.householdColumn}>
                <div className={styles.householdCountText}>{formatNumber(data.householdCount)}</div>
            </div>
            </Tooltip>
            <Tooltip tooltip={formatCombinedTooltip({
                ageData: data.ageData,
                educationData: data.educationData
            })}>
                <div className={styles.residentColumn}>{formatNumber(data.residentCount)}</div>
            </Tooltip>
            <div className={styles.petsColumn}>{formatNumber(data.petCount)}</div>
            <div className={styles.averageWealthColumn}>{data.wealthKey}</div>
            <Tooltip tooltip={formatEmployeesTooltip({
                educationDataEmployees: data.educationDataEmployees,
                educationDataWorkplaces: data.educationDataWorkplaces,
                employeeCount: data.employeeCount,
                maxEmployees: data.maxEmployees,
                
            })}>
                <div className={styles.employeeColumn}>{formatNumber(data.employeeCount)}</div>
            </Tooltip>
            <div className={styles.servicesColumn}>
                <ServiceBuildingsComponent serviceBuildings={data.localServiceBuildings || []} />
            </div>
            <div className={styles.policiesColumn}>
                <PoliciesComponent policies={data.policies || []} />
            </div>
            
        </div>
    );
};

const TableHeader: FC = () => (
    <div className={styles.tableHeader}>
        <div className={styles.headerRow}>
            <div className={styles.nameColumn}><b>Name</b></div>
            <Tooltip tooltip="The current and maximum number of households in the district. The size of a building determines the maximum number of households.">
                <div className={styles.householdColumn}><b>Households</b></div>
            </Tooltip>
            <Tooltip tooltip={
            <div className={styles.tooltipText}>
                <p className={styles.multiline}>
                    {"Citizen age determines their education opportunities and whether or not they can work. Age also affects citizen health. As they grow older, they are more prone to falling ill."}
                </p>
                <p className={styles.multiline}>
                    {"Education level determines the citizen's job opportunities, wages and their work efficiency. Their average education level also affects how much garbage they produce."}
                </p>
            </div>
            }>
           <div className={styles.residentColumn}><b>Residents</b></div>
            </Tooltip> 
            <div className={styles.petsColumn}><b>Pets</b></div>
            <Tooltip tooltip=
            {'The average financial situation of households in the district.'
                + '\n taking into account their member income and the'
                + '\n households net worth.'
                }
            >
                <div className={styles.averageWealthColumn}><b>Average wealth</b></div>
            </Tooltip>
            <Tooltip tooltip={
            <div className={styles.tooltipText}>
                <p className={styles.multiline}>
                    {"The maximum number of employees in a building is determined by the building's type, size, and zone density."}
                </p>
                <p className={styles.multiline}>
                    {"Open workplaces in companies and service buildings get filled whenever there are citizens with matching education."}
                </p>
                <p className={styles.multiline}>
                    {"If workplaces remain unfulfilled, the building efficiency suffers. The higher the education level, the larger the overall effect on building efficiency."}
                </p>
            </div>
                }>
                <div className={styles.employeeColumn}><b>Employees</b></div>
            </Tooltip>
            <Tooltip tooltip="Local service buildings assigned to this district">
                <div className={styles.servicesColumn}><b>Services</b></div>
            </Tooltip>
            <Tooltip tooltip="District policies that affect how this district operates">
                <div className={styles.policiesColumn}><b>Policies</b></div>
            </Tooltip>
        </div>
    </div>
);

const AllDistrictsPanel: FC<DraggablePanelProps> = ({ onClose, }) => {
    const districts: District[] = useValue(DistrictData$) ?? [];

    return (
        <Panel
            draggable
            onClose={onClose}
            initialPosition={{ x: 0.50, y: 0.50 }}
            className={styles.panel}
            header={<div className={styles.header}><span className={styles.headerText}>Districts</span></div>}
        >
            <Scrollable trackVisibility="scrollable" className={styles.scrollable}>
            {districts.length === 0 ? (
                <p className={styles.loadingText}>No Districts Found</p>
            ) : (
                <div>
                    <TableHeader />
                    <DataDivider />
                    <div className={styles.scrollableContent}>
                        {districts.map((district) => (
                            <DistrictLine
                                key={district.entity.index}
                                data={district}
                            />
                        ))}
                    </div>
                    <DataDivider />
                </div>
            )}
            </Scrollable>
        </Panel>
    );
};

export default AllDistrictsPanel;