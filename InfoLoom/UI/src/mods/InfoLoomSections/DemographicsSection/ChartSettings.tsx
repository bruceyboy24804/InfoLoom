// ChartSettings.tsx
import React, { ReactNode } from 'react';
import { InfoCheckbox } from '../../InfoCheckbox/InfoCheckbox';

// 1) The interface that represents our settings data
export interface ChartSettingsData {
  chartType: 'bar' | 'line';
  showGridLines: boolean;
  enableAnimation: boolean;
  stackedView: boolean;
  legendPosition: 'top' | 'bottom' | 'left' | 'right';
}

// 2) A default settings object
export const defaultChartSettings: ChartSettingsData = {
  chartType: 'bar',
  showGridLines: true,
  enableAnimation: false,
  stackedView: true,
  legendPosition: 'top',
};

// 3) Our ChartSettings component that shows checkboxes/controls
interface ChartSettingsProps {
  chartSettings: ChartSettingsData;
  setChartSettings: React.Dispatch<React.SetStateAction<ChartSettingsData>>;
  children?: ReactNode; // Add this line to accept children
}

export const ChartSettings: React.FC<ChartSettingsProps> = ({
  chartSettings,
  setChartSettings,
  children, // Destructure children
}) => {
  const {
    chartType,
    showGridLines,
    enableAnimation,
    stackedView,
    legendPosition,
  } = chartSettings;

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        gap: '0.3rem',
        padding: '1rem',
        backgroundColor: 'rgba(0, 0, 0, 0.2)',
        borderRadius: '4px',
        margin: '0 1rem',
      }}
    >
      <div
        style={{
          color: 'white',
          fontSize: '14px',
          marginBottom: '0.3rem',
          borderBottom: '1px solid rgba(255, 255, 255, 0.2)',
          paddingBottom: '0.25rem',
        }}
      >
        Chart Settings
      </div>

      {/* Chart Type */}
      <div style={{ marginBottom: '0.4rem' }}>
        <div style={{ color: 'white', fontSize: '13px', marginBottom: '0.1rem', opacity: 0.9 }}>
          Chart Type
        </div>
        <div style={{ display: 'flex', gap: '1rem' }}>
          <InfoCheckbox
            label="Bar Chart"
            isChecked={chartType === 'bar'}
            onToggle={() =>
              setChartSettings((prev) => ({ ...prev, chartType: 'bar' }))
            }
          />
          <InfoCheckbox
            label="Line Chart"
            isChecked={chartType === 'line'}
            onToggle={() =>
              setChartSettings((prev) => ({ ...prev, chartType: 'line' }))
            }
          />
        </div>
      </div>

      {/* Layout */}
      <div style={{ marginBottom: '0.4rem' }}>
        <div style={{ color: 'white', fontSize: '13px', marginBottom: '0.1rem', opacity: 0.9 }}>
          Layout
        </div>
        <div style={{ display: 'flex', gap: '1rem' }}>
          <InfoCheckbox
            label="Show Grid Lines"
            isChecked={showGridLines}
            onToggle={() =>
              setChartSettings((prev) => ({
                ...prev,
                showGridLines: !prev.showGridLines,
              }))
            }
          />
          <InfoCheckbox
            label="Enable Animation"
            isChecked={enableAnimation}
            onToggle={() =>
              setChartSettings((prev) => ({
                ...prev,
                enableAnimation: !prev.enableAnimation,
              }))
            }
          />
          <InfoCheckbox
            label="Stacked View"
            isChecked={stackedView}
            onToggle={() =>
              setChartSettings((prev) => ({
                ...prev,
                stackedView: !prev.stackedView,
              }))
            }
          />
        </div>
      </div>

      {/* Legend Position */}
      <div style={{ marginBottom: '0.4rem' }}>
        <div style={{ color: 'white', fontSize: '13px', marginBottom: '0.1rem', opacity: 0.9 }}>
          Legend Position
        </div>
        <div style={{ display: 'flex', gap: '1rem' }}>
          {(['top', 'bottom', 'left', 'right'] as const).map((pos) => (
            <InfoCheckbox
              key={pos}
              label={pos.charAt(0).toUpperCase() + pos.slice(1)}
              isChecked={legendPosition === pos}
              onToggle={() =>
                setChartSettings((prev) => ({ ...prev, legendPosition: pos }))
              }
            />
          ))}
        </div>
      </div>

      {/* Render any additional children passed to ChartSettings */}
      {children}
    </div>
  );
};

export default ChartSettings;
