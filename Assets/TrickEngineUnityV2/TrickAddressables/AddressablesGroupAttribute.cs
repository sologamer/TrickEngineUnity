using System;

public class AddressablesGroupAttribute : Attribute
{
    public string Value;

    public AddressablesGroupAttribute(string groupName)
    {
        Value = groupName;
    }

    
    public const string DefaultGameGroup = "DefaultGameGroup";
    public const string Maps = "Maps";
    public const string MapsData = "Maps-Data";
    public const string Pets = "Pets";
    public const string Icons = "Icons";
    public const string SkinsDefault = "Skins-Default";
    public const string Units = "Units";
    public const string Abilities = "Abilities";
    public const string Items = "Items";
    public const string Audio = "Audio";
    public const string UI = "UI";
    public const string UILocal = "UI-Local";
    public const string CollectableTargets = "CollectableTargets";
    public const string Animations = "Animations";
    public const string Localizations = "Localizations";
}