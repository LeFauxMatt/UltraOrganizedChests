using LeFauxMods.Common.Models;
using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using LeFauxMods.UltraOrganizedChests.Services;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;

namespace LeFauxMods.UltraOrganizedChests;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private ConfigMenu configMenu = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);
        I18n.Init(helper.Translation);
        ModState.Init(helper);
        Log.Init(this.Monitor, ModState.Config);

        // Events
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
    }

    private static void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        Log.Info("Organizing Nightly");
        OrganizeAll();
    }

    private static void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!ModState.Enabled)
        {
            return;
        }

        var cursor = ModState.Cursor;
        ModState.OrganizeButton.tryHover(cursor.X, cursor.Y);
        ModState.OrganizeButton.draw(
            e.SpriteBatch,
            ModState.Enabled ? Color.White : Color.Gray * 0.8f,
            1f);

        if (ModState.OrganizeButton.containsPoint(cursor.X, cursor.Y))
        {
            IClickableMenu.drawToolTip(e.SpriteBatch, ModState.OrganizeButton.hoverText, null, null);
        }

        Game1.activeClickableMenu.drawMouse(e.SpriteBatch);
    }

    private static void OrganizeAll()
    {
        // Organize across inventories
        var mutexes = new HashSet<NetMutex>();
        for (var receiveIndex = 0; receiveIndex < ModState.Organizer.Count - 1; receiveIndex++)
        {
            if (ModState.Organizer[receiveIndex] is not Chest receiver)
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

            for (var sendIndex = receiveIndex + 1; sendIndex < ModState.Organizer.Count; sendIndex++)
            {
                if (ModState.Organizer[sendIndex] is not Chest sender)
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

        foreach (var mutex in mutexes.Where(static mutex => mutex.IsLockHeld()))
        {
            mutex.ReleaseLock();
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button is not (SButton.MouseLeft or SButton.ControllerA) ||
            !ModState.TryGetMenu(out _, out var chest, out _))
        {
            return;
        }

        var cursor = ModState.Cursor;
        if (!ModState.OrganizeButton.containsPoint(cursor.X, cursor.Y))
        {
            return;
        }

        // Add current chest to organizer
        if (ModState.TryAddToOrganizer(chest))
        {
            this.configMenu.SetupForGame();
        }

        OrganizeAll();
        this.Helper.Input.Suppress(e.Button);

        // Relaunch menu for chest
        chest.ShowMenu();
        _ = Game1.playSound("Ship");
    }

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e)
    {
        Log.Info("""
                 Config Summary:
                 - EnabledByDefault: {0}
                 - OrganizeNightly: {1}
                 """,
            e.Config.EnabledByDefault,
            e.Config.OrganizeNightly);

        this.Helper.Events.World.ObjectListChanged -= this.OnObjectListChanged;
        this.Helper.Events.GameLoop.DayEnding -= OnDayEnding;

        if (e.Config.EnabledByDefault)
        {
            this.Helper.Events.World.ObjectListChanged += this.OnObjectListChanged;
        }

        if (e.Config.OrganizeNightly)
        {
            this.Helper.Events.GameLoop.DayEnding += OnDayEnding;
        }

        if (Context.IsWorldReady)
        {
            this.configMenu.SetupForGame();
            return;
        }

        if (Context.IsGameLaunched)
        {
            this.configMenu.SetupForTitle();
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var themeHelper = ThemeHelper.Init(this.Helper);
        themeHelper.AddAsset(Constants.TexturePath,
            this.Helper.ModContent.Load<IRawTextureData>("assets/organize.png"));

        this.configMenu = new ConfigMenu(this.Helper, this.ModManifest);
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (ModState.TryGetMenu(out _, out var chest, out _, e.OldMenu))
        {
            var proxy = ModState.Organizer.FirstOrDefault(item =>
                item is Chest itemChest &&
                itemChest.GlobalInventoryId.Equals(chest.GlobalInventoryId, StringComparison.OrdinalIgnoreCase));

            if (proxy is Chest proxyChest)
            {
                // Synchronize changes back to proxy
                proxyChest.CopyFieldsFrom(chest);
                proxyChest.playerChoiceColor.Value = chest.playerChoiceColor.Value;
            }
            else if (chest.GlobalInventoryId?.StartsWith(Constants.Prefix, StringComparison.OrdinalIgnoreCase) == true)
            {
                // Convert chest back to normal inventory
                chest.ToLocalInventory();
            }
        }

        // Setup overlay
        if (ModState.TryGetMenu(out var menu, out chest, out var component, e.NewMenu))
        {
            this.SetupOverlay(menu, chest, component);
        }
    }

    private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
    {
        var setupAny = false;
        foreach (var (_, obj) in e.Added)
        {
            if (obj is not Chest chest)
            {
                continue;
            }

            if (ModState.TryAddToOrganizer(chest))
            {
                setupAny = true;
            }
        }

        if (setupAny)
        {
            this.configMenu.SetupForGame();
        }
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        // Init
        this.configMenu.SetupForTitle();

        // Events
        this.Helper.Events.Display.MenuChanged -= this.OnMenuChanged;
        this.Helper.Events.Display.RenderedActiveMenu -= OnRenderedActiveMenu;
        this.Helper.Events.Input.ButtonPressed -= this.OnButtonPressed;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // Init
        this.configMenu.SetupForGame();

        // Events
        this.Helper.Events.Display.MenuChanged += this.OnMenuChanged;
        this.Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
        this.Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void SetupOverlay(IClickableMenu menu, Chest chest, ClickableComponent component)
    {
        // Adjust organize button
        ModState.OrganizeButton.bounds.X = component.bounds.Right + 16;
        ModState.OrganizeButton.bounds.Y = component.bounds.Y;
        ModState.OrganizeButton.upNeighborID = component.upNeighborID;
        ModState.OrganizeButton.downNeighborID = component.downNeighborID;
        ModState.OrganizeButton.leftNeighborID = component.myID;
        ModState.OrganizeButton.rightNeighborID = component.rightNeighborID;
        component.rightNeighborID = SharedConstants.OrganizeButtonId;
        menu.allClickableComponents.Add(ModState.OrganizeButton);

        // Add inventory to organizer
        if (ModState.Config.EnabledByDefault && ModState.TryAddToOrganizer(chest))
        {
            this.configMenu.SetupForGame();
        }
    }
}