import styles from './itemComponent.module.scss';
import { useValue } from 'cs2/api';
import { StorageCompaniesBinding } from '../../../../../bindings';

export const ItemComponent = (): JSX.Element => {
  const companies = useValue(StorageCompaniesBinding.binding);

  return (
    <>
      {companies
        .filter(
          company =>
            company.TransferRequests > 0 || company.Trips > 0 || company.OwnedVehicles > 0 || company.GuestVehicles > 0
        )
        .map((company, index) => (
          <div key={index} className={styles.row}>
            <div>{company.Brand.toString()}</div>
            <div>{company.TransferRequests}</div>
            <div>{company.Trips}</div>
            <div>{company.OwnedVehicles}</div>
            <div>{company.GuestVehicles}</div>
          </div>
        ))}
    </>
  );
};
