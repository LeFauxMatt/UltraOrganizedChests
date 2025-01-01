using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LeFauxMods.UltraOrganizedChests.Services;

/// <summary>Responsible for managing state.</summary>
internal sealed class ModState
{
    private static ModState? Instance;

    private readonly ConfigHelper<ModConfig> configHelper;
    private readonly PerScreen<object?> context = new();
    private readonly PerScreen<bool> enabled = new();
    private readonly IModHelper helper;

    private readonly PerScreen<ClickableTextureComponent> organizeButton = new(static () =>
        new ClickableTextureComponent(
            "organize",
            new Rectangle(0, 0, Game1.tileSize, Game1.tileSize),
            null,
            I18n.Component_OrganizeButton_HoverText(),
            Game1.content.Load<Texture2D>(Constants.TexturePath),
            new Rectangle(0, 0, 16, 16),
            Game1.pixelZoom) { myID = SharedConstants.OrganizeButtonId });

    private Inventory? organizer;

    private ModState(IModHelper helper)
    {
        this.helper = helper;
        this.configHelper = new ConfigHelper<ModConfig>(helper);
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
    }

    public static ModConfig Config => Instance!.configHelper.Config;

    public static ConfigHelper<ModConfig> ConfigHelper => Instance!.configHelper;

    public static Point Cursor =>
        Utility.ModifyCoordinatesForUIScale(Instance!.helper.Input.GetCursorPosition().GetScaledScreenPixels())
            .ToPoint();

    public static bool Active
    {
        get
        {
            if (!TryGetMenu(out var itemGrabMenu, out var chest, out _))
            {
                Instance!.context.Value = null;
                return false;
            }

            if (Instance!.context.Value == itemGrabMenu.context)
            {
                return true;
            }

            Instance!.context.Value = itemGrabMenu.context;
            Instance!.enabled.Value = Organizer.Any(item =>
                item.QualifiedItemId == chest.QualifiedItemId &&
                item is Chest proxyChest &&
                proxyChest.GlobalInventoryId.Equals(chest.GlobalInventoryId, StringComparison.OrdinalIgnoreCase));

            return true;
        }
    }

    public static bool Enabled => Active && Instance!.enabled.Value;

    public static ClickableTextureComponent OrganizeButton => Instance!.organizeButton.Value;

    public static Inventory Organizer => Instance!.organizer ??=
        Game1.player.team.GetOrCreateGlobalInventory(Constants.GlobalInventoryId);

    public static void Init(IModHelper helper) => Instance ??= new ModState(helper);

    public static bool TryAddToOrganizer(Chest chest)
    {
        if (string.IsNullOrWhiteSpace(chest.GlobalInventoryId))
        {
            var chestId = CommonHelper.GetUniqueId(Constants.Prefix);
            chest.ToGlobalInventory(chestId);
        }

        if (Organizer.Any(item =>
                item.QualifiedItemId == chest.QualifiedItemId &&
                item is Chest proxyChest &&
                proxyChest.GlobalInventoryId.Equals(chest.GlobalInventoryId, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var proxyChest = new Chest(true, chest.ItemId)
        {
            GlobalInventoryId = chest.GlobalInventoryId,
            playerChoiceColor = { Value = chest.playerChoiceColor.Value }
        };

        Log.Info("Adding chest to organizer");
        proxyChest.CopyFieldsFrom(chest);
        Organizer.Add(proxyChest);
        Instance!.enabled.Value = true;
        return true;
    }

    public static bool TryGetMenu([NotNullWhen(true)] out ItemGrabMenu? menu, [NotNullWhen(true)] out Chest? chest,
        [NotNullWhen(true)] out ClickableComponent? component, IClickableMenu? activeMenu = null)
    {
        if ((activeMenu ?? Game1.activeClickableMenu) is ItemGrabMenu
            {
                organizeButton: { } organizeButton,
                sourceItem: Chest { playerChest.Value: true } sourceItem
            } itemGrabMenu)
        {
            menu = itemGrabMenu;
            chest = sourceItem;
            component = organizeButton;
            return true;
        }

        menu = null;
        chest = null;
        component = null;
        return false;
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e) => this.organizer = null;
}