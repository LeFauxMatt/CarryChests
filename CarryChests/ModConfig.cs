using System.Globalization;
using System.Text;
using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;
using StardewModdingAPI.Utilities;

namespace LeFauxMods.CarryChest;

/// <inheritdoc cref="IModConfig{TConfig}" />
internal sealed class ModConfig : IModConfig<ModConfig>, IConfigWithLogAmount
{
    /// <summary>Gets or sets a value indicating whether carrying is enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether held chests can be opened.</summary>
    public bool OpenHeldChest { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether to override using a tool against a chest.</summary>
    public bool OverrideTool { get; set; }

    /// <summary>Gets or sets a value indicating whether empty chests will be picked up as items.</summary>
    public bool GrabEmptyAsItem { get; set; }

    /// <summary>Gets or sets the maximum reach for grabbing a chest.</summary>
    public int MaximumReach { get; set; } = 1;

    /// <summary>Gets or sets the amount the player will be slowed when carrying chests above the limit.</summary>
    public float SlownessAmount { get; set; } = -1f;

    /// <summary>Gets or sets the maximum number of chests the player can hold before being slowed.</summary>
    public int SlownessLimit { get; set; } = 1;

    /// <summary>Gets or sets a value indicating whether to allow swapping.</summary>
    public bool SwapChests { get; set; } = true;

    /// <summary>Gets or sets the keybind for toggling enabled.</summary>
    public KeybindList ToggleEnabled { get; set; } = new();

    /// <summary>Gets or sets the number of chests the player can carry.</summary>
    public int TotalLimit { get; set; } = 3;

    /// <inheritdoc />
    public LogAmount LogAmount { get; set; }

    /// <inheritdoc />
    public void CopyTo(ModConfig other)
    {
        other.LogAmount = this.LogAmount;
        other.GrabEmptyAsItem = this.GrabEmptyAsItem;
        other.MaximumReach = this.MaximumReach;
        other.OpenHeldChest = this.OpenHeldChest;
        other.OverrideTool = this.OverrideTool;
        other.SlownessAmount = this.SlownessAmount;
        other.SlownessLimit = this.SlownessLimit;
        other.SwapChests = this.SwapChests;
        other.ToggleEnabled = this.ToggleEnabled;
        other.TotalLimit = this.TotalLimit;
    }

    /// <inheritdoc />
    public string GetSummary() =>
        new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.GrabEmptyAsItem),25}: {this.GrabEmptyAsItem}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.MaximumReach),25}: {this.MaximumReach}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.OpenHeldChest),25}: {this.OpenHeldChest}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.OverrideTool),25}: {this.OverrideTool}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.SlownessAmount),25}: {this.SlownessAmount}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.SlownessLimit),25}: {this.SlownessLimit}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.SwapChests),25}: {this.SwapChests}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.ToggleEnabled),25}: {this.ToggleEnabled}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.TotalLimit),25}: {this.TotalLimit}")
            .ToString();
}