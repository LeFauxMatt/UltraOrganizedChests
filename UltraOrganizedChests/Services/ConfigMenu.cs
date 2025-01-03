﻿using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Services;

namespace LeFauxMods.UltraOrganizedChests.Services;

/// <summary>Responsible for handling the mod configuration menu.</summary>
internal sealed class ConfigMenu
{
    private readonly IGenericModConfigMenuApi api = null!;
    private readonly GenericModConfigMenuIntegration gmcm;
    private readonly IModHelper helper;
    private readonly IManifest manifest;
    private readonly List<Item> organizer = [];

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
        var organizerOption = new OrganizerOption(this.helper, this.organizer);

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
        this.gmcm.AddComplexOption(organizerOption);

        return;

        void Reset()
        {
            ConfigHelper.Reset();
            this.organizer.Clear();
            this.organizer.AddRange(ModState.Organizer);
            this.SetupForGame();
        }

        void Save()
        {
            organizerOption.Save();
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