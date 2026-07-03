import React, { FC } from 'react';
import { bindValue, useValue } from 'cs2/api';
import { minimizedBinding, toggleMinimize } from 'mods/bindings';
import { getModule } from 'cs2/modding';
import { DraggablePanelProps, Panel, Button, Icon } from 'cs2/ui';
import styles from './Workforce.module.scss';
import { useLocalization } from 'cs2/l10n';
import mod from 'mod.json';
import classNames from 'classnames';
import minimizeIcon from './../../../images/minimize.svg';
import { DetailedWorkforceMain } from './DetailedWorkforce/DetailedWorkforceMain/DeailedWorkforceMain';
import { SimplifiedWorkforceMain } from './SimplifiedWorkforce';

const ShowExtraWorkforce = bindValue<number>(mod.id, 'ShowExtraWorkforce', 0);
const roundButtonHighlightStyle = getModule(
  'game-ui/common/input/button/themes/round-highlight-button.module.scss',
  'classes'
);

const Workforce: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const { translate } = useLocalization();
  const minimized = useValue(minimizedBinding);
  const showExtraWorkforce = useValue(ShowExtraWorkforce);
  const stopPanelDragFromControl = (e: React.SyntheticEvent) => {
    e.stopPropagation();
  };

  const panelWidthClass =
    showExtraWorkforce == 1
      ? styles.panel1
      : showExtraWorkforce == 2
        ? styles.panel2
        : showExtraWorkforce == 3
          ? styles.panel3
          : showExtraWorkforce == 4
            ? styles.panel4
            : showExtraWorkforce == 5
              ? styles.panel5
              : showExtraWorkforce == 6
                ? styles.panel6
                : showExtraWorkforce == 7
                  ? styles.panel7
                  : '';

  return (
    <Panel
      draggable={true}
      onClose={onClose}
      initialPosition={{ x: 0.71, y: 0.7 }}
      className={classNames(styles.panel, minimized ? styles.panelMinimized : panelWidthClass)}
      header={
        <div className={styles.header}>
          <div className={styles.headerText}>{translate('InfoLoomTwo.WorkforcePanel[Title]', 'Workforce')}</div>
          <div
            className={styles.headerControls}
            onMouseDown={stopPanelDragFromControl}
            onPointerDown={stopPanelDragFromControl}
          >
            <Button
              variant="icon"
              onSelect={toggleMinimize}
              className={classNames(roundButtonHighlightStyle.button, styles.buttonStyle)}
            >
              <Icon src={'coui://il/minimize.svg'} tinted className={styles.headerIcon} />
            </Button>
          </div>
        </div>
      }
    >
      {minimized ? <SimplifiedWorkforceMain /> : <DetailedWorkforceMain />}
    </Panel>
  );
};
export default Workforce;
