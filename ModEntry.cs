using StardewModdingAPI;
using StardewValley;

namespace DearlyKept;

internal sealed class ModEntry : Mod
{
    private ModConfig Config = null!;
    private GiftJournal journal = null!;
    private IGenericModConfigMenuApi? configMenu;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Config.OpenKeepsakes ??= StardewModdingAPI.Utilities.KeybindList.Parse("K");
        helper.WriteConfig(Config);
        journal = new GiftJournal(helper, Monitor);
        var birthday = new HappyBirthdayIntegration(helper, Monitor, journal, () => Config.CaptureBirthdayGifts);
        birthday.Register();
        new SpouseGiftIntegrations(helper, Monitor, journal,
            () => Config.CaptureMarriageOverhaulGifts, () => Config.CaptureAnniversaryGifts).Register();
        var capture = new GiftCapture(helper, Monitor, journal, () => Config.CaptureMailGifts,
            () => Config.CaptureBirthdayGifts && birthday.IsActive);
        capture.Register();
        GiftPatches.Apply(ModManifest.UniqueID, capture);
        helper.Events.GameLoop.GameLaunched += (_, _) => RegisterConfigMenu();

        helper.Events.Input.ButtonsChanged += (_, _) =>
        {
            if (Config.OpenKeepsakes.JustPressed() && Context.IsPlayerFree)
            {
                helper.Input.SuppressActiveKeybinds(Config.OpenKeepsakes);
                OpenMenu();
            }
        };
        helper.ConsoleCommands.Add("dk", "Open your gift journal while no other menu is open.", (_, _) => OpenMenu());
        helper.ConsoleCommands.Add("dk_status", "Report the current save's gift journal.", (_, _) =>
        {
            if (!Context.IsWorldReady)
            {
                Monitor.Log("Load a save before inspecting the gift journal.", LogLevel.Info);
                return;
            }
            foreach (GiftEntry entry in journal.Entries)
                Monitor.Log($"{entry.QualifiedItemId} x{entry.Quantity}: {entry.SenderId}, {entry.Season} {entry.Day}, year {entry.Year} ({entry.Origin}: {entry.SourceId}).", LogLevel.Info);
            Monitor.Log($"Gift journal: {journal.Entries.Count} entries. Recording enabled: {journal.CanRecord}. Happy Birthday adapter active: {birthday.IsActive}.", LogLevel.Info);
        });
    }

    public override object GetApi() => new JournalApi(journal.GetGiftsJson, () => journal.Entries.Count);

    private void OpenMenu()
    {
        if (!Context.IsPlayerFree)
            return;
        Game1.activeClickableMenu = new KeepsakeMenu(Helper.Translation, journal, FormatNote, SenderName, configMenu is null ? null : () => configMenu.OpenModMenuAsChildMenu(ModManifest));
        Game1.playSound("bigSelect");
    }

    private void RegisterConfigMenu()
    {
        configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null) return;
        configMenu.Register(ModManifest, () => Config = new ModConfig(), () => Helper.WriteConfig(Config));
        configMenu.AddKeybindList(ModManifest, () => Config.OpenKeepsakes, value => Config.OpenKeepsakes = value,
            () => Helper.Translation.Get("menu.setting-key"), fieldId: nameof(ModConfig.OpenKeepsakes));
        configMenu.AddBoolOption(ModManifest, () => Config.CaptureMailGifts, value => Config.CaptureMailGifts = value,
            () => Helper.Translation.Get("menu.setting-mail"), fieldId: nameof(ModConfig.CaptureMailGifts));
        configMenu.AddBoolOption(ModManifest, () => Config.CaptureBirthdayGifts, value => Config.CaptureBirthdayGifts = value,
            () => Helper.Translation.Get("menu.setting-birthday"), fieldId: nameof(ModConfig.CaptureBirthdayGifts));
        configMenu.AddBoolOption(ModManifest, () => Config.CaptureMarriageOverhaulGifts, value => Config.CaptureMarriageOverhaulGifts = value,
            () => Helper.Translation.Get("menu.setting-marriage"), fieldId: nameof(ModConfig.CaptureMarriageOverhaulGifts));
        configMenu.AddBoolOption(ModManifest, () => Config.CaptureAnniversaryGifts, value => Config.CaptureAnniversaryGifts = value,
            () => Helper.Translation.Get("menu.setting-anniversary"), fieldId: nameof(ModConfig.CaptureAnniversaryGifts));
    }
    private string SenderName(string id)
    {
        if (id is "Mom" or "Dad")
            return Helper.Translation.Get("sender." + id.ToLowerInvariant());
        return Game1.getCharacterFromName(id, false)?.displayName ?? id;
    }

    private string FormatNote(GiftEntry entry)
    {
        string sender = Helper.Translation.Get("note.sender", new { name = Game1.getCharacterFromName(entry.SenderId, false)?.displayName ?? entry.SenderDisplayName ?? SenderName(entry.SenderId) });
        string date = Helper.Translation.Get("note.date", new
        {
            season = Helper.Translation.Get("season." + entry.Season).ToString(), day = entry.Day, year = entry.Year
        });
        string source = Helper.Translation.Get("note.source." + entry.Origin);
        if (entry.SourceModId is not null)
        {
            string modName = Helper.ModRegistry.Get(entry.SourceModId)?.Manifest.Name ?? entry.SourceDisplayName ?? entry.SourceModId;
            source += "\n" + Helper.Translation.Get("note.provider", new { name = modName });
        }
        return sender + "\n" + date + "\n" + source;
    }
}
