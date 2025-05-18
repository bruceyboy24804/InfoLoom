import React, { FC, useEffect, useState } from 'react';
import { useValue, trigger } from 'cs2/api';
import { BasicButton } from 'mods/base.scss';
import { Tooltip, Panel, DraggablePanelProps, Button, Dropdown, DropdownToggle, DropdownItem } from 'cs2/ui';
import {
  formatWords,
  formatNumber,
  formatPercentage2
} from 'mods/InfoLoomSections/utils/formatText';
import {
  BuildingHappinessFactor,
  compareHappinessFactors,
  ResidentialHouseholdData,
} from '../../../domain/ResidentialHouseholdData';
import styles from './ResidentialHousehold.module.scss';
import { ResidentialDataDebug, RoadNames, FilteredResidentialData, selectRoad, clearRoadFilter, DiscoverCurrentPage$, DiscoverMaxPages$, setDiscoverPage, setDiscoverMaxPages } from "mods/bindings";
import { Entity } from 'cs2/utils';
import mod from "mod.json";
import { Pagination } from 'mods/components/Pagination/Pagination';
import {clearfilter} from "images/findit_filterX.svg";
import { Theme } from "cs2/bindings";
import {getModule} from "cs2/modding"

const ITEMS_PER_PAGE = 15;
const ROADS_PER_PAGE = 10;
const DropdownStyle: Theme | any = getModule("game-ui/menu/themes/dropdown.module.scss", "classes");

const DataDivider: FC = () => (
  <div className={styles.dataDivider} />
);

const focusEntity = (e: Entity) => {
  trigger("camera", "focusEntity", e);
};

interface HappinessTooltipProps {
  household: ResidentialHouseholdData;
}

const HappinessTooltip: FC<HappinessTooltipProps> = ({ household }) => {
  return (
    <div className={styles.tooltipContent}>
      <div className={styles.tooltipText}>
        <p>Factors affecting happiness:</p>
        {household.HappinessFactors && household.HappinessFactors.sort(compareHappinessFactors).map((factor, index) => (
          <div key={index} className={styles.factorRow}>
            <span className={styles.factorName}>{formatWords(BuildingHappinessFactor[factor.Factor])} </span>
            <span className={
              factor.Value > 0 ? styles.positive :
                factor.Value < 0 ? styles.negative :
                  styles.neutral
            }>
              {factor.Value > 0 ? '+' : ''}{formatPercentage2(factor.Value)}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
};

interface HouseholdRowProps {
  household: ResidentialHouseholdData;
}

const HouseholdRow: FC<HouseholdRowProps> = ({ household }) => {
  const handleNavigate = () => {
    trigger(mod.id, "GoTo", household.ResidentialEntity);
  };

  return (
    <div className={styles.row}>
      <div className={styles.nameColumn}>
        {household.ResidentialName}
      </div>
      <div className={styles.iconColumn}>
        <img src={household.ResidentialIcon} alt={""} className={styles.magnifierIcon} />
      </div>
      <div className={styles.employeeColumn}>
        {`${household.CurrentHouseholdCount}/${formatNumber(household.MaxHouseholdCount)}`}
      </div>
      <Tooltip tooltip={<HappinessTooltip household={household} />}>
        <div className={styles.happinessColumn}>
          {household.OverallHappiness.key} <img src={household.OverallHappiness.iconPath} alt={household.OverallHappiness.key} className={styles.happinessImage} />
        </div>
      </Tooltip>
      <div className={styles.locationColumn}>
        <Button
          variant={"icon"}
          src={"Media/Game/Icons/MapMarker.svg"}
          onSelect={() => focusEntity(household.ResidentialEntity)}
          className={styles.magnifierIcon}
        />
      </div>
    </div>
  );
};

const TableHeader: FC = () => {
  return (
    <div className={styles.tableHeader}>
      <div className={styles.headerRow}>
        <div className={styles.nameColumn}><b>Household Address</b></div>
        <div className={styles.iconColumn}><b>Zone Type</b></div>
        <Tooltip tooltip="Current households vs maximum household capacity">
          <div className={styles.employeeColumn}><b>Households</b></div>
        </Tooltip>
        <Tooltip tooltip="Overall happiness of the household">
          <div className={styles.happinessColumn}><b>Happiness</b></div>
        </Tooltip>
        <Tooltip tooltip="Location of the household">
          <div className={styles.locationColumn}><b>Location</b></div>
        </Tooltip>
      </div>
    </div>
  );
};

const ResidentialHousehold: FC<DraggablePanelProps> = ({ onClose }) => {
  const residentialData = useValue(FilteredResidentialData);
  const roadNames = useValue(RoadNames);
  const currentPage = useValue(DiscoverCurrentPage$);
  const maxPages = useValue(DiscoverMaxPages$);
  const [selectedRoad, setSelectedRoad] = useState("");
  const [dropdownPage, setDropdownPage] = useState(1);

  // Load all households by default
  useEffect(() => {
    clearRoadFilter();
  }, []);

  // Calculate dropdown pagination
  const totalDropdownPages = Math.ceil(roadNames.length / ROADS_PER_PAGE);
  const startIndexDropdown = (dropdownPage - 1) * ROADS_PER_PAGE;
  const displayedRoadNames = roadNames.slice(
    startIndexDropdown,
    startIndexDropdown + ROADS_PER_PAGE
  );

  // Update max pages when data changes
  useEffect(() => {
    const totalPages = Math.max(1, Math.ceil(residentialData.length / ITEMS_PER_PAGE));
    setDiscoverMaxPages(totalPages);

    // Reset to first page when filter changes
    if (currentPage > totalPages) {
      setDiscoverPage(1);
    }
  }, [residentialData.length]);

  // Get current page items
  const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
  const currentItems = residentialData.slice(startIndex, startIndex + ITEMS_PER_PAGE);

  // Handle road selection
  const handleRoadSelect = (roadName: string) => {
    setSelectedRoad(roadName);
    selectRoad(roadName);
  };

  // Handle filter clearing
  const handleClearFilter = () => {
    setSelectedRoad("");
    clearRoadFilter();
  };

  // Handle dropdown pagination
  const handlePreviousPage = (e: React.MouseEvent) => {
    e.stopPropagation();
    setDropdownPage(prev => Math.max(1, prev - 1));
  };

  const handleNextPage = (e: React.MouseEvent) => {
    e.stopPropagation();
    setDropdownPage(prev => Math.min(totalDropdownPages, prev + 1));
  };

  return (
    <Panel
      draggable
      onClose={onClose}
      initialPosition={{ x: 0.50, y: 0.50 }}
      className={styles.panel}
      header={<div className={styles.header}><span className={styles.headerText}>Households</span></div>}
    >
      <div className={styles.filterContainer}>
        <Dropdown
          theme={DropdownStyle}
          content={
            <>
              {totalDropdownPages > 1 && dropdownPage > 1 && (
                <div className={styles.paginationItem} onClick={handlePreviousPage}>
                  ← Previous Page
                </div>
              )}

              {displayedRoadNames.map((roadName, index) => (
                <DropdownItem
                  key={index}
                  value={roadName}
                  closeOnSelect={true}
                  onChange={() => handleRoadSelect(roadName)}
                  selected={selectedRoad === roadName}
                  className={DropdownStyle.dropdownItem}
                >
                  <div className={styles.dropdownName}>{roadName}</div>
                </DropdownItem>
              ))}

              {totalDropdownPages > 1 && dropdownPage < totalDropdownPages && (
                <div className={styles.paginationItem} onClick={handleNextPage}>
                  Next Page →
                </div>
              )}

              {totalDropdownPages > 1 && (
                <div className={styles.pageIndicator}>
                  Page {dropdownPage} of {totalDropdownPages}
                </div>
              )}
            </>
          }
        >
          <DropdownToggle disabled={roadNames.length === 0}>
            <div className={styles.dropdownName}>
              {selectedRoad ? selectedRoad : "Filter by Road (Optional)"}
            </div>
          </DropdownToggle>
        </Dropdown>

        {selectedRoad && (
          <Button
            className={styles.clearFilterButton}
            onSelect={handleClearFilter}
          >
            <img src={clearfilter} alt="Clear Filter" className={styles.clearFilterIcon} />
          </Button>
        )}

        <div className={styles.resultCount}>
          Showing {residentialData.length} households
          {selectedRoad && ` on ${selectedRoad}`}
        </div>
      </div>

      {!roadNames.length ? (
        <p className={styles.loadingText}>Please wait...(dont forget to unpause)</p>
      ) : !residentialData.length ? (
        <div className={styles.noResultsMessage}>
          <p>No households found</p>
        </div>
      ) : (
        <div>
          <TableHeader />
          <DataDivider />
          <div className={styles.tableContainer}>
            {currentItems.map((household, index) => (
              <HouseholdRow key={startIndex + index} household={household} />
            ))}
          </div>
          <DataDivider />
          <div className={styles.paginationContainer}>
            <Pagination setPage={setDiscoverPage} />
          </div>
        </div>
      )}
    </Panel>
  );
};

export default ResidentialHousehold;