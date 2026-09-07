using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    private readonly Func<bool> birthdayEnabled;
    private readonly GiftJournal journal;
    private ConditionalWeakTable<LetterViewerMenu, Dictionary<Item, GiftEntry>> receipts = new();

    public GiftCapture(IModHelper helper, IMonitor monitor, GiftJournal journal, Func<bool> enabled, Func<bool> birthdayEnabled)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.enabled = enabled;
        this.birthdayEnabled = birthdayEnabled;
        this.journal = journal;
    }

    public void Register()
    {
        this.helper.Events.Content.AssetRequested += this.OnAssetRequested;
        this.helper.Events.GameLoop.ReturnedToTitle += (_, _) => this.receipts = new();
    }

    public LetterReceipt? Begin(LetterViewerMenu letter)
    {
        if (!this.enabled() || !this.journal.CanRecord
            || !letter.isMail || letter.isFromCollection || string.IsNullOrEmpty(letter.mailTitle))
        {
            return null;
        }

        try
        {
            Dictionary<string, string> senders = this.helper.GameContent.Load<Dictionary<string, string>>(SenderAssetName);
            string? birthdaySender = null;
            bool birthday = this.birthdayEnabled() && TryBirthdaySender(letter.mailTitle, out birthdaySender);
            string? sender = birthday ? birthdaySender : senders.GetValueOrDefault(letter.mailTitle);
            if (string.IsNullOrWhiteSpace(sender))
                return null;

            Dictionary<Item, GiftEntry> known = this.receipts.GetValue(letter, _ => new(ReferenceEqualityComparer.Instance));
            List<KeyValuePair<Item, GiftEntry>> pending = new();
            foreach (ClickableComponent component in letter.itemsToGrab)
            {
                Item item = component.item;
                // Recovered possessions and reused inventory objects are not newly sent gifts.
                if (item is null || item.HasBeenInInventory)
                    continue;

                if (!known.TryGetValue(item, out GiftEntry? entry))
                {
                    entry = this.journal.CreateEntry(item, sender, letter.mailTitle,
                        birthday ? "birthday" : "mail", birthday ? HappyBirthdayIntegration.ModId : null);
                    if (entry is null)
                        continue;
                    known.Add(item, entry);
                }
                pending.Add(new(item, entry));
            }

            return new LetterReceipt(Game1.player, pending);
        }
        catch (Exception ex)
        {
            this.monitor.Log($"Couldn't record the gift in mail '{letter.mailTitle}': {ex.Message}", LogLevel.Warn);
            return null;
        }
    }

    public void Complete(LetterViewerMenu letter, LetterReceipt? receipt)
    {
        if (receipt is null || !this.enabled() || !this.journal.CanRecord || !ReferenceEquals(receipt.Player, Game1.player))
            return;
        try
        {
            foreach (var pending in receipt.Items)
            {
                // A successful transfer returns with claimed attachments removed from the letter.
                if (!letter.itemsToGrab.Any(component => ReferenceEquals(component.item, pending.Key)))
                    this.journal.Record(pending.Value);
            }
        }
        catch (Exception ex)
        {
            this.monitor.Log($"Couldn't finish recording mail '{letter.mailTitle}': {ex.Message}", LogLevel.Warn);
        }
    }

    private static bool TryBirthdaySender(string mailId, out string? sender)
    {
        sender = mailId switch
        {
            "Omegasis.HappyBirthday_Mom" => "Mom",
            "Omegasis.HappyBirthday_Dad" or "Omegasis.HappyBirthday_Dad_Married" => "Dad",
            _ => null
        };
        const string prefix = "Omegasis.HappyBirthday_BelatedBirthdayWish_";
        if (sender is null && mailId.StartsWith(prefix, StringComparison.Ordinal))
        {
            string name = mailId[prefix.Length..];
            if (Game1.getCharacterFromName(name, false) is { } npc)
                sender = npc.Name;
        }
        return sender is not null;
    }

    internal sealed record LetterReceipt(Farmer Player, List<KeyValuePair<Item, GiftEntry>> Items);

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
