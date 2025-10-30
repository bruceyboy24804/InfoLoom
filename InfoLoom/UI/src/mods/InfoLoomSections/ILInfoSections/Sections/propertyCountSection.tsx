import { IndicatorValue, SelectedInfoSectionBase, Theme } from 'cs2/bindings';
import { getModule } from 'cs2/modding';
import { PanelSectionRow, FOCUS_AUTO, PanelFoldout, Tooltip, Icon, FOCUS_DISABLED } from 'cs2/ui';
import { InfoRowSCSS } from 'mods/InfoLoomSections/ILInfoSections/Modules/info-Row/info-Row.module.scss';
import { InfoSectionFoldout } from 'mods/InfoLoomSections/ILInfoSections/Modules/info-Section/info-section-foldout';
import classNames from 'classnames';
import { formatPercentage1 } from 'mods/InfoLoomSections/utils/formatText';
import styles from './buildingInfoComponent.module.scss';
import { FC } from 'react';
import { LocalizedNumber, Unit } from 'cs2/l10n';

const InfoRowTheme: Theme | any = getModule(
  'game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss',
  'classes'
);

interface ILPropertyCountSection extends SelectedInfoSectionBase {
  NoOfResProperties: number;
  NoOfComProperties: number;
  NoOfIndProperties: number;
  NoOfOffProperties: number;
  NoOfStorageProperties: number;
  ElementaryCapacity: number;
  HighSchoolCapacity: number;
  CollegeCapacity: number;
  UniversityCapacity: number;
  ElementaryStudents: number;
  HighSchoolStudents: number;
  CollegeStudents: number;
  UniversityStudents: number;
  ElementaryEligible: number;
  HighSchoolEligible: number;
  CollegeEligible: number;
  UniversityEligible: number;
  elementaryAvailability: IndicatorValue;
  highSchoolAvailability: IndicatorValue;
  collegeAvailability: IndicatorValue;
  universityAvailability: IndicatorValue;
  AverageLandValue: number;
  AverageBuildingLevel: number;
  hasSchool: boolean;
}

let PanelOpen: boolean = false;

// Calculate availability ratio from -100 (no capacity) to 100 (full capacity available)
const calculateAvailability = (capacity: number, eligible: number): number => {
  if (capacity <= 0) return -100; // No capacity at all

  // Calculate ratio: (capacity - eligible) / capacity
  // If eligible = 0: returns 100 (full capacity available)
  // If eligible = capacity: returns 0 (at capacity)
  // If eligible > capacity: returns negative (shortage)
  const ratio = ((capacity - eligible) / capacity) * 100;

  // Clamp between -100 and 100
  return Math.max(-100, Math.min(100, ratio));
};

// Get color based on availability value
const getAvailabilityColor = (availability: number): string => {
  if (availability >= 60) return '#478A36'; // Dark green - excellent capacity
  if (availability >= 20) return '#63B506'; // Light green - good capacity
  if (availability >= 0) return '#FF831B'; // Orange - at or near capacity
  if (availability >= -40) return '#FF4E18'; // Red - shortage
  return '#D32F2F'; // Dark red - severe shortage
};

export const ILPropertyInfoSection = (componentList: any): any => {
  componentList['InfoLoomTwo.Systems.Sections.ILDistrictSection'] = (props: ILPropertyCountSection) => {
    const elementaryAvailability = calculateAvailability(props.ElementaryCapacity, props.ElementaryEligible);
    const highSchoolAvailability = calculateAvailability(props.HighSchoolCapacity, props.HighSchoolEligible);
    const collegeAvailability = calculateAvailability(props.CollegeCapacity, props.CollegeEligible);
    const universityAvailability = calculateAvailability(props.UniversityCapacity, props.UniversityEligible);
    const totalProperties =
      props.NoOfResProperties +
      props.NoOfComProperties +
      props.NoOfIndProperties +
      props.NoOfOffProperties +
      props.NoOfStorageProperties;
    return (
      <InfoSectionFoldout
        header={
          <div className={InfoRowTheme.infoRow}>
            <div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>Info Loom</div>
          </div>
        }
        initialExpanded={PanelOpen}
        expandFromContent={false}
        focusKey={FOCUS_DISABLED}
        onToggleExpanded={(value: boolean) => {
          PanelOpen = value;
        }}
      >
        <PanelFoldout
          header={
            <div className={InfoRowTheme.infoRow}>
              <div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>Property Count</div>
              <div className={InfoRowSCSS.right}>
                Total Property count: <LocalizedNumber value={totalProperties} unit={Unit.Integer} />
              </div>
            </div>
          }
          initialExpanded={false}
          focusKey={FOCUS_DISABLED}
          tooltip={'number of properties which is in this district broken down by type of property'}
        >
          <PanelSectionRow
            left={'Residential'}
            right={<LocalizedNumber value={props.NoOfResProperties} unit={Unit.Integer} />}
            subRow={true}
          />
          <PanelSectionRow
            left={'Commercial'}
            right={<LocalizedNumber value={props.NoOfComProperties} unit={Unit.Integer} />}
            subRow={true}
          />
          <PanelSectionRow
            left={'Industrial'}
            right={<LocalizedNumber value={props.NoOfIndProperties} unit={Unit.Integer} />}
            subRow={true}
          />
          <PanelSectionRow
            left={'Office'}
            right={<LocalizedNumber value={props.NoOfOffProperties} unit={Unit.Integer} />}
            subRow={true}
          />
          <PanelSectionRow
            left={'Storage'}
            right={<LocalizedNumber value={props.NoOfStorageProperties} unit={Unit.Integer} />}
            subRow={true}
          />
        </PanelFoldout>
        {props.hasSchool && (
          <>
            <PanelFoldout
            header={
              <div className={InfoRowTheme.infoRow}>
                <div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>School Info</div>
              </div>
            }
            initialExpanded={false}
            focusKey={FOCUS_DISABLED}
          >
            <PanelFoldout
              header={
                <div className={InfoRowTheme.infoRow}>
                  <div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>Capacity</div>
                </div>
              }
              initialExpanded={false}
            >
              <PanelSectionRow
                left={'Elementary'}
                right={<LocalizedNumber value={props.ElementaryCapacity} unit={Unit.Integer} />}
                subRow={true}
                disableFocus={true}
              />
              <PanelSectionRow
                left={'High School'}
                right={<LocalizedNumber value={props.HighSchoolCapacity} unit={Unit.Integer} />}
                subRow={true}
                disableFocus={true}
              />
              <PanelSectionRow
                left={'College'}
                right={<LocalizedNumber value={props.CollegeCapacity} unit={Unit.Integer} />}
                subRow={true}
                disableFocus={true}
              />
              <PanelSectionRow
                left={'University'}
                right={<LocalizedNumber value={props.UniversityCapacity} unit={Unit.Integer} />}
                subRow={true}
                disableFocus={true}
              />
            </PanelFoldout>
            <PanelFoldout
              header={
                <div className={InfoRowTheme.infoRow}>
                  <div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>Students</div>
                </div>
              }
              initialExpanded={false}
            >
              <PanelSectionRow
                left={'Elementary'}
                right={<LocalizedNumber value={props.ElementaryStudents} unit={Unit.Integer} />}
                subRow={true}
                disableFocus={true}
              />
              <PanelSectionRow
                left={'High School'}
                right={<LocalizedNumber value={props.HighSchoolStudents} unit={Unit.Integer} />}
                subRow={true}
                disableFocus={true}
              />
              <PanelSectionRow
                left={'College'}
                right={<LocalizedNumber value={props.CollegeStudents} unit={Unit.Integer} />}
                subRow={true}
                disableFocus={true}
              />
              <PanelSectionRow
                left={'University'}
                right={<LocalizedNumber value={props.UniversityStudents} unit={Unit.Integer} />}
                subRow={true}
                disableFocus={true}
              />
            </PanelFoldout>
            <PanelFoldout
              header={
                <div className={InfoRowTheme.infoRow}>
                  <div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>Eligible Students</div>
                </div>
              }
              initialExpanded={false}
            >
              <PanelSectionRow
                left={'Elementary'}
                right={<LocalizedNumber value={props.ElementaryEligible} unit={Unit.Integer} />}
                subRow={true}
                disableFocus={true}
              />
              <PanelSectionRow
                left={'High School'}
                right={<LocalizedNumber value={props.HighSchoolEligible} unit={Unit.Integer} />}
                subRow={true}
                disableFocus={true}
              />
              <PanelSectionRow
                left={'College'}
                right={<LocalizedNumber value={props.CollegeEligible} unit={Unit.Integer} />}
                subRow={true}
                disableFocus={true}
              />
              <PanelSectionRow
                left={'University'}
                right={<LocalizedNumber value={props.UniversityEligible} unit={Unit.Integer} />}
                subRow={true}
                disableFocus={true}
              />
            </PanelFoldout>
            <PanelFoldout
              header={
                <div className={InfoRowTheme.infoRow}>
                  <div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>Availability</div>
                </div>
              }
              initialExpanded={false}
            >
              <PanelSectionRow
                left={'Elementary'}
                right={
                  <span style={{ color: getAvailabilityColor(elementaryAvailability) }}>
                    {<LocalizedNumber value={elementaryAvailability} unit={Unit.Percentage} />}
                  </span>
                }
                subRow={true}
                disableFocus={true}
                tooltip={'-100 means no capacity, 0 means at capacity, 100 means full capacity available'}
              />
              <PanelSectionRow
                left={'High School'}
                right={
                  <span style={{ color: getAvailabilityColor(highSchoolAvailability) }}>
                    {<LocalizedNumber value={highSchoolAvailability} unit={Unit.Percentage} />}
                  </span>
                }
                subRow={true}
                disableFocus={true}
                tooltip={'-100 means no capacity, 0 means at capacity, 100 means full capacity available'}
              />
              <PanelSectionRow
                left={'College'}
                right={
                  <span style={{ color: getAvailabilityColor(collegeAvailability) }}>
                    {<LocalizedNumber value={collegeAvailability} unit={Unit.Percentage} />}
                  </span>
                }
                subRow={true}
                disableFocus={true}
                tooltip={'-100 means no capacity, 0 means at capacity, 100 means full capacity available'}
              />
              <PanelSectionRow
                left={'University'}
                right={
                  <span style={{ color: getAvailabilityColor(universityAvailability) }}>
                    {<LocalizedNumber value={universityAvailability} unit={Unit.Percentage} />}
                  </span>
                }
                subRow={true}
                disableFocus={true}
                tooltip={'-100 means no capacity, 0 means at capacity, 100 means full capacity available'}
              />
            </PanelFoldout>
          </PanelFoldout>
          </>
        )}
        <PanelSectionRow
          left={'Average Land Value'}
          right={<LocalizedNumber value={props.AverageLandValue} unit={Unit.MoneyPerCell} />}
          subRow={true}
          disableFocus={true}
        />
        <PanelSectionRow
          left={'Average Building Level'}
          right={props.AverageBuildingLevel}
          subRow={true}
          disableFocus={true}
        />
      </InfoSectionFoldout>
    );
  };

  return componentList as any;
};
