import React from 'react';

// Map resource names to their icon paths
const resourceIconPaths: { [key: string]: string } = {
  'Beverages': 'Media/Game/Resources/Beverages.svg',
  'Chemicals': 'Media/Game/Resources/Chemicals.svg',
  'Coal': 'Media/Game/Resources/Coal.svg',
  'Concrete': 'Media/Game/Resources/Concrete.svg',
  'ConvenienceFood': 'Media/Game/Resources/ConvenienceFood.svg',
  'Cotton': 'Media/Game/Resources/Cotton.svg',
  'Electronics': 'Media/Game/Resources/Electronics.svg',
  'Entertainment': 'Media/Game/Resources/Entertainment.svg',
  'Financial': 'Media/Game/Resources/Financial.svg',
  'Food': 'Media/Game/Resources/Food.svg',
  'Furniture': 'Media/Game/Resources/Furniture.svg',
  'Garbage': 'Media/Game/Resources/Garbage.svg',
  'Grain': 'Media/Game/Resources/Grain.svg',
  'Groundwater': 'Media/Game/Resources/Groundwater.svg',
  'Livestock': 'Media/Game/Resources/Livestock.svg',
  'LocalMail': 'Media/Game/Resources/LocalMail.svg',
  'Lodging': 'Media/Game/Resources/Lodging.svg',
  'Machinery': 'Media/Game/Resources/Machinery.svg',
  'Meals': 'Media/Game/Resources/Meals.svg',
  'Media': 'Media/Game/Resources/Media.svg',
  'Metals': 'Media/Game/Resources/Metals.svg',
  'Minerals': 'Media/Game/Resources/Minerals.svg',
  'Oil': 'Media/Game/Resources/Oil.svg',
  'Ore': 'Media/Game/Resources/Ore.svg',
  'OutgoingMail': 'Media/Game/Resources/OutgoingMail.svg',
  'Paper': 'Media/Game/Resources/Paper.svg',
  'Petrochemicals': 'Media/Game/Resources/Petrochemicals.svg',
  'Pharmaceuticals': 'Media/Game/Resources/Pharmaceuticals.svg',
  'Plastics': 'Media/Game/Resources/Plastics.svg',
  'Recreation': 'Media/Game/Resources/Recreation.svg',
  'Software': 'Media/Game/Resources/Software.svg',
  'Steel': 'Media/Game/Resources/Steel.svg',
  'Stone': 'Media/Game/Resources/Stone.svg',
  'Telecom': 'Media/Game/Resources/Telecom.svg',
  'Textiles': 'Media/Game/Resources/Textiles.svg',
  'Timber': 'Media/Game/Resources/Timber.svg',
  'UnsortedMail': 'Media/Game/Resources/UnsortedMail.svg',
  'Vegetables': 'Media/Game/Resources/Vegetables.svg',
  'Vehicles': 'Media/Game/Resources/Vehicles.svg',
  'Wood': 'Media/Game/Resources/Wood.svg'
};

// Component to display the resource icon
export const ResourceIcon: React.FC<{ resourceName: string }> = ({ resourceName }) => {
  const iconPath = resourceIconPaths[resourceName];
  if (!iconPath) return null;

  return <img src={iconPath} alt={resourceName} className="resource-icon" />;
};

// Helper function to get resource icon
export const getResourceIcon = (resourceName: string) => {
  return <ResourceIcon resourceName={resourceName} />;
};
