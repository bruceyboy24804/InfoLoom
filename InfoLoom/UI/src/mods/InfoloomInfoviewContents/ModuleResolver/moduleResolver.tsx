import { Theme      } from "cs2/bindings";
import { getModule  } from "cs2/modding";

// Define props used by component modules.
type ToolTipProps =
    {
        tooltip: any,
        forceVisible?: any,
        disabled?: any,
        theme: any,
        direction: string,
        alignment?: any,
        className?: any,
        children: any,
        anchorElRef?: any
    }

type FormattedParagraphsProps =
    {
        focusKey?: any,
        text?: any,
        theme?: any,
        renderer?: any,
        className?: any,
        children: any,
        onLinkSelect?: any,
        maxLineLength?: any,
        splitLineLength?: any, 
        other?: any[]
    }

type LocalizedNumberProps =
    {
        value: number;
        unit?: any;
        signed?: boolean;
    }

// Provide access to modules from index.js.
export class ModuleResolver
{
    // Define instance.
    private static _instance: ModuleResolver = new ModuleResolver();
    public static get instance(): ModuleResolver { return this._instance }

    // Define component modules.
    private _tooltip: any;
    private _formattedParagraphs: any;
    private _localizedNumber: any;

    // Define code modules.
    private _buildCssLinearGradient: any;
    private _loc: any;
    private _uiSound: any;

    // Define SCSS modules.
    private _tooltipClasses: any;
    private _infomodeItemClasses: any;
    private _transparentButtonClasses: any;
    private _checkboxClasses: any;
    private _dropdownClasses: any;

    // Provide access to component modules.
    public get Tooltip():               (props: ToolTipProps)               => JSX.Element { return this._tooltip               ?? (this._tooltip               = getModule("game-ui/common/tooltip/tooltip.tsx",               "Tooltip"               )); }
    public get FormattedParagraphs():   (props: FormattedParagraphsProps)   => JSX.Element { return this._formattedParagraphs   ?? (this._formattedParagraphs   = getModule("game-ui/common/text/formatted-paragraphs.tsx",     "FormattedParagraphs"   )); }
    public get LocalizedNumber():       (props: LocalizedNumberProps)       => JSX.Element { return this._localizedNumber       ?? (this._localizedNumber       = getModule("game-ui/common/localization/localized-number.tsx", "LocalizedNumber"       )); }

    // Provide access to code modules.
    public get BuildCssLinearGradient() { return this._buildCssLinearGradient   ?? (this._buildCssLinearGradient    = getModule("game-ui/common/color.ts",                          "buildCssLinearGradient")); }
    public get Loc()                    { return this._loc                      ?? (this._loc                       = getModule("game-ui/common/localization/loc.generated.ts",     "Loc"                   )); }
    public get UISound()                { return this._uiSound                  ?? (this._uiSound                   = getModule("game-ui/common/data-binding/audio-bindings.ts",    "UISound"               )); }

    // Provide access to SCSS modules.
    public get TooltipClasses():            Theme | any { return this._tooltipClasses           ?? (this._tooltipClasses            = getModule("game-ui/common/tooltip/tooltip.module.scss",                                                                   "classes")); }
    public get InfomodeItemClasses():       Theme | any { return this._infomodeItemClasses      ?? (this._infomodeItemClasses       = getModule("game-ui/game/components/infoviews/active-infoview-panel/components/infomode-item/infomode-item.module.scss",   "classes")); }
    public get TransparentButtonClasses():  Theme | any { return this._transparentButtonClasses ?? (this._transparentButtonClasses  = getModule("game-ui/game/themes/transparent-button.module.scss",                                                           "classes")); }
    public get CheckboxClasses():           Theme | any { return this._checkboxClasses          ?? (this._checkboxClasses           = getModule("game-ui/common/input/toggle/checkbox/checkbox.module.scss",                                                    "classes")); }
    public get DropdownClasses():           Theme | any { return this._dropdownClasses          ?? (this._dropdownClasses           = getModule("game-ui/menu/themes/dropdown.module.scss",                                                                     "classes")); }
}