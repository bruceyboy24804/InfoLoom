import React, { useState, useEffect, useRef, useCallback, FC } from 'react';
import styles from './Panel.module.scss'; // Assuming you have a CSS module for styling

interface PanelProps {
    children: React.ReactNode;
    title: string;
    style?: React.CSSProperties;
    initialPosition?: { top: number; left: number };
    initialSize?: { width: number; height: number };
    onPositionChange?: (newPosition: { top: number; left: number }) => void;
    onSizeChange?: (newSize: { width: number; height: number }) => void;
    className?: string;
}

type InteractionState = 'none' | 'dragging' | 'resizing';

const Panel: FC<PanelProps> = ({
    children,
    title,
    style,
    initialPosition = { top: 100, left: 10 },
    initialSize = { width: 300, height: 600 },
    onPositionChange = () => {},
    onSizeChange = () => {},
    className = '',
}) => {
    // State for position and size
    const [position, setPosition] = useState(initialPosition);
    const [size, setSize] = useState(initialSize);

    // State for interaction (dragging/resizing) and relative cursor position
    const [interaction, setInteraction] = useState<{
        state: InteractionState;
        rel?: { x: number; y: number };
        initialSize?: { width: number; height: number };
    }>({ state: 'none' });

    // Refs
    const panelRef = useRef<HTMLDivElement>(null);
    const contentRef = useRef<HTMLDivElement>(null);

    // Handler for mouse down on header (start dragging)
    const handleDragMouseDown = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
        if (e.button !== 0) return; // Only left mouse button
        const rect = panelRef.current?.getBoundingClientRect();
        if (!rect) return;
        setInteraction({
            state: 'dragging',
            rel: { x: e.clientX - rect.left, y: e.clientY - rect.top },
        });
        e.stopPropagation();
        e.preventDefault();
    }, []);

    // Handler for mouse down on resizer (start resizing)
    const handleResizeMouseDown = useCallback((e: React.MouseEvent<HTMLDivElement>) => {
        if (e.button !== 0) return; // Only left mouse button
        setInteraction({
            state: 'resizing',
            rel: { x: e.clientX, y: e.clientY },
            initialSize: { ...size },
        });
        e.stopPropagation();
        e.preventDefault();
    }, [size]);

    // Unified mouse move handler
    const handleMouseMove = useCallback((e: MouseEvent) => {
        if (interaction.state === 'dragging') {
            const newLeft = e.clientX - (interaction.rel?.x || 0);
            const newTop = e.clientY - (interaction.rel?.y || 0);

            // Constrain within viewport
            const maxLeft = window.innerWidth - size.width;
            const maxTop = window.innerHeight - size.height;

            const clampedLeft = Math.min(Math.max(newLeft, 0), maxLeft);
            const clampedTop = Math.min(Math.max(newTop, 0), maxTop);

            const newPosition = { top: clampedTop, left: clampedLeft };
            setPosition(newPosition);
            onPositionChange(newPosition);
        } else if (interaction.state === 'resizing') {
            const deltaX = e.clientX - (interaction.rel?.x || 0);
            const deltaY = e.clientY - (interaction.rel?.y || 0);

            let newWidth = (interaction.initialSize?.width || size.width) + deltaX;
            let newHeight = (interaction.initialSize?.height || size.height) + deltaY;

            // Set minimum size constraints
            newWidth = Math.max(newWidth, 200); // Minimum width
            newHeight = Math.max(newHeight, 300); // Minimum height

            // Optionally, set maximum size based on viewport
            newWidth = Math.min(newWidth, window.innerWidth - position.left);
            newHeight = Math.min(newHeight, window.innerHeight - position.top);

            const newSize = { width: newWidth, height: newHeight };
            setSize(newSize);
            onSizeChange(newSize);
        }
    }, [interaction, size.width, size.height, position.left, position.top, onPositionChange, onSizeChange]);

    // Mouse up handler to end interaction
    const handleMouseUp = useCallback(() => {
        if (interaction.state !== 'none') {
            setInteraction({ state: 'none' });
        }
    }, [interaction.state]);

    // Attach global mouse move and mouse up listeners when interacting
    useEffect(() => {
        if (interaction.state === 'none') return;

        window.addEventListener('mousemove', handleMouseMove);
        window.addEventListener('mouseup', handleMouseUp);

        return () => {
            window.removeEventListener('mousemove', handleMouseMove);
            window.removeEventListener('mouseup', handleMouseUp);
        };
    }, [interaction.state, handleMouseMove, handleMouseUp]);

    // Function to dynamically adjust font size based on panel size
    const adjustFontSize = useCallback(() => {
        if (contentRef.current) {
            const fontSize = Math.max(Math.min(size.width * 0.02, size.height * 0.02), 12); // Clamp between 12px and 2% of width/height
            contentRef.current.style.fontSize = `${fontSize}px`;
        }
    }, [size]);

    // Adjust font size whenever size changes
    useEffect(() => {
        adjustFontSize();
    }, [size, adjustFontSize]);

    return (
        <div
            ref={panelRef}
            className={`${styles.panel} ${className}`}
            style={{
                top: position.top,
                left: position.left,
                width: size.width,
                height: size.height,
                ...style,
            }}
        >
            {/* Header */}
            <div
                className={styles.header}
                onMouseDown={handleDragMouseDown}
            >
                <span className={styles.title}>{title}</span>
            </div>

            {/* Content */}
            <div
                ref={contentRef}
                className={styles.content}
            >
                {children}
            </div>

            {/* Resizer */}
            <div
                className={styles.resizer}
                onMouseDown={handleResizeMouseDown}
            />
        </div>
    );
};

export default Panel;
