import { JSXElementConstructor, ReactElement, useState } from 'react';
import { DragEventData, useMouseDragEvents } from '../utils/use-mouse-drag-events';
import { Number2 } from 'cs2/bindings';
import React from 'react';

export interface BetterDragEventData {
  x: number;
  y: number;
  startX: number;
  startY: number;
}
export interface BetterDragHandleProps {
  children: any;
  onDragStart?: (e: BetterDragEventData) => boolean;
  onDrag: (e: BetterDragEventData) => void;
  onDragEnd?: (e: BetterDragEventData) => void;
}

export const BetterDragHandle = ({ children, onDragStart, onDrag, onDragEnd }: BetterDragHandleProps): JSX.Element => {
  const [val, setValue] = useState<Number2>({ x: 0, y: 0 });

  const handleDragStart = (e: MouseEvent): boolean => {
    const g = (x: number, y: number, e: any): Number2 => {
      if (e) {
        const s = e.getBoundingClientRect();
        return {
          x: x - s.left,
          y: y - s.top,
        };
      }
      return val;
    };

    const n = g(e.clientX, e.clientY, e.currentTarget);

    setValue(n);

    if (onDragStart) return onDragStart({ x: e.clientX, y: e.clientY, startX: n.x, startY: n.y });
    else onDrag({ x: e.clientX, y: e.clientY, startX: n.x, startY: n.y });
    return true;
  };

  const handleDragging = (e: DragEventData): void => {
    onDrag({ x: e.clientX, y: e.clientY, startX: val.x, startY: val.y });
    return;
  };

  const handleDragEnd = (e: DragEventData): void => {
    if (onDragEnd) onDragEnd({ x: e.clientX, y: e.clientY, startX: val.x, startY: val.y });
    else onDrag({ x: e.clientX, y: e.clientY, startX: val.x, startY: val.y });
    return;
  };

  const { handleMouseDown } = useMouseDragEvents({
    handleDragStart: handleDragStart,
    handleDragging: handleDragging,
    handleDragEnd: handleDragEnd,
  });

  return (
    <>
      {React.Children.map(children, e => {
        return React.isValidElement(e) ? (
          React.cloneElement(e as any, { onMouseDown: handleMouseDown })
        ) : (
          <div onMouseDown={handleMouseDown}>{e}</div>
        );
      })}
    </>
  );
};
