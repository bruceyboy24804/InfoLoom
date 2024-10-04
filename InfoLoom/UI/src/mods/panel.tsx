import React, { useState, useEffect, useRef } from 'react';
interface PanelProps {
    react: any;  // Adjust the type if you know the specific type
    children: React.ReactNode;  // This is for JSX children
    title: string;
    style?: React.CSSProperties;
    initialPosition?: { top: number; left: number };
    initialSize?: { width: number; height: number };
    onPositionChange?: (newPosition: { top: number; left: number }) => void;
    onSizeChange?: (newSize: { width: number; height: number }) => void;
    className?: string;
}

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
    const contentRef = useRef<HTMLDivElement>(null);  // Reference to the content div
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

        // Calculate maximum allowed positions
        const maxLeft = window.innerWidth - size.width;
        const maxTop = window.innerHeight - size.height;

        // Calculate new positions
        let newLeft = e.clientX - rel.x;
        let newTop = e.clientY - rel.y;

        // Constrain within the viewport
        newLeft = Math.min(Math.max(newLeft, 0), maxLeft);
        newTop = Math.min(Math.max(newTop, 0), maxTop);

        const newPosition = {
            top: newTop,
            left: newLeft,
        };

        setPosition(newPosition);
        onPositionChange(newPosition);
        e.stopPropagation();
        e.preventDefault();
    };

    const onResizeMouseDown = (e: React.MouseEvent<HTMLDivElement>) => {
        if (e.button !== 0) return;
        setResizing(true);
        initialSizeRef.current = { width: size.width, height: size.height }; // Store initial size
        setRel({ x: e.clientX, y: e.clientY });
        e.stopPropagation();
        e.preventDefault();
    };

    const onResizeMouseMove = (e: MouseEvent) => {
        if (!resizing) return;

        // Calculate new size
        const widthChange = e.clientX - rel.x;
        const heightChange = e.clientY - rel.y;
        let newWidth = initialSizeRef.current.width + widthChange;
        let newHeight = initialSizeRef.current.height + heightChange;

        // Set minimum size constraints
        newWidth = Math.max(newWidth, 200); // Minimum width
        newHeight = Math.max(newHeight, 300); // Minimum height

        // Optionally, set maximum size based on viewport
        newWidth = Math.min(newWidth, window.innerWidth - position.left);
        newHeight = Math.min(newHeight, window.innerHeight - position.top);

        const newSize = {
            width: newWidth,
            height: newHeight,
        };
        setSize(newSize);
        onSizeChange(newSize);
        setRel({ x: e.clientX, y: e.clientY });
        e.stopPropagation();
        e.preventDefault();
    };

    // Function to dynamically adjust font size based on panel size
    

    useEffect(() => {
        // Adjust the font size whenever the size changes
        adjustFontSize();
    }, [size]);

    useEffect(() => {
        const handleMouseMove = (e: MouseEvent) => {
            if (dragging) {
                onMouseMove(e);
            } else if (resizing) {
                onResizeMouseMove(e);
            }
        };

        const handleMouseUp = () => {
            onMouseUp();
        };

        if (dragging || resizing) {
            window.addEventListener('mousemove', handleMouseMove);
            window.addEventListener('mouseup', handleMouseUp);
        }

        return () => {
            window.removeEventListener('mousemove', handleMouseMove);
            window.removeEventListener('mouseup', handleMouseUp);
        };
    }, [dragging, resizing]);

    const adjustFontSize = () => {
        if (contentRef.current) {
            const fontSize = Math.max(Math.min(size.width * 0.02, size.height * 0.02), 12);  // Calculate based on both width and height
            contentRef.current.style.fontSize = `${fontSize}px`;
        }
    };

    const draggableStyle: React.CSSProperties = {
        position: 'absolute',
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
                    fontSize: '1.5vw',
                }}
            >
                <div className="title_SVH title_zQN" style={{ color: 'white', fontSize: '1.5vw', fontWeight: 'bold' }}>
                    {title}
                </div>
            </div>

            {/* Content */}
            <div
                ref={contentRef}  // Reference for adjusting font size
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
            <div
                style={{
                    position: 'absolute',
                    bottom: 0,
                    right: 0,
                    width: '20px',
                    height: '20px',
                    cursor: 'nwse-resize',
                    zIndex: 10,
                }}
                onMouseDown={onResizeMouseDown}
            >
                <div style={{ width: '100%', height: '100%', background: 'linear-gradient(45deg, transparent 50%, white 50%)' }}></div>
            </div>
        </div>
    );
};

export default $Panel;
