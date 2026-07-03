import React from 'react';
import { bindLocalValue, useValue } from 'cs2/api';
import { Tooltip } from 'cs2/ui';
import styles from './WorkplacesTableHeader.module.scss';

export const hideColumnsBinding = bindLocalValue(false);

const WorkplacesTableHeader: React.FC<{ translations: any }> = ({ translations }) => {
  const hideColumns = useValue(hideColumnsBinding);
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
      <div className={styles.col3}>
        <Tooltip tooltip={translations?.percentTooltip} direction="down" alignment="center">
          <span>%</span>
        </Tooltip>
      </div>
      <div className={styles.col4}>
        <Tooltip tooltip={translations?.workerTooltip} direction="down" alignment="center">
          <span>Employee</span>
        </Tooltip>
      </div>
      <div className={styles.col5}>
        <Tooltip tooltip={translations?.unemployedTooltip} direction="down" alignment="center">
          <span>Commuter</span>
        </Tooltip>
      </div>
      <div className={styles.col6}>
        <Tooltip tooltip={translations?.unemploymentTooltip} direction="down" alignment="center">
          <span>Open</span>
        </Tooltip>
      </div>
      <div className={styles.col7}>
        <Tooltip tooltip={translations?.outsideTooltip} direction="down" alignment="center">
          <span>Filled</span>
        </Tooltip>
      </div>
      {!hideColumns && (
        <>
          <div className={styles.col8}>
            <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
              <span>Service</span>
            </Tooltip>
          </div>
          <div className={styles.col9}>
            <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
              <span>Commercial</span>
            </Tooltip>
          </div>
          <div className={styles.col10}>
            <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
              <span>Leisure</span>
            </Tooltip>
          </div>
          <div className={styles.col11}>
            <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
              <span>Extractor</span>
            </Tooltip>
          </div>
          <div className={styles.col12}>
            <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
              <span>Industrial</span>
            </Tooltip>
          </div>
          <div className={styles.col13}>
            <Tooltip tooltip={translations?.homelessTooltip} direction="down" alignment="center">
              <span>Office</span>
            </Tooltip>
          </div>
        </>
      )}
    </div>
  );
};

export default WorkplacesTableHeader;
