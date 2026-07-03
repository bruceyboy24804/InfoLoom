import { Tooltip } from 'cs2/ui';
import styles from './Tableheader.module.scss';
import { bindValue } from 'cs2/api';
import mod from 'mod.json';

const ShowExtraWorkforce = bindValue<number>(mod.id, 'ShowExtraWorkforce', 0);

export const WorkforceTableHeader: React.FC<{ translations: any }> = ({ translations }) => {
  const value = ShowExtraWorkforce.value;

  return (
    <div className={styles.headerRow}>
      <div className={styles.col1}>
        <span>Education</span>
      </div>
      <div className={styles.col2}>
        <Tooltip tooltip={translations?.totalTooltip} direction="down" alignment="center">
          <span>Total</span>
        </Tooltip>
      </div>
      {value < 7 && (
        <div className={styles.col3}>
          <Tooltip tooltip={translations?.percentTooltip} direction="down" alignment="center">
            <span>%</span>
          </Tooltip>
        </div>
      )}
      {value < 6 && (
        <div className={styles.col4}>
          <Tooltip tooltip={translations?.workerTooltip} direction="down" alignment="center">
            <span>Worker</span>
          </Tooltip>
        </div>
      )}
      {value < 5 && (
        <div className={styles.col5}>
          <Tooltip tooltip={translations?.unemployedTooltip} direction="down" alignment="center">
            <span>Unemployed</span>
          </Tooltip>
        </div>
      )}
      {value < 4 && (
        <div className={styles.col6}>
          <Tooltip tooltip={translations?.unemploymentTooltip} direction="down" alignment="center">
            <span>%</span>
          </Tooltip>
        </div>
      )}
      {value < 3 && (
        <div className={styles.col7}>
          <Tooltip tooltip={translations?.underTooltip} direction="down" alignment="center">
            <span>Under</span>
          </Tooltip>
        </div>
      )}
      {value < 2 && (
        <div className={styles.col8}>
          <Tooltip tooltip={translations?.outsideTooltip} direction="down" alignment="center">
            <span>Outside</span>
          </Tooltip>
        </div>
      )}
      {value < 1 && (
        <div className={styles.col9}>
          <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
            <span>Homeless</span>
          </Tooltip>
        </div>
      )}
    </div>
  );
};
