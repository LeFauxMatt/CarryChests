using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Services;

namespace LeFauxMods.CarryChest.Services;

/// <summary>Responsible for handling the mod configuration menu.</summary>
internal sealed class ConfigMenu
{
    private readonly IGenericModConfigMenuApi api = null!;
    private readonly GenericModConfigMenuIntegration gmcm;
    private readonly IManifest manifest;

    public ConfigMenu(IModHelper helper, IManifest manifest)
    {
        this.manifest = manifest;
        this.gmcm = new GenericModConfigMenuIntegration(manifest, helper.ModRegistry);
        if (!this.gmcm.IsLoaded)
        {
            return;
        }

        this.api = this.gmcm.Api;
        this.SetupMenu();
    }

    private static ModConfig Config => ModState.ConfigHelper.Temp;

    private static ConfigHelper<ModConfig> ConfigHelper => ModState.ConfigHelper;

    private void SetupMenu()
    {
        this.gmcm.Register(ConfigHelper.Reset, ConfigHelper.Save);

        this.api.AddNumberOption(
            this.manifest,
            static () => Config.TotalLimit,
            static value => Config.TotalLimit = value,
            I18n.ConfigOption_TotalLimit_Name,
            I18n.ConfigOption_TotalLimit_Description);

        this.api.AddNumberOption(
            this.manifest,
            static () => Config.SlownessAmount,
            static value => Config.SlownessAmount = value,
            I18n.ConfigOption_SlownessAmount_Name,
            I18n.ConfigOption_SlownessAmount_Description);

        this.api.AddNumberOption(
            this.manifest,
            static () => Config.SlownessLimit,
            static value => Config.SlownessLimit = value,
            I18n.ConfigOption_SlownessLimit_Name,
            I18n.ConfigOption_SlownessLimit_Description);

        this.api.AddBoolOption(
            this.manifest,
            static () => Config.OpenHeldChest,
            static value => Config.OpenHeldChest = value,
            I18n.ConfigOption_OpenHeldChest_Name,
            I18n.ConfigOption_OpenHeldChest_Description);

        this.api.AddBoolOption(
            this.manifest,
            static () => Config.OverrideTool,
            static value => Config.OverrideTool = value,
            I18n.ConfigOption_OverrideToool_Name,
            I18n.ConfigOption_OverrideTool_Description);

        this.api.AddBoolOption(
            this.manifest,
            static () => Config.SwapChests,
            static value => Config.SwapChests = value,
            I18n.ConfigOption_SwapChests_Name,
            I18n.ConfigOption_SwapChests_Description);
    }
}