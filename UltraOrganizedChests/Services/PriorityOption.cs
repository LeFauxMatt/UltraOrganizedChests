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

    private int debounce;
    private Item? heldItem;
    private Item? hoveredItem;
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
            for (var col = 0; col < 12; col++)
            {
                var index = (row * 12) + col;
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

    public Item? Selected { get; private set; }

    /// <inheritdoc />
    public override void Draw(SpriteBatch spriteBatch, Vector2 pos)
    {
        pos.X -= Math.Min(1200, Game1.uiViewport.Width - 200) / 2f;
        var cursorPos = this.helper.Input.GetCursorPosition().GetScaledScreenPixels();
        var (mouseX, mouseY) = cursorPos.ToPoint();

        var mouseLeft = this.helper.Input.GetState(SButton.MouseLeft);
        var heldIndex = -1;
        var hoveredIndex = -1;
        this.hoveredItem = null;

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
            if ((slot.bounds with { X = slot.bounds.X + (int)pos.X, Y = slot.bounds.Y + (int)pos.Y }).Contains(mouseX,
                mouseY))
            {
                slot.scale = Math.Min(slot.scale + 0.05f, 1.1f);

                if (item is not null)
                {
                    if (mouseLeft is SButtonState.Pressed)
                    {
                        this.Selected = item;
                    }
                    else if (this.Selected is not null && this.heldItem is null && mouseLeft is SButtonState.Held)
                    {
                        this.heldItem = item;
                        this.offset = new Vector2(slot.bounds.X + pos.X - mouseX, slot.bounds.Y + pos.Y - mouseY);
                    }
                }

                hoveredIndex = index;
            }

            if (this.Selected is not null && this.Selected == item)
            {
                spriteBatch.Draw(
                    Game1.menuTexture,
                    pos + slot.bounds.Location.ToVector2(),
                    Game1.getSourceRectForStandardTileSheet(Game1.menuTexture, 56),
                    Color.Red,
                    0f,
                    Vector2.Zero,
                    1f,
                    SpriteEffects.None,
                    0.5f);
            }

            if (this.heldItem == item)
            {
                heldIndex = index;
                continue;
            }

            if (this.hoveredItem == item && this.heldItem is not null)
            {
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
            if (mouseLeft is not (SButtonState.Held or SButtonState.Pressed))
            {
                this.heldItem = null;
                if (heldIndex != -1 && hoveredIndex != -1 && heldIndex != hoveredIndex)
                {
                    (this.slots[heldIndex].name, this.slots[hoveredIndex].name) =
                        (this.slots[hoveredIndex].name, this.slots[heldIndex].name);
                }

                return;
            }

            if (this.hoveredItem != this.heldItem)
            {
                this.hoveredItem?.drawInMenu(spriteBatch, pos + this.slots[heldIndex].bounds.Location.ToVector2(), 1f);
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