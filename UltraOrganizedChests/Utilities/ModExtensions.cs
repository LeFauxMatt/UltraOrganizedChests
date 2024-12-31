using StardewValley.Inventories;
using StardewValley.Objects;

namespace LeFauxMods.UltraOrganizedChests.Utilities;

internal static class ModExtensions
{
    public static void AddProxy(this Inventory inventory, Chest chest)
    {
        if (inventory.Any(item =>
                item is Chest itemChest &&
                itemChest.GlobalInventoryId.Equals(chest.GlobalInventoryId, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var proxyChest = new Chest(true, chest.ItemId)
        {
            GlobalInventoryId = chest.GlobalInventoryId,
            playerChoiceColor = { Value = chest.playerChoiceColor.Value }
        };

        proxyChest.CopyFieldsFrom(chest);
        inventory.Add(proxyChest);
    }
}
