using LeFauxMods.Common.Integrations.GenericModConfigMenu;
using LeFauxMods.Common.Services;
using StardewModdingAPI.Events;

namespace LeFauxMods.CarryChest.Services;

internal sealed class ConfigMenu
{
    private readonly ModConfig config;
    private readonly ConfigHelper<ModConfig> configHelper;
    private readonly ModConfig defaultConfig;
    private readonly GenericModConfigMenuIntegration gmcm;
    private readonly IManifest manifest;
    private readonly ModConfig tempConfig;

    public ConfigMenu(IModHelper helper, IManifest manifest, ModConfig config, ConfigHelper<ModConfig> configHelper)
    {
        this.manifest = manifest;
        this.config = config;
        this.configHelper = configHelper;
        this.defaultConfig = new ModConfig();
        this.tempConfig = configHelper.Load();

        this.gmcm = new GenericModConfigMenuIntegration(manifest, helper.ModRegistry);
        if (!this.gmcm.IsLoaded)
        {
            return;
        }

        helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        if (!this.gmcm.IsLoaded)
        {
            return;
        }

        this.gmcm.Register(
            () => this.defaultConfig.CopyTo(this.tempConfig),
            () =>
            {
                this.tempConfig.CopyTo(this.config);
                this.configHelper.Save(this.tempConfig);
            });

        this.gmcm.Api.AddNumberOption(
            this.manifest,
            () => this.tempConfig.TotalLimit,
            value => this.tempConfig.TotalLimit = value,
            I18n.ConfigOption_TotalLimit_Name,
            I18n.ConfigOption_TotalLimit_Description);

        this.gmcm.Api.AddNumberOption(
            this.manifest,
            () => this.tempConfig.SlownessAmount,
            value => this.tempConfig.SlownessAmount = value,
            I18n.ConfigOption_SlownessAmount_Name,
            I18n.ConfigOption_SlownessAmount_Description);

        this.gmcm.Api.AddNumberOption(
            this.manifest,
            () => this.tempConfig.SlownessLimit,
            value => this.tempConfig.SlownessLimit = value,
            I18n.ConfigOption_SlownessLimit_Name,
            I18n.ConfigOption_SlownessLimit_Description);

        this.gmcm.Api.AddBoolOption(
            this.manifest,
            () => this.tempConfig.OpenHeldChest,
            value => this.tempConfig.OpenHeldChest = value,
            I18n.ConfigOption_OpenHeldChest_Name,
            I18n.ConfigOption_OpenHeldChest_Description);

        this.gmcm.Api.AddBoolOption(
            this.manifest,
            () => this.tempConfig.OverrideTool,
            value => this.tempConfig.OverrideTool = value,
            I18n.ConfigOption_OverrideToool_Name,
            I18n.ConfigOption_OverrideTool_Description);
    }
}
