using System.Reflection;
using HarmonyLib;
using LeFauxMods.Common.Services;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley.Inventories;
using StardewValley.Objects;

namespace LeFauxMods.CarryChest.Services;

internal sealed class ModState
{
    private static ModState? Instance;
    private readonly ConfigHelper<ModConfig> configHelper;
    private readonly PerScreen<int> frameCounter = new();
    private readonly FieldInfo currentLidFrame;
    private readonly PerScreen<int> lastLidFrame = new();
    private readonly PerScreen<int> startingLidFrame = new();
    private Inventory? backups;

    private ModState(IModHelper helper)
    {
        this.configHelper = new ConfigHelper<ModConfig>(helper);
        this.currentLidFrame = AccessTools.Field(typeof(Chest), "currentLidFrame");
        helper.Events.GameLoop.ReturnedToTitle += this.OnReturnedToTitle;
    }

    public static Inventory Backups => Instance!.backups ??=
        Game1.player.team.GetOrCreateGlobalInventory(Constants.GlobalInventoryId);

    public static ModConfig Config => Instance!.configHelper.Config;

    public static ConfigHelper<ModConfig> ConfigHelper => Instance!.configHelper;

    public static FieldInfo CurrentLidFrame => Instance!.currentLidFrame;

    public static int FrameCounter
    {
        get => Instance!.frameCounter.Value;
        set => Instance!.frameCounter.Value = value;
    }

    public static int LastLidFrame
    {
        get => Instance!.lastLidFrame.Value;
        set => Instance!.lastLidFrame.Value = value;
    }

    public static int StartingLidFrame
    {
        get => Instance!.startingLidFrame.Value;
        set => Instance!.startingLidFrame.Value = value;
    }

    public static void Init(IModHelper helper) => Instance ??= new ModState(helper);

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e) => this.backups = null;
}