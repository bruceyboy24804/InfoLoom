import { useValue } from 'cs2/api';
import * as bindings from '../../bindings';
import React, { useCallback } from 'react';
import { Button } from 'cs2/ui';
import styles from './SankeyMenu.module.scss';
import BudgetSankey from "mods/InfoLoomSections/SankeyUI's/BudgetSankeyUI/budgetSankey";
import WorkforcePipelineSankey from "mods/InfoLoomSections/SankeyUI's/WorkforcePipelineSankeyUI/workforcePipelineSankey";
import { Localekeys } from 'mods/locale';
import { useLocalization } from 'cs2/l10n';

interface SectionConfig {
  component: JSX.Element;
  openState: () => boolean;
  toggle: (state: boolean) => void;
  displayName: string | null;
}

export function SankeyMenuButton(): JSX.Element {
  const { translate } = useLocalization();

  const sections: Record<string, SectionConfig> = {
    'Budget Sankey': {
      component: <BudgetSankey />,
      openState: () => useValue(bindings.BudgetSankeyOpen),
      toggle: bindings.SetBudgetSankeyOpen,
      displayName: null,
    },
    'Workforce Pipeline': {
      component: <WorkforcePipelineSankey />,
      openState: () => useValue(bindings.WorkforcePipelineSankeyOpen),
      toggle: bindings.SetWorkforcePipelineSankeyOpen,
      displayName: null,
    },
  };

  const sankeyMenuOpen = useValue(bindings.SankeyMenuOpen);
  const sectionStates = Object.fromEntries(
    Object.entries(sections).map(([name, config]) => [name, config.openState()])
  );

  const toggleSection = useCallback((name: string) => sections[name]?.toggle(!sectionStates[name]), [sectionStates]);

  return (
    <div>
      {sankeyMenuOpen && (
        <div className={styles.panel}>
          <div className={styles.buttonRow}>
            {Object.keys(sections).map(name => {
              let displayName: string;
              switch (name) {
                case 'Budget Sankey':
                  displayName = translate(Localekeys.MenuSankeyBudget, 'Budget Sankey') ?? 'Budget Sankey';
                  break;
                case 'Workforce Sankey':
                  displayName = translate(Localekeys.MenuSankeyWorkforce, 'Workforce Sankey') ?? 'Workforce Sankey';
                  break;
                default:
                  displayName = name;
              }

              return (
                <Button
                  key={name}
                  variant="flat"
                  aria-label={name}
                  aria-expanded={sectionStates[name]}
                  className={`${styles.InfoLoomButton} ${sectionStates[name] ? styles.buttonSelected : ''}`}
                  onClick={() => toggleSection(name)}
                >
                  {displayName}
                </Button>
              );
            })}
          </div>
        </div>
      )}

      {Object.entries(sections).map(
        ([name, { component }]) =>
          sectionStates[name] && (
            <div key={name}>
              {React.cloneElement(component, {
                onClose: (e?: React.SyntheticEvent) => {
                  if (e && typeof e.stopPropagation === 'function') {
                    e.stopPropagation();
                  }
                  toggleSection(name);
                },
              })}
            </div>
          )
      )}
    </div>
  );
}

export default SankeyMenuButton;
