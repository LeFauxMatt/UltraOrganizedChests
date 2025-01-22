using LeFauxMods.Common.Models;
using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using LeFauxMods.UltraOrganizedChests.Services;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;

namespace LeFauxMods.UltraOrganizedChests;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private readonly PerScreen<IList<Item>?> grabInventory = new();
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

    [EventPriority(EventPriority.High)]
    private static void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!ModState.Active)
        {
            return;
        }

        Game1.InUIMode(() =>
        {
            var cursor = ModState.Cursor;
            ModState.OrganizeButton.tryHover(cursor.X, cursor.Y);
            ModState.OrganizeButton.draw(
                e.SpriteBatch,
                ModState.Enabled ? Color.White : Color.Gray * 0.8f,
                1f);
        });
    }

    private static void OrganizeAll()
    {
        var itemGrabMenu = new ItemGrabMenu([]);

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

                if (targetMutex.IsLockHeld() && sourceMutex.IsLockHeld())
                {
                    itemGrabMenu.ItemsToGrabMenu.actualInventory = targetItems;
                    itemGrabMenu.inventory.actualInventory = sourceItems;
                    itemGrabMenu.FillOutStacks();
                    sourceItems.RemoveEmptySlots();
                }
            }
        }

        foreach (var mutex in mutexes.Where(static mutex => mutex.IsLockHeld()))
        {
            mutex.ReleaseLock();
        }
    }

    [EventPriority(EventPriority.Low)]
    private void OnRenderedActiveMenuHover(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!ModState.TryGetMenu(out var menu, out var chest, out _))
        {
            return;
        }

        Game1.InUIMode(() =>
        {
            var cursor = ModState.Cursor;
            if (ModState.Active && ModState.OrganizeButton.containsPoint(cursor.X, cursor.Y))
            {
                IClickableMenu.drawToolTip(e.SpriteBatch, ModState.OrganizeButton.hoverText, null, null);
                Game1.activeClickableMenu.drawMouse(e.SpriteBatch);
            }

            if (this.grabInventory.Value is null ||
                !this.Helper.Input.IsSuppressed(SButton.MouseLeft) ||
                this.Helper.Input.GetState(ModState.Config.MoveItemsModifier) is not SButtonState.Held)
            {
                return;
            }

            menu.heldItem ??= this.grabInventory.Value == menu.inventory.actualInventory
                ? menu.inventory.leftClick(cursor.X, cursor.Y, menu.heldItem)
                : menu.ItemsToGrabMenu.leftClick(cursor.X, cursor.Y, menu.heldItem);

            if (menu.heldItem is not null)
            {
                menu.heldItem = this.grabInventory.Value == menu.inventory.actualInventory
                    ? chest.addItem(menu.heldItem)
                    : Game1.player.addItemToInventory(menu.heldItem);
            }
        });
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!e.Button.IsUseToolButton() ||
            !ModState.TryGetMenu(out var menu, out var chest, out _))
        {
            return;
        }

        Game1.InUIMode(() =>
        {
            var cursor = ModState.Cursor;
            if (ModState.OrganizeButton.containsPoint(cursor.X, cursor.Y))
            {
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
                return;
            }

            if (menu.heldItem is not null ||
                this.Helper.Input.GetState(ModState.Config.MoveItemsModifier) is not SButtonState.Held)
            {
                return;
            }

            menu.heldItem = menu.ItemsToGrabMenu.leftClick(cursor.X, cursor.Y, null);
            if (menu.heldItem is not null)
            {
                this.grabInventory.Value = menu.ItemsToGrabMenu.actualInventory;
                this.Helper.Input.Suppress(e.Button);
                return;
            }

            menu.heldItem = menu.inventory.leftClick(cursor.X, cursor.Y, null);
            if (menu.heldItem is not null)
            {
                this.grabInventory.Value = menu.inventory.actualInventory;
                this.Helper.Input.Suppress(e.Button);
            }
        });
    }

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e)
    {
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
            this.Helper.ModContent.Load<IRawTextureData>("assets/icons.png"));

        this.configMenu = new ConfigMenu(this.Helper, this.ModManifest);
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (ModState.TryGetMenu(out _, out var chest, out _, e.OldMenu))
        {
            ModState.Organizer.SyncBackup(chest);
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
        this.Helper.Events.Display.RenderedActiveMenu -= this.OnRenderedActiveMenuHover;
        this.Helper.Events.Input.ButtonPressed -= this.OnButtonPressed;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        // Init
        this.configMenu.SetupForGame();

        // Events
        this.Helper.Events.Display.MenuChanged += this.OnMenuChanged;
        this.Helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
        this.Helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenuHover;
        this.Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
    }

    private void SetupOverlay(IClickableMenu menu, Chest chest, ClickableComponent component)
    {
        // Adjust organize button
        ModState.OrganizeButton.bounds.X = component.bounds.Right + 16;
        ModState.OrganizeButton.bounds.Y = component.bounds.Y;
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