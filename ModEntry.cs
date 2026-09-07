using StardewModdingAPI;
using StardewValley;

namespace DearlyKept;

internal sealed class ModEntry : Mod
{
    private ModConfig Config = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();
        Config.OpenKeepsakes ??= StardewModdingAPI.Utilities.KeybindList.Parse("K");
        helper.WriteConfig(Config);

        var capture = new GiftCapture(helper, Monitor, () => Config.CaptureMailGifts);
        capture.Register();
        GiftPatches.Apply(ModManifest.UniqueID,
            stamp => Config.ShowGiftNotesInTooltips ? FormatNote(stamp) : "", capture.HandleMenu);

        helper.Events.Input.ButtonsChanged += (_, _) =>
        {
            if (Config.OpenKeepsakes.JustPressed() && Context.IsPlayerFree)
            {
                helper.Input.SuppressActiveKeybinds(Config.OpenKeepsakes);
                OpenMenu();
            }
        };
        helper.ConsoleCommands.Add("dk", "Open your backpack's keepsakes while no other menu is open.", (_, _) => OpenMenu());
        helper.ConsoleCommands.Add("dk_status", "Report the gift notes currently in your backpack.", (_, _) =>
        {
            if (!Context.IsWorldReady)
            {
                Monitor.Log("Load a save before inspecting keepsakes.", LogLevel.Info);
                return;
            }
            int count = 0;
            foreach (Item item in Game1.player.Items)
            {
                if (item is null || !GiftTagService.TryRead(item, out GiftStamp stamp))
                    continue;
                count++;
                Monitor.Log($"{item.QualifiedItemId} x{item.Stack}: {stamp.SenderId}, {stamp.Season} {stamp.Day}, year {stamp.Year} (mail: {stamp.MailId}).", LogLevel.Info);
            }
            Monitor.Log($"Keepsake stacks in backpack: {count}.", LogLevel.Info);
        });
    }

    private void OpenMenu()
    {
        if (!Context.IsPlayerFree)
            return;
        Game1.activeClickableMenu = new KeepsakeMenu(Helper.Translation, FormatNote, SenderName);
        Game1.playSound("bigSelect");
    }

    private string SenderName(string id)
    {
        if (id is "Mom" or "Dad")
            return Helper.Translation.Get("sender." + id.ToLowerInvariant());
        return Game1.getCharacterFromName(id)?.displayName ?? id;
    }

    private string FormatNote(GiftStamp stamp)
    {
        string sender = Helper.Translation.Get("note.sender", new { name = SenderName(stamp.SenderId) });
        string date = Helper.Translation.Get("note.date", new
        {
            season = Helper.Translation.Get("season." + stamp.Season).ToString(),
            day = stamp.Day,
            year = stamp.Year
        });
        return sender + "\n" + date + "\n" + Helper.Translation.Get("note.source");
    }
}
