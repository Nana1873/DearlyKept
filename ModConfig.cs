using StardewModdingAPI.Utilities;

namespace DearlyKept;

internal sealed class ModConfig
{
    public bool CaptureMailGifts { get; set; } = true;
    public bool ShowGiftNotesInTooltips { get; set; } = true;
    public KeybindList OpenKeepsakes { get; set; } = KeybindList.Parse("K");
}
