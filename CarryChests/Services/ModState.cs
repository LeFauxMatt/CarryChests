using LeFauxMods.Common.Services;
using LeFauxMods.Common.Utilities;
using StardewModdingAPI.Events;
using StardewValley.Inventories;
using StardewValley.Menus;
using StardewValley.Objects;

namespace LeFauxMods.CarryChest.Services;

internal sealed class ModState
{
    private static ModState? Instance;

    private readonly ConfigHelper<ModConfig> configHelper;

    private Inventory? backups;

    private ModState(IModHelper helper)
    {
        this.configHelper = new ConfigHelper<ModConfig>(helper);
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
    }

    public static Inventory Backups => Instance!.backups ??=
        Game1.player.team.GetOrCreateGlobalInventory(Constants.GlobalInventoryId);

    public static ModConfig Config => Instance!.configHelper.Config;

    public static ConfigHelper<ModConfig> ConfigHelper => Instance!.configHelper;

    public static void Init(IModHelper helper) => Instance ??= new ModState(helper);

    private static void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.OldMenu is not ItemGrabMenu { sourceItem: Chest { playerChest.Value: true } chest })
        {
            return;
        }

        Backups.SyncBackup(chest);
        Backups.RemoveEmptySlots();
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e) => this.backups = null;
}