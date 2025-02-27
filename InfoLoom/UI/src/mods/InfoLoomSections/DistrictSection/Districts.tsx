import React, { FC, useRef } from "react";
import { Panel, Scrollable, DraggablePanelProps, Number2, Tooltip, BalloonTheme } from "cs2/ui";
import { useValue } from "cs2/api";
import { useLocalization } from "cs2/l10n";
import { Name } from "cs2/bindings";
import styles from "./Districts.module.scss";
import { DistrictData$ } from "mods/bindings";
import { District, AgeData, EducationData, EmploymentData} from "mods/domain/District";

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
// Add after the formatCombinedTooltip function

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
    
    // Calculate education mismatch statistics with detailed breakdown
    const calculateEducationMismatch = () => {
        // Initial values for each education level
        const levels = {
            highlyEducated: {
                total: data.educationDataEmployees.highlyEducated,
                workplaces: data.educationDataWorkplaces.highlyEducated,
                inMatchingJobs: 0,
                inLowerJobs: 0,
                inWellEducated: 0,
                inEducated: 0,
                inPoorlyEducated: 0, 
                inUneducated: 0
            },
            wellEducated: {
                total: data.educationDataEmployees.wellEducated,
                workplaces: data.educationDataWorkplaces.wellEducated,
                inMatchingJobs: 0,
                inLowerJobs: 0,
                inEducated: 0,
                inPoorlyEducated: 0,
                inUneducated: 0
            }
        };

        // Calculate how many are in matching jobs
        levels.highlyEducated.inMatchingJobs = Math.min(
            levels.highlyEducated.total, 
            levels.highlyEducated.workplaces
        );
        
        levels.wellEducated.inMatchingJobs = Math.min(
            levels.wellEducated.total, 
            levels.wellEducated.workplaces
        );

        // Calculate total in lower jobs
        levels.highlyEducated.inLowerJobs = Math.max(0, 
            levels.highlyEducated.total - levels.highlyEducated.inMatchingJobs
        );
        
        levels.wellEducated.inLowerJobs = Math.max(0, 
            levels.wellEducated.total - levels.wellEducated.inMatchingJobs
        );

        // Calculate breakdown of where highly educated are working
        if (levels.highlyEducated.inLowerJobs > 0) {
            let remaining = levels.highlyEducated.inLowerJobs;
            
            // Well educated jobs
            let availableWellEducatedJobs = Math.max(0, 
                levels.wellEducated.workplaces - levels.wellEducated.inMatchingJobs
            );
            levels.highlyEducated.inWellEducated = Math.min(remaining, availableWellEducatedJobs);
            remaining -= levels.highlyEducated.inWellEducated;
            
            // Educated jobs
            let availableEducatedJobs = Math.max(0, 
                data.educationDataWorkplaces.educated - data.educationDataEmployees.educated
            );
            levels.highlyEducated.inEducated = Math.min(remaining, availableEducatedJobs);
            remaining -= levels.highlyEducated.inEducated;
            
            // Poorly educated jobs
            let availablePoorlyEducatedJobs = Math.max(0, 
                data.educationDataWorkplaces.poorlyEducated - data.educationDataEmployees.poorlyEducated
            );
            levels.highlyEducated.inPoorlyEducated = Math.min(remaining, availablePoorlyEducatedJobs);
            remaining -= levels.highlyEducated.inPoorlyEducated;
            
            // Uneducated jobs
            let availableUneducatedJobs = Math.max(0, 
                data.educationDataWorkplaces.uneducated - data.educationDataEmployees.uneducated
            );
            levels.highlyEducated.inUneducated = Math.min(remaining, availableUneducatedJobs);
        }
        
        // Calculate breakdown of where well educated are working
        if (levels.wellEducated.inLowerJobs > 0) {
            let remaining = levels.wellEducated.inLowerJobs;
            
            // Account for highly educated working in educated jobs
            let availableEducatedJobs = Math.max(0, 
                data.educationDataWorkplaces.educated - data.educationDataEmployees.educated - levels.highlyEducated.inEducated
            );
            levels.wellEducated.inEducated = Math.min(remaining, availableEducatedJobs);
            remaining -= levels.wellEducated.inEducated;
            
            // Poorly educated jobs
            let availablePoorlyEducatedJobs = Math.max(0, 
                data.educationDataWorkplaces.poorlyEducated - data.educationDataEmployees.poorlyEducated - levels.highlyEducated.inPoorlyEducated
            );
            levels.wellEducated.inPoorlyEducated = Math.min(remaining, availablePoorlyEducatedJobs);
            remaining -= levels.wellEducated.inPoorlyEducated;
            
            // Uneducated jobs
            let availableUneducatedJobs = Math.max(0, 
                data.educationDataWorkplaces.uneducated - data.educationDataEmployees.uneducated - levels.highlyEducated.inUneducated
            );
            levels.wellEducated.inUneducated = Math.min(remaining, availableUneducatedJobs);
        }
        
        return {
            highlyEducated: levels.highlyEducated,
            wellEducated: levels.wellEducated
        };
    };
    
    const mismatchStats = calculateEducationMismatch();

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
            
            {/* Education Mismatch Section - Always show this section */}
            <div className={styles.mismatchSection}>
    <div className={styles.sectionTitle}>Education Level Mismatch</div>
    
    {/* Highly Educated mismatch */}
    <div className={styles.mismatchRow}>
        <span className={styles.mismatchLabel}>
            <ColorIndicator color={educationColors.highlyEducated} />
            Highly Educated in lower positions:
        </span>
        <span className={styles.mismatchValue}>
            {formatNumber(mismatchStats.highlyEducated.inLowerJobs)}
            {mismatchStats.highlyEducated.total > 0 && (
                <span className={styles.mismatchPercentage}>
                    {` (${Math.round(mismatchStats.highlyEducated.inLowerJobs / mismatchStats.highlyEducated.total * 100)}%)`}
                </span>
            )}
        </span>
    </div>
    
    {/* Always show the breakdown, just show zeros when none */}
    <div className={styles.mismatchBreakdown}>
        <div className={styles.mismatchSubRow}>
            <span className={styles.mismatchSubLabel}>
                <ColorIndicator color={educationColors.wellEducated} />
                In Well Educated positions:
            </span>
            <span className={styles.mismatchSubValue}>
                {formatNumber(mismatchStats.highlyEducated.inWellEducated)}
            </span>
        </div>
        <div className={styles.mismatchSubRow}>
            <span className={styles.mismatchSubLabel}>
                <ColorIndicator color={educationColors.educated} />
                In Educated positions:
            </span>
            <span className={styles.mismatchSubValue}>
                {formatNumber(mismatchStats.highlyEducated.inEducated)}
            </span>
        </div>
        <div className={styles.mismatchSubRow}>
            <span className={styles.mismatchSubLabel}>
                <ColorIndicator color={educationColors.poorlyEducated} />
                In Poorly Educated positions:
            </span>
            <span className={styles.mismatchSubValue}>
                {formatNumber(mismatchStats.highlyEducated.inPoorlyEducated)}
            </span>
        </div>
        <div className={styles.mismatchSubRow}>
            <span className={styles.mismatchSubLabel}>
                <ColorIndicator color={educationColors.uneducated} />
                In Uneducated positions:
            </span>
            <span className={styles.mismatchSubValue}>
                {formatNumber(mismatchStats.highlyEducated.inUneducated)}
            </span>
        </div>
    </div>
    
    {/* Well Educated mismatch */}
    <div className={styles.mismatchRow}>
        <span className={styles.mismatchLabel}>
            <ColorIndicator color={educationColors.wellEducated} />
            Well Educated in lower positions:
        </span>
        <span className={styles.mismatchValue}>
            {formatNumber(mismatchStats.wellEducated.inLowerJobs)}
            {mismatchStats.wellEducated.total > 0 && (
                <span className={styles.mismatchPercentage}>
                    {` (${Math.round(mismatchStats.wellEducated.inLowerJobs / mismatchStats.wellEducated.total * 100)}%)`}
                </span>
            )}
        </span>
    </div>
    
    {/* Always show the breakdown, just show zeros when none */}
    <div className={styles.mismatchBreakdown}>
        <div className={styles.mismatchSubRow}>
            <span className={styles.mismatchSubLabel}>
                <ColorIndicator color={educationColors.educated} />
                In Educated positions:
            </span>
            <span className={styles.mismatchSubValue}>
                {formatNumber(mismatchStats.wellEducated.inEducated)}
            </span>
        </div>
        <div className={styles.mismatchSubRow}>
            <span className={styles.mismatchSubLabel}>
                <ColorIndicator color={educationColors.poorlyEducated} />
                In Poorly Educated positions:
            </span>
            <span className={styles.mismatchSubValue}>
                {formatNumber(mismatchStats.wellEducated.inPoorlyEducated)}
            </span>
        </div>
        <div className={styles.mismatchSubRow}>
            <span className={styles.mismatchSubLabel}>
                <ColorIndicator color={educationColors.uneducated} />
                In Uneducated positions:
            </span>
            <span className={styles.mismatchSubValue}>
                {formatNumber(mismatchStats.wellEducated.inUneducated)}
            </span>
        </div>
    </div>
</div>
        </div>
    );
}
    
const DistrictLine: FC<DistrictLineProps> = ({ data }) => {
    const { translate } = useLocalization();
    return (
        <div className={styles.row}>
            <div className={styles.nameColumn}>
                {getDisplayName(data.name, translate)}
            </div>
                <div className={styles.householdColumn}>{formatNumber(data.householdCount)} / {formatNumber(data.maxHouseholds)}</div>
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
                maxEmployees: data.maxEmployees
            })}>
                <div className={styles.employeeColumn}>{formatNumber(data.employeeCount)} / {formatNumber(data.maxEmployees)}</div>
            </Tooltip>
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
        </div>
    </div>
);

const AllDistrictsPanel: FC<DraggablePanelProps> = ({ onClose, }) => {
    const districts: District[] = useValue(DistrictData$) ?? [];

    return (
        <Panel
            draggable
            onClose={onClose}
            initialPosition={{ x: 0.038, y: 0.15 }}
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