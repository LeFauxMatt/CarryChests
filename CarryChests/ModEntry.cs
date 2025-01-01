using LeFauxMods.CarryChest.Services;
using LeFauxMods.CarryChest.Utilities;
using LeFauxMods.Common.Models;
using LeFauxMods.Common.Utilities;
using StardewModdingAPI.Events;
using StardewValley.Buffs;
using StardewValley.Objects;

namespace LeFauxMods.CarryChest;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Apply
        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);
        I18n.Init(helper.Translation);
        ModState.Init(helper);
        Log.Init(this.Monitor, ModState.Config);
        ModPatches.Apply();

        // TBD: Command to access global inventory chests

        // Events
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) =>
        _ = new ConfigMenu(this.Helper, this.ModManifest);

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree)
        {
            return;
        }

        if (e.Button.IsUseToolButton())
        {
            if (Game1.player.CurrentItem is Tool && !ModState.Config.OverrideTool)
            {
                return;
            }

            var tile = e.Button.TryGetController(out _) ? e.Cursor.GrabTile : e.Cursor.Tile;
            if (Math.Abs(Game1.player.Tile.X - tile.X) > 1 ||
                Math.Abs(Game1.player.Tile.Y - tile.Y) > 1 ||
                !Game1.currentLocation.Objects.TryGetValue(tile, out var obj)
                || obj is not Chest chest)
            {
                return;
            }

            if (InventoryHelper.SwapChest(chest))
            {
                this.Helper.Input.Suppress(e.Button);
                return;
            }

            if (InventoryHelper.PickUpChest(chest, ModState.Config.TotalLimit))
            {
                this.Helper.Input.Suppress(e.Button);
                _ = chest.Location.Objects.Remove(chest.TileLocation);
                _ = Game1.playSound("pickUpItem");
            }

            this.Helper.Input.Suppress(e.Button);
            return;
        }

        if (e.Button.IsActionButton())
        {
            if (!ModState.Config.OpenHeldChest || Game1.player.ActiveObject is not Chest heldChest)
            {
                return;
            }

            this.Helper.Input.Suppress(e.Button);
            heldChest.ShowMenu();
        }
    }

    private void OnConfigChanged(ConfigChangedEventArgs<ModConfig> e)
    {
        this.Helper.Events.GameLoop.OneSecondUpdateTicked -= OnOneSecondUpdateTicked;
        if (e.Config.SlownessLimit > 0 && e.Config.SlownessAmount != 0)
        {
            this.Helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
        }
    }

    private static void OnOneSecondUpdateTicked(object? sender, OneSecondUpdateTickedEventArgs e)
    {
        // Add status effect to the player
        if (Game1.player.Items.OfType<Chest>().Count() >= ModState.Config.SlownessLimit)
        {
            Game1.player.applyBuff(
                new Buff(
                    Constants.SlowEffectKey,
                    duration: 60_000,
                    iconTexture: Game1.buffsIcons,
                    iconSheetIndex: 13,
                    effects: new BuffEffects { Speed = { ModState.Config.SlownessAmount } },
                    displayName: I18n.Effect_Overburdened()));

            Log.Trace("Adding the slowness effect");
            return;
        }

        if (!Game1.player.hasBuff(Constants.SlowEffectKey))
        {
            return;
        }

        // Remove status effect from the player
        Game1.player.buffs.Remove(Constants.SlowEffectKey);
        Log.Trace("Removing the slowness effect");
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        this.Helper.Events.Input.ButtonPressed -= this.OnButtonPressed;
        this.Helper.Events.GameLoop.OneSecondUpdateTicked -= OnOneSecondUpdateTicked;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.Helper.Events.Input.ButtonPressed += this.OnButtonPressed;
        if (ModState.Config.SlownessLimit > 0 && ModState.Config.SlownessAmount != 0)
        {
            this.Helper.Events.GameLoop.OneSecondUpdateTicked += OnOneSecondUpdateTicked;
        }
    }
}