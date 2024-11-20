// React and Core
import React, { FC, useCallback, useState, useMemo, memo } from 'react';

// UI Components
import { 
    FormattedText, 
    PanelSection, 
    InfoSection 
} from "cs2/ui";
import $Panel from 'mods/panel';

// Game Bindings and Utils
import {
    type Entity,
    selectedInfo,
    toolbarBottom,
} from "cs2/bindings";
import { useValue, bindValue } from "cs2/api";
import useDataUpdate from 'mods/use-data-update';

// Styles and Config
import styles from './Districts.module.scss';
import mod from "mod.json";

// Types
interface DistrictsProps {
    onClose: () => void;
}

interface District {
    index: number;
    name: string;
    population?: number;
    buildings?: number;
    services?: number;
}

// Constants
const DISTRICT_BINDING = bindValue<string>("districts", "ilDistricts", "");

// Sub-Components
const DistrictTableHeader: FC = () => (
    <thead>
        <tr>
            <th>Name</th>
            <th>Population</th>
            <th>Buildings</th>
            <th>Services</th>
        </tr>
    </thead>
);

const DistrictTableRow: FC<{ district: District }> = ({ district }) => (
    <tr>
        <td>{district.name}</td>
        <td>{district.population || '-'}</td>
        <td>{district.buildings || '-'}</td>
        <td>{district.services || '-'}</td>
    </tr>
);

const DistrictTable: FC<{ districts: District[] }> = ({ districts }) => (
    <table className={styles.districtsTable}>
        <DistrictTableHeader />
        <tbody>
            {districts.length === 0 ? (
                <tr>
                    <td colSpan={4} className={styles.emptyMessage}>
                        No districts available
                    </td>
                </tr>
            ) : (
                districts.map(district => (
                    <DistrictTableRow 
                        key={district.index} 
                        district={district} 
                    />
                ))
            )}
        </tbody>
    </table>
);

// Utility Functions
const parseDistrictData = (): District[] => {
    const districtIds = useValue(DISTRICT_BINDING).split(",").filter(Boolean);
    
    return districtIds.map(id => {
        const entityIndex = Number.parseInt(id);
        const entity: Entity = { index: entityIndex, version: 1 };
        
        // Get district info
        selectedInfo.selectEntity(entity);
        const districtInfo = selectedInfo.selectedEntity$.value;
        selectedInfo.clearSelection();
        
        return {
            index: entityIndex,
            name: `District ${entityIndex}`,
            population: useValue(toolbarBottom.population$),
        };
    });
};

// Main Component
const Districts: FC<DistrictsProps> = ({ onClose }) => {
    const [districtIndices, setDistrictIndices] = useState<string>("");
    
    useDataUpdate('districts.ilDistricts', setDistrictIndices);
    
    const districts = useMemo(() => {
        if (!districtIndices) return [];
        return parseDistrictData();
    }, [districtIndices]);

    return (
        <$Panel
            title="Districts"
            onClose={onClose}
            initialSize={{ width: window.innerWidth * 0.25, height: window.innerHeight * 0.4 }}
            initialPosition={{ top: window.innerHeight * 0.1, left: window.innerWidth * 0.1 }}
        >
            <div className={styles.districtsContainer}>
                <PanelSection>
                    <InfoSection>
                        <FormattedText text="Districts" />
                        <DistrictTable districts={districts} />
                    </InfoSection>
                </PanelSection>
            </div>
        </$Panel>
    );
};

export default memo(Districts);