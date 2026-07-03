import { bindValue, useValue, trigger } from 'cs2/api';
import { Dropdown, DropdownItem, DropdownToggle } from 'cs2/ui';

import styles from './selectors.module.scss';
import mod from 'mod.json';
import { ModuleResolver } from '../../../ModuleResolver/moduleResolver';
import { OutsideConnectionType } from 'mods/domain/TradeCostEnums';

// Define bindings for outside connection type
const OutsideConnectionTypeSorting = bindValue<OutsideConnectionType>(
  mod.id,
  'BINDING:OutsideConnectionType',
  OutsideConnectionType.All
);
const SetOutsideConnectionTypeSorting = (type: OutsideConnectionType) =>
  trigger(mod.id, 'TRIGGER:SetOutsideConnectionType', type);

// Custom selector for outside connection type
export const OutsideConnectionSelector = () => {
  // Get current selected connection type
  const selectedType: OutsideConnectionType = useValue(OutsideConnectionTypeSorting);

  // Define connection type options
  const connectionTypes = [
    { type: OutsideConnectionType.All, name: 'All' },
    { type: OutsideConnectionType.Road, name: 'Road' },
    { type: OutsideConnectionType.Train, name: 'Train' },
    { type: OutsideConnectionType.Air, name: 'Air' },
    { type: OutsideConnectionType.Ship, name: 'Ship' },
  ];

  // Create dropdown items for each connection type
  let selectedTypeName: string = 'Road';
  const connectionTypeDropdownItems: JSX.Element[] = connectionTypes.map(connection => {
    const selected: boolean = connection.type === selectedType;

    // Get the name of the selected type
    if (selected) {
      selectedTypeName = connection.name;
    }

    // Return a dropdown item
    return (
      <DropdownItem
        theme={ModuleResolver.instance.DropdownClasses}
        value={connection.name}
        closeOnSelect={true}
        selected={selected}
        className={selected ? styles.selectedConnectionDropdownItem : ''}
        onChange={() => SetOutsideConnectionTypeSorting(connection.type)}
      >
        {connection.name}
      </DropdownItem>
    );
  });

  // Return dropdown row
  return (
    <ModuleResolver.instance.Tooltip
      direction="right"
      tooltip={
        <ModuleResolver.instance.FormattedParagraphs
          children={'Select the type of outside connection to filter trade data.'}
        />
      }
      theme={ModuleResolver.instance.TooltipClasses}
      children={
        <div className={styles.connectionDropdownRow}>
          <Dropdown theme={ModuleResolver.instance.DropdownClasses} content={connectionTypeDropdownItems}>
            <DropdownToggle>{selectedTypeName}</DropdownToggle>
          </Dropdown>
        </div>
      }
    />
  );
};
