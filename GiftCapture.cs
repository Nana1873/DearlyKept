using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace DearlyKept;

internal sealed class GiftCapture
{
    public const string SenderAssetName = "Mods/Nana1873.DearlyKept/MailSenders";

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly Func<bool> enabled;

    public GiftCapture(IModHelper helper, IMonitor monitor, Func<bool> enabled)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.enabled = enabled;
    }

    public void Register()
    {
        this.helper.Events.Content.AssetRequested += this.OnAssetRequested;
        this.helper.Events.Display.MenuChanged += this.OnMenuChanged;
    }

    public void HandleMenu(IClickableMenu menu)
    {
        if (!this.enabled() || !Context.IsWorldReady || menu is not LetterViewerMenu letter
            || !letter.isMail || letter.isFromCollection || string.IsNullOrEmpty(letter.mailTitle))
        {
            return;
        }

        try
        {
            Dictionary<string, string> senders = this.helper.GameContent.Load<Dictionary<string, string>>(SenderAssetName);
            if (!senders.TryGetValue(letter.mailTitle, out string? sender) || string.IsNullOrWhiteSpace(sender))
                return;

            GiftStamp stamp = new(sender, letter.mailTitle, Game1.year, Game1.currentSeason, Game1.dayOfMonth);
            int count = 0;
            foreach (ClickableComponent component in letter.itemsToGrab)
            {
                Item item = component.item;
                // Recovered possessions and reused inventory objects are not newly sent gifts.
                if (item is null || item.HasBeenInInventory || item.modData.ContainsKey(GiftTagService.DataKey))
                    continue;

                GiftTagService.Stamp(item, stamp);
                count++;
            }

            if (count > 0)
                this.monitor.Log($"Kept the sender and date on {count} gift stack(s) from mail '{letter.mailTitle}'.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            this.monitor.Log($"Couldn't record the gift in mail '{letter.mailTitle}': {ex.Message}", LogLevel.Warn);
        }
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is not null)
            this.HandleMenu(e.NewMenu);
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo(SenderAssetName))
            e.LoadFrom(CreateDefaultSenders, AssetLoadPriority.Exclusive);
    }

    private static Dictionary<string, string> CreateDefaultSenders() => new(StringComparer.Ordinal)
    {
        ["Caroline"] = "Caroline",
        ["Clint"] = "Clint",
        ["Demetrius"] = "Demetrius",
        ["Emily"] = "Emily",
        ["Evelyn"] = "Evelyn",
        ["George"] = "George",
        ["Gus"] = "Gus",
        ["Jodi"] = "Jodi",
        ["Kent"] = "Kent",
        ["Linus"] = "Linus",
        ["Marnie"] = "Marnie",
        ["Pam"] = "Pam",
        ["Robin"] = "Robin",
        ["Sandy"] = "Sandy",
        ["Shane"] = "Shane",
        ["Wizard"] = "Wizard",
        ["mom1"] = "Mom",
        ["mom4"] = "Mom",
        ["dad4"] = "Dad",
        ["ClintReward2"] = "Clint",
        ["emilyStones"] = "Emily",
        ["georgeGifts"] = "Evelyn",
        ["gusGiantOmelet"] = "Gus",
        ["MSB_Lewis"] = "Lewis",
        ["MSB_Pierre"] = "Pierre",
        ["WillyTropicalFish"] = "Willy"
    };
}
