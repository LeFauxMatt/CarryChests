using LeFauxMods.Common.Services;

namespace LeFauxMods.CarryChest.Services;

/// <inheritdoc />
internal sealed class ConfigMenu(IModHelper helper, IManifest manifest)
    : BaseConfigMenu<ModConfig>(helper, manifest)
{
    /// <inheritdoc />
    protected override ModConfig Config => ModState.ConfigHelper.Temp;

    /// <inheritdoc />
    protected override ConfigHelper<ModConfig> ConfigHelper => ModState.ConfigHelper;

    /// <inheritdoc />
    protected internal override void SetupOptions()
    {
        this.Api.AddBoolOption(
            this.Manifest,
            () => this.Config.Enabled,
            value => this.Config.Enabled = value,
            I18n.ConfigOption_Enabled_Name,
            I18n.ConfigOption_Enabled_Description);

        this.Api.AddNumberOption(
            this.Manifest,
            () => this.Config.MaximumReach,
            value => this.Config.MaximumReach = value,
            I18n.ConfigOption_MaximumReach_Name,
            I18n.ConfigOption_MaximumReach_Description,
            1,
            16);

        this.Api.AddNumberOption(
            this.Manifest,
            () => this.Config.TotalLimit,
            value => this.Config.TotalLimit = value,
            I18n.ConfigOption_TotalLimit_Name,
            I18n.ConfigOption_TotalLimit_Description);

        this.Api.AddNumberOption(
            this.Manifest,
            () => this.Config.SlownessAmount,
            value => this.Config.SlownessAmount = value,
            I18n.ConfigOption_SlownessAmount_Name,
            I18n.ConfigOption_SlownessAmount_Description);

        this.Api.AddNumberOption(
            this.Manifest,
            () => this.Config.SlownessLimit,
            value => this.Config.SlownessLimit = value,
            I18n.ConfigOption_SlownessLimit_Name,
            I18n.ConfigOption_SlownessLimit_Description);

        this.Api.AddBoolOption(
            this.Manifest,
            () => this.Config.OpenHeldChest,
            value => this.Config.OpenHeldChest = value,
            I18n.ConfigOption_OpenHeldChest_Name,
            I18n.ConfigOption_OpenHeldChest_Description);

        this.Api.AddBoolOption(
            this.Manifest,
            () => this.Config.OverrideTool,
            value => this.Config.OverrideTool = value,
            I18n.ConfigOption_OverrideToool_Name,
            I18n.ConfigOption_OverrideTool_Description);

        this.Api.AddBoolOption(
            this.Manifest,
            () => this.Config.SwapChests,
            value => this.Config.SwapChests = value,
            I18n.ConfigOption_SwapChests_Name,
            I18n.ConfigOption_SwapChests_Description);

        this.Api.AddKeybindList(
            this.Manifest,
            () => this.Config.ToggleEnabled,
            value => this.Config.ToggleEnabled = value,
            I18n.ConfigOption_ToggleEnabled_Name,
            I18n.ConfigOption_ToggleEnabled_Description);
    }
}