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

// Map display names to icon names and vice versa
const resourceNameMapping: { [key: string]: string } = {
  'MetalOre': 'Ore',
  'CrudeOil': 'Oil',
  'Ore': 'MetalOre',
  'Oil': 'CrudeOil',
  // Add lowercase to PascalCase mappings
  'beverages': 'Beverages',
  'chemicals': 'Chemicals',
  'coal': 'Coal',
  'concrete': 'Concrete',
  'conv.food': 'ConvenienceFood',
  'cotton': 'Cotton',
  'electronics': 'Electronics',
  'entertainment': 'Entertainment',
  'financial': 'Financial',
  'food': 'Food',
  'furniture': 'Furniture',
  'garbage': 'Garbage',
  'grain': 'Grain',
  'groundwater': 'Groundwater',
  'livestock': 'Livestock',
  'localmail': 'LocalMail',
  'lodging': 'Lodging',
  'machinery': 'Machinery',
  'meals': 'Meals',
  'media': 'Media',
  'metals': 'Metals',
  'minerals': 'Minerals',
  'oil': 'Oil',
  'ore': 'Ore',
  'outgoingmail': 'OutgoingMail',
  'paper': 'Paper',
  'petrochemicals': 'Petrochemicals',
  'pharmaceuticals': 'Pharmaceuticals',
  'plastics': 'Plastics',
  'recreation': 'Recreation',
  'software': 'Software',
  'steel': 'Steel',
  'stone': 'Stone',
  'telecom': 'Telecom',
  'textiles': 'Textiles',
  'timber': 'Timber',
  'unsortedmail': 'UnsortedMail',
  'vegetables': 'Vegetables',
  'vehicles': 'Vehicles',
  'wood': 'Wood',
  'metalore': 'Ore',
  'crudeoil': 'Oil'
};

// Helper function to convert any case to proper case for resource lookup
function normalizeResourceName(resourceName: string): string {
  if (!resourceName) return resourceName;

  // First check if there's a direct mapping
  if (resourceNameMapping[resourceName]) {
    return resourceNameMapping[resourceName];
  }

  // Otherwise try to normalize case
  return resourceName.charAt(0).toUpperCase() + resourceName.slice(1);
}

// Component to display the resource icon
export const ResourceIcon: React.FC<{ resourceName: string }> = ({ resourceName }) => {
  if (!resourceName) return null;

  // Normalize the resource name
  const normalizedName = normalizeResourceName(resourceName);
  const iconPath = resourceIconPaths[normalizedName];

  if (!iconPath) {
    console.log(`Resource icon not found for: ${resourceName} (normalized to ${normalizedName})`);
    return null;
  }

  return <img src={iconPath} alt={resourceName} style={{ width: '25rem', height: '25rem' }} />;
};

// Helper function to get resource icon
export function getResourceIcon(resourceName: string): string | undefined {
  if (!resourceName) return undefined;

  const normalizedName = normalizeResourceName(resourceName);
  return resourceIconPaths[normalizedName];
}
