import { StorageCompaniesBinding } from '../../../../../bindings';
import { StorageCompanyInfo } from '../../../../../domain/StorageCompanyInfo';
import styles from './headerComponent.module.scss';
import React from 'react';
export { ILPanel } from '../../../../../ILPanel/ilPanelComponent';

const headerComponent = () => {
  return (
    <div className={styles.headerRow}>
      <div>Brand</div>
      <div>Transfer Requests</div>
      <div>Trips</div>
      <div>Owned Vehicles</div>
      <div>Guest Vehicles</div>
    </div>
  );
};

export const HeaderComponent = () => {
  return headerComponent();
};
