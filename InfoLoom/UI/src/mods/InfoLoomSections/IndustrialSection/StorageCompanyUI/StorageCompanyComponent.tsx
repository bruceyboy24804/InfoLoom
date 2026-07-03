import { StorageCompaniesBinding } from '../../../bindings';
import { StorageCompanyInfo } from '../../../domain/StorageCompanyInfo';
import { ILPanel } from '../../../ILPanel/ilPanelComponent';
import { HeaderComponent } from './Components/headerComponent/headerComponent';
import { ItemComponent } from './Components/itemComponent/itemComponent';
import { bindValue, trigger, useValue } from 'cs2/api';
import { Number2 } from 'cs2/bindings';
import mod from 'mod.json';
import { PanelProps, Scrollable } from 'cs2/ui';
import styles from './StorageCompanyComponent.module.scss';
type StorageCompanyUISettings = {
  panelPositionX: number;
  panelPositionY: number;
};
const panelPosition$ = bindValue<StorageCompanyUISettings>(mod.id, 'StorageCompanyUISettings', {
  panelPositionX: 0.5,
  panelPositionY: 0.5,
});
const StorageCompanyComponent = (props: PanelProps) => {
  // Get panel position from binding or use default
  const panelSettings = useValue(panelPosition$);

  // Convert settings to Number2 format for ILPanel
  const panelPosition = {
    x: panelSettings.panelPositionX,
    y: panelSettings.panelPositionY,
  };

  // Function to update the binding when panel moves
  const handlePositionUpdate = (position: Number2) => {
    // Update the backend binding
    trigger(mod.id, 'StorageCompanyUISettings', {
      panelPositionX: position.x,
      panelPositionY: position.y,
    });
  };

  return (
    <>
      <ILPanel
        onClose={props.onClose}
        position={panelPosition}
        panelMovedTrigger="StoragePanelMoved"
        onPositionUpdate={handlePositionUpdate}
        title="Storage Companies"
        className={styles.panel}
      >
        <Scrollable smooth vertical trackVisibility="scrollable">
          <HeaderComponent />
          <ItemComponent />
        </Scrollable>
      </ILPanel>
    </>
  );
};
export default StorageCompanyComponent;
