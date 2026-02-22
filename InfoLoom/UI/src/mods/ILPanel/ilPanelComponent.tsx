import { MouseEvent, CSSProperties, HTMLAttributes, ReactNode } from 'react';
import { trigger, useValue } from 'cs2/api';
import { useLocalization } from 'cs2/l10n';
import { Button, Icon, Panel } from 'cs2/ui';
import { Number2 } from 'cs2/bindings';

import mod from 'mod.json';
import styles from './ilPanelComponent.module.scss';
import statsSrc from '../../images/Statistics.svg';

export interface ILPanelProps extends HTMLAttributes<HTMLDivElement> {
  header?: ReactNode;
  title?: string;
  onClose?: () => void;
  position?: Number2;
  className?: string;
  panelMovedTrigger?: string;
  onPositionUpdate?: (position: Number2) => void; // Add this
}

export const ILPanel = ({
  header,
  title,
  onClose,
  position,
  className,
  children,
  panelMovedTrigger,
  onPositionUpdate,
  ...props
}: ILPanelProps) => {
  // Define an element ID for each element that needs to be found by ID.
  // The prefix includes the Paradox username and mod name to avoid conflicts with elements from other mods.
  const elementIDPrefix: string = 'bruceyboy24804-infoloom-';
  const elementIDILPanel: string = elementIDPrefix + 'il-panel';
  const elementIDILPanelClose: string = elementIDPrefix + 'il-panel-close';

  // Get panel title.
  const { translate } = useLocalization();
  const panelTitle: string | null = translate(mod.id + '.InfoLoomPanel');

  // Default position if not provided
  const defaultPosition = { x: 100, y: 100 };
  const panelPosition = position || defaultPosition;

  // Verify panel position.
  let verifiedPanelPosition = { x: panelPosition.x, y: panelPosition.y };
  const panel: HTMLElement | null = document.getElementById(elementIDILPanel);
  if (panel) {
    // Prevent any part of panel from going outside the window.
    const panelRect = panel.getBoundingClientRect();
    verifiedPanelPosition = checkPositionOnWindow(panelPosition.x, panelPosition.y, panelRect.width, panelRect.height);

    // Check for any change in panel position.
    if (verifiedPanelPosition.x !== panelPosition.x || verifiedPanelPosition.y !== panelPosition.y) {
      // Move panel to verified position.
      if (onClose) {
        // This would typically trigger a position update in the parent component
        console.log('Panel position adjusted:', verifiedPanelPosition);
      }
    }
  }

  // Set panel to the verified position using a dynamic style.
  const panelStyle: Partial<CSSProperties> = {
    left: verifiedPanelPosition.x + 'px',
    top: verifiedPanelPosition.y + 'px',
  };

  // Function to join classes.
  function joinClasses(...classes: any) {
    return classes.join(' ');
  }

  // Variables for dragging.
  let ilPanelElement: HTMLElement | null = null;
  let relativePositionX: number = 0.0;
  let relativePositionY: number = 0.0;

  // Start dragging.
  // Dragging is initiated by mouse down on the panel header, but it is the whole panel that is moved.
  function onMouseDown(e: MouseEvent<HTMLDivElement, globalThis.MouseEvent>) {
    // Ignore mouse down if other than left mouse button.
    if (e.button !== 0) {
      return;
    }

    // Get close button.
    const closeButton = document.getElementById(elementIDILPanelClose);
    if (closeButton) {
      // Ignore mouse down if over the close button.
      const closeButtonRect = closeButton.getBoundingClientRect();
      if (
        e.clientX >= closeButtonRect.left &&
        e.clientX <= closeButtonRect.left + closeButtonRect.width &&
        e.clientY >= closeButtonRect.top &&
        e.clientY <= closeButtonRect.top + closeButtonRect.height
      ) {
        return;
      }
    }

    // Get panel.
    ilPanelElement = document.getElementById(elementIDILPanel);
    if (ilPanelElement) {
      // Save the position of the mouse relative to the panel.
      const panelRect = ilPanelElement.getBoundingClientRect();
      relativePositionX = e.clientX - panelRect.left;
      relativePositionY = e.clientY - panelRect.top;

      // Add mouse event listeners.
      window.addEventListener('mousemove', onMouseMove);
      window.addEventListener('mouseup', onMouseUp);

      // Stop event propagation.
      e.stopPropagation();
      e.preventDefault();
    }
  }

  // Move the panel while dragging.
  function onMouseMove(e: {
    clientX: number;
    clientY: number;
    stopPropagation: () => void;
    preventDefault: () => void;
  }) {
    // Check if panel is valid.
    if (ilPanelElement) {
      // Compute new panel position based on current mouse position.
      // Adjusting by relative position while dragging keeps the panel in the same location
      // under the pointer as when the panel was originally clicked to start dragging.
      const newPosition = { x: e.clientX - relativePositionX, y: e.clientY - relativePositionY };

      // Prevent any part of panel from going outside the window.
      const panelRect = ilPanelElement.getBoundingClientRect();
      const checkedPosition = checkPositionOnWindow(newPosition.x, newPosition.y, panelRect.width, panelRect.height);

      // Move panel to checked position.
      ilPanelElement.style.left = checkedPosition.x + 'px';
      ilPanelElement.style.top = checkedPosition.y + 'px';

      // Stop event propagation.
      e.stopPropagation();
      e.preventDefault();
    }
  }

  // Ensure element is not outside the window.
  function checkPositionOnWindow(
    positionX: number,
    positionY: number,
    elementWidth: number,
    elementHeight: number
  ): { x: number; y: number } {
    // Check position against left and top.
    if (positionX < 0) {
      positionX = 0.0;
    }
    if (positionY < 0) {
      positionY = 0.0;
    }

    // Check position against right and bottom.
    if (positionX > window.innerWidth - elementWidth) {
      positionX = window.innerWidth - elementWidth;
    }
    if (positionY > window.innerHeight - elementHeight) {
      positionY = window.innerHeight - elementHeight;
    }

    // Return the checked position.
    return { x: positionX, y: positionY };
  }

  // Finish dragging.
  // Finish dragging.
  function onMouseUp(e: { stopPropagation: () => void; preventDefault: () => void }) {
    if (ilPanelElement) {
      window.removeEventListener('mousemove', onMouseMove);
      window.removeEventListener('mouseup', onMouseUp);

      const panelRect = ilPanelElement.getBoundingClientRect();
      const newPosition = { x: panelRect.left, y: panelRect.top };

      // Trigger custom panel moved event if provided
      if (panelMovedTrigger) {
        trigger(mod.id, panelMovedTrigger, newPosition);
      }

      // Call position update function if provided
      if (onPositionUpdate) {
        onPositionUpdate(newPosition);
      }

      e.stopPropagation();
      e.preventDefault();
    }
  }

  // Handle click on close button.
  function onCloseClick() {
    trigger('audio', 'playSound', 'UISound.selectItem', 1);
    if (onClose) {
      onClose();
    }
  }

  // The panel consists of the header and content.
  // The header consists of an image, a div for the title, and a close button.
  return (
    <Panel
      id={elementIDILPanel}
      className={joinClasses(styles.ilPanel, className)}
      style={panelStyle}
      header={
        header || (
          <div className={styles.ilPanelHeader} onMouseDown={e => onMouseDown(e)}>
            <Icon className={styles.ilPanelIcon} src={statsSrc} />
            <div className={styles.ilPanelHeaderTitle}>{title || panelTitle || 'InfoLoom Panel'}</div>
            <Button id={elementIDILPanelClose} className={styles.ilPanelHeaderClose} onClick={() => onCloseClick()} variant="round">
              <Icon src='Media/Glyphs/Close.svg' tinted className={styles.ilPanelCloseIcon}/>
            </Button>
          </div>
        )
      }
    >
      {children}
    </Panel>
  );
};
