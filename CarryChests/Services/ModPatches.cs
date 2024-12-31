using HarmonyLib;
using LeFauxMods.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Objects;

namespace LeFauxMods.CarryChest.Services;

internal static class ModPatches
{
    private static readonly Harmony Harmony = new(Constants.ModId);

    public static void Init()
    {
        try
        {
            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.addItem)),
                new HarmonyMethod(typeof(ModPatches), nameof(Chest_addItem_prefix)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(Item), nameof(Item.canBeDropped)),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(Item_canBeDropped_postfix)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(Item), nameof(Item.canBeTrashed)),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(Item_canBeTrashed_postfix)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(Item), nameof(Item.canStackWith)),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(Item_canStackWith_postfix)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.drawInMenu)),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(Object_drawInMenu_postfix)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.drawWhenHeld)),
                new HarmonyMethod(typeof(ModPatches), nameof(Object_drawWhenHeld_prefix)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.maximumStackSize)),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(Object_maximumStackSize_postfix)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.placementAction)),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(Object_placementAction_postfix)));
        }
        catch (Exception)
        {
            Log.WarnOnce("Failed to apply patches");
        }
    }


    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static bool Chest_addItem_prefix(Chest __instance, ref Item __result, Item item)
    {
        if (__instance != item)
        {
            return true;
        }

        __result = item;
        return false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void Item_canBeDropped_postfix(Item __instance, ref bool __result) =>
        __result = __result && __instance is not Chest;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void Item_canBeTrashed_postfix(Item __instance, ref bool __result) =>
        __result = __result && __instance is not Chest;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void Item_canStackWith_postfix(Item __instance, ref bool __result, ISalable other) =>
        __result = __result && __instance is not Chest && other is not Chest;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void Object_drawInMenu_postfix(
        SObject __instance,
        SpriteBatch spriteBatch,
        Vector2 location,
        float scaleSize,
        Color color)
    {
        if (__instance is not Chest chest)
        {
            return;
        }

        // Draw Items count
        var items = chest.GetItemsForPlayer().CountItemStacks();
        if (items <= 0)
        {
            return;
        }

        var position = location
                       + new Vector2(
                           Game1.tileSize - Utility.getWidthOfTinyDigitString(items, 3f * scaleSize) - (3f * scaleSize),
                           2f * scaleSize);

        Utility.drawTinyDigits(items, spriteBatch, position, 3f * scaleSize, 1f, color);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static bool Object_drawWhenHeld_prefix(SObject __instance, SpriteBatch spriteBatch, Vector2 objectPosition)
    {
        if (__instance is not Chest chest)
        {
            return true;
        }

        var (x, y) = objectPosition;
        chest.draw(spriteBatch, (int)x, (int)y + Game1.tileSize, 1f, true);
        return false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void Object_maximumStackSize_postfix(SObject __instance, ref int __result) =>
        __result = __result > 1 && __instance is Chest ? 1 : __result;

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void Object_placementAction_postfix(
        SObject __instance,
        GameLocation location,
        int x,
        int y,
        Farmer who,
        ref bool __result)
    {
        if (!__result)
        {
            return;
        }

        var placementTile = new Vector2((int)(x / (float)Game1.tileSize), (int)(y / (float)Game1.tileSize));

        if (!location.Objects.TryGetValue(
                new Vector2((int)(x / (float)Game1.tileSize), (int)(y / (float)Game1.tileSize)),
                out var placedObject)
            || placedObject is not Chest placedChest)
        {
            return;
        }

        string id;
        if (__instance is not Chest chest)
        {
            // Attempt to restore Better Chest proxy
            if (__instance.modData.TryGetValue(Constants.GlobalInventoryKey, out id) &&
                Game1.player.team.globalInventories.ContainsKey(id))
            {
                var color = Color.Black;
                if (__instance.modData.TryGetValue(Constants.ColorKey, out var colorString) &&
                    int.TryParse(colorString, out var colorValue))
                {
                    var r = (byte)(colorValue & 0xFF);
                    var g = (byte)((colorValue >> 8) & 0xFF);
                    var b = (byte)((colorValue >> 16) & 0xFF);
                    color = new Color(r, g, b);
                }

                chest = placedChest;
                chest.GlobalInventoryId = id;
                chest.playerChoiceColor.Value = color;
                chest.fridge.Value = __instance.modData.ContainsKey(Constants.FridgeKey);

                foreach (var (key, value) in __instance.modData.Pairs)
                {
                    chest.modData[key] = value;
                }

                chest.modData.Remove(Constants.FridgeKey);
                chest.modData.Remove(Constants.ColorKey);
                chest.modData.Remove(Constants.GlobalInventoryKey);
            }
            else
            {
                return;
            }
        }

        location.Objects[placementTile] = chest;
        chest.localKickStartTile = null;
        chest.kickProgress = -1f;
        chest.shakeTimer = 50;
        who.removeItemFromInventory(who.CurrentItem);
        who.showNotCarrying();

        // Move items from temporary global inventory back to chest
        if (string.IsNullOrWhiteSpace(chest.GlobalInventoryId) ||
            !chest.GlobalInventoryId.StartsWith(Constants.Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        chest.ToLocalInventory();
    }
}
