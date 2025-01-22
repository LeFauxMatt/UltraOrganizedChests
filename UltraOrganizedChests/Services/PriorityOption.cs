using System.Globalization;
using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Inventories;
using StardewValley.Menus;

namespace LeFauxMods.UltraOrganizedChests.Services;

internal sealed class PriorityOption : ComplexOption
{
    private readonly int capacity;
    private readonly IModHelper helper;
    private readonly Inventory items;
    private readonly List<ClickableComponent> slots = [];
    private Item? heldItem;
    private Vector2 offset = Vector2.Zero;

    public PriorityOption(IModHelper helper, Inventory items)
    {
        this.helper = helper;
        this.items = items;

        var rows = (int)Math.Ceiling(this.items.Count * 2 / 12f);
        this.capacity = 12 * rows;
        this.Height = rows * Game1.tileSize;

        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < 14; col++)
            {
                var index = (row * 14) + col;
                var component = new ClickableComponent(
                    new Rectangle(col * Game1.tileSize, row * Game1.tileSize, Game1.tileSize, Game1.tileSize),
                    index.ToString(CultureInfo.InvariantCulture)) { myID = index };

                if (col > 0)
                {
                    component.leftNeighborID = index - 1;
                    this.slots[component.leftNeighborID].rightNeighborID = index;
                }

                if (row > 0)
                {
                    component.upNeighborID = index - 12;
                    this.slots[component.upNeighborID].downNeighborID = index;
                }

                this.slots.Add(component);
            }
        }
    }

    /// <inheritdoc />
    public override int Height { get; }

    /// <inheritdoc />
    public override void Draw(SpriteBatch spriteBatch, Vector2 pos)
    {
        var availableWidth = Math.Min(1200, Game1.uiViewport.Width - 200);
        pos.X -= availableWidth / 2f;
        var (originX, originY) = pos.ToPoint();
        var cursorPos = this.helper.Input.GetCursorPosition().GetScaledScreenPixels();
        var (mouseX, mouseY) = cursorPos.ToPoint();

        mouseX -= originX;
        mouseY -= originY;

        var mouseLeft = this.helper.Input.GetState(SButton.MouseLeft);
        var controllerA = this.helper.Input.GetState(SButton.ControllerA);
        var pressed = mouseLeft is SButtonState.Pressed || controllerA is SButtonState.Pressed;
        var held = mouseLeft is SButtonState.Held || controllerA is SButtonState.Held;
        var heldIndex = -1;
        var hoveredIndex = -1;
        var hoverText = default(string);

        for (var index = 0; index < this.slots.Count; index++)
        {
            var slot = this.slots[index];

            if (index >= this.items.Count)
            {
                spriteBatch.Draw(
                    Game1.uncoloredMenuTexture,
                    pos + slot.bounds.Location.ToVector2(),
                    Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 57),
                    Color.White * 0.5f,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    0.5f);
            }

            spriteBatch.Draw(
                Game1.menuTexture,
                pos + slot.bounds.Location.ToVector2(),
                Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 10),
                Color.White,
                0f,
                Vector2.Zero,
                1f,
                SpriteEffects.None,
                0.5f);

            if (!int.TryParse(slot.name, out var actualIndex))
            {
                continue;
            }

            var item = this.items.ElementAtOrDefault(actualIndex);
            if (index < this.items.Count)
            {
                spriteBatch.DrawString(
                    Game1.tinyFont,
                    index.ToString(CultureInfo.InvariantCulture),
                    pos + slot.bounds.Location.ToVector2() + new Vector2(8f, -4f),
                    Color.DimGray);
            }

            slot.scale = Math.Max(1f, slot.scale - 0.025f);

            // Check for click
            if (slot.bounds.Contains(mouseX, mouseY))
            {
                slot.scale = Math.Min(slot.scale + 0.05f, 1.1f);

                if (item is not null)
                {
                    hoverText ??= item.DisplayName;
                    if (this.heldItem is null && held)
                    {
                        Game1.playSound("smallSelect");
                        this.heldItem = item;
                        this.offset = new Vector2(slot.bounds.X - mouseX, slot.bounds.Y - mouseY);
                    }
                }

                hoveredIndex = index;
            }

            if (this.heldItem == item)
            {
                heldIndex = index;
                continue;
            }

            item?.drawInMenu(
                spriteBatch,
                pos + slot.bounds.Location.ToVector2(),
                slot.scale,
                1f,
                1f,
                StackDrawType.Hide,
                index >= this.items.Count ? Color.Gray * 0.8f : Color.White,
                true);
        }

        if (this.heldItem is not null)
        {
            // Check for release
            if (!held && !pressed)
            {
                Game1.playSound("shwip");
                this.heldItem = null;
                if (heldIndex != -1 && hoveredIndex != -1 && heldIndex != hoveredIndex)
                {
                    (this.slots[heldIndex].name, this.slots[hoveredIndex].name) =
                        (this.slots[hoveredIndex].name, this.slots[heldIndex].name);
                }

                return;
            }

            this.heldItem.drawInMenu(
                spriteBatch,
                cursorPos + this.offset,
                1f,
                1f,
                1f,
                StackDrawType.Hide,
                hoveredIndex >= this.items.Count ? Color.Gray * 0.8f : Color.White,
                false);

            return;
        }

        if (!string.IsNullOrWhiteSpace(hoverText))
        {
            IClickableMenu.drawHoverText(spriteBatch, hoverText, Game1.smallFont);
        }
    }

    public void Save()
    {
        var sortedItems = new Item?[this.capacity];
        for (var index = 0; index < this.items.Count; index++)
        {
            var slot = this.slots[index];
            if (!int.TryParse(slot.name, out var newIndex) || newIndex >= this.items.Count)
            {
                continue;
            }

            sortedItems[newIndex] = this.items[index];
        }

        this.items.Clear();
        foreach (var item in sortedItems)
        {
            if (item is not null)
            {
                this.items.Add(item);
            }
        }
    }
}