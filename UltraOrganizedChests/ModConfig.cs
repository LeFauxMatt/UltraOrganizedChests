using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.UltraOrganizedChests;

/// <inheritdoc cref="IConfigWithCopyTo{TConfig}" />
/// <summary>Represents the mod's configuration.</summary>
internal sealed class ModConfig : IConfigWithCopyTo<ModConfig>, IConfigWithLogAmount
{
    /// <summary>Gets or sets a value indicating whether the mod should be enabled by default.</summary>
    public bool EnabledByDefault { get; set; }

    /// <summary>Gets or sets a value indicating whether organization will happen automatically.</summary>
    public bool OrganizeNightly { get; set; }

    /// <inheritdoc />
    public LogAmount LogAmount { get; set; }

    /// <inheritdoc />
    public void CopyTo(ModConfig other)
    {
        other.EnabledByDefault = this.EnabledByDefault;
        other.LogAmount = this.LogAmount;
        other.OrganizeNightly = this.OrganizeNightly;
    }
}