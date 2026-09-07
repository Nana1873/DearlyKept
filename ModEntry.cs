using StardewModdingAPI;
using StardewValley;

namespace DearlyKept;

internal sealed class ModEntry : Mod
{
    private ModConfig Config = null!;
    private GiftJournal journal = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Config.OpenKeepsakes ??= StardewModdingAPI.Utilities.KeybindList.Parse("K");
        helper.WriteConfig(Config);
        journal = new GiftJournal(helper, Monitor);
        var birthday = new HappyBirthdayIntegration(helper, Monitor, journal, () => Config.CaptureBirthdayGifts);
        birthday.Register();
        var capture = new GiftCapture(helper, Monitor, journal, () => Config.CaptureMailGifts,
            () => Config.CaptureBirthdayGifts && birthday.IsActive);
        capture.Register();
        GiftPatches.Apply(ModManifest.UniqueID, capture);

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
        Game1.activeClickableMenu = new KeepsakeMenu(Helper.Translation, journal, FormatNote, SenderName);
        Game1.playSound("bigSelect");
    }

    private string SenderName(string id)
    {
        if (id is "Mom" or "Dad")
            return Helper.Translation.Get("sender." + id.ToLowerInvariant());
        return Game1.getCharacterFromName(id, false)?.displayName ?? id;
    }

    private string FormatNote(GiftEntry entry)
    {
        string sender = Helper.Translation.Get("note.sender", new { name = SenderName(entry.SenderId) });
        string date = Helper.Translation.Get("note.date", new
        {
            season = Helper.Translation.Get("season." + entry.Season).ToString(), day = entry.Day, year = entry.Year
        });
        string source = Helper.Translation.Get("note.source." + entry.Origin);
        if (entry.SourceModId is not null)
        {
            string modName = Helper.ModRegistry.Get(entry.SourceModId)?.Manifest.Name ?? entry.SourceModId;
            source += "\n" + Helper.Translation.Get("note.provider", new { name = modName });
        }
        return sender + "\n" + date + "\n" + source;
    }
}
