// $Panel.tsx

import React, { useState, useEffect, useRef } from 'react';

interface PanelProps {
    react: any;
    children: React.ReactNode;
    title: string;
    style?: React.CSSProperties;
    initialPosition?: { top: number; left: number; };
    initialSize?: { width: number; height: number; };
    onPositionChange?: (newPosition: { top: number; left: number; }) => void;
    onSizeChange?: (newSize: { width: number; height: number; }) => void;
    className?: string;
}

const defaultStyle: React.CSSProperties = {
    position: 'absolute',
    // Removed fixed width and height to allow dynamic sizing
};

const Resizer = ({ onMouseDown }: { onMouseDown: (e: React.MouseEvent<HTMLDivElement>) => void }) => {
    const style: React.CSSProperties = {
        position: 'absolute',
        bottom: 0,
        right: 0,
        width: '20px',
        height: '20px',
        cursor: 'nwse-resize',
        zIndex: 10,
    };
    const triangle: React.CSSProperties = {
        width: '100%',
        height: '100%',
        background: 'linear-gradient(45deg, transparent 50%, white 50%)',
    };
    return (
        <div style={style} onMouseDown={onMouseDown}>
            <div style={triangle}></div>
        </div>
    );
};



const $Panel = ({
    react,
    children,
    title,
    style,
    
    initialPosition,
    initialSize,
    onPositionChange = () => {},
    onSizeChange = () => {},
}: PanelProps) => {
    const [position, setPosition] = useState<{ top: number; left: number; }>(
        initialPosition || { top: 100, left: 10 }
    );
    const [size, setSize] = useState<{ width: number; height: number; }>(
        initialSize || { width: 300, height: 600 }
    );

    const initialSizeRef = useRef<{ width: number; height: number; }>({
        width: 0,
        height: 0,
    });
    const [dragging, setDragging] = useState(false);
    const [resizing, setResizing] = useState(false);
    const [rel, setRel] = useState<{ x: number; y: number; }>({ x: 0, y: 0 }); // Position relative to the cursor

    const onMouseDown = (e: React.MouseEvent<HTMLDivElement>) => {
        if (e.button !== 0) return;
        setDragging(true);
        const panelElement = (e.target as HTMLElement).closest(".panel_YqS") as HTMLDivElement;
        const rect = panelElement.getBoundingClientRect();
        setRel({
            x: e.clientX - rect.left,
            y: e.clientY - rect.top,
        });
        e.stopPropagation();
        e.preventDefault();
    };

    const onMouseUp = () => {
        setDragging(false);
        setResizing(false);
        window.removeEventListener('mousemove', onMouseMove);
        window.removeEventListener('mouseup', onMouseUp);
        window.removeEventListener('mousemove', onResizeMouseMove);
    };

    const onMouseMove = (e: MouseEvent) => {
        if (!dragging || resizing) return;

        const newTop = e.clientY - rel.y;
        const newLeft = e.clientX - rel.x;

        const newPosition = {
            top: newTop > 0 ? newTop : 0,
            left: newLeft > 0 ? newLeft : 0,
        };

        setPosition(newPosition);
        onPositionChange(newPosition);
        e.stopPropagation();
        e.preventDefault();
    };

    const onResizeMouseDown = (e: React.MouseEvent<HTMLDivElement>) => {
        setResizing(true);
        initialSizeRef.current = { width: size.width, height: size.height }; // Store initial size
        setRel({ x: e.clientX, y: e.clientY });
        e.stopPropagation();
        e.preventDefault();
    };

    const onResizeMouseMove = (e: MouseEvent) => {
        if (!resizing) return;

        const widthChange = e.clientX - rel.x;
        const heightChange = e.clientY - rel.y;
        const newSize = {
            width: Math.max(initialSizeRef.current.width + widthChange, 200), // Minimum width
            height: Math.max(initialSizeRef.current.height + heightChange, 300), // Minimum height
        };
        setSize(newSize);
        onSizeChange(newSize);
        setRel({ x: e.clientX, y: e.clientY });
        e.stopPropagation();
        e.preventDefault();
    };

    useEffect(() => {
        if (dragging || resizing) {
            window.addEventListener('mousemove', dragging ? onMouseMove : onResizeMouseMove);
            window.addEventListener('mouseup', onMouseUp);
        }

        return () => {
            window.removeEventListener('mousemove', dragging ? onMouseMove : onResizeMouseMove);
            window.removeEventListener('mouseup', onMouseUp);
        };
    }, [dragging, resizing]);

    const draggableStyle: React.CSSProperties = {
        ...defaultStyle,
        ...style,
        top: `${position.top}px`,
        left: `${position.left}px`,
        width: `${size.width}px`,
        height: `${size.height}px`,
        display: 'flex',
        flexDirection: 'column',
        backgroundColor: '#2c3e50', // Ensure dark background
        border: '1px solid #444', // Optional: Add a border for distinction
        borderRadius: '8px', // Optional: Rounded corners
        boxShadow: '0 4px 8px rgba(0, 0, 0, 0.2)', // Optional: Add shadow
    };

    return (
        <div className="panel_YqS" style={draggableStyle}>
            {/* Header */}
            <div
                className="header_H_U header_Bpo child-opacity-transition_nkS"
                onMouseDown={onMouseDown}
                style={{
                    cursor: 'move',
                    padding: '10px',
                    backgroundColor: '#34495e', // Darker header background
                    display: 'flex',
                    justifyContent: 'space-between',
                    alignItems: 'center',
                    borderTopLeftRadius: '8px',
                    borderTopRightRadius: '8px',
                }}
            >
                <div className="title_SVH title_zQN" style={{ color: 'white', fontSize: '16px', fontWeight: 'bold' }}>
                    {title}
                </div>
                
            </div>

            {/* Content */}
            <div
                className="content_XD5 content_AD7 child-opacity-transition_nkS"
                style={{
                    flex: '1 1 auto',
                    padding: '10px',
                    overflow: 'hidden', // Prevent content from overflowing
                    display: 'flex',
                    flexDirection: 'column',
                }}
            >
                {children}
            </div>

            {/* Resizer */}
            <Resizer onMouseDown={onResizeMouseDown} />
        </div>
    );
};

export default $Panel;
