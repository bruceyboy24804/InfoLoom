import { SelectedInfoSectionBase, Theme } from 'cs2/bindings';
import { getModule } from 'cs2/modding';
import { PanelSectionRow, FOCUS_AUTO, PanelFoldout } from 'cs2/ui';
import { InfoRowSCSS } from 'mods/InfoLoomSections/ILInfoSections/Modules/info-Row/info-Row.module.scss';
import { InfoSectionFoldout } from 'mods/InfoLoomSections/ILInfoSections/Modules/info-Section/info-section-foldout';
import classNames from 'classnames';
import { formatPercentage1 } from 'mods/InfoLoomSections/utils/formatText';
import { LocalizedFraction, LocalizedNumber, LocalizedString, Unit } from 'cs2/l10n';

export const InfoRowTheme: Theme | any = getModule(
  'game-ui/game/components/selected-info-panel/shared-components/info-row/info-row.module.scss',
  'classes'
);

interface ILRentSection extends SelectedInfoSectionBase {
  AreaType: string;
  Level: number;
  LotSize: number;
  SpaceMultiplier: number;
  BaseRent: number;
  LandValueModifier: number;
  LandValueBase: number;
  LandValueRate: number;
  TotalRent: number;
  IsMixedUse: boolean;
  BusinessRentPercent: number;
  PropertiesCount: number;
  RentPerHousehold: number;
}

let PanelOpen: boolean = false;

export const ILRentInfoSection = (componentList: any): any => {
  componentList['InfoLoomTwo.Systems.Sections.ILRentSection'] = (props: ILRentSection) => {
    return (
      <PanelFoldout
        header={
          <div className={InfoRowTheme.infoRow}>
            <div className={classNames(InfoRowSCSS.left, InfoRowSCSS.uppercase)}>Rent</div>
            <div className={classNames(InfoRowSCSS.right)}>
              Total Rent: <LocalizedNumber value={props.TotalRent} unit={Unit.Money} />
            </div>
          </div>
        }
        initialExpanded={PanelOpen}
        expandFromContent={false}
        focusKey={FOCUS_AUTO}
        onToggleExpanded={(value: boolean) => {
          PanelOpen = value;
        }}
      >
        <PanelSectionRow
          left="Area Type"
          right={<LocalizedString id={props.AreaType} fallback={props.AreaType} />}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
        />
        <PanelSectionRow
          left="Level"
          right={props.Level}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
        />
        <PanelSectionRow
          left="Lot Size"
          right={props.LotSize}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
        />
        <PanelSectionRow
          left="Space Multiplier"
          right={<LocalizedNumber value={props.SpaceMultiplier} unit={Unit.FloatThreeFractions} />}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
        />
        <PanelSectionRow
          left="Base Rent"
          right={<LocalizedNumber value={props.BaseRent} unit={Unit.FloatThreeFractions} />}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
        />
        <PanelSectionRow
          left="Land Value Modifier"
          right={<LocalizedNumber value={props.LandValueModifier} unit={Unit.FloatThreeFractions} />}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
        />
        <PanelSectionRow
          left="Land Value Base"
          right={<LocalizedNumber value={props.LandValueBase} unit={Unit.FloatThreeFractions} />}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
        />
        <InfoSectionFoldout
          header={
            <div className={InfoRowTheme.infoRow}>
              <div className={classNames(InfoRowSCSS.left)}>Land Value Rate</div>
              <div className={classNames(InfoRowSCSS.right)}>
                {<LocalizedNumber value={props.LandValueRate} unit={Unit.FloatThreeFractions} />}
              </div>
            </div>
          }
          initialExpanded={PanelOpen}
          expandFromContent={false}
          focusKey={FOCUS_AUTO}
          onToggleExpanded={(value: boolean) => {
            PanelOpen = value;
          }}
        >
          <PanelSectionRow
            left="Calculation:"
            right={
              <>
                <LocalizedNumber value={props.LandValueModifier} unit={Unit.FloatThreeFractions} />
                <span style={{ margin: '0 4rem' }}>×</span>
                <LocalizedNumber value={props.LandValueBase} unit={Unit.FloatThreeFractions} />
              </>
            }
            disableFocus={true}
            subRow={true}
            className={InfoRowTheme.infoRow}
          />
        </InfoSectionFoldout>

        <InfoSectionFoldout
          header={
            <div className={InfoRowTheme.infoRow}>
              <div className={classNames(InfoRowSCSS.left)}>Total Rent</div>
              <div className={classNames(InfoRowSCSS.right)}>
                {<LocalizedNumber value={props.TotalRent} unit={Unit.Money} />}
              </div>
            </div>
          }
          initialExpanded={PanelOpen}
          expandFromContent={false}
          focusKey={FOCUS_AUTO}
          onToggleExpanded={(value: boolean) => {
            PanelOpen = value;
          }}
        >
          <PanelSectionRow
            left="Calculation:"
            right={
              <>
                ((
                <LocalizedNumber value={props.LandValueRate} unit={Unit.FloatThreeFractions} />
                <span style={{ margin: '0 4rem' }}>+</span>
                (<LocalizedNumber value={props.BaseRent} unit={Unit.Integer} />
                <span style={{ margin: '0 4rem' }}>×</span>
                <LocalizedNumber value={props.Level} unit={Unit.Integer} />
                ))
                <span style={{ margin: '0 4rem' }}>×</span>
                <LocalizedNumber value={props.LotSize} unit={Unit.Integer} />
                <span style={{ margin: '0 4rem' }}>×</span>
                <LocalizedNumber value={props.SpaceMultiplier} unit={Unit.FloatTwoFractions} />)
              </>
            }
            disableFocus={true}
            subRow={true}
            className={InfoRowTheme.infoRow}
          />
        </InfoSectionFoldout>
        {props.IsMixedUse && (
          <>
            <PanelSectionRow
              left="Is Mixed Use"
              right={props.IsMixedUse ? 'Yes' : 'No'}
              disableFocus={true}
              subRow={true}
              className={InfoRowTheme.infoRow}
            />
            <PanelSectionRow
              left="Business Rent Percent (if Mixed Use)"
              right={formatPercentage1(props.BusinessRentPercent)}
              disableFocus={true}
              subRow={true}
              className={InfoRowTheme.infoRow}
            />
          </>
        )}
        <PanelSectionRow
          left="Properties Count"
          right={props.PropertiesCount}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
        />
        <PanelSectionRow
          left="Rent Per Renter"
          right={<LocalizedNumber value={props.RentPerHousehold} unit={Unit.Money} />}
          disableFocus={true}
          subRow={true}
          className={InfoRowTheme.infoRow}
        />
      </PanelFoldout>
    );
  };
  return componentList as any;
};
