import React, { memo } from 'react';
import classNames from 'classnames';
import styles from './Panel.module.scss';
import { trigger, bindValue, useValue } from 'cs2/api';
import mod from 'mod.json';        
import { PanelProps } from 'cs2/ui';

const PanelStates$ = bindValue<PanelState[]>(mod.id, 'PanelStates');

interface PanelState {
  Id: string;
  Position: { top: number; left: number };
  Size: { width: number; height: number };
}

interface ExtendedPanelProps extends PanelProps {
  id?: string;
  zIndex?: number;
}

const PanelComponent = ({
  children,
  header,
  style,
  className,
  onClose,
  zIndex = 1,
  id = 'default',
  ...props
}: ExtendedPanelProps): JSX.Element => {
  const panelStatesFromCSharp = useValue(PanelStates$);
  const matchedState = panelStatesFromCSharp?.find((st) => st.Id === id);
  
  return (
    <div
      style={{
        position: 'absolute',
        top: matchedState?.Position.top ?? 100,
        left: matchedState?.Position.left ?? 10,
        width: matchedState?.Size.width ?? 300,
        height: matchedState?.Size.height ?? 600,
        backgroundColor: 'var(--panelColorNormal)',
        border: '1px solid #444',
        borderRadius: '8px',
        boxShadow: '0 4px 8px rgba(0, 0, 0, 0.2)',
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
        zIndex,
        ...style,
      }}
      className={classNames(styles.panel, className)}
      {...props}
    >
      <div className={styles.header}>
        {header}
        {onClose && (
          <button
            className={classNames(styles.exitbutton, 'button_bvQ close-button_wKK')}
            onClick={onClose}
            aria-label="Close panel"
          >
            <div
              className="tinted-icon_iKo"
              style={{
                maskImage: 'url(Media/Glyphs/Close.svg)',
                width: 'var(--iconWidth)',
                height: 'var(--iconHeight)',
              }}
            />
          </button>
        )}
      </div>
      <div className={styles.content}>
        {children}
      </div>
    </div>
  );
};

export const Panel = memo(PanelComponent);
export default Panel;
