import React from 'react';
import { AlignedParagraph } from './AlignedParagraph';
import { Totals } from '../types';
import styles from '../Demographics.module.scss';

interface StatisticsPanelProps {
	totals: number[];
	oldestCitizen: number;
	translate: (key: string, fallback: string) => string | null;
}

export const StatisticsPanel: React.FC<StatisticsPanelProps> = ({ totals, oldestCitizen, translate }) => {
	return (
		<div className={styles.statisticsContainer}>
			<div className={`${styles.statisticsColumn} ${styles.left}`}>
				<AlignedParagraph
					left={translate('InfoLoomTwo.DemographicsPanel[StatItem1]', 'All Citizens')}
					right={totals[Totals.AllCitizens]}
				/>
				<div className={styles.spacer} />
				<AlignedParagraph
					left={translate('InfoLoomTwo.DemographicsPanel[StatItem2]', '- Tourists')}
					right={totals[Totals.Tourists]}
				/>
				<div className={styles.spacer} />
				<AlignedParagraph
					left={translate('InfoLoomTwo.DemographicsPanel[StatItem3]', '- Commuters')}
					right={totals[Totals.Commuters]}
				/>
				<div className={styles.spacer} />
				<AlignedParagraph
					left={translate('InfoLoomTwo.DemographicsPanel[StatItem4]', '- Moving Away')}
					right={totals[Totals.MovingAways]}
				/>
				<div className={styles.spacer} />
				<AlignedParagraph
					left={translate('InfoLoomTwo.DemographicsPanel[StatItem5]', 'Population')}
					right={totals[Totals.Locals]}
				/>
			</div>
			<div className={`${styles.statisticsColumn} ${styles.right}`}>
				<AlignedParagraph
					left={translate('InfoLoomTwo.DemographicsPanel[StatItem6]', 'Dead')}
					right={totals[Totals.DeadCitizens]}
				/>
				<div className={styles.spacer} />
				<AlignedParagraph
					left={translate('InfoLoomTwo.DemographicsPanel[StatItem7]', 'Students')}
					right={totals[Totals.Students]}
				/>
				<div className={styles.spacer} />
				<AlignedParagraph
					left={translate('InfoLoomTwo.DemographicsPanel[StatItem8]', 'Workers')}
					right={totals[Totals.Workers]}
				/>
				<div className={styles.spacer} />
				<AlignedParagraph
					left={translate('InfoLoomTwo.DemographicsPanel[StatItem9]', 'Homeless')}
					right={totals[Totals.HomelessCitizens]}
				/>
				<div className={styles.spacer} />
				<AlignedParagraph
					left={translate('InfoLoomTwo.DemographicsPanel[StatItem10]', 'Oldest Citizen')}
					right={oldestCitizen}
				/>
			</div>
		</div>
	);
};
