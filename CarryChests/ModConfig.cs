using System.Globalization;
using System.Text;
using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.CarryChest;

/// <inheritdoc cref="IModConfig{TConfig}" />
internal class ModConfig : IModConfig<ModConfig>, IConfigWithLogAmount
{
    /// <summary>Gets or sets a value indicating whether held chests can be opened.</summary>
    public bool OpenHeldChest { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether to override using a tool against a chest.</summary>
    public bool OverrideTool { get; set; }

    /// <summary>Gets or sets the amount the player will be slowed when carrying chests above the limit.</summary>
    public float SlownessAmount { get; set; } = -1f;

    /// <summary>Gets or sets the maximum number of chests the player can hold before being slowed.</summary>
    public int SlownessLimit { get; set; } = 1;

    /// <summary>Gets or sets the number of chests the player can carry.</summary>
    public int TotalLimit { get; set; } = 3;

    /// <inheritdoc />
    public LogAmount LogAmount { get; set; }

    /// <inheritdoc />
    public void CopyTo(ModConfig other)
    {
        other.LogAmount = this.LogAmount;
        other.OpenHeldChest = this.OpenHeldChest;
        other.OverrideTool = this.OverrideTool;
        other.SlownessAmount = this.SlownessAmount;
        other.SlownessLimit = this.SlownessLimit;
        other.TotalLimit = this.TotalLimit;
    }

    public string GetSummary() =>
        new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.OpenHeldChest),25}: {this.OpenHeldChest}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.OverrideTool),25}: {this.OverrideTool}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.SlownessAmount),25}: {this.SlownessAmount}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.SlownessLimit),25}: {this.SlownessLimit}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.TotalLimit),25}: {this.TotalLimit}")
            .ToString();
}