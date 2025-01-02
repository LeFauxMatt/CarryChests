using System.Globalization;
using System.Text;
using LeFauxMods.CarryChest.Services;
using LeFauxMods.CarryChest.Utilities;
using LeFauxMods.Common.Models;
using LeFauxMods.Common.Utilities;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using StardewValley.Buffs;
using StardewValley.Menus;
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
        helper.ConsoleCommands.Add("carry_chests", I18n.Command_CarryChests_Description(), OnCommand);

        // Events
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
    }

    private static void OnCommand(string arg1, string[] arg2)
    {
        switch (arg2.ElementAtOrDefault(0)?.ToLower(CultureInfo.InvariantCulture))
        {
            case "backup" when !Context.IsWorldReady:
                Log.Info(I18n.Alert_CommandBackup_InvalidContext());
                return;

            case "backup":
                Game1.activeClickableMenu = new ItemGrabMenu(ModState.Backups);
                return;

            case "help":
                switch (arg2.ElementAtOrDefault(1)?.ToLower(CultureInfo.InvariantCulture))
                {
                    case "backup":
                        Log.Info(I18n.Command_Backups_Description());
                        return;

                    case "help":
                        Log.Info(I18n.Command_Help_Description());
                        return;

                    case null:
                        Log.Info(
                            new StringBuilder()
                                .AppendLine("Commands:")
                                .AppendLine()
                                .AppendLine("carry_chests backup")
                                .AppendLine(CultureInfo.InvariantCulture, $"\t{I18n.Command_Backups_Description()}")
                                .AppendLine()
                                .AppendLine("carry_chests help")
                                .AppendLine(CultureInfo.InvariantCulture, $"\t{I18n.Command_Help_Description()}")
                                .ToString());
                        return;

                    default:
                        Log.Info(I18n.Command_Unknown_Description());
                        return;
                }

            case null:
                Log.Info(I18n.Command_CarryChests_Description());
                return;

            default:
                Log.Info(I18n.Command_Unknown_Description());
                return;
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

            if (ModState.Config.SwapChests && chest.TrySwap())
            {
                this.Helper.Input.Suppress(e.Button);
                return;
            }

            if (chest.TryCarry())
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

        // Create missing backups
        for (var i = 0; i < Game1.player.Items.Count; i++)
        {
            var item = Game1.player.Items[i];
            if (item is not Chest chest)
            {
                if (item is null ||
                    !item.modData.TryGetValue(Constants.BetterChestsGlobalInventoryKey, out var id) ||
                    !Game1.player.team.globalInventories.ContainsKey(id))
                {
                    continue;
                }

                // Attempt to restore a Better Chest proxy
                var color = Color.Black;
                if (item.modData.TryGetValue(Constants.BetterChestsColorKey, out var colorString) &&
                    int.TryParse(colorString, out var colorValue))
                {
                    var r = (byte)(colorValue & 0xFF);
                    var g = (byte)((colorValue >> 8) & 0xFF);
                    var b = (byte)((colorValue >> 16) & 0xFF);
                    color = new Color(r, g, b);
                }

                chest = new Chest(true, item.ItemId)
                {
                    GlobalInventoryId = id,
                    fridge = { Value = item.modData.ContainsKey(Constants.BetterChestsFridgeKey) },
                    playerChoiceColor = { Value = color }
                };

                chest.CopyFieldsFrom(item);
                _ = chest.modData.Remove(Constants.BetterChestsFridgeKey);
                _ = chest.modData.Remove(Constants.BetterChestsColorKey);
                _ = chest.modData.Remove(Constants.BetterChestsGlobalInventoryKey);
                Game1.player.Items[i] = chest;
            }

            _ = ModState.Backups.TryAddBackup(chest, Constants.Prefix);
        }

        // Create generic backups for any missing
        foreach (var (id, _) in Game1.player.team.globalInventories.Pairs)
        {
            // Only create backups for known ids
            if (!id.StartsWith(Constants.Prefix, StringComparison.OrdinalIgnoreCase) &&
                !id.StartsWith("furyx639.BetterChests-ProxyChestFactory-", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Do not create if backup already exists
            if (ModState.Backups.OfType<Chest>()
                .Any(chest => chest.GlobalInventoryId.Equals(id, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            ModState.Backups.Add(new Chest(true) { GlobalInventoryId = id });
        }
    }
}