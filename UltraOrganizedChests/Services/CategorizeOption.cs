using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LeFauxMods.UltraOrganizedChests.Services;

internal sealed class CategorizeOption : ComplexOption
{
    private readonly IModHelper helper;
    private readonly PriorityOption priorityOption;
    private readonly Texture2D icons;

    private int height;

    public CategorizeOption(IModHelper helper, PriorityOption priorityOption)
    {
        this.helper = helper;
        this.priorityOption = priorityOption;
        this.icons = helper.GameContent.Load<Texture2D>(Constants.TexturePath);
    }

    /// <inheritdoc />
    public override int Height => this.height;

    /// <inheritdoc />
    public override void Draw(SpriteBatch spriteBatch, Vector2 pos)
    {
        if (this.priorityOption.Selected is not Chest chest)
        {
            this.height = 0;
            return;
        }

        pos.X -= Math.Min(1200, Game1.uiViewport.Width - 200) / 2f;
        var (originX, originY) = pos.ToPoint();

        if (!chest.modData.TryGetValue(Constants.NameKey, out var name))
        {
            name = chest.DisplayName;
        }

        if (!chest.modData.TryGetValue(Constants.CategoryKey, out var category))
        {
            category = chest.getDescription();
        }

        category = string.Join(' ',
            category.Split(['\r', '\n', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        Utility.drawTextWithShadow(
            spriteBatch,
            name,
            Game1.dialogueFont,
            pos,
            SpriteText.color_Black);

        var size = Game1.dialogueFont.MeasureString(name);
        pos.Y += size.Y;

        spriteBatch.DrawString(
            Game1.smallFont,
            category,
            pos,
            SpriteText.color_Black);

        size = Game1.smallFont.MeasureString(category);
        pos.Y += size.Y + 16;

        Utility.drawTextWithShadow(
            spriteBatch,
            "Stack to Existing",
            Game1.dialogueFont,
            pos,
            SpriteText.color_Black);

        pos.X += Math.Min(1200, Game1.uiViewport.Width - 200) / 2f;

        spriteBatch.Draw(
            Game1.mouseCursors,
            pos,
            OptionsCheckbox.sourceRectChecked,
            Color.White,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);

        size = Game1.dialogueFont.MeasureString("Stack to Existing");
        pos.X = originX;
        pos.Y += size.Y + 16;

        Utility.drawTextWithShadow(
            spriteBatch,
            "Additional Rules",
            Game1.dialogueFont,
            pos,
            SpriteText.color_Black);

        size = Game1.dialogueFont.MeasureString("Additional Rules");
        pos.Y += size.Y + 16;

        spriteBatch.DrawString(
            Game1.smallFont,
            "Category",
            pos,
            SpriteText.color_Black);

        pos.X += Math.Min(1200, Game1.uiViewport.Width - 200) / 2f;

        spriteBatch.Draw(
            this.icons,
            pos,
            new Rectangle(32, 0, 16, 16),
            Color.Gray * 0.8f,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);

        pos.X += 16 * Game1.pixelZoom;

        spriteBatch.Draw(
            this.icons,
            pos,
            new Rectangle(48, 0, 16, 16),
            Color.Gray * 0.8f,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);

        pos.X += 16 * Game1.pixelZoom;

        spriteBatch.Draw(
            this.icons,
            pos,
            new Rectangle(64, 0, 16, 16),
            Color.Gray * 0.8f,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);

        pos.X += 16 * Game1.pixelZoom;

        spriteBatch.Draw(
            this.icons,
            pos,
            new Rectangle(0, 16, 16, 16),
            Color.White,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);

        pos.X += 16 * Game1.pixelZoom;

        spriteBatch.Draw(
            this.icons,
            pos,
            new Rectangle(16, 16, 16, 16),
            Color.Gray * 0.8f,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);

        pos.X += 16 * Game1.pixelZoom;

        spriteBatch.Draw(
            this.icons,
            pos,
            new Rectangle(32, 16, 16, 16),
            Color.Gray * 0.8f,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);

        pos.X += 16 * Game1.pixelZoom;

        spriteBatch.Draw(
            this.icons,
            pos,
            new Rectangle(48, 16, 16, 16),
            Color.Gray * 0.8f,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);

        size = Game1.smallFont.MeasureString("Category");
        pos.X = originX;
        pos.Y += size.Y + 24;

        spriteBatch.DrawString(
            Game1.smallFont,
            "Quality",
            pos,
            SpriteText.color_Black);

        pos.X += Math.Min(1200, Game1.uiViewport.Width - 200) / 2f;

        spriteBatch.Draw(
            Game1.mouseCursors,
            pos,
            new Rectangle(338, 400, 8, 8),
            Color.Gray * 0.8f,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);

        pos.X += 38;

        spriteBatch.Draw(
            Game1.mouseCursors,
            pos,
            new Rectangle(346, 400, 8, 8),
            Color.Gray * 0.8f,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);

        pos.X += 38;

        spriteBatch.Draw(
            Game1.mouseCursors,
            pos,
            new Rectangle(346, 392, 8, 8),
            Color.White,
            0f,
            Vector2.Zero,
            Game1.pixelZoom,
            SpriteEffects.None,
            1f);

        size = Game1.smallFont.MeasureString("Quality");
        pos.X = originX;
        pos.Y += size.Y + 16;

        Utility.drawTextWithShadow(
            spriteBatch,
            "Included Items",
            Game1.dialogueFont,
            pos,
            SpriteText.color_Black);

        size = Game1.dialogueFont.MeasureString("Included Items");
        pos.Y += size.Y;

        spriteBatch.DrawString(
            Game1.smallFont,
            "Click on an item to exclude it",
            pos,
            SpriteText.color_Black);

        size = Game1.smallFont.MeasureString("Click on an item to exclude it");
        pos.Y += size.Y + 16;

        var count = 0;
        foreach (var item in chest.GetItemsForPlayer())
        {
            item?.drawInMenu(
                spriteBatch,
                pos,
                0.8f,
                1f,
                1f,
                StackDrawType.HideButShowQuality,
                Color.White,
                false);

            count++;
            pos.X += Game1.tileSize * 0.8f;
            if (pos.X + (Game1.tileSize * 0.8f) >= originX + Math.Min(1200, Game1.uiViewport.Width - 200))
            {
                pos.X = originX;
                pos.Y += Game1.tileSize * 0.8f;
            }

            if (count == 70)
            {
                pos.X = originX;
                pos.Y += (Game1.tileSize * 0.8f) + 16;

                Utility.drawTextWithShadow(
                    spriteBatch,
                    "Excluded Items",
                    Game1.dialogueFont,
                    pos,
                    SpriteText.color_Black);

                size = Game1.dialogueFont.MeasureString("Excluded Items");
                pos.Y += size.Y;

                spriteBatch.DrawString(
                    Game1.smallFont,
                    "Click on an item to remove it from exclusions",
                    pos,
                    SpriteText.color_Black);

                size = Game1.smallFont.MeasureString("Click on an item to remove it from exclusions");
                pos.Y += size.Y + 16;
            }
        }

        pos.X = originX;
        pos.Y += Game1.tileSize;

        this.height = (int)pos.Y - originY;
    }
}