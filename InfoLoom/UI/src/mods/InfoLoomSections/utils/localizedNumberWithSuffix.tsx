import { FC, ReactNode } from 'react';
import { LocalizedNumber, Unit } from 'cs2/l10n';

interface LocalizedNumberWithSuffixProps {
  value: number;
  unit: Unit;
  suffix?: string;
  suffixSpacing?: boolean;
}

export const LocalizedNumberWithSuffix: FC<LocalizedNumberWithSuffixProps> = ({
  value,
  unit,
  suffix,
  suffixSpacing = true
}) => {
  const formatSuffix = (text: string): ReactNode => {
    // Replace ² with proper <sup>2</sup>
    if (text.includes('²')) {
      const parts = text.split('²');
      return (
        <>
          {parts.map((part, i) => (
            <span key={i}>
              {part}
              {i < parts.length - 1 && <sup>²</sup>}
            </span>
          ))}
        </>
      );
    }
    return text;
  };

  return (
    <>
      <LocalizedNumber value={value} unit={unit} />
      {suffix && (
        <>
          {suffixSpacing && ' '}
          {formatSuffix(suffix)}
        </>
      )}
    </>
  );
};
