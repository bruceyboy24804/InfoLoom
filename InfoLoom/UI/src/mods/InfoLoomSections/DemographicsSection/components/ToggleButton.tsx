import React from 'react';
import { Button } from 'cs2/ui';
import styles from '../Demographics.module.scss';

interface ToggleButtonProps {
  selected: boolean;
  onSelected: () => void;
  children?: React.ReactNode;
  className?: string;
}

export const ToggleButton: React.FC<ToggleButtonProps> = ({ selected, onSelected, children, className }) => {
  return (
    <Button
      selected={selected}
      onSelect={onSelected}
      className={`${styles.toggleButton} ${selected ? styles.buttonSelected : ''} ${className ?? ''}`}
      variant="flat"
      type="button"
    >
      {children}
    </Button>
  );
};
