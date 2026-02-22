import { Entity } from 'cs2/utils';
import { LocalizedString } from 'cs2/l10n';

export interface StorageCompanyInfo {
  EntityId: Entity;
  Brand: String;
  TransferRequests: number;
  Trips: number;
  OwnedVehicles: number;
  GuestVehicles: number;
}
