using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.UltraOrganizedChests;

/// <summary>Represents the mod's configuration.</summary>
internal sealed class ModConfig : IConfigWithLogAmount
{
    /// <summary>Gets or sets a value indicating whether the mod should be enabled by default.</summary>
    public bool EnabledByDefault { get; set; }

    /// <inheritdoc />
    public LogAmount LogAmount { get; set; }

    /// <summary>
    ///     Copies the values from another instance of <see cref="ModConfig" />.
    /// </summary>
    /// <param name="other">The other config to copy to.</param>
    public void CopyTo(ModConfig other)
    {
        other.EnabledByDefault = this.EnabledByDefault;
        other.LogAmount = this.LogAmount;
    }
}
