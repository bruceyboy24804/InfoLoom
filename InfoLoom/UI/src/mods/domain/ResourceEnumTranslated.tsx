import { useLocalization } from "cs2/l10n";
const { translate } = useLocalization();

export const ResourceEnumTranslated = {
    NoResource: translate("InfoLoomTwo.Resource[NoResource]", "No Resource"),
    Money: translate("InfoLoomTwo.Resource[Money]", "Money"),
    Grain: translate("InfoLoomTwo.Resource[Grain]", "Grain"),
    ConvenienceFood: translate("InfoLoomTwo.Resource[ConvenienceFood]", "Convenience Food"),
    Food: translate("InfoLoomTwo.Resource[Food]", "Food"),
    Vegetables: translate("InfoLoomTwo.Resource[Vegetables]", "Vegetables"),
    Meals: translate("InfoLoomTwo.Resource[Meals]", "Meals"),
    Wood: translate("InfoLoomTwo.Resource[Wood]", "Wood"),
    Timber: translate("InfoLoomTwo.Resource[Timber]", "Timber"),
    Paper: translate("InfoLoomTwo.Resource[Paper]", "Paper"),
    Furniture: translate("InfoLoomTwo.Resource[Furniture]", "Furniture"),
    Vehicles: translate("InfoLoomTwo.Resource[Vehicles]", "Vehicles"),
    Lodging: translate("InfoLoomTwo.Resource[Lodging]", "Lodging"),
    UnsortedMail: translate("InfoLoomTwo.Resource[UnsortedMail]", "Unsorted Mail"),
    LocalMail: translate("InfoLoomTwo.Resource[LocalMail]", "Local Mail"),
    OutgoingMail: translate("InfoLoomTwo.Resource[OutgoingMail]", "Outgoing Mail"),
    Oil: translate("InfoLoomTwo.Resource[Oil]", "Oil"),
    Petrochemicals: translate("InfoLoomTwo.Resource[Petrochemicals]", "Petrochemicals"),
    Ore: translate("InfoLoomTwo.Resource[Ore]", "Ore"),
    Plastics: translate("InfoLoomTwo.Resource[Plastics]", "Plastics"),
    Metals: translate("InfoLoomTwo.Resource[Metals]", "Metals"),
    Electronics: translate("InfoLoomTwo.Resource[Electronics]", "Electronics"),
    Software: translate("InfoLoomTwo.Resource[Software]", "Software"),
    Coal: translate("InfoLoomTwo.Resource[Coal]", "Coal"),
    Stone: translate("InfoLoomTwo.Resource[Stone]", "Stone"),
    Livestock: translate("InfoLoomTwo.Resource[Livestock]", "Livestock"),
    Cotton: translate("InfoLoomTwo.Resource[Cotton]", "Cotton"),
    Steel: translate("InfoLoomTwo.Resource[Steel]", "Steel"),
    Minerals: translate("InfoLoomTwo.Resource[Minerals]", "Minerals"),
    Concrete: translate("InfoLoomTwo.Resource[Concrete]", "Concrete"),
    Machinery: translate("InfoLoomTwo.Resource[Machinery]", "Machinery"),
    Chemicals: translate("InfoLoomTwo.Resource[Chemicals]", "Chemicals"),
    Pharmaceuticals: translate("InfoLoomTwo.Resource[Pharmaceuticals]", "Pharmaceuticals"),
    Beverages: translate("InfoLoomTwo.Resource[Beverages]", "Beverages"),
    Textiles: translate("InfoLoomTwo.Resource[Textiles]", "Textiles"),
    Telecom: translate("InfoLoomTwo.Resource[Telecom]", "Telecom"),
    Financial: translate("InfoLoomTwo.Resource[Financial]", "Financial"),
    Media: translate("InfoLoomTwo.Resource[Media]", "Media"),
    Entertainment: translate("InfoLoomTwo.Resource[Entertainment]", "Entertainment"),
    Recreation: translate("InfoLoomTwo.Resource[Recreation]", "Recreation"),
    Garbage: translate("InfoLoomTwo.Resource[Garbage]", "Garbage"),
    Fish: translate("InfoLoomTwo.Resource[Fish]", "Fish"),
    Last: translate("InfoLoomTwo.Resource[Last]", "Last"),
    All: translate("InfoLoomTwo.Resource[All]", "All")
}

// Helper to get the translated string for a resource
export function getResourceTranslation(resource: keyof typeof ResourceEnumTranslated) {
    return ResourceEnumTranslated[resource];
}
