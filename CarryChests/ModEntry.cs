using LeFauxMods.CarryChest.Services;
using LeFauxMods.CarryChest.Utilities;
using LeFauxMods.Common.Models;
using LeFauxMods.Common.Services;
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
    private CommandHelper commandHelper = null!;

    /// <inheritdoc />
    public override void Entry(IModHelper helper)
    {
        // Init
        ModEvents.Subscribe<ConfigChangedEventArgs<ModConfig>>(this.OnConfigChanged);
        I18n.Init(helper.Translation);
        ModState.Init(helper);
        Log.Init(this.Monitor, ModState.Config);
        ModPatches.Apply();

        // Commands
        this.commandHelper = new CommandHelper(
                helper,
                "carry_chests",
                I18n.Command_CarryChests_Description,
                I18n.Command_Unknown_Description)
            .AddCommand("help", I18n.Command_Help_Description)
            .AddCommand("backup", I18n.Command_Backups_Description);

        // Events
        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
        ModEvents.Subscribe<CommandReceivedEventArgs>(this.OnCommandReceived);
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

    private void OnCommandReceived(CommandReceivedEventArgs e)
    {
        switch (e.Command)
        {
            case "help":
                Log.Info(this.commandHelper.HelpText);
                return;
            case "backup" when !Context.IsWorldReady:
                Log.Warn(I18n.Alert_CommandBackup_InvalidContext());
                return;
            case "backup":
                Game1.activeClickableMenu = new ItemGrabMenu(ModState.Backups);
                return;
            default:
                Log.Warn(I18n.Command_Unknown_Description());
                break;
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) =>
        _ = new ConfigMenu(this.Helper, this.ModManifest);

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button.IsUseToolButton() && Context.IsPlayerFree)
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

        if (!e.Button.IsActionButton() || !ModState.Config.OpenHeldChest)
        {
            return;
        }

        if (Context.IsPlayerFree && Game1.player.ActiveObject is Chest heldChest)
        {
            this.Helper.Input.Suppress(e.Button);
            heldChest.ShowMenu();
            return;
        }

        Game1.InUIMode(() =>
        {
            var (mouseX, mouseY) = e.Cursor.GetScaledScreenPixels().ToPoint();
            var inventoryMenu = Game1.activeClickableMenu switch
            {
                ItemGrabMenu { ItemsToGrabMenu: { } itemsToGrabMenu } when itemsToGrabMenu.isWithinBounds(mouseX,
                    mouseY) => itemsToGrabMenu,
                ItemGrabMenu { inventory: { } inventory } when inventory.isWithinBounds(mouseX, mouseY) =>
                    inventory,
                GameMenu gameMenu when gameMenu.GetCurrentPage() is InventoryPage { inventory: { } inventory } &&
                    inventory.isWithinBounds(mouseX, mouseY) => inventory,
                _ => null
            };

            if (inventoryMenu is not { inventory: { } slots, actualInventory: { } items })
            {
                return;
            }

            var slot = slots.FirstOrDefault(slot => slot.containsPoint(mouseX, mouseY));
            if (slot is null || !int.TryParse(slot.name, out var index) ||
                items[index] is not Chest chest)
            {
                return;
            }

            this.Helper.Input.Suppress(e.Button);
            chest.ShowMenu();
        });
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
                !id.StartsWith(Constants.BetterChestsPrefix, StringComparison.OrdinalIgnoreCase))
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