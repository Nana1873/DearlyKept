using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace DearlyKept;

internal sealed partial class KeepsakeMenu
{
    private readonly TextBox searchBox;
    private readonly ModConfig? settings;
    private readonly Action? persistSettings;
    private Rectangle yearBounds, seasonBounds, settingsBounds;
    private int yearFilter;
    private string seasonFilter = "";
    private string searchTerm = "";
    private string pendingSearch = "";
    private double searchDelay;
    private List<(string Label, Action Select)>? picker;
    private string pickerTitle = "";
    private int pickerIndex, pickerFirst;
    private bool captureBinding;
    private int layoutWidth, layoutHeight;

    private static string? SourceName(GiftEntry entry) => entry.SourceDisplayName ?? (entry.SourceModId switch
    {
        "Omegasis.HappyBirthday" => "Happy Birthday",
        "TitanmasterRy.MarriageOverhaul" => "Marriage Overhaul",
        "Kantrip.WeddingAnniversaries" => "Wedding Anniversaries",
        _ => entry.SourceModId
    });
    private string SenderLabel(GiftEntry entry) => entry.SenderId is "Mom" or "Dad" ? senderName(entry.SenderId)
        : Game1.getCharacterFromName(entry.SenderId, false)?.displayName ?? entry.SenderDisplayName ?? entry.SenderId;

    private string SenderChoiceLabel(string id) => allEntries.FirstOrDefault(e => e.Entry.SenderId == id) is { } entry ? SenderLabel(entry.Entry) : senderName(id);

    private bool MatchesSearch(KeepsakeEntry entry) => ArchiveSearch.Matches(searchTerm,
        entry.DisplayName, entry.Entry.ItemName, entry.Entry.SenderDisplayName ?? entry.Entry.SenderId,
        entry.Entry.SenderId, entry.Entry.MessageText);

    private void UpdateControls(GameTime time)
    {
        if (layoutWidth != Game1.uiViewport.Width || layoutHeight != Game1.uiViewport.Height)
        {
            layoutWidth = Game1.uiViewport.Width; layoutHeight = Game1.uiViewport.Height;
            Layout();
        }
        if (searchBox.Text != pendingSearch)
        {
            pendingSearch = searchBox.Text; searchDelay = 180;
        }
        if (searchDelay > 0)
        {
            searchDelay -= time.ElapsedGameTime.TotalMilliseconds;
            if (searchDelay <= 0) ApplySearch();
        }
    }

    private void ApplySearch()
    {
        searchTerm = searchBox.Text.Trim(); searchDelay = 0;
        ApplyFilters(SelectedEntry?.Entry.Id);
    }

    private void DrawControls(SpriteBatch b)
    {
        DrawButton(b, yearBounds, yearFilter == 0 ? i18n.Get("menu.year-all") : i18n.Get("menu.year", new { year = yearFilter }), filterFocus == FilterFocus.Year);
        DrawButton(b, seasonBounds, seasonFilter.Length == 0 ? i18n.Get("menu.season-all") : i18n.Get("season." + seasonFilter), filterFocus == FilterFocus.Season);
        searchBox.Draw(b);
        if (searchBox.Text.Length == 0 && !searchBox.Selected)
            b.DrawString(Game1.smallFont, FitText(i18n.Get("menu.search"), Game1.smallFont, searchBox.Width - 28),
                new Vector2(searchBox.X + 14, searchBox.Y + 10), MutedText);
        DrawButton(b, settingsBounds, i18n.Get("menu.settings"), filterFocus == FilterFocus.Settings, settings != null);
    }

    private void OpenPicker(FilterFocus focus)
    {
        var values = focus == FilterFocus.Occasion
            ? new[] { "" }.Concat(new[] { "mail", "birthday", "spouse", "anniversary", "other" }.Where(o => allEntries.Any(e => e.Entry.Origin == o)))
            : new[] { "" }.Concat(allEntries.Select(e => e.Entry.SenderId).Distinct().OrderBy(senderName));
        BeginPicker(i18n.Get(focus == FilterFocus.Occasion ? "menu.pick-occasion" : "menu.pick-sender"), values.Select(value =>
            (value.Length == 0 ? i18n.Get("menu.filter.all").ToString() : focus == FilterFocus.Occasion ? OccasionName(value) : SenderChoiceLabel(value),
            (Action)(() => { if (focus == FilterFocus.Occasion) originFilter = value; else senderFilter = value; ApplyFilters(null); }))));
    }

    private void BeginPicker(string title, IEnumerable<(string Label, Action Select)> choices)
    {
        searchBox.Selected = false;
        pickerTitle = title; picker = choices.ToList(); pickerIndex = pickerFirst = 0; hoverText = "";
    }
    private Rectangle PickerArea => new(xPositionOnScreen + 28, yPositionOnScreen + 88, width - 56, height - 156);
    private int PickerRows => Math.Max(1, (PickerArea.Height - 100) / 40);
    private Rectangle PickerRow(int index) => new(PickerArea.X + 20, PickerArea.Y + 54 + index * 40, PickerArea.Width - 40, 38);
    private Rectangle PickerPreviousBounds => new(PickerArea.X + 20, PickerArea.Bottom - 44, 48, 40);
    private Rectangle PickerNextBounds => new(PickerArea.Right - 68, PickerArea.Bottom - 44, 48, 40);
    private void MovePicker(int step)
    {
        if (picker == null) return;
        pickerIndex = Math.Clamp(pickerIndex + step, 0, picker.Count - 1);
        if (pickerIndex < pickerFirst) pickerFirst = pickerIndex;
        if (pickerIndex >= pickerFirst + PickerRows) pickerFirst = pickerIndex - PickerRows + 1;
    }
    private void ChoosePicker()
    {
        if (picker == null || picker.Count == 0) return;
        Action select = picker[pickerIndex].Select; picker = null;
        select();
        Game1.playSound("smallSelect");
    }
    private void DrawPicker(SpriteBatch b)
    {
        if (picker == null) return;
        b.Draw(Game1.fadeToBlackRect, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height), Color.Black * .5f);
        DrawBox(b, PickerArea);
        DrawCentered(b, captureBinding ? i18n.Get("menu.bind-prompt") : pickerTitle, Game1.smallFont,
            new Rectangle(PickerArea.X + 20, PickerArea.Y + 6, PickerArea.Width - 40, 44), Game1.textColor);
        for (int row = 0; row < PickerRows && pickerFirst + row < picker.Count; row++)
            DrawButton(b, PickerRow(row), picker[pickerFirst + row].Label, pickerFirst + row == pickerIndex);
        Rectangle footer = new(PickerArea.X + 20, PickerArea.Bottom - 44, PickerArea.Width - 40, 40);
        DrawArrowButton(b, PickerPreviousBounds, false, pickerFirst > 0 && !captureBinding);
        DrawArrowButton(b, PickerNextBounds, true, pickerFirst + PickerRows < picker.Count && !captureBinding);
        DrawCentered(b, i18n.Get("menu.pick-help"), Game1.smallFont, new Rectangle(footer.X + 52, footer.Y, footer.Width - 104, 40), MutedText);
    }

    private bool HandleControlClick(int x, int y)
    {
        if (picker != null)
        {
            if (!PickerArea.Contains(x, y)) { picker = null; captureBinding = false; return true; }
            if (captureBinding) return true;
            for (int row = 0; row < PickerRows && pickerFirst + row < picker.Count; row++)
                if (PickerRow(row).Contains(x, y)) { pickerIndex = pickerFirst + row; ChoosePicker(); return true; }
            if (PickerPreviousBounds.Contains(x, y) && pickerFirst > 0) MovePicker(-PickerRows);
            else if (PickerNextBounds.Contains(x, y) && pickerFirst + PickerRows < picker.Count) MovePicker(PickerRows);
            return true;
        }
        if (messageEntry != null) return false;
        searchBox.Selected = new Rectangle(searchBox.X, searchBox.Y, searchBox.Width, 44).Contains(x, y);
        if (searchBox.Selected) return true;
        if (yearBounds.Contains(x, y))
        {
            BeginPicker(i18n.Get("menu.year-all"), new[] { 0 }.Concat(allEntries.Select(e => e.Entry.Year).Distinct().OrderByDescending(y => y))
                .Select(value => (value == 0 ? i18n.Get("menu.filter.all").ToString() : i18n.Get("menu.year", new { year = value }).ToString(),
                    (Action)(() => { yearFilter = value; ApplyFilters(null); }))));
            return true;
        }
        if (seasonBounds.Contains(x, y))
        {
            BeginPicker(i18n.Get("menu.season-all"), new[] { "", "spring", "summer", "fall", "winter" }
                .Select(value => (i18n.Get(value.Length == 0 ? "menu.filter.all" : "season." + value).ToString(),
                    (Action)(() => { seasonFilter = value; ApplyFilters(null); }))));
            return true;
        }
        if (settingsBounds.Contains(x, y) && settings != null) { OpenSettings(); return true; }
        return false;
    }

    private bool HandleControlKey(Keys key, bool cancel, bool activate, bool up, bool down)
    {
        if (captureBinding)
        {
            if (key is Keys.None or Keys.LeftShift or Keys.RightShift or Keys.LeftControl or Keys.RightControl or Keys.LeftAlt or Keys.RightAlt) return true;
            captureBinding = false;
            if (key != Keys.Escape && key is not (Keys.None or Keys.LeftShift or Keys.RightShift or Keys.LeftControl or Keys.RightControl))
            { settings!.OpenKeepsakes = KeybindList.Parse(key.ToString()); persistSettings?.Invoke(); }
            OpenSettings(); return true;
        }
        if (picker != null)
        {
            if (cancel) picker = null;
            else if (up) MovePicker(-1);
            else if (down) MovePicker(1);
            else if (key == Keys.PageUp) MovePicker(-PickerRows);
            else if (key == Keys.PageDown) MovePicker(PickerRows);
            else if (activate) ChoosePicker();
            return true;
        }
        if (searchBox.Selected)
        {
            if (key is Keys.Escape or Keys.Enter) { searchBox.Selected = false; ApplySearch(); }
            return true;
        }
        if (messageEntry != null) return false;
        if (key == Keys.F2 && settings != null) { OpenSettings(); return true; }
        if (key == Keys.F3) { searchBox.Selected = true; return true; }
        if (key == Keys.F4) { HandleControlClick(yearBounds.Center.X, yearBounds.Center.Y); return true; }
        if (key == Keys.F5) { HandleControlClick(seasonBounds.Center.X, seasonBounds.Center.Y); return true; }
        return false;
    }

    private void OpenSettings()
    {
        if (settings == null) return;
        string Toggle(string key, bool value) => i18n.Get(key) + ": " + i18n.Get(value ? "menu.on" : "menu.off");
        Action Change(Action apply) => () => { apply(); persistSettings?.Invoke(); OpenSettings(); };
        BeginPicker(i18n.Get("menu.settings"), new (string, Action)[]
        {
            (Toggle("menu.setting-mail", settings.CaptureMailGifts), Change(() => settings.CaptureMailGifts = !settings.CaptureMailGifts)),
            (Toggle("menu.setting-birthday", settings.CaptureBirthdayGifts), Change(() => settings.CaptureBirthdayGifts = !settings.CaptureBirthdayGifts)),
            (Toggle("menu.setting-marriage", settings.CaptureMarriageOverhaulGifts), Change(() => settings.CaptureMarriageOverhaulGifts = !settings.CaptureMarriageOverhaulGifts)),
            (Toggle("menu.setting-anniversary", settings.CaptureAnniversaryGifts), Change(() => settings.CaptureAnniversaryGifts = !settings.CaptureAnniversaryGifts)),
            (i18n.Get("menu.setting-key") + ": " + settings.OpenKeepsakes, () => { OpenSettings(); captureBinding = true; }),
            (i18n.Get("menu.back"), () => { })
        });
    }

    private void ActivateFocusedControl()
    {
        switch (filterFocus)
        {
            case FilterFocus.Occasion:
            case FilterFocus.Sender: OpenPicker(filterFocus); break;
            case FilterFocus.Year: HandleControlClick(yearBounds.Center.X, yearBounds.Center.Y); break;
            case FilterFocus.Season: HandleControlClick(seasonBounds.Center.X, seasonBounds.Center.Y); break;
            case FilterFocus.Settings: OpenSettings(); break;
            case FilterFocus.Search:
                searchBox.Selected = true;
                if (Game1.options.gamepadControls) Game1.showTextEntry(searchBox);
                break;
        }
    }
}
