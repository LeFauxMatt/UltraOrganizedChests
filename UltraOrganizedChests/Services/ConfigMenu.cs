using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Services;
using StardewValley.Inventories;
using StardewValley.Objects;

namespace LeFauxMods.UltraOrganizedChests.Services;

/// <summary>Responsible for handling the mod configuration menu.</summary>
internal sealed class ConfigMenu
{
    private readonly IGenericModConfigMenuApi api = null!;
    private readonly GenericModConfigMenuIntegration gmcm;
    private readonly IModHelper helper;
    private readonly IManifest manifest;

    private readonly Dictionary<string, Dictionary<string, string>> modDataChanges =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Inventory organizer = [];

    public ConfigMenu(IModHelper helper, IManifest manifest)
    {
        this.helper = helper;
        this.manifest = manifest;
        this.gmcm = new GenericModConfigMenuIntegration(manifest, helper.ModRegistry, true);
        if (!this.gmcm.IsLoaded)
        {
            return;
        }

        this.api = this.gmcm.Api;
        this.SetupForTitle();
    }

    private static ModConfig Config => ModState.ConfigHelper.Temp;

    private static ConfigHelper<ModConfig> ConfigHelper => ModState.ConfigHelper;

    public void SetupForGame()
    {
        this.organizer.Clear();
        this.organizer.AddRange(ModState.Organizer);
        this.modDataChanges.Clear();
        foreach (var chest in this.organizer.OfType<Chest>())
        {
            var modData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            this.modDataChanges.Add(chest.GlobalInventoryId, modData);
        }

        var priorityOption = new PriorityOption(this.helper, this.organizer);
        var categorizeOption = new CategorizeOption(this.helper, priorityOption);

        this.gmcm.Register(Reset, Save);

        this.api.AddBoolOption(
            this.manifest,
            static () => Config.EnabledByDefault,
            static value => Config.EnabledByDefault = value,
            I18n.ConfigOption_EnabledByDefault_Name,
            I18n.ConfigOption_EnabledByDefault_Description);

        this.api.AddBoolOption(
            this.manifest,
            static () => Config.OrganizeNightly,
            static value => Config.OrganizeNightly = value,
            I18n.ConfigOption_OrganizeNightly_Name,
            I18n.ConfigOption_OrganizeNightly_Description);

        this.api.AddSectionTitle(this.manifest, I18n.ConfigSection_ChestPriority_Name);
        this.api.AddParagraph(this.manifest, I18n.ConfigSection_ChestPriority_Description);
        this.gmcm.AddComplexOption(priorityOption);
        this.gmcm.AddComplexOption(categorizeOption);

        return;

        void Reset()
        {
            ConfigHelper.Reset();
            this.organizer.Clear();
            this.organizer.AddRange(ModState.Organizer);
            foreach (var (_, modData) in this.modDataChanges)
            {
                modData.Clear();
            }

            this.SetupForGame();
        }

        void Save()
        {
            priorityOption.Save();
            ModState.Organizer.Clear();
            ModState.Organizer.AddRange(this.organizer);
            ModState.Organizer.RemoveEmptySlots();
            ConfigHelper.Save();
        }
    }

    public void SetupForTitle()
    {
        this.gmcm.Register(ConfigHelper.Reset, ConfigHelper.Save, true);

        this.api.AddBoolOption(
            this.manifest,
            static () => Config.EnabledByDefault,
            static value => Config.EnabledByDefault = value,
            I18n.ConfigOption_EnabledByDefault_Name,
            I18n.ConfigOption_EnabledByDefault_Description);

        this.api.AddBoolOption(
            this.manifest,
            static () => Config.OrganizeNightly,
            static value => Config.OrganizeNightly = value,
            I18n.ConfigOption_OrganizeNightly_Name,
            I18n.ConfigOption_OrganizeNightly_Description);
    }
}