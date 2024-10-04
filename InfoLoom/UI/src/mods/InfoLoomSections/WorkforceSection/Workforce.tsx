import React, { useState } from 'react';
import useDataUpdate from 'mods/use-data-update';
import $Panel from 'mods/panel';
import engine from 'cohtml/cohtml';

// Workforce level component
const WorkforceLevel: React.FC<{
  levelColor: string;
  levelName: string;
  levelValues: {
    total: number;
    worker: number;
    unemployed: number;
    under: number;
    outside: number;
    homeless: number;
  };
  total: number;
}> = ({ levelColor, levelName, levelValues, total }) => {
  const percent = total > 0 ? (100 * levelValues.total / total).toFixed(1) + "%" : "";
  const unemployment = levelValues.total > 0 ? (100 * levelValues.unemployed / levelValues.total).toFixed(1) + "%" : "";

  return (
    <div className="labels_L7Q row_S2v" style={{ width: "99%", paddingTop: "1rem", paddingBottom: "1rem" }}>
      <div style={{ width: "1%" }}></div>
      <div style={{ display: "flex", alignItems: "center", width: "22%" }}>
        <div className="symbol_aAH" style={{ backgroundColor: levelColor, width: "1.2em" }}></div>
        <div>{levelName}</div>
      </div>
      <div className="row_S2v" style={{ width: "11%", justifyContent: "center" }}>
        {levelValues.total}
      </div>
      <div className="row_S2v" style={{ width: "8%", justifyContent: "center" }}>
        {percent}
      </div>
      <div className="row_S2v" style={{ width: "11%", justifyContent: "center" }}>
        {levelValues.worker}
      </div>
      <div className="row_S2v" style={{ width: "12%", justifyContent: "center" }}>
        {levelValues.unemployed}
      </div>
      <div className="row_S2v small_ExK" style={{ width: "8%", justifyContent: "center" }}>
        {unemployment}
      </div>
      <div className="row_S2v small_ExK" style={{ width: "9%", justifyContent: "center" }}>
        {levelValues.under}
      </div>
      <div className="row_S2v small_ExK" style={{ width: "9%", justifyContent: "center" }}>
        {levelValues.outside}
      </div>
      <div className="row_S2v small_ExK" style={{ width: "9%", justifyContent: "center" }}>
        {levelValues.homeless}
      </div>
    </div>
  );
};

// Workforce panel component
const $Workforce = ({ react }: React.ComponentProps<any>) => {
  const [workforce, setWorkforce] = useState<Array<any>>([]);
  useDataUpdate('populationInfo.ilWorkforce', setWorkforce);

  

  const defaultWorkforce = [
    { total: 0, worker: 0, unemployed: 0, under: 0, outside: 0, homeless: 0 },
    { total: 0, worker: 0, unemployed: 0, under: 0, outside: 0, homeless: 0 },
    { total: 0, worker: 0, unemployed: 0, under: 0, outside: 0, homeless: 0 },
    { total: 0, worker: 0, unemployed: 0, under: 0, outside: 0, homeless: 0 },
    { total: 0, worker: 0, unemployed: 0, under: 0, outside: 0, homeless: 0 },
    { total: 0, worker: 0, unemployed: 0, under: 0, outside: 0, homeless: 0 }
  ];

  const workforceData = workforce.length > 0 ? workforce : defaultWorkforce;

  // Header component for better alignment
  const WorkforceHeaders = () => (
    <div className="labels_L7Q row_S2v" style={{ width: "99%", paddingTop: "1rem", paddingBottom: "1rem" }}>
      <div style={{ width: "1%" }}></div>
      <div style={{ display: "flex", alignItems: "center", width: "22%" }}>
        <div>Education</div>
      </div>
      <div className="row_S2v" style={{ width: "11%", justifyContent: "center" }}>
        Total
      </div>
      <div className="row_S2v" style={{ width: "8%", justifyContent: "center" }}>
        %
      </div>
      <div className="row_S2v" style={{ width: "11%", justifyContent: "center" }}>
        Workers
      </div>
      <div className="row_S2v" style={{ width: "12%", justifyContent: "center" }}>
        Unemployed
      </div>
      <div className="row_S2v small_ExK" style={{ width: "8%", justifyContent: "center" }}>
        %
      </div>
      <div className="row_S2v small_ExK" style={{ width: "9%", justifyContent: "center" }}>
        Under
      </div>
      <div className="row_S2v small_ExK" style={{ width: "9%", justifyContent: "center" }}>
        Outside
      </div>
      <div className="row_S2v small_ExK" style={{ width: "9%", justifyContent: "center" }}>
        Homeless
      </div>
    </div>
  );

  return (
    <$Panel
      react={react}
      title="Workforce Structure"
      
      initialSize={{ width: window.innerWidth * 0.33, height: window.innerHeight * 0.20 }}
      initialPosition={{ top: window.innerHeight * 0.05, left: window.innerWidth * 0.005 }}
    >
      {workforce.length === 0 ? (
        <p>Waiting...</p>
      ) : (
        <div>
          <WorkforceHeaders />
          <WorkforceLevel levelColor="#808080" levelName="Uneducated" levelValues={workforceData[0]} total={workforceData[5].total} />
          <WorkforceLevel levelColor="#B09868" levelName="Poorly Educated" levelValues={workforceData[1]} total={workforceData[5].total} />
          <WorkforceLevel levelColor="#368A2E" levelName="Educated" levelValues={workforceData[2]} total={workforceData[5].total} />
          <WorkforceLevel levelColor="#B981C0" levelName="Well Educated" levelValues={workforceData[3]} total={workforceData[5].total} />
          <WorkforceLevel levelColor="#5796D1" levelName="Highly Educated" levelValues={workforceData[4]} total={workforceData[5].total} />
          <WorkforceLevel levelColor="" levelName="TOTAL" levelValues={workforceData[5]} total={0} />
        </div>
      )}
    </$Panel>
  );
};

export default $Workforce;
