using LeFauxMods.CarryChest.Services;
using LeFauxMods.Common.Utilities;
using Microsoft.Xna.Framework;
using StardewValley.Objects;

namespace LeFauxMods.CarryChest.Utilities;

internal static class ModExtensions
{
    public static bool TryCarry(this Chest chest)
    {
        // Grab as item
        if (ModState.Config.GrabEmptyAsItem
            && chest.GetItemsForPlayer().CountItemStacks() == 0
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
        if ((ModState.Config.TotalLimit > 0
             && Game1.player.Items.OfType<Chest>().Count() >= ModState.Config.TotalLimit)
            || !Game1.player.addItemToInventoryBool(chest, true))
        {
            return false;
        }

        Log.Trace(
            "CarryChest: Grabbed chest from {0} at ({1}, {2})",
            chest.Location.Name,
            chest.TileLocation.X,
            chest.TileLocation.Y);

        // Copy a backup for safety
        _ = ModState.Backups.TryAddBackup(chest, Constants.Prefix);
        return true;
    }

    public static bool TrySwap(this Chest chest)
    {
        if (!chest.performObjectDropInAction(Game1.player.CurrentItem, true, Game1.player) ||
            Game1.player.CurrentItem is not Chest heldChest)
        {
            return false;
        }

        // Safely replicate the vanilla swap action
        if (chest.GetMutex().IsLocked())
        {
            return true;
        }

        var newChest = new Chest(true, chest.TileLocation, heldChest.ItemId)
        {
            GlobalInventoryId = chest.GlobalInventoryId,
            fridge = { Value = chest.fridge.Value },
            playerChoiceColor = { Value = chest.playerChoiceColor.Value },
            SpecialChestType = chest.SpecialChestType,
            Tint = chest.Tint
        };

        newChest.CopyFieldsFrom(chest);

        // Try adding held items to new chest
        foreach (var item in heldChest.GetItemsForPlayer())
        {
            var remaining = newChest.addItem(item);
            if (remaining is not null)
            {
                return true;
            }
        }

        // Try adding existing items to new chest
        foreach (var item in chest.GetItemsForPlayer())
        {
            var remaining = newChest.addItem(item);
            if (remaining is not null)
            {
                return true;
            }
        }

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