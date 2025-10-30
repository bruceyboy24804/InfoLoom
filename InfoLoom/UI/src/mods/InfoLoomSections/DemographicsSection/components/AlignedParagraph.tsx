import React from 'react';
import styles from '../Demographics.module.scss';

interface AlignedParagraphProps {
	left: string | null;
	right: number;
}

export const AlignedParagraph = ({ left, right }: AlignedParagraphProps): JSX.Element => {
	return (
		<div className={styles.alignedParagraph}>
			<div className={styles.label}>{left}</div>
			<div className={styles.value}>{right}</div>
		</div>
	);
};
