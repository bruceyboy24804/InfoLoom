import { bindValue, useValue, trigger } from 'cs2/api';
import { useLocalization } from 'cs2/l10n';
import { Dropdown, DropdownItem, DropdownToggle, FOCUS_DISABLED } from 'cs2/ui';

import styles from '../IndustrialCompany.module.scss';
import mod from 'mod.json';
import { ModuleResolver } from '../../../../InfoloomInfoviewContents/ModuleResolver/moduleResolver';

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
            trigger: 'SetSelectedInput1Resource'
        },
        input2: {
            list: 'listOfInput2Resources',
            selected: 'selectedInput2Resource',
            trigger: 'SetSelectedInput2Resource'
        },
        output: {
            list: 'listOfOutputResources',
            selected: 'selectedOutputResource',
            trigger: 'SetSelectedOutputResource'
        }
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
                {resourceName}
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