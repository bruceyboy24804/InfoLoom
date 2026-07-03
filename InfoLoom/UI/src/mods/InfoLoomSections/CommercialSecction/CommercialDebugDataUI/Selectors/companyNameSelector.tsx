import { bindValue, useValue, trigger } from 'cs2/api';
import { useLocalization } from 'cs2/l10n';
import { Dropdown, DropdownItem, DropdownToggle, FOCUS_DISABLED } from 'cs2/ui';

import styles from './companyNameSelector.module.scss';
import mod from 'mod.json';
import { ModuleResolver } from '../../../../ModuleResolver/moduleResolver';
import {OneWayBinding} from "utils/onewayBinding";
import Trigger from "utils/trigger";
import {TwoWayBinding} from "utils/bidirectionalBinding";

const companyNames$ = new OneWayBinding<string[]>('listOfCommercialCompanyNames', []);
const companyName$ = new TwoWayBinding<string>("CommercialCompanyName", "All Companies");

export const CompanyNameSelector = () => {
  const { translate } = useLocalization();

  // Get list of company names and currently selected company
  const companyNamesList: string[] = useValue(companyNames$.binding);
  const selectedCompany: string = useValue(companyName$.binding);

  // Create dropdown items for each company
  const companyDropdownItems: JSX.Element[] = companyNamesList.map((companyName, index) => {
    const selected: boolean = companyName === selectedCompany;

    return (
      <DropdownItem
        key={`company-${index}`}
        focusKey={FOCUS_DISABLED}
        theme={ModuleResolver.instance.DropdownClasses}
        value={companyName}
        closeOnSelect={true}
        selected={selected}
        className={selected ? styles.selectedCompanyNameDropdownItem : ''}
        onChange={() => companyName$.set(companyName)}
      >
        {companyName}
      </DropdownItem>
    );
  });

  // Get display name for selected company
  const displayName = selectedCompany || 'Select Company';

  // Return dropdown row
  return (
    <ModuleResolver.instance.Tooltip
      direction="right"
      tooltip={
        <ModuleResolver.instance.FormattedParagraphs
          children={translate('InfoLoom.CompanySelector[Tooltip]', 'Select a company to view detailed information.')}
        />
      }
      theme={ModuleResolver.instance.TooltipClasses}
      children={
        <div className={styles.companyNameDropdownRow}>
          <Dropdown theme={ModuleResolver.instance.DropdownClasses} content={companyDropdownItems}>
            <DropdownToggle>{displayName}</DropdownToggle>
          </Dropdown>
        </div>
      }
    />
  );
};
