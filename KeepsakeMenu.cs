using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace DearlyKept;

/// <summary>Browse this farmer's saved gift memories without changing any inventory items.</summary>
internal sealed partial class KeepsakeMenu : IClickableMenu
{
    private const int RowHeight = 92;
    private static readonly Color MutedText = new(114, 91, 74);
    private static readonly Color Highlight = new(255, 237, 190);
    private readonly ITranslationHelper i18n;
    private readonly GiftJournal journal;
    private readonly Func<GiftEntry, string> formatNote;
    private readonly Func<string, string> senderName;
    private readonly List<KeepsakeEntry> allEntries = new();
    private readonly List<KeepsakeEntry> entries = new();
    private readonly List<string> originChoices = new();
    private readonly List<string> senderChoices = new();
    private readonly List<string> messagePages = new();
    private Rectangle listBounds;
    private Rectangle detailBounds;
    private Rectangle readBounds;
    private Rectangle originBounds;
    private Rectangle senderBounds;
    private Rectangle previousBounds;
    private Rectangle nextBounds;
    private Rectangle messageBounds;
    private Rectangle messagePreviousBounds;
    private Rectangle messageNextBounds;
    private Rectangle backBounds;
    private int selectedIndex = -1;
    private int firstVisibleIndex;
    private int visibleRows;
    private int rowHeight = RowHeight;
    private bool compactLayout;
    private FilterFocus filterFocus;
    private string originFilter = "";
    private string senderFilter = "";
    private KeepsakeEntry? messageEntry;
    private int messagePage;
    private string hoverText = "";
    private int displayedRevision = int.MinValue;

    public KeepsakeMenu(ITranslationHelper i18n, GiftJournal journal, Func<GiftEntry, string> formatNote, Func<string, string> senderName, Action? openConfigMenu = null)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        this.i18n = i18n;
        this.journal = journal;
        this.formatNote = formatNote;
        this.senderName = senderName;
        this.openConfigMenu = openConfigMenu;
        searchBox = new TextBox(Game1.content.Load<Texture2D>("LooseSprites\\textBox"), null, Game1.smallFont, Game1.textColor) { Text = "", textLimit = 160 };
        exitFunction += () => { searchBox.Selected = false; };
        Layout();
        RefreshEntries();
    }

    private void Layout()
    {
        compactLayout = Game1.uiViewport.Height < 600 || Game1.uiViewport.Width < 900;
        int margin = compactLayout ? 48 : 80;
        width = Math.Min(1040, Game1.uiViewport.Width - margin);
        height = Math.Min(704, Game1.uiViewport.Height - margin);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;

        int columnWidth = (width - 88) * 45 / 100;
        rowHeight = RowHeight;
        int contentTop = yPositionOnScreen + (compactLayout ? 216 : 234);
        int actionTop = yPositionOnScreen + height - (compactLayout ? 88 : 116);
        visibleRows = Math.Max(1, (actionTop - contentTop) / rowHeight);
        listBounds = new Rectangle(xPositionOnScreen + 32, contentTop, columnWidth, visibleRows * rowHeight);
        detailBounds = new Rectangle(listBounds.Right + 24, contentTop, width - columnWidth - 88, actionTop - contentTop - 16);
        readBounds = new Rectangle(detailBounds.X, actionTop, detailBounds.Width, 48);
        previousBounds = new Rectangle(listBounds.X, actionTop, 48, 48);
        nextBounds = new Rectangle(listBounds.Right - 48, previousBounds.Y, 48, previousBounds.Height);
        int filterWidth = (width - 100) / 4;
        originBounds = new Rectangle(listBounds.X, yPositionOnScreen + (compactLayout ? 106 : 120), filterWidth, 44);
        senderBounds = new Rectangle(originBounds.Right + 12, originBounds.Y, filterWidth, 44);
        yearBounds = new Rectangle(senderBounds.Right + 12, originBounds.Y, filterWidth, 44);
        seasonBounds = new Rectangle(yearBounds.Right + 12, originBounds.Y, filterWidth, 44);
        searchBox.X = listBounds.X;
        searchBox.Y = originBounds.Bottom + 10;
        searchBox.Width = width - (openConfigMenu is null ? 64 : 286);
        settingsBounds = openConfigMenu is null ? Rectangle.Empty : new Rectangle(searchBox.X + searchBox.Width + 14, searchBox.Y, 208, 44);
        messageBounds = new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + (compactLayout ? 146 : 164), width - 64,
            actionTop - yPositionOnScreen - (compactLayout ? 162 : 180));
        messagePreviousBounds = new Rectangle(messageBounds.X, actionTop, 48, 48);
        messageNextBounds = new Rectangle(messageBounds.Right - 48, actionTop, 48, 48);
        backBounds = new Rectangle(xPositionOnScreen + (width - 160) / 2, actionTop, 160, 48);

        if (upperRightCloseButton != null)
            upperRightCloseButton.bounds = new Rectangle(xPositionOnScreen + width - 24, yPositionOnScreen - 8, 48, 48);
        EnsureSelectionVisible();
        RebuildMessagePages();
    }

    private void RefreshEntries()
    {
        if (displayedRevision == journal.Revision)
            return;

        string? selectedId = SelectedEntry?.Entry.Id;
        Dictionary<string, KeepsakeEntry> previousEntries = allEntries.ToDictionary(entry => entry.Entry.Id);
        allEntries.Clear();
        foreach (GiftEntry entry in journal.Entries.Reverse())
        {
            allEntries.Add(previousEntries.TryGetValue(entry.Id, out KeepsakeEntry? previous) && previous.Entry == entry
                ? previous
                : CreatePreview(entry));
        }
        displayedRevision = journal.Revision;

        if (originFilter.Length > 0 && !allEntries.Any(entry => entry.Entry.Origin == originFilter))
            originFilter = "";
        if (senderFilter.Length > 0 && !allEntries.Any(entry => entry.Entry.SenderId == senderFilter
            && (originFilter.Length == 0 || entry.Entry.Origin == originFilter)))
            senderFilter = "";
        ApplyFilters(selectedId);

        if (messageEntry != null)
        {
            KeepsakeEntry? current = allEntries.Find(entry => entry.Entry.Id == messageEntry.Entry.Id);
            if (current != messageEntry)
            {
                messageEntry = current;
                RebuildMessagePages();
            }
        }
    }

    private void ApplyFilters(string? selectedId)
    {
        int previousIndex = selectedIndex;
        entries.Clear();
        entries.AddRange(allEntries.Where(entry => (originFilter.Length == 0 || entry.Entry.Origin == originFilter)
            && (senderFilter.Length == 0 || entry.Entry.SenderId == senderFilter)
            && (yearFilter == 0 || entry.Entry.Year == yearFilter)
            && (seasonFilter.Length == 0 || entry.Entry.Season == seasonFilter)
            && MatchesSearch(entry)));
        selectedIndex = selectedId == null ? -1 : entries.FindIndex(entry => entry.Entry.Id == selectedId);
        if (selectedIndex < 0 && entries.Count > 0)
            selectedIndex = Math.Clamp(previousIndex, 0, entries.Count - 1);
        EnsureSelectionVisible();

        originChoices.Clear();
        originChoices.Add("");
        originChoices.AddRange(new[] { "mail", "birthday", "spouse", "anniversary", "other" }.Where(origin =>
            allEntries.Any(entry => entry.Entry.Origin == origin && (senderFilter.Length == 0 || entry.Entry.SenderId == senderFilter))));
        senderChoices.Clear();
        senderChoices.Add("");
        senderChoices.AddRange(allEntries.Where(entry => originFilter.Length == 0 || entry.Entry.Origin == originFilter)
            .Select(entry => entry.Entry.SenderId).Distinct(StringComparer.Ordinal)
            .OrderBy(senderName, StringComparer.CurrentCulture).ThenBy(id => id, StringComparer.Ordinal));
        hoverText = "";
    }

    private void CycleFilter(FilterFocus focus, int direction)
    {
        filterFocus = focus;
        List<string> choices = focus == FilterFocus.Occasion ? originChoices : senderChoices;
        if (choices.Count < 2)
            return;
        string current = focus == FilterFocus.Occasion ? originFilter : senderFilter;
        string next = choices[(choices.IndexOf(current) + direction + choices.Count) % choices.Count];
        string? selectedId = SelectedEntry?.Entry.Id;
        if (focus == FilterFocus.Occasion)
            originFilter = next;
        else
            senderFilter = next;
        ApplyFilters(selectedId);
        Game1.playSound("smallSelect");
    }

    private string OccasionName(string origin) => i18n.Get("menu.occasion." + origin).ToString();

    private string FilterLabel(FilterFocus focus)
    {
        string value = focus == FilterFocus.Occasion ? originFilter : senderFilter;
        string label = value.Length == 0 ? i18n.Get("menu.filter.all").ToString()
            : focus == FilterFocus.Occasion ? OccasionName(value) : SenderChoiceLabel(value);
        return i18n.Get(focus == FilterFocus.Occasion ? "menu.filter.occasion" : "menu.filter.sender", new { value = label }).ToString();
    }

    private string OccasionAndSource(GiftEntry entry)
    {
        string occasion = OccasionName(entry.Origin);
        return SourceName(entry) is { } source
            ? i18n.Get("menu.occasion-source", new { occasion, source }).ToString()
            : occasion;
    }

    private static KeepsakeEntry CreatePreview(GiftEntry entry)
    {
        try
        {
            // This is a separate display item. It is never added to a player's inventory.
            return new KeepsakeEntry(entry, ItemRegistry.GetData(entry.QualifiedItemId)?.DisplayName ?? entry.ItemName);
        }
        catch (Exception)
        {
            // A saved memory remains readable after the mod that supplied its item is removed.
            return new KeepsakeEntry(entry, entry.ItemName);
        }
    }

    private KeepsakeEntry? SelectedEntry => selectedIndex >= 0 && selectedIndex < entries.Count ? entries[selectedIndex] : null;

    private void EnsureSelectionVisible()
    {
        if (selectedIndex < firstVisibleIndex)
            firstVisibleIndex = Math.Max(0, selectedIndex);
        if (selectedIndex >= firstVisibleIndex + visibleRows)
            firstVisibleIndex = selectedIndex - visibleRows + 1;
        firstVisibleIndex = Math.Clamp(firstVisibleIndex, 0, Math.Max(0, entries.Count - visibleRows));
    }

    private Rectangle RowBounds(int visibleIndex) => new(listBounds.X, listBounds.Y + visibleIndex * rowHeight, listBounds.Width, rowHeight - 6);

    private string DateText(GiftEntry entry) => i18n.Get("menu.date", new
    {
        day = entry.Day,
        season = i18n.Get($"season.{entry.Season}").ToString(),
        year = entry.Year
    }).ToString();

    private string ItemText(KeepsakeEntry entry) => i18n.Get("menu.item", new
    {
        quantity = entry.Entry.Quantity,
        item = entry.DisplayName
    }).ToString();

    private void Select(int index)
    {
        filterFocus = FilterFocus.None;
        if (entries.Count == 0)
            return;
        int nextIndex = Math.Clamp(index, 0, entries.Count - 1);
        if (nextIndex != selectedIndex)
        {
            selectedIndex = nextIndex;
            Game1.playSound("smallSelect");
        }
        EnsureSelectionVisible();
    }

    private void OpenMessage()
    {
        RefreshEntries();
        if (SelectedEntry is not { } selected)
            return;
        messageEntry = selected;
        messagePage = 0;
        RebuildMessagePages();
        hoverText = "";
        Game1.playSound("smallSelect");
    }

    private void CloseMessage()
    {
        messageEntry = null;
        messagePages.Clear();
        messagePage = 0;
        hoverText = "";
        filterFocus = FilterFocus.None;
    }

    private void ChangeMessagePage(int direction)
    {
        int next = Math.Clamp(messagePage + direction, 0, Math.Max(0, messagePages.Count - 1));
        if (next != messagePage)
        {
            messagePage = next;
            Game1.playSound("smallSelect");
        }
    }

    private void RebuildMessagePages()
    {
        messagePages.Clear();
        if (messageEntry is null)
            return;

        string? storedText = messageEntry.Entry.MessageText;
        string text = string.IsNullOrWhiteSpace(storedText)
            ? i18n.Get("menu.message-missing").ToString()
            : storedText;
        if (messageEntry.Entry.MessageIncomplete)
            text = i18n.Get("menu.message-incomplete") + (string.IsNullOrWhiteSpace(storedText) ? "" : "\n\n" + storedText);
        int textWidth = Math.Max(1, messageBounds.Width - 40);
        int linesPerPage = Math.Max(1, (messageBounds.Height - 32) / Game1.smallFont.LineSpacing);
        List<string> lines = new();
        foreach (string wrapped in Game1.parseText(text, Game1.smallFont, textWidth).Replace("\r", "").Split('\n'))
        {
            string remaining = wrapped;
            // Long words must remain readable too, without shrinking or losing stored text.
            while (remaining.Length > 1 && Game1.smallFont.MeasureString(remaining).X > textWidth)
            {
                int lower = 1;
                int upper = remaining.Length;
                while (lower < upper)
                {
                    int middle = (lower + upper + 1) / 2;
                    if (Game1.smallFont.MeasureString(remaining[..middle]).X <= textWidth)
                        lower = middle;
                    else
                        upper = middle - 1;
                }
                if (lower > 1 && char.IsHighSurrogate(remaining[lower - 1]))
                    lower--;
                lines.Add(remaining[..lower]);
                remaining = remaining[lower..];
            }
            lines.Add(remaining);
        }
        for (int first = 0; first < lines.Count; first += linesPerPage)
            messagePages.Add(string.Join("\n", lines.Skip(first).Take(linesPerPage)));
        messagePage = Math.Clamp(messagePage, 0, Math.Max(0, messagePages.Count - 1));
    }

    public override void update(GameTime time)
    {
        base.update(time);
        UpdateControls(time);
        RefreshEntries();
    }

    public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
    {
        base.gameWindowSizeChanged(oldBounds, newBounds);
        Layout();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        RefreshEntries();
        if (HandleControlClick(x, y)) return;
        if (upperRightCloseButton?.containsPoint(x, y) == true)
        {
            exitThisMenu();
            return;
        }
        if (messageEntry != null)
        {
            if (backBounds.Contains(x, y))
                CloseMessage();
            else if (messagePreviousBounds.Contains(x, y))
                ChangeMessagePage(-1);
            else if (messageNextBounds.Contains(x, y))
                ChangeMessagePage(1);
            return;
        }
        if (originBounds.Contains(x, y))
        {
            OpenPicker(FilterFocus.Occasion);
            return;
        }
        if (senderBounds.Contains(x, y))
        {
            OpenPicker(FilterFocus.Sender);
            return;
        }
        for (int row = 0; row < visibleRows && firstVisibleIndex + row < entries.Count; row++)
        {
            if (RowBounds(row).Contains(x, y))
            {
                Select(firstVisibleIndex + row);
                return;
            }
        }
        if (readBounds.Contains(x, y))
            OpenMessage();
        else if (entries.Count > visibleRows && firstVisibleIndex > 0 && previousBounds.Contains(x, y))
            Select(selectedIndex - visibleRows);
        else if (entries.Count > visibleRows && firstVisibleIndex + visibleRows < entries.Count && nextBounds.Contains(x, y))
            Select(selectedIndex + visibleRows);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        if (picker != null) { picker = null; return; }
        if (messageEntry != null)
            CloseMessage();
        else if (originBounds.Contains(x, y))
            CycleFilter(FilterFocus.Occasion, -1);
        else if (senderBounds.Contains(x, y))
            CycleFilter(FilterFocus.Sender, -1);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (picker != null) { MovePicker(direction > 0 ? -1 : 1); return; }
        if (messageEntry != null)
            ChangeMessagePage(direction > 0 ? -1 : 1);
        else
            Select(selectedIndex + (direction > 0 ? -1 : 1));
    }

    public override void receiveKeyPress(Keys key)
    {
        if (key == Keys.None)
            return;

        // Stardew forwards mapped controller buttons here after receiveGamePadButton.
        // Using the same path also preserves configured keys and native direction repeats.
        bool cancel = key == Keys.Escape || Game1.options.menuButton.Any(button => button.key == key) || Game1.options.journalButton.Any(button => button.key == key);
        bool activate = key is Keys.Enter or Keys.Space || Game1.options.actionButton.Any(button => button.key == key);
        bool left = key == Keys.Left || Game1.options.moveLeftButton.Any(button => button.key == key);
        bool right = key == Keys.Right || Game1.options.moveRightButton.Any(button => button.key == key);
        bool up = key == Keys.Up || Game1.options.moveUpButton.Any(button => button.key == key);
        bool down = key == Keys.Down || Game1.options.moveDownButton.Any(button => button.key == key);
        bool previousChoice = Game1.options.useToolButton.Any(button => button.key == key);
        if (HandleControlKey(key, cancel, activate, up, down)) return;

        if (messageEntry != null)
        {
            if (cancel)
                CloseMessage();
            else if (left || up || key == Keys.PageUp)
                ChangeMessagePage(-1);
            else if (right || down || activate || key == Keys.PageDown)
                ChangeMessagePage(1);
            else if (key == Keys.Home)
                ChangeMessagePage(-messagePages.Count);
            else if (key == Keys.End)
                ChangeMessagePage(messagePages.Count);
            return;
        }

        if (cancel)
        {
            exitThisMenu();
            return;
        }
        if (up)
        {
            Select(selectedIndex - 1);
            return;
        }
        if (down)
        {
            Select(selectedIndex + 1);
            return;
        }
        if (left || right)
        {
            int nextFocus = filterFocus == FilterFocus.None ? (right ? 2 : 1) : (int)filterFocus + (right ? 1 : -1);
            int lastFocus = openConfigMenu is null ? 5 : 6;
            filterFocus = (FilterFocus)(nextFocus < 1 ? lastFocus : nextFocus > lastFocus ? 1 : nextFocus);
            return;
        }
        if (previousChoice && filterFocus is FilterFocus.Occasion or FilterFocus.Sender)
        {
            CycleFilter(filterFocus, -1);
            return;
        }
        if (activate)
        {
            if (filterFocus == FilterFocus.None)
                OpenMessage();
            else
                ActivateFocusedControl();
            return;
        }

        switch (key)
        {
            case Keys.PageUp:
                Select(selectedIndex - visibleRows);
                break;
            case Keys.PageDown:
                Select(selectedIndex + visibleRows);
                break;
            case Keys.Home:
                Select(0);
                break;
            case Keys.End:
                Select(entries.Count - 1);
                break;
            case Keys.Tab:
                filterFocus = (FilterFocus)(((int)filterFocus + 1) % 7);
                break;
            default:
                base.receiveKeyPress(key);
                break;
        }
    }

    public override void receiveGamePadButton(Buttons button)
    {
        // Only shoulder paging is unmapped. All other controls use receiveKeyPress once.
        switch (button)
        {
            case Buttons.LeftShoulder:
                if (messageEntry != null)
                    ChangeMessagePage(-1);
                else
                    Select(selectedIndex - visibleRows);
                break;
            case Buttons.RightShoulder:
                if (messageEntry != null)
                    ChangeMessagePage(1);
                else
                    Select(selectedIndex + visibleRows);
                break;
        }
    }

    public override bool areGamePadControlsImplemented() => true;

    public override void performHoverAction(int x, int y)
    {
        hoverText = "";
        upperRightCloseButton?.tryHover(x, y);
        if (picker != null) return;
        if (messageEntry != null)
            return;
        if (originBounds.Contains(x, y) || senderBounds.Contains(x, y))
        {
            FilterFocus focus = originBounds.Contains(x, y) ? FilterFocus.Occasion : FilterFocus.Sender;
            hoverText = FilterLabel(focus) + "\n" + i18n.Get("menu.filter-hint");
            if (focus == FilterFocus.Sender && senderFilter.Length > 0)
                hoverText += "\n" + senderFilter;
            return;
        }
        for (int row = 0; row < visibleRows && firstVisibleIndex + row < entries.Count; row++)
        {
            if (!RowBounds(row).Contains(x, y))
                continue;
            KeepsakeEntry entry = entries[firstVisibleIndex + row];
            hoverText = ItemText(entry) + "\n" + formatNote(entry.Entry);
            return;
        }
        if (readBounds.Contains(x, y) && SelectedEntry != null)
            hoverText = i18n.Get("menu.read-hint").ToString();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.45f);
        DrawBox(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));
        if (messageEntry != null)
            DrawMessage(b);
        else
            DrawArchive(b);
        if (picker != null) DrawPicker(b);
        upperRightCloseButton?.draw(b);
        if (hoverText.Length > 0 && messageEntry is null)
            drawHoverText(b, hoverText, Game1.smallFont);
        drawMouse(b);
    }

    private void DrawArchive(SpriteBatch b)
    {
        b.DrawString(Game1.dialogueFont, i18n.Get("menu.title").ToString(), new Vector2(xPositionOnScreen + 36, yPositionOnScreen + (compactLayout ? 18 : 28)), Game1.textColor);
        string collection = i18n.Get(originFilter.Length > 0 || senderFilter.Length > 0 || yearFilter != 0 || seasonFilter.Length > 0 || searchTerm.Length > 0 ? "menu.collection-filtered" : "menu.collection",
            new { count = entries.Count, total = allEntries.Count }).ToString();
        if (journal.StatusKey is { } status) collection = i18n.Get(status).ToString();
        b.DrawString(Game1.smallFont, FitText(collection, Game1.smallFont, width - 76), new Vector2(xPositionOnScreen + 38, yPositionOnScreen + (compactLayout ? 62 : 75)), MutedText);
        DrawRule(b, new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + (compactLayout ? 94 : 108), width - 64, 2));
        DrawButton(b, originBounds, FilterLabel(FilterFocus.Occasion), filterFocus == FilterFocus.Occasion, originChoices.Count > 1);
        DrawButton(b, senderBounds, FilterLabel(FilterFocus.Sender), filterFocus == FilterFocus.Sender, senderChoices.Count > 1);
        DrawControls(b);

        if (entries.Count == 0)
            DrawEmptyState(b);
        else
        {
            DrawRows(b);
            DrawDetails(b);
            DrawButton(b, readBounds, i18n.Get("menu.read").ToString(), filterFocus == FilterFocus.None);
            if (entries.Count > visibleRows)
            {
                DrawArrowButton(b, previousBounds, false, firstVisibleIndex > 0);
                DrawArrowButton(b, nextBounds, true, firstVisibleIndex + visibleRows < entries.Count);
                string range = i18n.Get("menu.range", new { first = firstVisibleIndex + 1, last = Math.Min(entries.Count, firstVisibleIndex + visibleRows), total = entries.Count }).ToString();
                DrawCentered(b, range, Game1.smallFont, new Rectangle(previousBounds.Right + 4, previousBounds.Y, nextBounds.Left - previousBounds.Right - 8, previousBounds.Height), MutedText);
            }
        }

        DrawFooter(b, filterFocus != FilterFocus.None ? "menu.help-filter" : compactLayout ? "menu.help-compact" : "menu.help");
    }

    private void DrawRows(SpriteBatch b)
    {
        for (int row = 0; row < visibleRows && firstVisibleIndex + row < entries.Count; row++)
        {
            int index = firstVisibleIndex + row;
            KeepsakeEntry entry = entries[index];
            Rectangle bounds = RowBounds(row);
            bool selected = index == selectedIndex;
            DrawBox(b, bounds, selected ? Highlight : Color.White);
            if (selected && filterFocus == FilterFocus.None)
                b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 9, bounds.Y + 12, 6, bounds.Height - 24), new Color(62, 107, 69));

            DrawPreview(b, entry, new Vector2(bounds.X + 16, bounds.Y + 5), compactLayout ? 0.75f : 0.9f);
            float textX = bounds.X + (compactLayout ? 70 : 82);
            float availableWidth = bounds.Right - textX - 16;
            b.DrawString(Game1.smallFont, FitText(ItemText(entry), Game1.smallFont, availableWidth), new Vector2(textX, bounds.Y + (compactLayout ? 9 : 13)), Game1.textColor);
            b.DrawString(Game1.smallFont, FitText(SenderLabel(entry.Entry), Game1.smallFont, availableWidth), new Vector2(textX, bounds.Y + 42), MutedText);
        }
    }

    private void DrawDetails(SpriteBatch b)
    {
        if (SelectedEntry is not { } entry)
            return;
        DrawBox(b, detailBounds);
        if (compactLayout)
        {
            // The selected row already contains the item and sender. Reserve the
            // small detail panel for the date and producer instead of repeating it.
            string[] metadata = { DateText(entry.Entry), OccasionName(entry.Entry.Origin), SourceName(entry.Entry) ?? "" };
            for (int line = 0; line < metadata.Length; line++)
            {
                int y = detailBounds.Y + 10 + line * 30;
                if (y + Game1.smallFont.LineSpacing > detailBounds.Bottom - 4) break;
                b.DrawString(Game1.smallFont, FitText(metadata[line], Game1.smallFont, detailBounds.Width - 40),
                    new Vector2(detailBounds.X + 20, y), MutedText);
            }
            return;
        }
        DrawPreview(b, entry, new Vector2(detailBounds.X + 20, detailBounds.Y + (compactLayout ? 12 : 18)), compactLayout ? 0.75f : 1f);
        int textX = detailBounds.X + (compactLayout ? 82 : 100);
        int textWidth = detailBounds.Right - textX - 20;
        b.DrawString(Game1.smallFont, FitText(ItemText(entry), Game1.smallFont, textWidth), new Vector2(textX, detailBounds.Y + (compactLayout ? 12 : 25)), Game1.textColor);
        string from = i18n.Get("menu.from", new { sender = SenderLabel(entry.Entry) }).ToString();
        b.DrawString(Game1.smallFont, FitText(from, Game1.smallFont, textWidth), new Vector2(textX, detailBounds.Y + (compactLayout ? 43 : 56)), MutedText);
        DrawRule(b, new Rectangle(detailBounds.X + 20, detailBounds.Y + (compactLayout ? 81 : 101), detailBounds.Width - 40, 2));

        int noteX = detailBounds.X + 24;
        int noteY = detailBounds.Y + (compactLayout ? 91 : 123);
        if (!compactLayout && Game1.getCharacterFromName(entry.Entry.SenderId, false) is { Portrait: { } portrait })
        {
            b.Draw(portrait, new Rectangle(noteX, noteY, 96, 96), new Rectangle(0, 0, 64, 64), Color.White);
            noteX += 116;
        }

        int noteWidth = detailBounds.Right - noteX - 24;
        if (noteY + 32 > detailBounds.Bottom - 12) return;
        string date = FitText(DateText(entry.Entry), Game1.smallFont, noteWidth);
        b.DrawString(Game1.smallFont, date, new Vector2(noteX, noteY), MutedText);
        string occasion = compactLayout ? OccasionAndSource(entry.Entry) : OccasionName(entry.Entry.Origin);
        b.DrawString(Game1.smallFont, FitText(occasion, Game1.smallFont, noteWidth), new Vector2(noteX, noteY + 32), MutedText);
        if (!compactLayout && SourceName(entry.Entry) is { } providerName && noteY + 92 < detailBounds.Bottom)
        {
            string provider = i18n.Get("note.provider", new { name = providerName }).ToString();
            b.DrawString(Game1.smallFont, FitText(provider, Game1.smallFont, noteWidth), new Vector2(noteX, noteY + 64), MutedText);
        }
    }

    private static void DrawPreview(SpriteBatch b, KeepsakeEntry entry, Vector2 position, float scale)
    {
        if (entry.Preview != null)
            entry.Preview.drawInMenu(b, position, scale);
        else
            DrawCentered(b, "?", Game1.dialogueFont, new Rectangle((int)position.X, (int)position.Y, (int)(64 * scale), (int)(64 * scale)), MutedText);
    }

    private void DrawEmptyState(SpriteBatch b)
    {
        Rectangle titleBounds = new(xPositionOnScreen + 48, listBounds.Y + (compactLayout ? 6 : 46), width - 96, 50);
        DrawCentered(b, i18n.Get(allEntries.Count > 0 ? "menu.no-results" : "menu.empty-title").ToString(), Game1.dialogueFont, titleBounds, Game1.textColor);
        int textWidth = Math.Min(620, width - 128);
        string text = Game1.parseText(i18n.Get(allEntries.Count > 0 ? "menu.no-results-help" : "menu.empty-body").ToString(), Game1.smallFont, textWidth);
        Vector2 size = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2(xPositionOnScreen + (width - size.X) / 2, titleBounds.Bottom + 8), MutedText);
    }

    private void DrawMessage(SpriteBatch b)
    {
        if (messageEntry is not { } entry)
            return;
        string title = i18n.Get("menu.message-title", new { sender = SenderLabel(entry.Entry) }).ToString();
        b.DrawString(Game1.dialogueFont, FitText(title, Game1.dialogueFont, width - 76),
            new Vector2(xPositionOnScreen + 36, yPositionOnScreen + (compactLayout ? 18 : 28)), Game1.textColor);
        b.DrawString(Game1.smallFont, FitText(ItemText(entry), Game1.smallFont, width - 76),
            new Vector2(xPositionOnScreen + 38, yPositionOnScreen + (compactLayout ? 62 : 75)), MutedText);
        string context = DateText(entry.Entry) + " | " + OccasionAndSource(entry.Entry);
        b.DrawString(Game1.smallFont, FitText(context, Game1.smallFont, width - 76),
            new Vector2(xPositionOnScreen + 38, yPositionOnScreen + (compactLayout ? 98 : 112)), MutedText);
        DrawBox(b, messageBounds);
        if (messagePages.Count > 0)
            b.DrawString(Game1.smallFont, messagePages[messagePage], new Vector2(messageBounds.X + 20, messageBounds.Y + 16), Game1.textColor);
        DrawArrowButton(b, messagePreviousBounds, false, messagePage > 0);
        DrawArrowButton(b, messageNextBounds, true, messagePage + 1 < messagePages.Count);
        DrawButton(b, backBounds, i18n.Get("menu.back").ToString(), false);
        string page = i18n.Get("menu.message-page", new { current = messagePage + 1, total = messagePages.Count }).ToString();
        DrawCentered(b, page, Game1.smallFont,
            new Rectangle(messagePreviousBounds.Right + 12, backBounds.Y, backBounds.Left - messagePreviousBounds.Right - 24, backBounds.Height), MutedText);
        DrawFooter(b, "menu.help-message");
    }

    private void DrawFooter(SpriteBatch b, string key) => b.DrawString(Game1.smallFont,
        FitText(i18n.Get(key).ToString(), Game1.smallFont, width - 72),
        new Vector2(xPositionOnScreen + 36, yPositionOnScreen + height - (compactLayout ? 37 : 49)), MutedText);

    private static void DrawBox(SpriteBatch b, Rectangle bounds, Color? tint = null) => drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, tint ?? Color.White);

    private static void DrawRule(SpriteBatch b, Rectangle bounds) => b.Draw(Game1.staminaRect, bounds, new Color(155, 117, 80) * 0.4f);

    private static void DrawButton(SpriteBatch b, Rectangle bounds, string text, bool focused, bool enabled = true)
    {
        bool hovered = bounds.Contains(Game1.getMouseX(), Game1.getMouseY());
        DrawBox(b, bounds, enabled && (focused || hovered) ? Highlight : Color.White);
        DrawCentered(b, text, Game1.smallFont, bounds, enabled ? Game1.textColor : MutedText * 0.55f);
    }

    private static void DrawArrowButton(SpriteBatch b, Rectangle bounds, bool right, bool enabled)
    {
        bool hovered = bounds.Contains(Game1.getMouseX(), Game1.getMouseY());
        DrawBox(b, bounds, enabled && hovered ? Highlight : Color.White);
        Color color = enabled ? Game1.textColor : MutedText * 0.55f;
        // Draw pixel geometry: Stardew's fonts map '<' to a heart glyph.
        for (int column = 0; column < 7; column++)
        {
            int halfHeight = right ? 6 - column : column;
            b.Draw(Game1.staminaRect, new Rectangle(bounds.Center.X - 7 + column * 2,
                bounds.Center.Y - halfHeight * 2 - 1, 2, halfHeight * 4 + 2), color);
        }
    }
    private static void DrawCentered(SpriteBatch b, string text, SpriteFont font, Rectangle bounds, Color color)
    {
        text = FitText(text, font, bounds.Width - 20);
        Vector2 size = font.MeasureString(text);
        b.DrawString(font, text, new Vector2(bounds.X + (bounds.Width - size.X) / 2, bounds.Y + (bounds.Height - size.Y) / 2 + 3), color);
    }

    private static string FitText(string text, SpriteFont font, float width)
    {
        if (font.MeasureString(text).X <= width)
            return text;
        const string ellipsis = "...";
        int length = text.Length;
        while (length > 0 && font.MeasureString(text[..length] + ellipsis).X > width)
            length--;
        return text[..length].TrimEnd() + ellipsis;
    }

    private sealed class KeepsakeEntry
    {
        public GiftEntry Entry { get; }
        public string DisplayName { get; }
        private Item? preview;
        private bool attempted;
        public Item? Preview
        {
            get
            {
                if (!attempted)
                {
                    attempted = true;
                    try { preview = ItemRegistry.Create(Entry.QualifiedItemId, 1, Entry.Quality, allowNull: true); }
                    catch { preview = null; }
                }
                return preview;
            }
        }
        public KeepsakeEntry(GiftEntry entry, string displayName) { Entry = entry; DisplayName = displayName; }
    }

    private enum FilterFocus { None, Occasion, Sender, Year, Season, Search, Settings }
}
