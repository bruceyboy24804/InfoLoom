const PRIMARY_KEY = 'INFOLOOM';

type SECTION =
  | 'menu'
  | 'commercial'
  | 'demand'
  | 'tradecost'
  | 'workforce'
  | 'workplace'
  | 'residential'
  | 'industrial'
  | 'demographic'
  | 'misc';

export const getKey = (name: string, section?: SECTION, decorator?: string): string => {
  let key = `${PRIMARY_KEY}.${name}`;

  if (section) {
    key = `${PRIMARY_KEY}.${section.toUpperCase()}.${name}`;
  }

  if (decorator) {
    key += `|${decorator}`;
  }

  return key;
};
