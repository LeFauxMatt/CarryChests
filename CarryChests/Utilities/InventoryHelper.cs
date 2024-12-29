using LeFauxMods.Common.Utilities;
using Microsoft.Xna.Framework;
using StardewValley.Objects;

namespace LeFauxMods.CarryChest.Utilities;

internal static class InventoryHelper
{
    public static bool PickUpChest(Chest chest, int limit)
    {
        // Grab as item
        if (chest.GetItemsForPlayer().CountItemStacks() == 0
            && Game1.player.addItemToInventoryBool(ItemRegistry.Create(chest.QualifiedItemId)))
        {
            Log.Trace(
                "CarryChest: Grabbed chest from {0} at ({1}, {2})",
                chest.Location.Name,
                chest.TileLocation.X,
                chest.TileLocation.Y);

            return true;
        }

        // Grab as chest
        if ((limit > 0
             && Game1.player.Items.OfType<Chest>().Count() >= limit)
            || !Game1.player.addItemToInventoryBool(chest, true))
        {
            return false;
        }

        Log.Trace(
            "CarryChest: Grabbed chest from {0} at ({1}, {2})",
            chest.Location.Name,
            chest.TileLocation.X,
            chest.TileLocation.Y);

        if (!string.IsNullOrWhiteSpace(chest.GlobalInventoryId))
        {
            return true;
        }

        var temporaryId = CommonHelper.GetTemporaryId(Constants.Prefix);
        var globalInventory = Game1.player.team.GetOrCreateGlobalInventory(temporaryId);
        globalInventory.OverwriteWith(chest.GetItemsForPlayer());
        chest.GlobalInventoryId = temporaryId;
        chest.Items.Clear();
        return true;
    }

    public static bool SwapChest(Chest chest)
    {
        if (!chest.performObjectDropInAction(Game1.player.CurrentItem, true, Game1.player))
        {
            return false;
        }

        if (Game1.player.CurrentItem is not Chest heldChest)
        {
            return false;
        }

        // Safely replicate the vanilla swap action
        if (chest.GetMutex().IsLocked())
        {
            return true;
        }

        var newChest = new Chest(true, chest.TileLocation, heldChest.ItemId);

        // Try adding held items to new chest
        foreach (var item in heldChest.GetItemsForPlayer())
        {
            var remaining = newChest.addItem(item);
            if (remaining is not null)
            {
                return true;
            }
        }

        // Try adding exiting items to new chest
        foreach (var item in chest.GetItemsForPlayer())
        {
            var remaining = newChest.addItem(item);
            if (remaining is not null)
            {
                return true;
            }
        }

        newChest.playerChoiceColor.Value = chest.playerChoiceColor.Value;
        newChest.Tint = chest.Tint;
        newChest.modData.CopyFrom(chest.modData);

        var location = chest.Location;
        var tileLocation = chest.TileLocation;
        _ = location.Objects.Remove(chest.TileLocation);
        location.Objects.Add(chest.TileLocation, newChest);
        Game1.player.reduceActiveItemByOne();
        Game1.createMultipleItemDebris(
            ItemRegistry.Create(chest.QualifiedItemId),
            (tileLocation * Game1.tileSize) + new Vector2(32f),
            -1);

        Log.Trace(
            "CarryChest: Swapped chest from {0} at ({1}, {2})",
            location.Name,
            tileLocation.X,
            tileLocation.Y);

        Game1.currentLocation.playSound("axchop");
        return true;
    }
}
