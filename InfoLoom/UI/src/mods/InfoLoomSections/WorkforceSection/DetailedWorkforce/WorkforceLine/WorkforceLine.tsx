import React, { FC } from 'react';
import { LocalizedPercentage } from 'cs2/l10n';
import { workforceInfo } from '../../../../domain/workforceInfo';
import styles from './WorkforceLine.module.scss';

interface WorkforceLineProps {
  levelColor?: string;
  levelName: string | null;
  levelValues: workforceInfo;
  total: number;
  useOverallTotalForUnemployment?: boolean;
  unemploymentOverride?: number;
}
export const WorkforceLine: React.FC<WorkforceLineProps> = ({
  levelColor,
  levelName,
  levelValues,
  total,
  useOverallTotalForUnemployment = false,
  unemploymentOverride,
}) => {
  const denominator = useOverallTotalForUnemployment ? total : levelValues.Total;

  const percent = total > 0 ? ((100 * levelValues.Total) / total).toFixed(1) + '%' : '';
  const unemploymentValue = typeof unemploymentOverride === 'number' ? unemploymentOverride : levelValues.Unemployed;

  const unemploymentMax =
    typeof unemploymentOverride === 'number'
      ? 100 // unemploymentOverride is already a percentage value
      : denominator;

  return (
    <div className={styles.row_S2v}>
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
        <span>{levelValues.Worker}</span>
      </div>

      <div className={styles.col5}>
        <span>{levelValues.Unemployed}</span>
      </div>

      <div className={styles.col6}>
        <span>{levelValues.UnemploymentRate.toFixed(1)}%</span>
      </div>

      <div className={styles.col7}>
        <span>{levelValues.Under.toLocaleString()}</span>
      </div>

      <div className={styles.col8}>
        <span>{levelValues.Outside.toLocaleString()}</span>
      </div>

      <div className={styles.col9}>
        <span>{levelValues.Homeless.toLocaleString()}</span>
      </div>
    </div>
  );
};
