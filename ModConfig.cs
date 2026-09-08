using StardewModdingAPI.Utilities;

namespace DearlyKept;

internal sealed class ModConfig
{
    public bool CaptureMailGifts { get; set; } = true;
    public bool CaptureBirthdayGifts { get; set; } = true;
    public bool CaptureMarriageOverhaulGifts { get; set; } = true;
    public bool CaptureAnniversaryGifts { get; set; } = true;
    public KeybindList OpenKeepsakes { get; set; } = KeybindList.Parse("K");
}
