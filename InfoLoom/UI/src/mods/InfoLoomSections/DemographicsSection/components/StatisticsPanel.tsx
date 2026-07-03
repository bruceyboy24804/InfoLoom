import React from 'react';
import { AlignedParagraph } from './AlignedParagraph';
import { Totals } from '../types';
import styles from '../Demographics.module.scss';
import { Localekeys } from 'mods/locale';

interface StatisticsPanelProps {
  totals: number[];
  oldestCitizen: number;
  translate: (key: string, fallback: string) => string | null;
}

export const StatisticsPanel: React.FC<StatisticsPanelProps> = ({ totals, oldestCitizen, translate }) => {
  return (
    <div className={styles.statisticsContainer}>
      <div className={`${styles.statisticsColumn} ${styles.left}`}>
        <AlignedParagraph left={translate(Localekeys.AllCitizens, 'All Citizens')} right={totals[Totals.AllCitizens]} />
        <div className={styles.spacer} />
        <AlignedParagraph left={translate(Localekeys.Tourists, '- Tourists')} right={totals[Totals.Tourists]} />
        <div className={styles.spacer} />
        <AlignedParagraph left={translate(Localekeys.Commuter, '- Commuters')} right={totals[Totals.Commuters]} />
        <div className={styles.spacer} />
        <AlignedParagraph left={translate(Localekeys.MovingAway, '- Moving Away')} right={totals[Totals.MovingAways]} />
        <div className={styles.spacer} />
        <AlignedParagraph left={translate(Localekeys.Population, 'Population')} right={totals[Totals.Locals]} />
      </div>
      <div className={`${styles.statisticsColumn} ${styles.right}`}>
        <AlignedParagraph left={translate(Localekeys.Dead, 'Dead')} right={totals[Totals.DeadCitizens]} />
        <div className={styles.spacer} />
        <AlignedParagraph left={translate(Localekeys.Student, 'Students')} right={totals[Totals.Students]} />
        <div className={styles.spacer} />
        <AlignedParagraph left={translate(Localekeys.Worker, 'Workers')} right={totals[Totals.Workers]} />
        <div className={styles.spacer} />
        <AlignedParagraph left={translate(Localekeys.Homeless, 'Homeless')} right={totals[Totals.HomelessCitizens]} />
        <div className={styles.spacer} />
        <AlignedParagraph left={translate(Localekeys.OldestCitizen, 'Oldest Citizen')} right={oldestCitizen} />
      </div>
    </div>
  );
};
