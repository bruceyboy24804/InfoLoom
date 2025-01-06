import React, {
  useState,
  useEffect,
  useCallback,
  useRef,
  FC,
  memo,
  MouseEvent as ReactMouseEvent,
  KeyboardEvent as ReactKeyboardEvent,
} from 'react';
import classNames from 'classnames';
import styles from './Panel.module.scss';
import { trigger, bindValue, useValue } from 'cs2/api'; 
import mod from 'mod.json';        

// These establishes the binding with C# side. Without C# side game ui will crash.
const PanelStates$ = bindValue<PanelState[]>(mod.id, 'PanelStates');

const SNAP_THRESHOLD = 20;
const MIN_SIZE = { width: 200, height: 200 };

type ResizeHandle = 'n' | 's' | 'e' | 'w' | 'ne' | 'nw' | 'se' | 'sw';

interface Position {
  top: number;
  left: number;
}

interface Size {
  width: number;
  height: number;
}

interface PanelState {
  Id: string,
  Position : Position,
  Size : Size,
}

interface PanelProps {
  children: React.ReactNode;
  title: string;
  style?: React.CSSProperties;
  initialPosition?: Position;
  initialSize?: Size;
  onPositionChange?: (newPosition: Position) => void;
  onSizeChange?: (newSize: Size) => void;
  className?: string;
  onClose?: () => void;
  savedPosition?: Position;
  savedSize?: Size;
  onSavePosition?: (position: Position) => void;
  onSaveSize?: (size: Size) => void;
  zIndex?: number;
  id?: string; 
}


function usePanelState({
  id,
  initialPosition,
  initialSize,
  savedPosition,
  savedSize,
}: {
  id: string;
  initialPosition: Position;
  initialSize: Size;
  savedPosition?: Position;
  savedSize?: Size;
}) {
 
  const getDefaultPosition = useCallback((): Position => {
    return savedPosition || initialPosition;
  }, [savedPosition, initialPosition]);

  
  const getDefaultSize = useCallback((): Size => {
    return savedSize || initialSize;
  }, [savedSize, initialSize]);

  const [position, setPosition] = useState<Position>(getDefaultPosition);
  const [size, setSize] = useState<Size>(getDefaultSize);

  
  const savePanelState = useCallback(
    (newPosition: Position, newSize: Size) => {
      trigger(mod.id, 'SavePanelState',id, newPosition, newSize);
      console.log("savePanelState");
    },
    [id]
  );

  return {
    position,
    setPosition,
    size,
    setSize,
    savePanelState,
  };
}


function useDrag({
  position,
  setPosition,
  size,
  onPositionChange,
  onSavePosition,
  savePanelState,
}: {
  position: Position;
  setPosition: React.Dispatch<React.SetStateAction<Position>>;
  size: Size;
  onPositionChange?: (p: Position) => void;
  onSavePosition?: (p: Position) => void;
  savePanelState: (p: Position, s: Size) => void;
}) {
  const [isDragging, setIsDragging] = useState(false);
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 });

  const snapToEdge = useCallback((value: number, threshold: number, edge: number): number => {
    return Math.abs(value - edge) < threshold ? edge : value;
  }, []);

  const handleDragStart = useCallback(
    (e: ReactMouseEvent<HTMLDivElement>) => {
      // Only allow left-click
      if (e.button !== 0) return;
      e.preventDefault();
      setIsDragging(true);
      setDragOffset({
        x: e.clientX - position.left,
        y: e.clientY - position.top,
      });
    },
    [position]
  );

  const handleMouseMove = useCallback(
    (e: MouseEvent) => {
      if (!isDragging) return;

      const newLeft = e.clientX - dragOffset.x;
      const newTop = e.clientY - dragOffset.y;
      const maxLeft = window.innerWidth - size.width;
      const maxTop = window.innerHeight - size.height;

      // Snap to edges
      const snappedLeft = snapToEdge(newLeft, SNAP_THRESHOLD, 0);
      const snappedTop = snapToEdge(newTop, SNAP_THRESHOLD, 0);
      const snappedRight = snapToEdge(newLeft, SNAP_THRESHOLD, maxLeft);
      const snappedBottom = snapToEdge(newTop, SNAP_THRESHOLD, maxTop);

      const nextPosition = {
        left: Math.max(0, Math.min(snappedRight, snappedLeft)),
        top: Math.max(0, Math.min(snappedBottom, snappedTop)),
      };

      setPosition(nextPosition);
      onPositionChange?.(nextPosition);
      onSavePosition?.(nextPosition);

      
      savePanelState(nextPosition, size);
    },
    [
      isDragging,
      dragOffset,
      size,
      setPosition,
      snapToEdge,
      onPositionChange,
      onSavePosition,
      savePanelState,
    ]
  );

  const handleMouseUp = useCallback(() => {
    setIsDragging(false);
  }, []);

  useEffect(() => {
    if (isDragging) {
      window.addEventListener('mousemove', handleMouseMove);
      window.addEventListener('mouseup', handleMouseUp);
      return () => {
        window.removeEventListener('mousemove', handleMouseMove);
        window.removeEventListener('mouseup', handleMouseUp);
      };
    }
  }, [isDragging, handleMouseMove, handleMouseUp]);

  return { handleDragStart };
}

interface ResizeState {
  isResizing: boolean;
  handle: ResizeHandle | null;
  startSize: Size;
  startPosition: Position;
  startMousePosition: { x: number; y: number };
}

/**
 * Hook that manages resizing logic.
 */
function useResize({
  position,
  setPosition,
  size,
  setSize,
  onPositionChange,
  onSizeChange,
  onSavePosition,
  onSaveSize,
  savePanelState,
}: {
  position: Position;
  setPosition: React.Dispatch<React.SetStateAction<Position>>;
  size: Size;
  setSize: React.Dispatch<React.SetStateAction<Size>>;
  onPositionChange?: (p: Position) => void;
  onSizeChange?: (s: Size) => void;
  onSavePosition?: (p: Position) => void;
  onSaveSize?: (s: Size) => void;
  savePanelState: (p: Position, s: Size) => void;
}) {
  const [resizeState, setResizeState] = useState<ResizeState>({
    isResizing: false,
    handle: null,
    startSize: size,
    startPosition: position,
    startMousePosition: { x: 0, y: 0 },
  });

  const handleResizeStart = useCallback(
    (e: ReactMouseEvent, handle: ResizeHandle) => {
      e.preventDefault();
      e.stopPropagation();

      setResizeState({
        isResizing: true,
        handle,
        startSize: size,
        startPosition: position,
        startMousePosition: { x: e.clientX, y: e.clientY },
      });
    },
    [position, size]
  );

  const performHandleResize = useCallback(
    (handle: ResizeHandle, deltaX: number, deltaY: number) => {
      let newSize = { ...resizeState.startSize };
      let newPos = { ...resizeState.startPosition };

      const constraints = {
        n: () => {
          const nextHeight = Math.max(MIN_SIZE.height, newSize.height - deltaY);
          newPos.top += newSize.height - nextHeight;
          newSize.height = nextHeight;
        },
        s: () => {
          newSize.height = Math.max(MIN_SIZE.height, newSize.height + deltaY);
        },
        e: () => {
          newSize.width = Math.max(MIN_SIZE.width, newSize.width + deltaX);
        },
        w: () => {
          const nextWidth = Math.max(MIN_SIZE.width, newSize.width - deltaX);
          newPos.left += newSize.width - nextWidth;
          newSize.width = nextWidth;
        },
        ne: () => {
          constraints.n();
          constraints.e();
        },
        nw: () => {
          constraints.n();
          constraints.w();
        },
        se: () => {
          constraints.s();
          constraints.e();
        },
        sw: () => {
          constraints.s();
          constraints.w();
        },
      };

      constraints[handle]();

      // Constrain to viewport
      const maxWidth = window.innerWidth - newPos.left;
      const maxHeight = window.innerHeight - newPos.top;
      newSize.width = Math.min(newSize.width, maxWidth);
      newSize.height = Math.min(newSize.height, maxHeight);

      return { newSize, newPos };
    },
    [resizeState.startSize, resizeState.startPosition]
  );

  const handleResizeMove = useCallback(
    (e: MouseEvent) => {
      if (!resizeState.isResizing || !resizeState.handle) return;

      const deltaX = e.clientX - resizeState.startMousePosition.x;
      const deltaY = e.clientY - resizeState.startMousePosition.y;
      const { newSize, newPos } = performHandleResize(resizeState.handle, deltaX, deltaY);

      setSize(newSize);
      setPosition(newPos);
      onSizeChange?.(newSize);
      onPositionChange?.(newPos);
    },
    [
      resizeState,
      performHandleResize,
      setSize,
      setPosition,
      onSizeChange,
      onPositionChange,
    ]
  );

  const handleResizeEnd = useCallback(() => {
    if (!resizeState.isResizing) return;

    setResizeState((prev) => ({ ...prev, isResizing: false, handle: null }));
    // Once done resizing, call any "onSave..." and also `savePanelState`
    onSaveSize?.(size);
    onSavePosition?.(position);
    savePanelState(position, size);
  }, [resizeState.isResizing, position, size, onSaveSize, onSavePosition, savePanelState]);

  useEffect(() => {
    if (resizeState.isResizing) {
      window.addEventListener('mousemove', handleResizeMove);
      window.addEventListener('mouseup', handleResizeEnd);
      return () => {
        window.removeEventListener('mousemove', handleResizeMove);
        window.removeEventListener('mouseup', handleResizeEnd);
      };
    }
  }, [resizeState.isResizing, handleResizeMove, handleResizeEnd]);

  return { handleResizeStart };
}

/**
 * Renders the 8 resize handles (n, e, s, w, ne, nw, se, sw).
 */
interface ResizeHandlesProps {
  onResizeStart: (e: ReactMouseEvent, handle: ResizeHandle) => void;
  classNameMap?: Record<ResizeHandle, string>;
}

const ResizeHandles: FC<ResizeHandlesProps> = memo(({ onResizeStart, classNameMap = {} }) => {
  const handles: ResizeHandle[] = ['n', 'e', 's', 'w', 'ne', 'nw', 'se', 'sw'];
  return (
    <>
      {handles.map((handle) => (
        <div
          key={handle}
          className={classNames(styles[handle], classNameMap[handle])}
          onMouseDown={(e) => onResizeStart(e, handle)}
          style={{
            position: 'absolute',
            ...(handle.includes('n') && { top: 0 }),
            ...(handle.includes('s') && { bottom: 0 }),
            ...(handle.includes('e') && { right: 0 }),
            ...(handle.includes('w') && { left: 0 }),
            width: handle.length === 1 ? '4px' : '12px',
            height: handle.length === 1 ? '4px' : '12px',
            cursor: `${handle}-resize`,
            zIndex: 1000,
          }}
        />
      ))}
    </>
  );
});

/**
 * Main Panel component
 */
const PanelComponent: FC<PanelProps> = ({
  children,
  title,
  style,
  initialPosition = { top: 100, left: 10 },
  initialSize = { width: 300, height: 600 },
  onPositionChange,
  onSizeChange,
  className = '',
  onClose,
  savedPosition,
  savedSize,
  onSavePosition,
  onSaveSize,
  zIndex = 1,
  id = 'default',
}) => {
 // These get the value of the bindings.
  const PanelStates = useValue(PanelStates$);

  
  const {
    position,
    setPosition,
    size,
    setSize,
    savePanelState,
  } = usePanelState({
    id,
    initialPosition,
    initialSize,
    savedPosition,
    savedSize,
  });

  
 
  const { handleDragStart } = useDrag({
    position,
    setPosition,
    size,
    onPositionChange,
    onSavePosition,
    savePanelState,
  });

  
  const { handleResizeStart } = useResize({
    position,
    setPosition,
    size,
    setSize,
    onPositionChange,
    onSizeChange,
    onSavePosition,
    onSaveSize,
    savePanelState,
  });

  
  const handleKeyDown = useCallback(
    (e: ReactKeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose?.();
      }
    },
    [onClose]
  );

  return (
    <div
      ref={useRef<HTMLDivElement>(null)}
      style={{
        position: 'absolute',
        top: position.top,
        left: position.left,
        width: size.width,
        height: size.height,
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
      onKeyDown={handleKeyDown}
      tabIndex={0}
      role="dialog"
      aria-labelledby="panel-title"
    >
      {/* Panel Header */}
      <div
        className={styles.header}
        onMouseDown={handleDragStart}
        role="heading"
        aria-level={1}
        id="panel-title"
      >
        <span>{title}</span>

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

      {/* Panel Content */}
      <div
        ref={useRef<HTMLDivElement>(null)}
        className={styles.content}
        role="region"
        aria-label={`${title} content`}
      >
        {children}
      </div>

      {/* Resize Handles */}
      <ResizeHandles onResizeStart={handleResizeStart} />
    </div>
  );
};

export const Panel = memo(PanelComponent);
export default Panel;
