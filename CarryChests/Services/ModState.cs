using LeFauxMods.Common.Services;

namespace LeFauxMods.CarryChest.Services;

internal sealed class ModState
{
    private static ModState? Instance;
    private readonly ConfigHelper<ModConfig> configHelper;

    private ModState(IModHelper helper) => this.configHelper = new ConfigHelper<ModConfig>(helper);

    public static ModConfig Config => Instance!.configHelper.Config;

    public static ConfigHelper<ModConfig> ConfigHelper => Instance!.configHelper;

    public static void Init(IModHelper helper) => Instance ??= new ModState(helper);
}