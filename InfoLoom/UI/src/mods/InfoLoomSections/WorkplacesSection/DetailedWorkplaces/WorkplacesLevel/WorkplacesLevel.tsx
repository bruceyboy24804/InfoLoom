import React, { FC } from 'react';
import { useValue } from 'cs2/api';
import { LocalizedPercentage } from 'cs2/l10n';
import { InfoCheckbox } from 'mods/components/InfoCheckbox/InfoCheckbox';
import { hideColumnsBinding } from '../WorkplacesTableHeader/WorkplacesTableHeader';
import { workplacesInfo } from 'mods/domain/WorkplacesInfo';
import styles from './WorkplacesLevel.module.scss';

export interface WorkplaceLevelProps {
  levelColor?: string;
  levelName: string | null;
  levelValues: workplacesInfo;
  total: number;
  people?: Array<{ id: number; isEmployee: boolean; isCommuter: boolean }>;
}

export const WorkplacesLevel: React.FC<WorkplaceLevelProps> = ({
  levelColor,
  levelName,
  levelValues,
  total,
  people,
}) => {
  const hideColumns = useValue(hideColumnsBinding);

  const counted = new Set<number>();
  let employeeCount = 0;
  let commuterCount = 0;

  if (people) {
    for (const person of people) {
      if (!counted.has(person.id)) {
        if (person.isEmployee) {
          employeeCount++;
        } else if (person.isCommuter) {
          commuterCount++;
        }
        counted.add(person.id);
      }
    }
  } else {
    employeeCount = levelValues.Employee;
    commuterCount = levelValues.Commuter;
  }

  const filledCount = employeeCount + commuterCount;

  return (
    <div className={styles.row}>
      <div className={styles.col1}>
        <div className={styles.colorLegend}>
          <div className={styles.symbol} style={{ backgroundColor: levelColor }} />
          <div className={styles.label}>{levelName}</div>
        </div>
      </div>
      <div className={styles.col2}>
        <span>{levelValues.Total.toLocaleString()}</span>
      </div>
      <div className={styles.col3}>
        <span>
          <LocalizedPercentage value={levelValues.Total} max={total} />
        </span>
      </div>
      <div className={styles.col4}>
        <span>{levelValues.Employee}</span>
      </div>
      <div className={styles.col5}>
        <span>{levelValues.Commuter}</span>
      </div>
      <div className={styles.col6}>
        <span>{levelValues.Open}</span>
      </div>
      <div className={styles.col7}>
        <span>
          <LocalizedPercentage value={Math.min(filledCount, levelValues.Total)} max={levelValues.Total} />
        </span>
      </div>
      {!hideColumns && (
        <>
          <div className={styles.col8}>
            <span>{levelValues.Service}</span>
          </div>
          <div className={styles.col9}>
            <span>{levelValues.Commercial}</span>
          </div>
          <div className={styles.col10}>
            <span>{levelValues.Leisure}</span>
          </div>
          <div className={styles.col11}>
            <span>{levelValues.Extractor}</span>
          </div>
          <div className={styles.col12}>
            <span>{levelValues.Industrial}</span>
          </div>
          <div className={styles.col13}>
            <span>{levelValues.Office}</span>
          </div>
        </>
      )}
    </div>
  );
};

export const HideColumnsToggle: FC = () => {
  const hideColumns = useValue(hideColumnsBinding);
  return (
    <InfoCheckbox
      label="Hide Columns"
      isChecked={hideColumns}
      onToggle={newVal => hideColumnsBinding.update(newVal)}
      className={styles.hideColumnsToggle}
    />
  );
};
