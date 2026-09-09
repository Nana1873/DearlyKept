using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace DearlyKept.LiveHarness;

internal sealed partial class ModEntry
{
    private JournalSnapshot? collisionBaseline;
    private Dictionary<string, int>? collisionItems;
    private void CollisionProbe(string[] args, GuardContext owned)
    {
        RequireNoMenu();
        if (args.ElementAtOrDefault(1) == "prepare")
        {
            RunBirthdayCommand(new[] { "birthday", "prepare" }, owned);
            collisionBaseline = CaptureJournal();
            collisionItems = SpouseItems();
            RunSpouseCommand(new[] { "spouse", "birthday" });
            Monitor.Log("DKCOLLISION started the original Marriage Overhaul gift while Happy Birthday can replace Leah's greeting. Close the real dialogue, then collision assert.", LogLevel.Info);
            return;
        }
        var entries = NewJournalEntries(collisionBaseline ?? throw new InvalidOperationException("Prepare the overlap first."));
        Check(entries.Length == 2, "two original birthday producers create exactly two receipts");
        var marriage = entries.Single(e => e.SourceModId == "TitanmasterRy.MarriageOverhaul");
        var birthday = entries.Single(e => e.SourceModId == BirthdayModId);
        Check(entries.All(e => e.SenderId == "Leah" && e.Origin == "birthday"), "overlapping receipts retain their actual sender and occasion");
        Check(!string.IsNullOrWhiteSpace(birthday.MessageText), "Happy Birthday retains its displayed replacement greeting");
        Check(marriage.MessageText != birthday.MessageText, "Marriage Overhaul does not acquire the other producer's replacement greeting");
        var after = SpouseItems();
        foreach (var group in entries.GroupBy(e => e.QualifiedItemId))
            Check(after.GetValueOrDefault(group.Key) - collisionItems!.GetValueOrDefault(group.Key) == group.Sum(e => e.Quantity), "received inventory delta matches both independent birthday receipts");
        Monitor.Log("DKCOLLISION PASS: original producers co-loaded, distinct provenance/text, exact delivery quantities.", LogLevel.Info);
    }

    private void SearchProbe(string[] args)
    {
        IClickableMenu menu = Game1.activeClickableMenu ?? throw new InvalidOperationException("Open the archive first.");
        if (menu.GetType().FullName != "DearlyKept.KeepsakeMenu") throw new InvalidOperationException("Not the archive.");
        var box = (TextBox)menu.GetType().GetField("searchBox", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(menu)!;
        Check(box.Selected, "search field was selected through normal menu input");
        box.Text = "";
        // Owned synthetic text-entry fixture: invokes the native widget callback, not OS input.
        box.RecieveTextInput(string.Join(" ", args.Skip(1)));
        menu.receiveKeyPress(Microsoft.Xna.Framework.Input.Keys.Enter);
        ProbeArchive();
    }

    private void OpenArchiveTimed()
    {
        RequireNoMenu();
        object metadata = Helper.ModRegistry.Get("Nana1873.DearlyKept")!;
        object mod = metadata.GetType().GetProperty("Mod")!.GetValue(metadata)!;
        Stopwatch timer = Stopwatch.StartNew();
        mod.GetType().GetMethod("OpenMenu", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(mod, null);
        Monitor.Log($"DKREADINESS OPEN: {timer.ElapsedMilliseconds} ms.", LogLevel.Info);
        ProbeArchive();
    }

    private void DisplayProbe(string[] args)
    {
        string mode = args.ElementAtOrDefault(1) ?? "report";
        // Never change the desktop display mode/resolution in the shared lab.
        if (mode == "borderless")
            Game1.options.setWindowedOption(0);
        else if (mode == "scale")
        {
            if (args[2] is not ("100" or "125" or "150" or "200" or "250" or "300")) throw new InvalidOperationException("Unlisted review UI scale.");
            Game1.options.changeDropDownOption(39, args[2] + "%");
        }
        else if (mode != "report") throw new InvalidOperationException("Unknown display probe.");
        Monitor.Log($"DKDISPLAY mode={mode}, fullscreen={Game1.graphics.IsFullScreen}, borderless={Game1.options.isCurrentlyWindowedBorderless()}, viewport={Game1.uiViewport.Width}x{Game1.uiViewport.Height}, backbuffer={Game1.graphics.PreferredBackBufferWidth}x{Game1.graphics.PreferredBackBufferHeight}, scale={Game1.options.uiScale}", LogLevel.Info);
    }

    private object ProductionJournal()
    {
        object metadata = Helper.ModRegistry.Get("Nana1873.DearlyKept")!;
        object mod = metadata.GetType().GetProperty("Mod")!.GetValue(metadata)!;
        return mod.GetType().GetField("journal", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mod)!;
    }

    private void SeedArchive(string[] args)
    {
        RequireNoMenu();
        string kind = args.ElementAtOrDefault(1) ?? "small";
        int count = kind == "large" ? 10000 : kind == "medium" ? 1000 : 12;
        object journal = ProductionJournal();
        var rows = Enumerable.Range(0, count).Select(i => new
        {
            Id = Guid.NewGuid().ToString("N"), SenderId = i % 3 == 0 ? "Missing.QA.NPC" : i % 3 == 1 ? "Leah" : "Penny",
            SourceId = "synthetic-ui-" + i, Origin = i % 2 == 0 ? "birthday" : "anniversary", SourceModId = "TitanmasterRy.MarriageOverhaul",
            Year = i % 10 + 1, Season = new[] { "spring", "summer", "fall", "winter" }[i % 4], Day = i % 28 + 1,
            QualifiedItemId = i % 3 == 0 ? "(O)Missing.QA.Item" : "(O)223", ItemName = i % 3 == 0 ? "A remembered custom gift" : "Cookies",
            Quantity = i % 5 + 1, Quality = 0,
            MessageText = "Dear farmer,\n\nThis is synthetic UI/performance evidence, not an actual gift delivery.\n\nKeepsake number " + i,
            MessageIncomplete = i == 0, SenderDisplayName = i % 3 == 0 ? "A remembered friend" : null, SourceDisplayName = "Marriage Overhaul"
        });
        string raw = kind == "broken" ? "{invalid" : JsonSerializer.Serialize(new { SchemaVersion = 1, Gifts = rows });
        Stopwatch timer = Stopwatch.StartNew();
        Game1.player.modData["Nana1873.DearlyKept/GiftJournal"] = raw;
        journal.GetType().GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(journal, null);
        Monitor.Log($"DKREADINESS SEED {kind}: {count} synthetic entries, {raw.Length} JSON chars, load {timer.ElapsedMilliseconds} ms.", LogLevel.Info);
    }

    private void ProbeArchive()
    {
        IClickableMenu menu = Game1.activeClickableMenu ?? throw new InvalidOperationException("Open the archive first.");
        if (menu.GetType().FullName != "DearlyKept.KeepsakeMenu") throw new InvalidOperationException("Not the archive.");
        Rectangle view = new(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height);
        Check(view.Contains(new Rectangle(menu.xPositionOnScreen, menu.yPositionOnScreen, menu.width, menu.height)), "archive outer bounds fit the actual UI viewport");
        foreach (string name in new[] { "originBounds", "senderBounds", "yearBounds", "seasonBounds", "readBounds", "settingsBounds" })
        {
            Rectangle bounds = (Rectangle)menu.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(menu)!;
            if (name == "settingsBounds" && bounds == Rectangle.Empty) continue;
            Check(bounds.Width > 0 && bounds.Height > 0 && view.Contains(bounds), name + " has positive in-viewport hit bounds");
        }
        var entries = (System.Collections.ICollection)menu.GetType().GetField("entries", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(menu)!;
        Monitor.Log($"DKREADINESS UI PASS: {view.Width}x{view.Height}; visible result count={entries.Count}", LogLevel.Info);
    }
}
