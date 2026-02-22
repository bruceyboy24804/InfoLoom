import { bindValue, useValue, trigger } from 'cs2/api';
import { LocalizedString, useLocalization } from 'cs2/l10n';
import { Dropdown, DropdownItem, DropdownToggle, FOCUS_DISABLED } from 'cs2/ui';

import styles from '../CommercialCompanyDebugData.module.scss';
import mod from 'mod.json';
import { ModuleResolver } from '../../../../ModuleResolver/moduleResolver';
import { Localekeys } from 'mods/locale';

export const getResourceLoc = (resourceName: string): [string, string] => {
  switch (resourceName) {
    case 'Beverages':
      return [Localekeys.Resource_Beverages, 'Beverages'];
    case 'Chemicals':
      return [Localekeys.Resource_Chemicals, 'Chemicals'];
    case 'Coal':
      return [Localekeys.Resource_Coal, 'Coal'];
    case 'Concrete':
      return [Localekeys.Resource_Concrete, 'Concrete'];
    case 'Convenience Food':
      return [Localekeys.Resource_ConvenienceFood, 'Convenience Food'];
    case 'Cotton':
      return [Localekeys.Resource_Cotton, 'Cotton'];
    case 'Electronics':
      return [Localekeys.Resource_Electronics, 'Electronics'];
    case 'Entertainment':
      return [Localekeys.Resource_Entertainment, 'Entertainment'];
    case 'Financial':
      return [Localekeys.Resource_Financial, 'Financial'];
    case 'Food':
      return [Localekeys.Resource_Food, 'Food'];
    case 'Furniture':
      return [Localekeys.Resource_Furniture, 'Furniture'];
    case 'Garbage':
      return [Localekeys.Resource_Garbage, 'Garbage'];
    case 'Grain':
      return [Localekeys.Resource_Grain, 'Grain'];
    case 'Livestock':
      return [Localekeys.Resource_Livestock, 'Livestock'];
    case 'LocalMail':
      return [Localekeys.Resource_LocalMail, 'Local Mail'];
    case 'Lodging':
      return [Localekeys.Resource_Lodging, 'Lodging'];
    case 'Machinery':
      return [Localekeys.Resource_Machinery, 'Machinery'];
    case 'Meals':
      return [Localekeys.Resource_Meals, 'Meals'];
    case 'Media':
      return [Localekeys.Resource_Media, 'Media'];
    case 'Metals':
      return [Localekeys.Resource_Metals, 'Metals'];
    case 'Minerals':
      return [Localekeys.Resource_Minerals, 'Minerals'];
    case 'Money':
      return [Localekeys.Resource_Money, 'Money'];
    case 'Oil':
      return [Localekeys.Resource_Oil, 'Oil'];
    case 'Ore':
      return [Localekeys.Resource_Ore, 'Ore'];
    case 'OutgoingMail':
      return [Localekeys.Resource_OutgoingMail, 'Outgoing Mail'];
    case 'Paper':
      return [Localekeys.Resource_Paper, 'Paper'];
    case 'Petrochemicals':
      return [Localekeys.Resource_Petrochemicals, 'Petrochemicals'];
    case 'Pharmaceuticals':
      return [Localekeys.Resource_Pharmaceuticals, 'Pharmaceuticals'];
    case 'Plastics':
      return [Localekeys.Resource_Plastics, 'Plastics'];
    case 'Recreation':
      return [Localekeys.Resource_Recreation, 'Recreation'];
    case 'Software':
      return [Localekeys.Resource_Software, 'Software'];
    case 'Steel':
      return [Localekeys.Resource_Steel, 'Steel'];
    case 'Stone':
      return [Localekeys.Resource_Stone, 'Stone'];
    case 'Telecom':
      return [Localekeys.Resource_Telecom, 'Telecom'];
    case 'Textiles':
      return [Localekeys.Resource_Textiles, 'Textiles'];
    case 'Timber':
      return [Localekeys.Resource_Timber, 'Timber'];
    case 'UnsortedMail':
      return [Localekeys.Resource_UnsortedMail, 'Unsorted Mail'];
    case 'Vegetables':
      return [Localekeys.Resource_Vegetables, 'Vegetables'];
    case 'Vehicles':
      return [Localekeys.Resource_Vehicles, 'Vehicles'];
    case 'Wood':
      return [Localekeys.Resource_Wood, 'Wood'];
    case 'Fish':
      return [Localekeys.Resource_Fish, 'Fish'];
    default:
      return [resourceName, resourceName];
  }
};
interface ResourceSelectorProps {
  resourceType: 'input1' | 'output';
  label: string;
  tooltipText?: string;
}

export const ResourceSelector = ({ resourceType, label, tooltipText }: ResourceSelectorProps) => {
  const { translate } = useLocalization();

  // Determine which bindings to use based on resourceType
  const bindingNames = {
    input1: {
      list: 'listOfCommercialInput1Resources',
      selected: 'selectedCommercialInput1Resource',
      trigger: 'SetSelectedCommercialInput1Resource',
    },
    output: {
      list: 'listOfCommercialOutputResources',
      selected: 'selectedCommercialOutputResource',
      trigger: 'SetSelectedCommercialOutputResource',
    },
  };

  const bindings = bindingNames[resourceType];

  // Create bindings dynamically
  const resourceNames$ = bindValue<string[]>(mod.id, bindings.list, []);
  const selectedResourceName$ = bindValue<string>(mod.id, bindings.selected, '');
  const setSelectedResourceName$ = (resource: string) => trigger(mod.id, bindings.trigger, resource);

  // Get list of resources and currently selected resource
  const resourceNamesList: string[] = useValue(resourceNames$);
  const selectedResource: string = useValue(selectedResourceName$);

  // Create dropdown items for each resource
  const resourceDropdownItems: JSX.Element[] = resourceNamesList.map((resourceName, index) => {
    const selected: boolean = resourceName === selectedResource;
    const [locId, fallback] = getResourceLoc(resourceName);

    return (
      <DropdownItem
        key={`${resourceType}-${index}`}
        focusKey={FOCUS_DISABLED}
        theme={ModuleResolver.instance.DropdownClasses}
        value={resourceName}
        closeOnSelect={true}
        selected={selected}
        className={selected ? styles.selectedCompanyNameDropdownItem : ''}
        onChange={() => setSelectedResourceName$(resourceName)}
      >
        <LocalizedString id={locId} showIdOnFail={true} />
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
          <DropdownToggle>{displayName}</DropdownToggle>
        </Dropdown>
      }
    />
  );
};
