import React, { useState, useMemo, useCallback, useRef, useEffect, memo, useLayoutEffect } from 'react';
import styles from './Demographics.module.scss';

// Local or app-level imports
import { bindValue, useValue } from 'cs2/api';
import { InfoCheckbox } from 'mods/components/InfoCheckbox/InfoCheckbox';
import {DraggablePanelProps, PanelProps, PanelTheme, Panel, Scrollable} from "cs2/ui";
import {populationAtAge} from "../../domain/populationAtAge";
import {DemographicsDataDetails, DemographicsDataTotals, DemographicsDataOldestCitizen} from "../../bindings";

// ==== Bindings (connect to your mod's data) ====



interface AlignedParagraphProps {
  left: string;
  right: number;
}

type GroupingStrategy = 'none' | 'fiveYear' | 'tenYear' | 'lifecycle';
type ChartOrientation = 'horizontal'; // we only have horizontal

interface AgeRange {
  label: string;
  min: number;
  max: number;
}

interface GroupingOption {
  label: string;
  value: GroupingStrategy;
  ranges: AgeRange[];
}

interface AggregatedInfo {
  label: string;
  work: number;
  elementary: number;
  highSchool: number;
  college: number;
  university: number;
  other: number;
  total: number;
}

interface StatisticsSummaryProps {
  StructureTotals: number[];
  OldestCitizen: number;
}

interface GroupingOptionsProps {
  groupingStrategy: GroupingStrategy;
  setGroupingStrategy: React.Dispatch<React.SetStateAction<GroupingStrategy>>;
  totalEntries: number;
}

// === Constants & helper functions ===

/** Common font settings used in chart options and UI elements. */
const commonFont = {
  family: 'Arial, sans-serif',
  size: 14,
  weight: 'normal' as const,
};

const yaxisfont = {
  family: 'Arial, sans-serif',
  size: 11,
  weight: 'normal' as const,
};

/**  
 * Hardcoded orientation properties because we only have a horizontal chart.
 */
function getOrientationProps() {
  return {
    indexAxis: 'y' as const,
    mainAxisTitle: 'Age in Days',
    crossAxisTitle: 'Number of People',
    minChartHeight: '400px',
  };
}

/** Utility to generate AgeRange[] for grouping. */
function generateRanges(step: number): AgeRange[] {
  const ranges: AgeRange[] = [];
  for (let i = 0; i < 120; i += step) {
    ranges.push({
      label:
        i === 0
          ? `0-${step}`
          : i + step >= 120
          ? `${i}-${120}`
          : `${i}-${i + step}`,
      min: i,
      max: i + step,
    });
  }
  return ranges;
}

/** Predefined grouping strategies. */
const GROUP_STRATEGIES: GroupingOption[] = [
  {
    label: 'Detailed View',
    value: 'none',
    ranges: [],
  },
  {
    label: '5-Year Groups',
    value: 'fiveYear',
    ranges: generateRanges(5),
  },
  {
    label: '10-Year Groups',
    value: 'tenYear',
    ranges: generateRanges(10),
  },
  {
    label: 'Lifecycle Groups',
    value: 'lifecycle',
    ranges: [
      { label: 'Child', min: 0, max: 20 },
      { label: 'Teen', min: 21, max: 35 },
      { label: 'Adult', min: 36, max: 83 },
      { label: 'Elderly', min: 84, max: 120 },
    ],
  },
];

/**
 * Helper function to group raw population data based on the selected grouping strategy.
 */
function aggregatePopulationData(
  details: populationAtAge[],
  grouping: GroupingOption
): AggregatedInfo[] {
  // Initialize aggregated data structure
  const aggregated = grouping.ranges.map((range) => ({
    label: range.label,
    work: 0,
    elementary: 0,
    highSchool: 0,
    college: 0,
    university: 0,
    other: 0,
    total: 0,
  }));

  details.forEach((info) => {
    if (info.Age > 120) return;
    const idx = grouping.ranges.findIndex(
      (range) => info.Age >= range.min && info.Age <= range.max
    );
    if (idx !== -1) {
      const agg = aggregated[idx];
      agg.work += info.Work;
      agg.elementary += info.School1;
      agg.highSchool += info.School2;
      agg.college += info.School3;
      agg.university += info.School4;
      agg.other += info.Other;
      agg.total += info.Total;
    }
  });

  return aggregated;
}

// === Reusable sub-components ===

/**
 * A simple row displaying "left" label and "right" numeric value, aligned and spaced.
 */
const AlignedParagraph = ({ left, right }: AlignedParagraphProps): JSX.Element => {
  return (
    <div className={styles.alignedParagraph}>
      <div className={styles.label}>{left}</div>
      <div className={styles.value}>{right}</div>
    </div>
  );
};

/**
 * Checkbox UI for selecting a grouping strategy.
 */
const GroupingOptions = ({
  groupingStrategy,
  setGroupingStrategy,
  totalEntries,
}: GroupingOptionsProps): JSX.Element => {
  return (
    <div className={styles.groupingOptions}>
      <div className={styles.label}>Age Grouping</div>
      {GROUP_STRATEGIES.map((strategy) => (
        <InfoCheckbox
          key={strategy.value}
          label={strategy.label}
          isChecked={groupingStrategy === strategy.value}
          onToggle={() => setGroupingStrategy(strategy.value)}
          count={strategy.ranges.length || totalEntries}
        />
      ))}
    </div>
  );
};

/**
 * Displays high-level statistics (Population, Tourists, etc.).
 */
const StatisticsSummary = ({
  StructureTotals,
  OldestCitizen,
}: StatisticsSummaryProps): JSX.Element => {
  // Safely extract required totals
  const allCitizens = StructureTotals[0];
  const population = StructureTotals[1];
  const tourists = StructureTotals[2];
  const commuters = StructureTotals[3];
  const students = StructureTotals[4];
  const workers = StructureTotals[5];
  const movingAway = StructureTotals[7];
  const dead = StructureTotals[8];
  const homeless = StructureTotals[9];

  return (
    <div className={styles.statisticsContainer}>
      <div className={`${styles.statisticsColumn} ${styles.left}`}>
        <AlignedParagraph left="All Citizens" right={allCitizens} />
        <div className={styles.spacer} />
        <AlignedParagraph left="- Tourists" right={tourists} />
        <div className={styles.spacer} />
        <AlignedParagraph left="- Commuters" right={commuters} />
        <div className={styles.spacer} />
        <AlignedParagraph left="- Moving Away" right={movingAway} />
        <div className={styles.spacer} />
        <AlignedParagraph left="Population" right={population} />
      </div>
      <div className={`${styles.statisticsColumn} ${styles.right}`}>
        <AlignedParagraph left="Dead" right={dead} />
        <div className={styles.spacer} />
        <AlignedParagraph left="Students" right={students} />
        <div className={styles.spacer} />
        <AlignedParagraph left="Workers" right={workers} />
        <div className={styles.spacer} />
        <AlignedParagraph left="Homeless" right={homeless} />
        <div className={styles.spacer} />
        <AlignedParagraph left="Oldest Citizen" right={OldestCitizen} />
      </div>
    </div>
  );
};

/**
 * Redone DemographicsChart component for improved clarity and responsiveness
 */
const DemographicsChart = ({
	StructureDetails,
	groupingStrategy,
}: {
	StructureDetails: populationAtAge[];
	groupingStrategy: GroupingStrategy;
}): JSX.Element => {
	const canvasRef = useRef<HTMLCanvasElement>(null);
	const [tooltip, setTooltip] = useState({ visible: false, x: 0, y: 0, content: '' });
	const segmentsRef = useRef<Array<{ x: number; y: number; width: number; height: number; datasetLabel: string; value: number }>>([]);

	// Reuse existing logic to build chart data
	const buildChartData = useCallback((): { labels: string[]; datasets: { label: string; data: number[]; backgroundColor: string; }[] } => {
		if (!StructureDetails.length) {
			return { labels: [], datasets: [] };
		}

		let labels: string[] = [];
		let datasets: { label: string; data: number[]; backgroundColor: string; }[] = [];

		const chartColors = {
			work: '#624532',
			elementary: '#7E9EAE',
			highSchool: '#00C217',
			college: '#005C4E',
			university: '#2462FF',
			other: '#A1A1A1',
		};

		if (groupingStrategy === 'none') {
			// Generate detailed view from age 0 to 120, filling missing ages with zero values
			const age_range = Array.from({ length: 121 }, (_, i) => i);
			labels = age_range.map(age => String(age));
			
			// Helper function to sum values for a given attribute at a specific age
			const get_value = (attr: keyof populationAtAge, age: number): number => {
				return StructureDetails.filter(d => d.Age === age)
					.reduce((sum, d) => sum + (d[attr] || 0), 0);
			};
			
			datasets = [
				{ label: 'Work', data: age_range.map(age => get_value('Work', age)), backgroundColor: chartColors.work },
				{ label: 'Elementary', data: age_range.map(age => get_value('School1', age)), backgroundColor: chartColors.elementary },
				{ label: 'High School', data: age_range.map(age => get_value('School2', age)), backgroundColor: chartColors.highSchool },
				{ label: 'College', data: age_range.map(age => get_value('School3', age)), backgroundColor: chartColors.college },
				{ label: 'University', data: age_range.map(age => get_value('School4', age)), backgroundColor: chartColors.university },
				{ label: 'Other', data: age_range.map(age => get_value('Other', age)), backgroundColor: chartColors.other },
			];
		} else {
			const step = groupingStrategy === 'fiveYear' ? 5 : 10;
			const groups: { label: string; work: number; elementary: number; highSchool: number; college: number; university: number; other: number; }[] = [];
			for (let i = 0; i < 120; i += step) {
				groups.push({
					label: `${i}-${i + step}`,
					work: 0,
					elementary: 0,
					highSchool: 0,
					college: 0,
					university: 0,
					other: 0,
				});
			}
			StructureDetails.forEach(d => {
				if (d.Age > 120) return;
				const idx = Math.floor(d.Age / step);
				if (groups[idx]) {
					groups[idx].work += d.Work;
					groups[idx].elementary += d.School1;
					groups[idx].highSchool += d.School2;
					groups[idx].college += d.School3;
					groups[idx].university += d.School4;
					groups[idx].other += d.Other;
				}
			});
			labels = groups.map(g => g.label);
			datasets = [
				{ label: 'Work', data: groups.map(g => g.work), backgroundColor: chartColors.work },
				{ label: 'Elementary', data: groups.map(g => g.elementary), backgroundColor: chartColors.elementary },
				{ label: 'High School', data: groups.map(g => g.highSchool), backgroundColor: chartColors.highSchool },
				{ label: 'College', data: groups.map(g => g.college), backgroundColor: chartColors.college },
				{ label: 'University', data: groups.map(g => g.university), backgroundColor: chartColors.university },
				{ label: 'Other', data: groups.map(g => g.other), backgroundColor: chartColors.other },
			];
		}
		return { labels, datasets };
	}, [StructureDetails, groupingStrategy]);

	useEffect(() => {
		const canvas = canvasRef.current;
		if (!canvas) return;
		const ctx = canvas.getContext('2d');
		if (!ctx) return;

		const chartData = buildChartData();
		const { labels, datasets } = chartData;
		if (!labels.length || !datasets.length) return;

			// Define drawing dimensions
		const margin_left = 50;
		const margin_top = 20;
		const bar_height = 30;
		const bar_gap = 15;
		const canvas_width = canvas.width;

		// Adjust canvas height dynamically for detailed view
		if (groupingStrategy === 'none') {
			canvas.height = margin_top + labels.length * (bar_height + bar_gap);
		} else {
			canvas.height = 600; // fixed height for grouped views
		}

		const canvas_height = canvas.height;

		// Clear canvas
		ctx.clearRect(0, 0, canvas_width, canvas_height);

		// Compute total values for stacking per category
		const totals = labels.map((_, i) =>
			datasets.reduce((sum, ds) => sum + (ds.data[i] || 0), 0)
		);
		const max_total = Math.max(...totals);

		// Calculate scale factor for horizontal bars
		const scale = (canvas_width - margin_left - 20) / max_total;

			// Add vertical grid lines
		const gridSteps = 10;
		ctx.strokeStyle = 'rgba(255, 255, 255, 0.2)';
		ctx.lineWidth = 1;
		for (let i = 0; i <= gridSteps; i++) {
			const gridValue = (max_total / gridSteps) * i;
			const xPos = margin_left + gridValue * scale;
			ctx.beginPath();
			ctx.moveTo(xPos, margin_top);
			ctx.lineTo(xPos, canvas_height - 10);
			ctx.stroke();
			}

		// Draw each bar group
		labels.forEach((label, i) => {
			let x = margin_left;
			const y = margin_top + i * (bar_height + bar_gap);

			// Draw label text
			ctx.fillStyle = '#ffffff';
			ctx.font = '12px Arial';
			ctx.textAlign = 'right';
			ctx.fillText(label, margin_left - 10, y + bar_height / 2 + 4);

			// Draw each dataset segment
			datasets.forEach(ds => {
				const value = ds.data[i] || 0;
				const segment_width = value * scale;
				ctx.fillStyle = ds.backgroundColor;
				ctx.fillRect(x, y, segment_width, bar_height);
				x += segment_width;
			});
		});

		// New tooltip logic: determine bar group index based on mouseY
		const handleMouseMove = (e: MouseEvent) => {
			const rect = canvas.getBoundingClientRect();
			const mouseX = e.clientX - rect.left;
			const mouseY = e.clientY - rect.top;
			const group_index = Math.floor((mouseY - margin_top) / (bar_height + bar_gap));
			if (group_index < 0 || group_index >= labels.length) {
				setTooltip(prev => ({ ...prev, visible: false }));
				return;
			}
			const content_lines = [];
			content_lines.push(`Age: ${labels[group_index]}`);
			datasets.forEach(ds => {
				content_lines.push(`${ds.label}: ${ds.data[group_index] || 0}`);
			});
			setTooltip({
				visible: true,
				x: mouseX,
				y: mouseY,
				content: content_lines.join(' | ')
			});
		};

		const handleMouseLeave = () => {
			setTooltip(prev => ({ ...prev, visible: false }));
		};

		canvas.addEventListener('mousemove', handleMouseMove);
		canvas.addEventListener('mouseleave', handleMouseLeave);

		// Cleanup event listeners
		return () => {
			canvas.removeEventListener('mousemove', handleMouseMove);
			canvas.removeEventListener('mouseleave', handleMouseLeave);
		};
	}, [buildChartData, groupingStrategy]);

	return (
		<div className={styles.chartContainer}>
			<div className={styles.chartWrapper} style={{ position: 'relative' }}>
				<canvas ref={canvasRef} width={800} height={600} style={{ display: 'block' }} />
				{tooltip.visible && (
					<div style={{
						position: 'absolute',
						left: tooltip.x,
						top: tooltip.y - 30,
						background: 'rgba(0, 0, 0, 0.7)',
						color: 'white',
						padding: '4px 8px',
						borderRadius: '4px',
						pointerEvents: 'none',
						fontSize: '12px'
					}}>
					{tooltip.content}
					</div>
				)}
			</div>
		</div>
	);
};

// Memoize the chart component to optimize rendering
const MemoizedDemographicsChart = memo(DemographicsChart);

// === Main Demographics Component ===
const Demographics = ({ onClose }: DraggablePanelProps): JSX.Element => {
  // Toggles with batch updates
  const [displayState, setDisplayState] = useState({
    showStatistics: true,
    showAgeGrouping: true
  });
  const [groupingStrategy, setGroupingStrategy] = useState<GroupingStrategy>('none');

  // Data from ECS / mod
  const demographicsDataStructureDetails = useValue(DemographicsDataDetails);
  const demographicsDataStructureTotals = useValue(DemographicsDataTotals);
  const demographicsDataOldestCitizen = useValue(DemographicsDataOldestCitizen);

  // Handlers with useCallback to maintain reference stability
  const handleToggleStatistics = useCallback(() => {
    setDisplayState(prev => ({
      ...prev,
      showStatistics: !prev.showStatistics
    }));
  }, []);

  const handleToggleAgeGrouping = useCallback(() => {
    setDisplayState(prev => ({
      ...prev,
      showAgeGrouping: !prev.showAgeGrouping
    }));
  }, []);

  return (
    <Panel
      draggable
      onClose={onClose}
      className={styles.panel}
      header={
        <div className={styles.header}>
          <span className={styles.headerText}>Demographics</span>
        </div>
      }
    >
      <div className={styles.container}>
        {/* Toggle checkboxes */}
        <div className={styles.toggleContainer}>
          <InfoCheckbox
            label="Show Statistics"
            isChecked={displayState.showStatistics}
            onToggle={handleToggleStatistics}
          />
          <InfoCheckbox
            label="Show Age Grouping"
            isChecked={displayState.showAgeGrouping}
            onToggle={handleToggleAgeGrouping}
          />
        </div>

        {/* Statistics Summary */}
        {displayState.showStatistics && (
          <StatisticsSummary
            StructureTotals={demographicsDataStructureTotals}
            OldestCitizen={demographicsDataOldestCitizen}
          />
        )}

        {/* Age Grouping Options */}
        {displayState.showAgeGrouping && (
          <GroupingOptions
            groupingStrategy={groupingStrategy}
            setGroupingStrategy={setGroupingStrategy}
            totalEntries={demographicsDataStructureDetails?.length || 0} // Ensure defined value
          />
        )}

        {/* Chart Section */}
        <Scrollable vertical trackVisibility="always" style={{ flex: 1 }}>
          <div className={styles.chartContainer}>
            <MemoizedDemographicsChart
              StructureDetails={demographicsDataStructureDetails}
              groupingStrategy={groupingStrategy}
            />
          </div>
        </Scrollable>
      </div>
    </Panel>
  );
};

export default memo(Demographics);
