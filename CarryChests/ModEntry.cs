using LeFauxMods.CarryChest.Services;
using LeFauxMods.CarryChest.Utilities;
using LeFauxMods.Common.Models;
using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using StardewModdingAPI.Events;
using StardewValley.Buffs;
using StardewValley.Objects;

namespace LeFauxMods.CarryChest;

/// <inheritdoc />
internal sealed class ModEntry : Mod
{
    private ModConfig config = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        I18n.Init(this.Helper.Translation);
        var configHelper = new ConfigHelper<ModConfig>(helper);
        this.config = configHelper.Load();
        Log.Init(this.Monitor, this.config);
        ModPatches.Init();
        _ = new ConfigMenu(helper, this.ModManifest, this.config, configHelper);

        // TBD: Command to access global inventory chests

        // Events
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree
            || (Game1.player.CurrentItem is Tool && !this.config.OverrideTool))
        {
            return;
        }

        // Handle if tool button is used, without a tool, on a placed chest
        if (e.Button.IsUseToolButton() && Game1.currentLocation.Objects.TryGetValue(e.Cursor.GrabTile, out var @object)
                                       && @object is Chest chest)
        {
            if (InventoryHelper.SwapChest(chest))
            {
                this.Helper.Input.Suppress(e.Button);
            }
            else if (InventoryHelper.PickUpChest(chest, this.config.TotalLimit))
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
}
