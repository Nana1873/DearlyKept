using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace DearlyKept;

// Matching subset of Generic Mod Config Menu's public API.
public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    void AddKeybindList(IManifest mod, Func<KeybindList> getValue, Action<KeybindList> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
    void OpenModMenuAsChildMenu(IManifest mod);
}
