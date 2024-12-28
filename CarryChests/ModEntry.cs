using HarmonyLib;
using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Models;
using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley.Buffs;
using StardewValley.Objects;

namespace LeFauxMods.CarryChest;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private ModConfig config = null!;
    private ConfigHelper<ModConfig> configHelper = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        I18n.Init(this.Helper.Translation);
        this.configHelper = new ConfigHelper<ModConfig>(helper);
        this.config = this.configHelper.Load();
        Log.Init(this.Monitor, this.config);

        // Patches
        var harmony = new Harmony(this.ModManifest.UniqueID);

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Chest), nameof(Chest.addItem)),
            new HarmonyMethod(typeof(ModEntry), nameof(Chest_addItem_prefix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Item), nameof(Item.canBeDropped)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(Item_canBeDropped_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Item), nameof(Item.canBeTrashed)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(Item_canBeTrashed_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Item), nameof(Item.canStackWith)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(Item_canStackWith_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.drawInMenu)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(Object_drawInMenu_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.drawWhenHeld)),
            new HarmonyMethod(typeof(ModEntry), nameof(Object_drawWhenHeld_prefix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.maximumStackSize)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(Object_maximumStackSize_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.placementAction)),
            postfix: new HarmonyMethod(typeof(ModEntry), nameof(Object_placementAction_postfix)));

        // Events
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);
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
        // Only handle placed chests
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
    }

    private static bool SwapChest(Chest chest)
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

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree
            || Game1.player.CurrentItem is Tool)
        {
            return;
        }

        // Handle if tool button is used, without a tool, on a placed chest
        if (e.Button.IsUseToolButton() && Game1.currentLocation.Objects.TryGetValue(e.Cursor.GrabTile, out var @object)
                                       && @object is Chest chest)
        {
            if (SwapChest(chest))
            {
                this.Helper.Input.Suppress(e.Button);
            }
            else if (this.PickUpChest(chest))
            {
                this.Helper.Input.Suppress(e.Button);
                _ = chest.Location.Objects.Remove(chest.TileLocation);
                _ = Game1.playSound("pickUpItem");
            }

            return;
        }

        if (this.config.OpenHeldChest && e.Button.IsActionButton() && Game1.player.ActiveObject is Chest heldChest)
        {
            this.Helper.Input.Suppress(e.Button);
            heldChest.ShowMenu();
        }
    }

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e)
    {
        this.Helper.Events.GameLoop.OneSecondUpdateTicked -= this.OnOneSecondUpdateTicked;
        if (e.Config.SlownessLimit > 0 && e.Config.SlownessAmount != 0)
        {
            this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdateTicked;
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        var gmcm = new GenericModConfigMenuIntegration(this.ModManifest, this.Helper.ModRegistry);
        if (!gmcm.IsLoaded)
        {
            return;
        }

        var defaultConfig = new ModConfig();
        var tempConfig = this.configHelper.Load();

        gmcm.Register(
            () => defaultConfig.CopyTo(tempConfig),
            () =>
            {
                tempConfig.CopyTo(this.config);
                this.configHelper.Save(tempConfig);
            });

        gmcm.Api.AddNumberOption(
            this.ModManifest,
            () => tempConfig.TotalLimit,
            value => tempConfig.TotalLimit = value,
            I18n.ConfigOption_TotalLimit_Name,
            I18n.ConfigOption_TotalLimit_Description);

        gmcm.Api.AddNumberOption(
            this.ModManifest,
            () => tempConfig.SlownessAmount,
            value => tempConfig.SlownessAmount = value,
            I18n.ConfigOption_SlownessAmount_Name,
            I18n.ConfigOption_SlownessAmount_Description);

        gmcm.Api.AddNumberOption(
            this.ModManifest,
            () => tempConfig.SlownessLimit,
            value => tempConfig.SlownessLimit = value,
            I18n.ConfigOption_SlownessLimit_Name,
            I18n.ConfigOption_SlownessLimit_Description);

        gmcm.Api.AddBoolOption(
            this.ModManifest,
            () => tempConfig.OpenHeldChest,
            value => tempConfig.OpenHeldChest = value,
            I18n.ConfigOption_OpenHeldChest_Name,
            I18n.ConfigOption_OpenHeldChest_Description);
    }

    private void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        // Add status effect to the player
        if (!Game1.player.hasBuff(Constants.SlowEffectKey)
            && Game1.player.Items.OfType<Chest>().Count() >= this.config.SlownessLimit)
        {
            Game1.player.applyBuff(
                new Buff(
                    Constants.SlowEffectKey,
                    duration: 60_000,
                    iconTexture: Game1.buffsIcons,
                    iconSheetIndex: 13,
                    effects: new BuffEffects { Speed = { this.config.SlownessAmount } },
                    displayName: I18n.Effect_Overburdened()));

            Log.Trace("Adding the slowness effect");
            return;
        }

        // Remove status effect from the player
        if (Game1.player.hasBuff(Constants.SlowEffectKey))
        {
            Game1.player.buffs.Remove(Constants.SlowEffectKey);
            Log.Trace("Removing the slowness effect");
        }
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.Helper.Events.Input.ButtonPressed -= this.OnButtonPressed;
        this.Helper.Events.GameLoop.OneSecondUpdateTicked -= this.OnOneSecondUpdateTicked;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        if (this.config.SlownessLimit > 0 && this.config.SlownessAmount != 0)
        {
            this.Helper.Events.GameLoop.OneSecondUpdateTicked += this.OnOneSecondUpdateTicked;
        }
    }

    private bool PickUpChest(Chest chest)
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
        if ((this.config.TotalLimit > 0
             && Game1.player.Items.OfType<Chest>().Count() >= this.config.TotalLimit)
            || !Game1.player.addItemToInventoryBool(chest, true))
        {
            return false;
        }

        Log.Trace(
            "CarryChest: Grabbed chest from {0} at ({1}, {2})",
            chest.Location.Name,
            chest.TileLocation.X,
            chest.TileLocation.Y);

        return true;
    }
}
