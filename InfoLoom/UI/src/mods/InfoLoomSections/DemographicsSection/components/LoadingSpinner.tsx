import React from 'react';
import styles from '../Demographics.module.scss';

interface LoadingSpinnerProps {
	message?: string;
}

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({ message = 'Loading...' }) => {
	return (
		<div className={styles.loadingContainer}>
			<div className={styles.spinner} />
			<div className={styles.loadingMessage}>{message}</div>
		</div>
	);
};
