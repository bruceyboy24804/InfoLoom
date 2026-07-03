import { bindValue, useValue, trigger } from 'cs2/api';
import { LocalizedString, useLocalization } from 'cs2/l10n';
import { Dropdown, DropdownItem, DropdownToggle, FOCUS_DISABLED } from 'cs2/ui';
import styles from '../CommercialCompanyDebugData.module.scss';
import { ModuleResolver } from '../../../../ModuleResolver/moduleResolver';
import { OneWayBinding } from 'utils/onewayBinding';
import { TwoWayBinding } from 'utils/bidirectionalBinding';

interface OutputSelectorProps {
  label: string;
  tooltipText?: string;
}
const outputResource$ = new TwoWayBinding<string>('CommercialOutputResource', 'All');
const resourceList$ = new OneWayBinding<string[]>('listOfCommercialOutputResources', []);

export const OutputSelector = ({ label, tooltipText }: OutputSelectorProps) => {
  const { translate } = useLocalization();

  const selectedInputResource = useValue(outputResource$.binding);
  const resourceName = useValue(resourceList$.binding);

  const resourceDropdownItems: JSX.Element[] = resourceName.map(resourceName => {
    const selected = resourceName === selectedInputResource;

    return (
      <DropdownItem
        key={resourceName}
        focusKey={FOCUS_DISABLED}
        theme={ModuleResolver.instance.DropdownClasses}
        value={resourceName === 'All' ? 'All' : translate(`Resources.TITLE[${resourceName}]`)}
        closeOnSelect={true}
        selected={selected}
        className={selected ? styles.selectedCompanyNameDropdownItem : ''}
        onChange={() => outputResource$.set(resourceName)}
      >
        {resourceName === 'All' ? 'All' : translate(`Resources.TITLE[${resourceName}]`)}
      </DropdownItem>
    );
  });
  const defaultTooltip = `Select ${label.toLowerCase()} to filter companies.`;

  // Return dropdown
  return (
    <ModuleResolver.instance.Tooltip
      direction="right"
      tooltip={
        <ModuleResolver.instance.FormattedParagraphs
          children={translate(`InfoLoom.ResourceSelector[${resourceList$}Tooltip]`, tooltipText || defaultTooltip)}
        />
      }
      theme={ModuleResolver.instance.TooltipClasses}
      children={
        <Dropdown theme={ModuleResolver.instance.DropdownClasses} content={resourceDropdownItems}>
          <DropdownToggle>{selectedInputResource === 'All' ? 'All' : translate(`Resources.TITLE[${selectedInputResource}]`) || selectedInputResource}</DropdownToggle>
        </Dropdown>
      }
    />
  );
};
