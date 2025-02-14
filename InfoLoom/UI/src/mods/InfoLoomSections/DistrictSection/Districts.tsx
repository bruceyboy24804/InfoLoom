import React from "react";
import { Panel, Scrollable, DraggablePanelProps, Number2 } from "cs2/ui";
import { bindValue, useValue } from "cs2/api";
import { Entity, Name, CustomName, LocalizedName, FormattedName, ResidentsSection } from "cs2/bindings";
import { useLocalization } from "cs2/l10n";
import mod from "mod.json";
import styles from "./Districts.module.scss";

export interface DistrictOutputData {
  name: Name;
  residentCount: number;
  petCount: number;
  householdCount: number;
  maxHouseholds: number;
  entity: Entity;
}

const DistrictList$ = bindValue<DistrictOutputData[]>("ilDistrictData", "ilDistricts", []);

const DataDivider = (): JSX.Element => (
  <div style={{ display: "flex", height: "4rem", flexDirection: "column", justifyContent: "center" }}>
    <div style={{ borderBottom: "1px solid gray", width: "100%" }}></div>
  </div>
);

const AllDistrictsPanel = ({ onClose, initialPosition }: DraggablePanelProps): JSX.Element => {
  const { translate } = useLocalization();
  const districts = useValue(DistrictList$);
  const defaultPos: Number2 = { x: 0.038, y: 0.15 };
  const initPos = initialPosition || defaultPos;

  const getDisplayName = (name: Name, translate: (id: string, fallback?: (string | null)) => (string | null)): string => {
    if (!name) return "";
    if (typeof name === "string") return name;
    const type = (name as any).__Type;
    if (typeof type === "undefined") return name.toString();
    if (type === "names.CustomName") {
      return (name as CustomName).name;
    } else if (type === "names.LocalizedName") {
      const key = (name as LocalizedName).nameId;
      return translate(key) || key;
    } else if (type === "names.FormattedName") {
      const formatted = (name as FormattedName).nameId;
      const args = (name as FormattedName).nameArgs;
      return formatted + (args ? " " + JSON.stringify(args) : "");
    }
    return name.toString();
  };

  const DistrictDataLevel = ({ levelValues }: { levelValues: DistrictOutputData }): JSX.Element => (
    <div className="labels_L7Q row_S2v" style={{ width: "100%", padding: "1rem 25rem", display: "flex", alignItems: "center", boxSizing: "border-box" }}>
      <div style={{ flex: "0 0 15%", paddingRight: "1rem", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
        {levelValues.entity.index}
      </div>
      <div style={{ flex: "0 0 15%", textAlign: "center" }}>
        {getDisplayName(levelValues.name, translate)}
      </div>
      <div style={{ flex: "0 0 15%", textAlign: "center" }}>
        {levelValues.householdCount}
      </div>
      <div style={{ flex: "0 0 15%", textAlign: "center" }}>
        {levelValues.maxHouseholds}
      </div>
      <div style={{ flex: "0 0 15%", textAlign: "center" }}>
        {levelValues.petCount}
      </div>
      <div style={{ flex: "0 0 15%", textAlign: "center" }}>
        {levelValues.residentCount}
      </div>
    </div>
  );

  const handleDrag = (position: Number2) => {
    console.log("New position:", position);
  };

  return (
    <Panel
      draggable={true}
      onClose={onClose}
      initialPosition={initPos}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>Districts</span>
        </div>
      }
    >
      {(!districts || districts.length === 0) ? (
        <p className={styles.loadingText}>No Districts Found</p>
      ) : (
        <div>
          <div style={{ maxWidth: "1200px", margin: "0 auto", padding: "0 25rem" }}>
            <div className="labels_L7Q row_S2v" style={{ width: "100%", padding: "1rem 0", display: "flex", alignItems: "center" }}>
              <div style={{ flex: "0 0 15" }}>
                <div><b>District index</b></div>
              </div>
              <div style={{ flex: "0 0 15%", textAlign: "center" }}>
                <b>District name</b>
              </div>
              <div style={{ flex: "0 0 15%", textAlign: "center" }}>
                <b>Households</b>
              </div>
              <div style={{ flex: "0 0 15%", textAlign: "center" }}>
                <b>Max households</b>
              </div>
              <div style={{ flex: "0 0 15%", textAlign: "center" }}>
                <b>Pets</b>
              </div>
              <div style={{ flex: "0 0 15%", textAlign: "center" }}>
                <b>Residents</b>
              </div>
            </div>
          </div>
          <DataDivider />
          <div style={{ padding: "1rem 0" }}>
            <Scrollable smooth vertical trackVisibility="scrollable">
              {districts.map((district, index) => (
                <DistrictDataLevel key={index} levelValues={district} />
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