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
internal sealed class KeepsakeMenu : IClickableMenu
{
    private const int RowHeight = 82;
    private static readonly Color MutedText = new(114, 91, 74);
    private static readonly Color Highlight = new(255, 237, 190);
    private readonly ITranslationHelper i18n;
    private readonly GiftJournal journal;
    private readonly Func<GiftEntry, string> formatNote;
    private readonly Func<string, string> senderName;
    private readonly List<KeepsakeEntry> entries = new();
    private Rectangle listBounds;
    private Rectangle detailBounds;
    private Rectangle removeBounds;
    private Rectangle previousBounds;
    private Rectangle nextBounds;
    private Rectangle confirmationBounds;
    private Rectangle cancelBounds;
    private Rectangle confirmBounds;
    private int selectedIndex = -1;
    private int firstVisibleIndex;
    private int visibleRows;
    private int rowHeight = RowHeight;
    private bool compactLayout;
    private bool removeFocused;
    private bool confirmFocused;
    private string? pendingRemoval;
    private string hoverText = "";
    private int displayedRevision = int.MinValue;
    private double statusMilliseconds;

    public KeepsakeMenu(ITranslationHelper i18n, GiftJournal journal, Func<GiftEntry, string> formatNote, Func<string, string> senderName)
        : base(0, 0, 0, 0, showUpperRightCloseButton: true)
    {
        this.i18n = i18n;
        this.journal = journal;
        this.formatNote = formatNote;
        this.senderName = senderName;
        Layout();
        RefreshEntries();
    }

    private void Layout()
    {
        compactLayout = Game1.uiViewport.Height < 600;
        int margin = compactLayout ? 48 : 80;
        width = Math.Min(1040, Game1.uiViewport.Width - margin);
        height = Math.Min(704, Game1.uiViewport.Height - margin);
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;

        int columnWidth = (width - 88) * 45 / 100;
        rowHeight = RowHeight;
        visibleRows = Math.Max(1, (height - (compactLayout ? 224 : 276)) / rowHeight);
        listBounds = new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + (compactLayout ? 128 : 150), columnWidth, visibleRows * rowHeight);
        detailBounds = new Rectangle(listBounds.Right + 24, yPositionOnScreen + (compactLayout ? 112 : 120), width - columnWidth - 88, height - (compactLayout ? 186 : 204));
        removeBounds = new Rectangle(detailBounds.X + 20, detailBounds.Bottom - (compactLayout ? 52 : 68), detailBounds.Width - 40, compactLayout ? 38 : 48);
        previousBounds = new Rectangle(listBounds.X, yPositionOnScreen + height - (compactLayout ? 88 : 116), 48, compactLayout ? 36 : 40);
        nextBounds = new Rectangle(listBounds.Right - 48, previousBounds.Y, 48, previousBounds.Height);

        int confirmationWidth = Math.Min(600, width - 64);
        confirmationBounds = new Rectangle(xPositionOnScreen + (width - confirmationWidth) / 2, yPositionOnScreen + (height - 282) / 2, confirmationWidth, 282);
        int buttonWidth = (confirmationWidth - 72) / 2;
        cancelBounds = new Rectangle(confirmationBounds.X + 24, confirmationBounds.Bottom - 76, buttonWidth, 48);
        confirmBounds = new Rectangle(cancelBounds.Right + 24, cancelBounds.Y, buttonWidth, 48);

        if (upperRightCloseButton != null)
            upperRightCloseButton.bounds = new Rectangle(xPositionOnScreen + width - 24, yPositionOnScreen - 8, 48, 48);
        EnsureSelectionVisible();
    }

    private void RefreshEntries()
    {
        if (displayedRevision == journal.Revision)
            return;

        string? selectedId = SelectedEntry?.Entry.Id;
        int previousIndex = selectedIndex;
        Dictionary<string, KeepsakeEntry> previousEntries = entries.ToDictionary(entry => entry.Entry.Id);
        entries.Clear();
        foreach (GiftEntry entry in journal.Entries.Reverse())
        {
            entries.Add(previousEntries.TryGetValue(entry.Id, out KeepsakeEntry? previous) && previous.Entry == entry
                ? previous
                : CreatePreview(entry));
        }
        displayedRevision = journal.Revision;

        selectedIndex = selectedId == null ? -1 : entries.FindIndex(entry => entry.Entry.Id == selectedId);
        if (selectedIndex < 0 && entries.Count > 0)
            selectedIndex = Math.Clamp(previousIndex, 0, entries.Count - 1);

        if (pendingRemoval != null && SelectedEntry?.Entry.Id != pendingRemoval)
            CancelRemoval();
        if (entries.Count == 0)
            removeFocused = false;
        EnsureSelectionVisible();
    }

    private static KeepsakeEntry CreatePreview(GiftEntry entry)
    {
        try
        {
            // This is a separate display item. It is never added to a player's inventory.
            Item? preview = ItemRegistry.Create(entry.QualifiedItemId, 1, entry.Quality, allowNull: true);
            return new KeepsakeEntry(entry, preview, preview?.DisplayName ?? entry.ItemName);
        }
        catch (Exception)
        {
            // A saved memory remains readable after the mod that supplied its item is removed.
            return new KeepsakeEntry(entry, null, entry.ItemName);
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
        if (entries.Count == 0)
            return;
        int nextIndex = Math.Clamp(index, 0, entries.Count - 1);
        if (nextIndex != selectedIndex)
        {
            selectedIndex = nextIndex;
            Game1.playSound("smallSelect");
        }
        removeFocused = false;
        EnsureSelectionVisible();
    }

    private void BeginRemoval()
    {
        RefreshEntries();
        if (SelectedEntry is not { } selected)
            return;
        pendingRemoval = selected.Entry.Id;
        confirmFocused = false;
        hoverText = "";
        Game1.playSound("smallSelect");
    }

    private void CancelRemoval()
    {
        pendingRemoval = null;
        confirmFocused = false;
    }

    private void ConfirmRemoval()
    {
        string? id = pendingRemoval;
        CancelRemoval();

        if (id != null && journal.Remove(id))
        {
            statusMilliseconds = 4000;
            Game1.playSound("smallSelect");
        }
        RefreshEntries();
    }

    public override void update(GameTime time)
    {
        base.update(time);
        statusMilliseconds = Math.Max(0, statusMilliseconds - time.ElapsedGameTime.TotalMilliseconds);
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
        if (pendingRemoval != null)
        {
            if (cancelBounds.Contains(x, y))
                CancelRemoval();
            else if (confirmBounds.Contains(x, y))
                ConfirmRemoval();
            return;
        }

        if (upperRightCloseButton?.containsPoint(x, y) == true)
        {
            exitThisMenu();
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
        if (removeBounds.Contains(x, y))
            BeginRemoval();
        else if (entries.Count > visibleRows && firstVisibleIndex > 0 && previousBounds.Contains(x, y))
            Select(selectedIndex - visibleRows);
        else if (entries.Count > visibleRows && firstVisibleIndex + visibleRows < entries.Count && nextBounds.Contains(x, y))
            Select(selectedIndex + visibleRows);
    }

    public override void receiveRightClick(int x, int y, bool playSound = true)
    {
        if (pendingRemoval != null)
            CancelRemoval();
    }

    public override void receiveScrollWheelAction(int direction)
    {
        if (pendingRemoval == null)
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

        if (pendingRemoval != null)
        {
            if (cancel)
                CancelRemoval();
            else if (left || right || key == Keys.Tab)
            {
                confirmFocused = key == Keys.Tab ? !confirmFocused : right;
                Game1.playSound("smallSelect");
            }
            else if (activate)
            {
                if (confirmFocused)
                    ConfirmRemoval();
                else
                    CancelRemoval();
            }
            return;
        }

        if (cancel)
        {
            exitThisMenu();
            return;
        }
        if (key == Keys.Up || Game1.options.moveUpButton.Any(button => button.key == key))
        {
            Select(selectedIndex - 1);
            return;
        }
        if (key == Keys.Down || Game1.options.moveDownButton.Any(button => button.key == key))
        {
            Select(selectedIndex + 1);
            return;
        }
        if (left || right)
        {
            removeFocused = right && entries.Count > 0;
            return;
        }
        if (key == Keys.Delete || Game1.options.useToolButton.Any(button => button.key == key))
        {
            BeginRemoval();
            return;
        }
        if (activate)
        {
            if (removeFocused)
                BeginRemoval();
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
                removeFocused = entries.Count > 0 && !removeFocused;
                break;
            default:
                base.receiveKeyPress(key);
                break;
        }
    }

    public override void receiveGamePadButton(Buttons button)
    {
        // Only shoulder paging is unmapped. All other controls use receiveKeyPress once.
        if (pendingRemoval != null)
            return;

        switch (button)
        {
            case Buttons.LeftShoulder:
                Select(selectedIndex - visibleRows);
                break;
            case Buttons.RightShoulder:
                Select(selectedIndex + visibleRows);
                break;
        }
    }

    public override bool areGamePadControlsImplemented() => true;

    public override void performHoverAction(int x, int y)
    {
        hoverText = "";
        if (pendingRemoval != null)
            return;
        upperRightCloseButton?.tryHover(x, y);
        for (int row = 0; row < visibleRows && firstVisibleIndex + row < entries.Count; row++)
        {
            if (!RowBounds(row).Contains(x, y))
                continue;
            KeepsakeEntry entry = entries[firstVisibleIndex + row];
            hoverText = ItemText(entry) + "\n" + formatNote(entry.Entry);
            return;
        }
        if (removeBounds.Contains(x, y) && SelectedEntry != null)
            hoverText = i18n.Get("menu.remove-hint").ToString();
    }

    public override void draw(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.45f);
        DrawBox(b, new Rectangle(xPositionOnScreen, yPositionOnScreen, width, height));

        b.DrawString(Game1.dialogueFont, i18n.Get("menu.title").ToString(), new Vector2(xPositionOnScreen + 36, yPositionOnScreen + (compactLayout ? 18 : 28)), Game1.textColor);
        b.DrawString(Game1.smallFont, FitText(i18n.Get("menu.subtitle").ToString(), Game1.smallFont, width - 76), new Vector2(xPositionOnScreen + 38, yPositionOnScreen + (compactLayout ? 62 : 75)), MutedText);
        DrawRule(b, new Rectangle(xPositionOnScreen + 32, yPositionOnScreen + (compactLayout ? 94 : 108), width - 64, 2));

        if (entries.Count == 0)
            DrawEmptyState(b);
        else
        {
            string collection = i18n.Get("menu.collection", new { count = entries.Count }).ToString();
            b.DrawString(Game1.smallFont, FitText(collection, Game1.smallFont, listBounds.Width), new Vector2(listBounds.X + 4, yPositionOnScreen + (compactLayout ? 102 : 120)), MutedText);
            DrawRows(b);
            DrawDetails(b);
            if (entries.Count > visibleRows)
            {
                DrawButton(b, previousBounds, "<", false, firstVisibleIndex > 0);
                DrawButton(b, nextBounds, ">", false, firstVisibleIndex + visibleRows < entries.Count);
                string range = i18n.Get("menu.range", new { first = firstVisibleIndex + 1, last = Math.Min(entries.Count, firstVisibleIndex + visibleRows), total = entries.Count }).ToString();
                DrawCentered(b, range, Game1.smallFont, new Rectangle(previousBounds.Right + 4, previousBounds.Y, nextBounds.Left - previousBounds.Right - 8, previousBounds.Height), MutedText);
            }
        }

        string footer = i18n.Get(statusMilliseconds > 0 ? "menu.removed" : compactLayout ? "menu.help-compact" : "menu.help").ToString();
        b.DrawString(Game1.smallFont, FitText(footer, Game1.smallFont, width - 72), new Vector2(xPositionOnScreen + 36, yPositionOnScreen + height - (compactLayout ? 37 : 49)), statusMilliseconds > 0 ? new Color(62, 107, 69) : MutedText);
        upperRightCloseButton?.draw(b);

        if (pendingRemoval != null)
            DrawConfirmation(b);
        else if (hoverText.Length > 0)
            drawHoverText(b, hoverText, Game1.smallFont);
        drawMouse(b);
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
            if (selected && !removeFocused)
                b.Draw(Game1.staminaRect, new Rectangle(bounds.X + 9, bounds.Y + 12, 6, bounds.Height - 24), new Color(62, 107, 69));

            DrawPreview(b, entry, new Vector2(bounds.X + 16, bounds.Y + 5), compactLayout ? 0.75f : 0.9f);
            float textX = bounds.X + (compactLayout ? 70 : 82);
            float availableWidth = bounds.Right - textX - 16;
            b.DrawString(Game1.smallFont, FitText(ItemText(entry), Game1.smallFont, availableWidth), new Vector2(textX, bounds.Y + (compactLayout ? 9 : 13)), Game1.textColor);
            b.DrawString(Game1.smallFont, FitText(senderName(entry.Entry.SenderId), Game1.smallFont, availableWidth), new Vector2(textX, bounds.Y + (compactLayout ? 34 : 42)), MutedText);
        }
    }

    private void DrawDetails(SpriteBatch b)
    {
        if (SelectedEntry is not { } entry)
            return;
        DrawBox(b, detailBounds);
        DrawPreview(b, entry, new Vector2(detailBounds.X + 22, detailBounds.Y + (compactLayout ? 12 : 18)), 1f);
        int textX = detailBounds.X + 100;
        int textWidth = detailBounds.Width - 120;
        b.DrawString(Game1.smallFont, FitText(ItemText(entry), Game1.smallFont, textWidth), new Vector2(textX, detailBounds.Y + (compactLayout ? 17 : 25)), Game1.textColor);
        string from = i18n.Get("menu.from", new { sender = senderName(entry.Entry.SenderId) }).ToString();
        b.DrawString(Game1.smallFont, FitText(from, Game1.smallFont, textWidth), new Vector2(textX, detailBounds.Y + (compactLayout ? 48 : 56)), MutedText);
        DrawRule(b, new Rectangle(detailBounds.X + 20, detailBounds.Y + (compactLayout ? 87 : 101), detailBounds.Width - 40, 2));

        int noteX = detailBounds.X + 24;
        int noteY = detailBounds.Y + (compactLayout ? 98 : 123);
        if (!compactLayout && Game1.getCharacterFromName(entry.Entry.SenderId, false) is { Portrait: { } portrait })
        {
            b.Draw(portrait, new Rectangle(noteX, noteY, 96, 96), new Rectangle(0, 0, 64, 64), Color.White);
            noteX += 116;
        }

        int noteWidth = detailBounds.Right - noteX - 24;
        string date = FitText(DateText(entry.Entry), Game1.smallFont, noteWidth);
        b.DrawString(Game1.smallFont, date, new Vector2(noteX, noteY), MutedText);
        string occasion = i18n.Get(entry.Entry.Origin == "birthday" ? "menu.occasion.birthday" : "menu.occasion.mail").ToString();
        b.DrawString(Game1.smallFont, FitText(occasion, Game1.smallFont, noteWidth), new Vector2(noteX, noteY + 32), MutedText);
        if (entry.Entry.SourceModId == "Omegasis.HappyBirthday")
        {
            string provider = i18n.Get("note.provider", new { name = "Happy Birthday" }).ToString();
            b.DrawString(Game1.smallFont, FitText(provider, Game1.smallFont, noteWidth), new Vector2(noteX, noteY + 64), MutedText);
        }
        DrawButton(b, removeBounds, i18n.Get("menu.remove").ToString(), removeFocused);
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
        Rectangle titleBounds = new(xPositionOnScreen + 48, yPositionOnScreen + height / 2 - 90, width - 96, 60);
        DrawCentered(b, i18n.Get("menu.empty-title").ToString(), Game1.dialogueFont, titleBounds, Game1.textColor);
        int textWidth = Math.Min(620, width - 128);
        string text = Game1.parseText(i18n.Get("menu.empty-body").ToString(), Game1.smallFont, textWidth);
        Vector2 size = Game1.smallFont.MeasureString(text);
        b.DrawString(Game1.smallFont, text, new Vector2(xPositionOnScreen + (width - size.X) / 2, titleBounds.Bottom + 8), MutedText);
    }

    private void DrawConfirmation(SpriteBatch b)
    {
        b.Draw(Game1.fadeToBlackRect, new Rectangle(0, 0, Game1.uiViewport.Width, Game1.uiViewport.Height), Color.Black * 0.5f);
        DrawBox(b, confirmationBounds);
        b.DrawString(Game1.dialogueFont, i18n.Get("menu.confirm-title").ToString(), new Vector2(confirmationBounds.X + 28, confirmationBounds.Y + 28), Game1.textColor);
        string explanation = Game1.parseText(i18n.Get("menu.confirm-body").ToString(), Game1.smallFont, confirmationBounds.Width - 56);
        b.DrawString(Game1.smallFont, explanation, new Vector2(confirmationBounds.X + 28, confirmationBounds.Y + 89), MutedText);
        DrawButton(b, cancelBounds, i18n.Get("menu.cancel").ToString(), !confirmFocused);
        DrawButton(b, confirmBounds, i18n.Get("menu.remove").ToString(), confirmFocused);
    }

    private static void DrawBox(SpriteBatch b, Rectangle bounds, Color? tint = null) => drawTextureBox(b, bounds.X, bounds.Y, bounds.Width, bounds.Height, tint ?? Color.White);

    private static void DrawRule(SpriteBatch b, Rectangle bounds) => b.Draw(Game1.staminaRect, bounds, new Color(155, 117, 80) * 0.4f);

    private static void DrawButton(SpriteBatch b, Rectangle bounds, string text, bool focused, bool enabled = true)
    {
        bool hovered = bounds.Contains(Game1.getMouseX(), Game1.getMouseY());
        DrawBox(b, bounds, enabled && (focused || hovered) ? Highlight : Color.White);
        DrawCentered(b, text, Game1.smallFont, bounds, enabled ? Game1.textColor : MutedText * 0.55f);
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

    private sealed record KeepsakeEntry(GiftEntry Entry, Item? Preview, string DisplayName);
}
