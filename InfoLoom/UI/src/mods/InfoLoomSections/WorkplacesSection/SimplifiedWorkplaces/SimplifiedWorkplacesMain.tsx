import React from 'react';
import { useValue } from 'cs2/api';
import { LocalizedPercentage, useLocalization } from 'cs2/l10n';
import { Tooltip } from 'cs2/ui';
import { WorkplacesData } from 'mods/bindings';
import { Localekeys } from 'mods/locale';
import styles from './SimplifiedWorkplaces.module.scss';

const polarToCartesian = (centerX: number, centerY: number, radius: number, angleInDegrees: number) => {
  const angleInRadians = ((angleInDegrees - 90) * Math.PI) / 180.0;
  return {
    x: centerX + radius * Math.cos(angleInRadians),
    y: centerY + radius * Math.sin(angleInRadians),
  };
};

const calculateArc = (centerX: number, centerY: number, radius: number, startAngle: number, endAngle: number) => {
  const start = polarToCartesian(centerX, centerY, radius, endAngle);
  const end = polarToCartesian(centerX, centerY, radius, startAngle);
  const largeArcFlag = endAngle - startAngle <= 180 ? '0' : '1';
  return ['M', start.x, start.y, 'A', radius, radius, 0, largeArcFlag, 0, end.x, end.y].join(' ');
};

interface WorkplaceCircleProps {
  color: string;
  label: string;
  levelTotal: number;
  grandTotal: number;
}

const WorkplaceCircle = ({ color, label, levelTotal, grandTotal }: WorkplaceCircleProps) => {
  const percent = grandTotal > 0 ? Math.max(0, Math.min(1, levelTotal / grandTotal)) : 0;
  const size = 68;
  const strokeWidth = 6;
  const radius = 28;
  const center = size / 2;
  const minMaxProgressOffset = 0.005;

  let ringPercent = percent;
  if (grandTotal > 0) {
    if (ringPercent === 1) {
      ringPercent -= minMaxProgressOffset;
    } else if (ringPercent <= minMaxProgressOffset) {
      ringPercent = minMaxProgressOffset;
    }
  }

  const endAngle = 360 * ringPercent;
  const arcString = grandTotal > 0 ? calculateArc(center, center, radius, 0, endAngle) : '';
  const tooltip = `${label}: ${grandTotal > 0 ? `${((100 * levelTotal) / grandTotal).toFixed(1)}%` : '0.0%'} of total workplaces (${levelTotal.toLocaleString()})`;

  return (
    <Tooltip direction="down" tooltip={tooltip}>
      <div className={styles.educationCard}>
        <div className={styles.educationCircle} aria-label={tooltip}>
          <svg className={styles.educationSvg} viewBox={`0 0 ${size} ${size}`}>
            <circle className={styles.educationTrack} cx={center} cy={center} r={radius} strokeWidth={strokeWidth} />
            {grandTotal > 0 && (
              <path fill="none" d={arcString} stroke={color} strokeLinecap="round" strokeWidth={strokeWidth} />
            )}
          </svg>
          <div className={styles.educationCenter}>
            <span className={styles.educationPercent}>
              <LocalizedPercentage value={levelTotal} max={grandTotal} />
            </span>
          </div>
        </div>
        <div className={styles.educationLabel}>{label}</div>
      </div>
    </Tooltip>
  );
};

export const SimplifiedWorkplacesMain = () => {
  const { translate } = useLocalization();
  const workplaces = useValue(WorkplacesData.binding);

  if (workplaces.length < 6) {
    return <p className={styles.waiting}>{translate('InfoLoomTwo.WorkplacesPanel[Waiting]', 'Waiting...')}</p>;
  }

  const grandTotal = workplaces[5].Total;

  const educationLevels = [
    { color: '#808080', label: translate(Localekeys.Uneducated, 'Uneducated') ?? 'Uneducated', data: workplaces[0] },
    {
      color: '#B09868',
      label: translate(Localekeys.PoorlyEducated, 'Poorly Educated') ?? 'Poorly Educated',
      data: workplaces[1],
    },
    { color: '#368A2E', label: translate(Localekeys.Educated, 'Educated') ?? 'Educated', data: workplaces[2] },
    {
      color: '#B981C0',
      label: translate(Localekeys.WellEducated, 'Well Educated') ?? 'Well Educated',
      data: workplaces[3],
    },
    {
      color: '#5796D1',
      label: translate(Localekeys.HighlyEducated, 'Highly Educated') ?? 'Highly Educated',
      data: workplaces[4],
    },
  ];

  return (
    <div className={styles.root}>
      <div className={styles.educationGrid}>
        {educationLevels.map(level => (
          <WorkplaceCircle
            key={level.label}
            color={level.color}
            label={level.label}
            levelTotal={level.data.Total}
            grandTotal={grandTotal}
          />
        ))}
      </div>
    </div>
  );
};
