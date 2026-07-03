import { useLocalization } from 'cs2/l10n';
import { Localekeys } from 'mods/locale';

export const resourceKeyMap: Record<string, [string, string]> = {
  Money: [Localekeys.Resource_Money, 'Money'],
  Grain: [Localekeys.Resource_Grain, 'Grain'],
  ConvenienceFood: [Localekeys.Resource_ConvenienceFood, 'Convenience Food'],
  Food: [Localekeys.Resource_Food, 'Food'],
  Vegetables: [Localekeys.Resource_Vegetables, 'Vegetables'],
  Meals: [Localekeys.Resource_Meals, 'Meals'],
  Wood: [Localekeys.Resource_Wood, 'Wood'],
  Timber: [Localekeys.Resource_Timber, 'Timber'],
  Paper: [Localekeys.Resource_Paper, 'Paper'],
  Furniture: [Localekeys.Resource_Furniture, 'Furniture'],
  Vehicles: [Localekeys.Resource_Vehicles, 'Vehicles'],
  Lodging: [Localekeys.Resource_Lodging, 'Lodging'],
  UnsortedMail: [Localekeys.Resource_UnsortedMail, 'Unsorted Mail'],
  LocalMail: [Localekeys.Resource_LocalMail, 'Local Mail'],
  OutgoingMail: [Localekeys.Resource_OutgoingMail, 'Outgoing Mail'],
  Oil: [Localekeys.Resource_Oil, 'Oil'],
  Petrochemicals: [Localekeys.Resource_Petrochemicals, 'Petrochemicals'],
  Ore: [Localekeys.Resource_Ore, 'Ore'],
  Plastics: [Localekeys.Resource_Plastics, 'Plastics'],
  Metals: [Localekeys.Resource_Metals, 'Metals'],
  Electronics: [Localekeys.Resource_Electronics, 'Electronics'],
  Software: [Localekeys.Resource_Software, 'Software'],
  Coal: [Localekeys.Resource_Coal, 'Coal'],
  Stone: [Localekeys.Resource_Stone, 'Stone'],
  Livestock: [Localekeys.Resource_Livestock, 'Livestock'],
  Cotton: [Localekeys.Resource_Cotton, 'Cotton'],
  Steel: [Localekeys.Resource_Steel, 'Steel'],
  Minerals: [Localekeys.Resource_Minerals, 'Minerals'],
  Concrete: [Localekeys.Resource_Concrete, 'Concrete'],
  Machinery: [Localekeys.Resource_Machinery, 'Machinery'],
  Chemicals: [Localekeys.Resource_Chemicals, 'Chemicals'],
  Pharmaceuticals: [Localekeys.Resource_Pharmaceuticals, 'Pharmaceuticals'],
  Beverages: [Localekeys.Resource_Beverages, 'Beverages'],
  Textiles: [Localekeys.Resource_Textiles, 'Textiles'],
  Telecom: [Localekeys.Resource_Telecom, 'Telecom'],
  Financial: [Localekeys.Resource_Financial, 'Financial'],
  Media: [Localekeys.Resource_Media, 'Media'],
  Entertainment: [Localekeys.Resource_Entertainment, 'Entertainment'],
  Recreation: [Localekeys.Resource_Recreation, 'Recreation'],
  Garbage: [Localekeys.Resource_Garbage, 'Garbage'],
  Fish: [Localekeys.Resource_Fish, 'Fish'],
};

export const useResourceTranslation = () => {
  const { translate } = useLocalization();
  return (resource: keyof typeof resourceKeyMap) => {
    const [key, fallback] = resourceKeyMap[resource] ?? [undefined, resource];
    return key ? translate(key, fallback) || fallback : resource;
  };
};
