using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Models;
using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using LeFauxMods.UltraOrganizedChests.Services;
using LeFauxMods.UltraOrganizedChests.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;

namespace LeFauxMods.UltraOrganizedChests;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private readonly PerScreen<ClickableTextureComponent?> organizeButton = new();
    private ModConfig config = null!;
    private ConfigHelper<ModConfig> configHelper = null!;
    private GenericModConfigMenuIntegration gmcm = null!;
    private Inventory? organizer;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        I18n.Init(helper.Translation);
        this.configHelper = new ConfigHelper<ModConfig>(this.Helper);
        this.config = this.configHelper.Load();
        Log.Init(this.Monitor, this.config);
        this.gmcm = new GenericModConfigMenuIntegration(this.ModManifest, helper.ModRegistry);

        // Events
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);

        if (this.config.EnabledByDefault)
        {
            helper.Events.World.ObjectListChanged += this.OnObjectListChanged;
        }

        if (this.config.OrganizeNightly)
        {
            helper.Events.GameLoop.DayEnding += this.OnDayEnding;
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button is not (SButton.MouseLeft or SButton.ControllerA) ||
            this.organizeButton.Value is null ||
            Game1.activeClickableMenu is not ItemGrabMenu { sourceItem: Chest { playerChest.Value: true } chest })
        {
            return;
        }

        var pos = Utility.ModifyCoordinatesForUIScale(e.Cursor.GetScaledScreenPixels()).ToPoint();
        if (!this.organizeButton.Value.containsPoint(pos.X, pos.Y))
        {
            return;
        }

        this.OrganizeAll(chest);
        this.Helper.Input.Suppress(e.Button);
    }

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e)
    {
        this.Helper.Events.World.ObjectListChanged -= this.OnObjectListChanged;
        this.Helper.Events.GameLoop.DayEnding -= this.OnDayEnding;

        if (e.Config.EnabledByDefault)
        {
            this.Helper.Events.World.ObjectListChanged += this.OnObjectListChanged;
        }

        if (e.Config.OrganizeNightly)
        {
            this.Helper.Events.GameLoop.DayEnding += this.OnDayEnding;
        }

        if (Context.IsWorldReady)
        {
            this.SetupGameMenu();
            return;
        }

        if (Context.IsGameLaunched)
        {
            this.SetupTitleMenu();
        }
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e) => this.OrganizeAll();

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var themeHelper = ThemeHelper.Init(this.Helper);
        themeHelper.AddAsset(Constants.TexturePath,
            this.Helper.ModContent.Load<IRawTextureData>("assets/organize.png"));
        this.SetupTitleMenu();
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (this.organizer is null)
        {
            return;
        }

        if (e.NewMenu is ItemGrabMenu newMenu && this.TrySetupMenu(newMenu))
        {
            return;
        }

        // Synchronize changes back to proxy
        if (e.OldMenu is ItemGrabMenu
            {
                sourceItem: Chest { playerChest.Value: true } chest
            })
        {
            var proxy = this.organizer.FirstOrDefault(item =>
                item is Chest itemChest &&
                itemChest.GlobalInventoryId.Equals(chest.GlobalInventoryId, StringComparison.OrdinalIgnoreCase));

            proxy?.CopyFieldsFrom(chest);
        }

        this.organizeButton.Value = null;
    }

    private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
    {
        if (this.organizer is null)
        {
            return;
        }

        var setupAny = false;
        foreach (var (_, obj) in e.Added)
        {
            if (obj is Chest chest && this.TryAddToOrganizer(chest))
            {
                setupAny = true;
            }
        }

        if (setupAny)
        {
            this.SetupGameMenu();
        }
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (this.organizer is null ||
            this.organizeButton.Value is null ||
            Game1.activeClickableMenu is not ItemGrabMenu { sourceItem: Chest { playerChest.Value: true } chest })
        {
            return;
        }

        var enabled = this.organizer.Any(item =>
            item is Chest itemChest &&
            itemChest.GlobalInventoryId.Equals(chest.GlobalInventoryId, StringComparison.OrdinalIgnoreCase));

        var pos = Utility.ModifyCoordinatesForUIScale(this.Helper.Input.GetCursorPosition().GetScaledScreenPixels())
            .ToPoint();
        this.organizeButton.Value.tryHover(pos.X, pos.Y);
        this.organizeButton.Value.draw(
            e.SpriteBatch,
            enabled ? Color.White : Color.Gray * 0.8f,
            1f);

        if (this.organizeButton.Value.containsPoint(pos.X, pos.Y))
        {
            IClickableMenu.drawToolTip(e.SpriteBatch, this.organizeButton.Value.hoverText, null, null);
        }

        Game1.activeClickableMenu.drawMouse(e.SpriteBatch);
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        // Init
        this.organizer = null;
        this.SetupTitleMenu();

        // Events
        this.Helper.Events.Display.MenuChanged -= this.OnMenuChanged;
        this.Helper.Events.Display.RenderedActiveMenu -= this.OnRenderedActiveMenu;
        this.Helper.Events.Input.ButtonPressed -= this.OnButtonPressed;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // Init
        this.organizer = Game1.player.team.GetOrCreateGlobalInventory(Constants.GlobalInventoryId);
        this.SetupGameMenu();

        // Events
        this.Helper.Events.Display.MenuChanged += this.OnMenuChanged;
        this.Helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
        this.Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void OrganizeAll(Chest? chest = null)
    {
        if (this.organizer is null)
        {
            return;
        }

        // Check if current chest is included in organizer
        if (chest is not null && this.TryAddToOrganizer(chest))
        {
            this.SetupGameMenu();
        }

        // Organize across inventories
        var mutexes = new HashSet<NetMutex>();
        for (var receiveIndex = 0; receiveIndex < this.organizer.Count - 1; receiveIndex++)
        {
            if (this.organizer[receiveIndex] is not Chest receiver)
            {
                continue;
            }

            var targetItems = receiver.GetItemsForPlayer();
            var targetMutex = receiver.GetMutex();
            if (mutexes.Add(targetMutex))
            {
                targetMutex.RequestLock(failed: () => mutexes.Remove(targetMutex));
            }

            ItemGrabMenu.organizeItemsInList(targetItems);

            for (var sendIndex = receiveIndex + 1; sendIndex < this.organizer.Count; sendIndex++)
            {
                if (this.organizer[sendIndex] is not Chest sender)
                {
                    continue;
                }

                var sourceItems = sender.GetItemsForPlayer();
                var sourceMutex = sender.GetMutex();
                if (mutexes.Add(sourceMutex))
                {
                    sourceMutex.RequestLock(failed: () => mutexes.Remove(sourceMutex));
                }

                for (var i = sourceItems.Count - 1; i >= 0; i--)
                {
                    if (sourceItems[i] is not { } item || !targetItems.Any(item.canStackWith))
                    {
                        continue;
                    }

                    if (!targetMutex.IsLockHeld() || !sourceMutex.IsLockHeld())
                    {
                        continue;
                    }

                    var amount = item.Stack;
                    var remaining = receiver.addItem(item);
                    if (remaining is not null && remaining.Stack == amount)
                    {
                        continue;
                    }

                    Log.Trace("Added item {0} from {1} to {2}", item.Name, sender.Name, receiver.Name);

                    if (remaining != null)
                    {
                        continue;
                    }

                    sourceItems.Remove(item);
                    sourceItems.RemoveEmptySlots();
                }
            }
        }

        foreach (var mutex in mutexes.Where(mutex => mutex.IsLockHeld()))
        {
            mutex.ReleaseLock();
        }

        if (chest is null)
        {
            return;
        }

        chest.ShowMenu();
        _ = Game1.playSound("Ship");
    }

    private void SetupGameMenu()
    {
        if (!this.gmcm.IsLoaded || this.organizer is null)
        {
            return;
        }

        var tempConfig = this.configHelper.Load();
        var defaultConfig = new ModConfig();

        var tempItems = new List<Item>(this.organizer);
        var organizerOption = new OrganizerOption(this.Helper, tempItems);

        this.gmcm.Register(Reset, Save);
        this.gmcm.Api.AddBoolOption(
            this.ModManifest,
            () => tempConfig.EnabledByDefault,
            value => tempConfig.EnabledByDefault = value,
            I18n.ConfigOption_EnabledByDefault_Name,
            I18n.ConfigOption_EnabledByDefault_Description);

        this.gmcm.Api.AddBoolOption(
            this.ModManifest,
            () => tempConfig.OrganizeNightly,
            value => tempConfig.OrganizeNightly = value,
            I18n.ConfigOption_OrganizeNightly_Name,
            I18n.ConfigOption_OrganizeNightly_Description);

        this.gmcm.Api.AddSectionTitle(this.ModManifest, I18n.ConfigSection_ChestPriority_Name);
        this.gmcm.Api.AddParagraph(this.ModManifest, I18n.ConfigSection_ChestPriority_Description);
        this.gmcm.AddComplexOption(organizerOption);

        return;

        void Reset()
        {
            defaultConfig.CopyTo(tempConfig);
            tempItems.Clear();
            tempItems.AddRange(this.organizer);
            this.SetupGameMenu();
        }

        void Save()
        {
            tempConfig.CopyTo(this.config);
            organizerOption.Save();
            this.organizer.Clear();
            this.organizer.AddRange(tempItems);
            this.organizer.RemoveEmptySlots();
            this.configHelper.Save(tempConfig);
            this.gmcm.Api.OpenModMenu(this.ModManifest);
        }
    }

    private void SetupTitleMenu()
    {
        if (!this.gmcm.IsLoaded)
        {
            return;
        }

        var tempConfig = this.configHelper.Load();
        var defaultConfig = new ModConfig();

        this.gmcm.Register(Reset, Save, true);
        this.gmcm.Api.AddBoolOption(
            this.ModManifest,
            () => tempConfig.EnabledByDefault,
            value => tempConfig.EnabledByDefault = value,
            I18n.ConfigOption_EnabledByDefault_Name,
            I18n.ConfigOption_EnabledByDefault_Description);

        this.gmcm.Api.AddBoolOption(
            this.ModManifest,
            () => tempConfig.OrganizeNightly,
            value => tempConfig.OrganizeNightly = value,
            I18n.ConfigOption_OrganizeNightly_Name,
            I18n.ConfigOption_OrganizeNightly_Description);

        return;

        void Reset()
        {
            defaultConfig.CopyTo(tempConfig);
        }

        void Save()
        {
            tempConfig.CopyTo(this.config);
            this.configHelper.Save(tempConfig);
        }
    }

    private bool TryAddToOrganizer(Chest chest)
    {
        if (this.organizer is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(chest.GlobalInventoryId))
        {
            var chestId = CommonHelper.GetUniqueId(Constants.Prefix);
            chest.ToGlobalInventory(chestId);
        }

        this.organizer.AddProxy(chest);
        return true;
    }

    private bool TrySetupMenu(ItemGrabMenu menu)
    {
        if (this.organizer is null ||
            menu is not { organizeButton: { } component, sourceItem: Chest { playerChest.Value: true } chest })
        {
            return false;
        }

        this.organizeButton.Value = new ClickableTextureComponent(
            "organize",
            component.bounds with { X = component.bounds.Right + 16 },
            null,
            I18n.Component_OrganizeButton_HoverText(),
            this.Helper.GameContent.Load<Texture2D>(Constants.TexturePath),
            new Rectangle(0, 0, 16, 16),
            Game1.pixelZoom);

        // Add inventory to organizer
        if (this.config.EnabledByDefault)
        {
            this.TryAddToOrganizer(chest);
            this.SetupGameMenu();
            return true;
        }

        // Convert inventory back to normal
        if (chest.GlobalInventoryId?.StartsWith(Constants.Prefix, StringComparison.OrdinalIgnoreCase) == true &&
            !this.organizer.Any(item =>
                item is Chest itemChest &&
                itemChest.GlobalInventoryId.Equals(chest.GlobalInventoryId, StringComparison.OrdinalIgnoreCase)))
        {
            chest.ToLocalInventory();
        }

        return true;
    }
}
