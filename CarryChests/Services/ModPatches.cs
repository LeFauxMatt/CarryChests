using HarmonyLib;
using LeFauxMods.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LeFauxMods.CarryChest.Services;

/// <summary>Encapsulates mod patches.</summary>
internal static class ModPatches
{
    private static readonly Harmony Harmony = new(Constants.ModId);

    public static void Apply()
    {
        try
        {
            Log.Info("Applying patches");

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.addItem)),
                new HarmonyMethod(typeof(ModPatches), nameof(Chest_addItem_prefix)));

            _ = Harmony.Patch(
                AccessTools.DeclaredMethod(typeof(InventoryMenu), nameof(InventoryMenu.rightClick)),
                transpiler: new HarmonyMethod(typeof(ModPatches), nameof(InventoryMenu_rightClick_transpiler)));

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
                AccessTools.DeclaredPropertyGetter(typeof(SObject), nameof(SObject.Location)),
                postfix: new HarmonyMethod(typeof(ModPatches), nameof(Object_Location_postfix)));

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

    private static IEnumerable<CodeInstruction>
        InventoryMenu_rightClick_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(new CodeMatch(CodeInstruction.LoadField(typeof(InventoryMenu),
                nameof(InventoryMenu.highlightMethod))))
            .Repeat(static matcher =>
                matcher
                    .Advance(1)
                    .InsertAndAdvance(
                        CodeInstruction.Call(typeof(ModPatches), nameof(HighlightMethod))))
            .InstructionEnumeration();

    private static InventoryMenu.highlightThisItem HighlightMethod(InventoryMenu.highlightThisItem highlightMethod) =>
        item =>
            highlightMethod.Invoke(item) && item is not Chest;

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
    [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Harmony")]
    [HarmonyAfter("furyx639.ExpandedStorage")]
    private static void Object_placementAction_postfix(
        SObject __instance,
        GameLocation location,
        int x,
        int y,
        Farmer who,
        ref bool __result)
    {
        if (!__result || __instance is not Chest chest)
        {
            return;
        }

        var placementTile = new Vector2((int)(x / (float)Game1.tileSize), (int)(y / (float)Game1.tileSize));
        if (!location.Objects.TryGetValue(
                new Vector2((int)(x / (float)Game1.tileSize), (int)(y / (float)Game1.tileSize)),
                out var placedObject)
            || placedObject is not Chest)
        {
            return;
        }

        location.Objects[placementTile] = chest;
        chest.localKickStartTile = null;
        chest.kickProgress = -1f;
        chest.shakeTimer = 50;
        who.removeItemFromInventory(who.CurrentItem);
        who.showNotCarrying();

        // Remove backup
        if (ModState.Backups.TryGetBackup(chest, out var backup))
        {
            _ = ModState.Backups.Remove(backup);
        }

        // Move items from temporary global inventory back to chest
        if (!string.IsNullOrWhiteSpace(chest.GlobalInventoryId) &&
            chest.GlobalInventoryId.StartsWith(Constants.Prefix, StringComparison.OrdinalIgnoreCase))
        {
            chest.ToLocalInventory();
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void Object_Location_postfix(SObject __instance, ref GameLocation __result)
    {
        if (__instance is Chest)
        {
            __result ??= Game1.player.currentLocation;
        }
    }
}