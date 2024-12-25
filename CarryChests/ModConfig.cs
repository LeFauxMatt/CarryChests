using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.CarryChest;

/// <summary>Represents the mod configuration.</summary>
internal class ModConfig : IConfigWithLogAmount
{
    /// <inheritdoc />
    public LogAmount LogAmount { get; set; }

    /// <summary>Gets or sets the amount the player will be slowed when carrying chests above the limit.</summary>
    public float SlownessAmount { get; set; } = -1f;

    /// <summary>Gets or sets the maximum number of chests the player can hold before being slowed.</summary>
    public int SlownessLimit { get; set; } = 1;

    /// <summary>Gets or sets the number of chests the player can carry.</summary>
    public int TotalLimit { get; set; } = 3;

    /// <summary>
    ///     Copies the values from this instance to another instance.
    /// </summary>
    /// <param name="other">The other config instance.</param>
    public void CopyTo(ModConfig other)
    {
        other.LogAmount = this.LogAmount;
        other.SlownessAmount = this.SlownessAmount;
        other.SlownessLimit = this.SlownessLimit;
        other.TotalLimit = this.TotalLimit;
    }
}
