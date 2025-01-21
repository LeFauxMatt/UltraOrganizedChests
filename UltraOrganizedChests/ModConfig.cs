using System.Globalization;
using System.Text;
using LeFauxMods.Common.Interface;
using LeFauxMods.Common.Models;

namespace LeFauxMods.UltraOrganizedChests;

/// <inheritdoc cref="IModConfig{TConfig}" />
internal sealed class ModConfig : IModConfig<ModConfig>, IConfigWithLogAmount
{
    /// <summary>Gets or sets a value indicating whether the mod should be enabled by default.</summary>
    public bool EnabledByDefault { get; set; }

    /// <inheritdoc />
    public LogAmount LogAmount { get; set; }

    /// <summary>Gets or sets a modifier for moving items.</summary>
    public SButton MoveItemsModifier { get; set; } = SButton.LeftShift;

    /// <summary>Gets or sets a value indicating whether organization will happen automatically.</summary>
    public bool OrganizeNightly { get; set; }

    /// <summary>Gets or sets a value indicating whether the vanilla organization button will be replaced.</summary>
    public bool ReplaceOrganizeButton { get; set; }

    /// <inheritdoc />
    public void CopyTo(ModConfig other)
    {
        other.EnabledByDefault = this.EnabledByDefault;
        other.MoveItemsModifier = this.MoveItemsModifier;
        other.LogAmount = this.LogAmount;
        other.OrganizeNightly = this.OrganizeNightly;
        other.ReplaceOrganizeButton = this.ReplaceOrganizeButton;
    }

    /// <inheritdoc />
    public string GetSummary() =>
        new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.EnabledByDefault),25}: {this.EnabledByDefault}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.MoveItemsModifier),25}: {this.MoveItemsModifier}")
            .AppendLine(CultureInfo.InvariantCulture, $"{nameof(this.OrganizeNightly),25}: {this.OrganizeNightly}")
            .AppendLine(CultureInfo.InvariantCulture,
                $"{nameof(this.ReplaceOrganizeButton),25}: {this.ReplaceOrganizeButton}")
            .ToString();
}