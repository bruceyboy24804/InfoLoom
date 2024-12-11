import React, { useState, useEffect, useRef, useCallback, FC, memo } from 'react';
import classNames from 'classnames';
import styles from './Panel.module.scss';

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

interface ResizeState {
  isResizing: boolean;
  handle: ResizeHandle | null;
  startSize: Size;
  startPosition: Position;
  startMousePosition: { x: number; y: number };
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

const PanelComponent: FC<PanelProps> = ({
  children,
  title,
  style,
  initialPosition = { top: 100, left: 10 },
  initialSize = { width: 300, height: 600 },
  onPositionChange = () => {},
  onSizeChange = () => {},
  className = '',
  onClose,
  savedPosition,
  savedSize,
  onSavePosition = () => {},
  onSaveSize = () => {},
  zIndex = 1,
  id = 'default',
}) => {
  const getSavedState = useCallback(() => {
    try {
      const savedState = localStorage.getItem(`panel_state_${id}`);
      if (savedState) {
        const { position, size } = JSON.parse(savedState);
        return { position, size };
      }
    } catch (error) {
      console.error('Error loading panel state:', error);
    }
    return null;
  }, [id]);

  const [position, setPosition] = useState<Position>(() => {
    const saved = getSavedState();
    return saved?.position || savedPosition || initialPosition;
  });

  const [size, setSize] = useState<Size>(() => {
    const saved = getSavedState();
    return saved?.size || savedSize || initialSize;
  });

  useEffect(() => {
    try {
      localStorage.setItem(`panel_state_${id}`, JSON.stringify({ position, size }));
    } catch (error) {
      console.error('Error saving panel state:', error);
    }
  }, [position, size, id]);

  const [isDragging, setIsDragging] = useState(false);
  const [dragOffset, setDragOffset] = useState({ x: 0, y: 0 });
  const [resizeState, setResizeState] = useState<ResizeState>({
    isResizing: false,
    handle: null,
    startSize: size,
    startPosition: position,
    startMousePosition: { x: 0, y: 0 },
  });

  const panelRef = useRef<HTMLDivElement>(null);
  const contentRef = useRef<HTMLDivElement>(null);

  const snapToEdge = useCallback((value: number, threshold: number, edge: number): number => {
    if (Math.abs(value - edge) < threshold) {
      return edge;
    }
    return value;
  }, []);

  const handleDragStart = useCallback((e: React.MouseEvent) => {
    if (e.button !== 0) return;
    const rect = panelRef.current?.getBoundingClientRect();
    if (!rect) return;
    setIsDragging(true);
    setDragOffset({
      x: e.clientX - rect.left,
      y: e.clientY - rect.top,
    });
    e.preventDefault();
  }, []);

  const handleResizeStart = useCallback((e: React.MouseEvent, handle: ResizeHandle) => {
    e.preventDefault();
    e.stopPropagation();
    const rect = panelRef.current?.getBoundingClientRect();
    if (!rect) return;
    
    setResizeState({
      isResizing: true,
      handle,
      startSize: { width: rect.width, height: rect.height },
      startPosition: { top: rect.top, left: rect.left },
      startMousePosition: { x: e.clientX, y: e.clientY },
    });
  }, []);

  const handleResizeMove = useCallback((e: MouseEvent) => {
    if (!resizeState.isResizing || !resizeState.handle) return;

    const deltaX = e.clientX - resizeState.startMousePosition.x;
    const deltaY = e.clientY - resizeState.startMousePosition.y;
    let newSize = { ...resizeState.startSize };
    let newPosition = { ...resizeState.startPosition };

    const handleResize: Record<ResizeHandle, () => void> = {
      n: () => {
        const newHeight = Math.max(MIN_SIZE.height, resizeState.startSize.height - deltaY);
        newPosition.top = resizeState.startPosition.top + (resizeState.startSize.height - newHeight);
        newSize.height = newHeight;
      },
      s: () => {
        newSize.height = Math.max(MIN_SIZE.height, resizeState.startSize.height + deltaY);
      },
      e: () => {
        newSize.width = Math.max(MIN_SIZE.width, resizeState.startSize.width + deltaX);
      },
      w: () => {
        const newWidth = Math.max(MIN_SIZE.width, resizeState.startSize.width - deltaX);
        newPosition.left = resizeState.startPosition.left + (resizeState.startSize.width - newWidth);
        newSize.width = newWidth;
      },
      ne: () => {
        handleResize.n();
        handleResize.e();
      },
      nw: () => {
        handleResize.n();
        handleResize.w();
      },
      se: () => {
        handleResize.s();
        handleResize.e();
      },
      sw: () => {
        handleResize.s();
        handleResize.w();
      }
    };

    handleResize[resizeState.handle]();

    // Ensure the panel stays within viewport bounds
    const maxWidth = window.innerWidth - newPosition.left;
    const maxHeight = window.innerHeight - newPosition.top;
    newSize.width = Math.min(newSize.width, maxWidth);
    newSize.height = Math.min(newSize.height, maxHeight);

    setSize(newSize);
    setPosition(newPosition);
    onSizeChange(newSize);
    onPositionChange(newPosition);
  }, [resizeState, onSizeChange, onPositionChange]);

  const handleResizeEnd = useCallback(() => {
    if (resizeState.isResizing) {
      setResizeState((prev: ResizeState) => ({ ...prev, isResizing: false, handle: null }));
      onSaveSize(size);
      onSavePosition(position);
      
      // Save to localStorage after resize
      try {
        localStorage.setItem(`panel_state_${id}`, JSON.stringify({ position, size }));
      } catch (error) {
        console.error('Error saving panel state:', error);
      }
    }
  }, [resizeState.isResizing, size, position, onSaveSize, onSavePosition, id]);

  const handleMouseMove = useCallback((e: MouseEvent) => {
    if (isDragging) {
      const newLeft = e.clientX - dragOffset.x;
      const newTop = e.clientY - dragOffset.y;
      const maxLeft = window.innerWidth - size.width;
      const maxTop = window.innerHeight - size.height;

      // Snap to edges
      const snappedLeft = snapToEdge(newLeft, SNAP_THRESHOLD, 0);
      const snappedTop = snapToEdge(newTop, SNAP_THRESHOLD, 0);
      const snappedRight = snapToEdge(newLeft, SNAP_THRESHOLD, maxLeft);
      const snappedBottom = snapToEdge(newTop, SNAP_THRESHOLD, maxTop);

      const newPosition = {
        left: Math.max(0, Math.min(snappedRight, snappedLeft)),
        top: Math.max(0, Math.min(snappedBottom, snappedTop)),
      };

      setPosition(newPosition);
      onPositionChange(newPosition);
      onSavePosition(newPosition);
      
      // Save to localStorage immediately after position change
      try {
        localStorage.setItem(`panel_state_${id}`, JSON.stringify({ position: newPosition, size }));
      } catch (error) {
        console.error('Error saving panel state:', error);
      }
    }
  }, [isDragging, dragOffset, size, onPositionChange, onSavePosition, snapToEdge, id]);

  const handleMouseUp = useCallback(() => {
    setIsDragging(false);
    handleResizeEnd();
  }, [handleResizeEnd]);

  const handleKeyDown = useCallback((e: React.KeyboardEvent) => {
    if (e.key === 'Escape' && onClose) {
      onClose();
    }
  }, [onClose]);

  useEffect(() => {
    if (isDragging || resizeState.isResizing) {
      window.addEventListener('mousemove', handleMouseMove);
      window.addEventListener('mouseup', handleMouseUp);
      if (resizeState.isResizing) {
        window.addEventListener('mousemove', handleResizeMove);
      }
      return () => {
        window.removeEventListener('mousemove', handleMouseMove);
        window.removeEventListener('mouseup', handleMouseUp);
        if (resizeState.isResizing) {
          window.removeEventListener('mousemove', handleResizeMove);
        }
      };
    }
  }, [isDragging, resizeState.isResizing, handleMouseMove, handleMouseUp, handleResizeMove]);

  return (
    <div
      ref={panelRef}
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
        overflow: 'hidden',
        display: 'flex',
        flexDirection: 'column',
        zIndex,
        ...style,
      }}
      className={classNames(styles.panel, className)}
      onKeyDown={handleKeyDown}
      tabIndex={0}
      role="dialog"
      aria-labelledby="panel-title"
    >
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
            className={classNames(styles.exitbutton, 'button_bvQ button_bvQ close-button_wKK')}
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

      <div
        ref={contentRef}
        className={styles.content}
        role="region"
        aria-label={`${title} content`}
      >
        {children}
      </div>

      {(['n', 'e', 's', 'w', 'ne', 'nw', 'se', 'sw'] as ResizeHandle[]).map(handle => (
        <div
          key={handle}
          className={styles[handle]}
          onMouseDown={(e) => handleResizeStart(e, handle)}
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
    </div>
  );
};

const Panel = memo(PanelComponent);
export default Panel;
