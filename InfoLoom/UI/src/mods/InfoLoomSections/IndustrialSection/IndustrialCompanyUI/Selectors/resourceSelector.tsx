import { bindValue, useValue, trigger } from 'cs2/api';
import { useLocalization } from 'cs2/l10n';
import { Dropdown, DropdownItem, DropdownToggle, FOCUS_DISABLED } from 'cs2/ui';

import styles from '../IndustrialCompany.module.scss';
import mod from 'mod.json';
import { ModuleResolver } from '../../../../ModuleResolver/moduleResolver';
import { Localekeys } from 'mods/locale';
import { TwoWayBinding } from 'utils/bidirectionalBinding';
import { OneWayBinding } from 'utils/onewayBinding';

interface ResourceSelectorProps {
  resourceType: 'input1' | 'input2' | 'output';
  label: string;
  tooltipText?: string;
}

export const ResourceSelector = ({ resourceType, label, tooltipText }: ResourceSelectorProps) => {
  const { translate } = useLocalization();

  // Determine which bindings to use based on resourceType
  const bindingNames = {
    input1: {
      list: 'listOfInput1Resources',
      selected: 'selectedInput1Resource',
    },
    input2: {
      list: 'listOfInput2Resources',
      selected: 'selectedInput2Resource',
    },
    output: {
      list: 'listOfOutputResources',
      selected: 'selectedOutputResource',
    },
  };

  const bindings = bindingNames[resourceType];

  // Create bindings dynamically
  const resourceNames$ = new OneWayBinding<string[]>(bindings.list, []);
  const selectResourceName = new TwoWayBinding<string>(bindings.selected, 'All Companies');

  // Get list of resources and currently selected resource
  const resourceNamesList: string[] = useValue(resourceNames$.binding);
  const selectedResource: string = useValue(selectResourceName.binding);

  // Create dropdown items for each resource
  const resourceDropdownItems: JSX.Element[] = resourceNamesList.map(resourceName => {
    const selected: boolean = resourceName === selectedResource;

    return (
      <DropdownItem
        key={resourceName}
        focusKey={FOCUS_DISABLED}
        theme={ModuleResolver.instance.DropdownClasses}
        value={resourceName === 'All' ? 'All' : translate(`Resources.TITLE[${resourceName}]`)}
        closeOnSelect={true}
        selected={selected}
        className={selected ? styles.selectedCompanyNameDropdownItem : ''}
        onChange={() => selectResourceName.set(resourceName)}
      >
        {resourceName === 'All' ? 'All' : translate(`Resources.TITLE[${resourceName}]`)}
      </DropdownItem>
    );
  });

  // Get display name for selected resource
  const displayName = selectedResource || label;

  // Default tooltip text
  const defaultTooltip = `Select ${label.toLowerCase()} to filter companies.`;

  // Return dropdown
  return (
    <ModuleResolver.instance.Tooltip
      direction="right"
      tooltip={
        <ModuleResolver.instance.FormattedParagraphs
          children={translate(`InfoLoom.ResourceSelector[${resourceType}Tooltip]`, tooltipText || defaultTooltip)}
        />
      }
      theme={ModuleResolver.instance.TooltipClasses}
      children={
        <Dropdown theme={ModuleResolver.instance.DropdownClasses} content={resourceDropdownItems}>
          <DropdownToggle>{displayName === 'All' ? 'All' : translate(`Resources.TITLE[${displayName}]`) || displayName}</DropdownToggle>
        </Dropdown>
      }
    />
  );
};
