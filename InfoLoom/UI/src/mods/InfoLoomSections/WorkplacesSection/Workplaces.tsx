import React, { FC } from 'react';
import { useValue } from 'cs2/api';
import { workplacesMinimizedBinding, toggleWorkplacesMinimize } from 'mods/bindings';
import { DraggablePanelProps, Panel, Button, Icon } from 'cs2/ui';
import { getModule } from 'cs2/modding';
import { useLocalization } from 'cs2/l10n';
import classNames from 'classnames';
import styles from './Workplaces.module.scss';
import minimizeIcon from './../../../images/minimize.svg';
import { DetailedWorkplacesMain } from './DetailedWorkplaces/DetailedWorkplacesMain/DetailedWorkplacesMain';
import { SimplifiedWorkplacesMain } from './SimplifiedWorkplaces';

const roundButtonHighlightStyle = getModule(
  'game-ui/common/input/button/themes/round-highlight-button.module.scss',
  'classes'
);

const Workplaces: FC<DraggablePanelProps> = ({ onClose, initialPosition }) => {
  const { translate } = useLocalization();
  const minimized = useValue(workplacesMinimizedBinding);

  const stopPanelDragFromControl = (e: React.SyntheticEvent) => {
    e.stopPropagation();
  };

  return (
    <Panel
      draggable={true}
      onClose={onClose}
      initialPosition={{ x: 0.038, y: 0.15 }}
      className={classNames(styles.panel, minimized ? styles.panelMinimized : '')}
      header={
        <div className={styles.header}>
          <div className={styles.headerText}>{translate('InfoLoomTwo.WorkplacesPanel[Title]', 'Workplaces')}</div>
          <div
            className={styles.headerControls}
            onMouseDown={stopPanelDragFromControl}
            onPointerDown={stopPanelDragFromControl}
          >
            <Button
              variant="icon"
              onSelect={toggleWorkplacesMinimize}
              className={classNames(roundButtonHighlightStyle.button, styles.buttonStyle)}
            >
              <Icon src={'coui://il/minimize.svg'} tinted className={styles.headerIcon} />
            </Button>
          </div>
        </div>
      }
    >
      {minimized ? <SimplifiedWorkplacesMain /> : <DetailedWorkplacesMain />}
    </Panel>
  );
};

export default Workplaces;
