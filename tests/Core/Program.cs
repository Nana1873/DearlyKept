using System.Text.Json;
using DearlyKept;

int passed = 0;
void Check(string name, bool result)
{
    if (!result)
        throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}

void RejectLoadWithoutChangingLedger(string name, GiftLedger ledger, JournalData invalid)
{
    GiftEntry[] before = ledger.Entries.ToArray();
    int revision = ledger.Revision;
    bool rejected = false;
    try { ledger.Load(invalid); }
    catch (InvalidDataException) { rejected = true; }
    Check(name, rejected && ledger.Entries.SequenceEqual(before) && ledger.Revision == revision);
}

var mail = new GiftEntry("320d7d25e6a2447d87710644b070a00e", "Mom", "mom1", "mail", null,
    1, "spring", 8, "(O)223", "Cookies", 1, 0);
var birthday = mail with
{
    Id = "83b93f4cd8264c0e99d3e4158eeb5a44",
    SenderId = "Evelyn",
    SourceId = "birthday:Evelyn",
    Origin = "birthday",
    SourceModId = "Omegasis.HappyBirthday",
    Quantity = 3,
    Quality = 2
};
var secondIdenticalMail = mail with { Id = "5a485f1790c64ec4b742b3f379c71558" };
var ledger = new GiftLedger();
Check("valid mail and birthday receipts are accepted", ledger.Add(mail) && ledger.Add(birthday));
Check("identical gifts received separately keep separate journal entries", ledger.Add(secondIdenticalMail)
    && ledger.Entries.Count == 3 && ledger.Entries[0] == mail && ledger.Entries[2] == secondIdenticalMail);

int acceptedRevision = ledger.Revision;
Check("a repeated callback for the same receipt is ignored", !ledger.Add(mail)
    && !ledger.Add(mail with { Quantity = 99 }) && ledger.Entries.Count == 3
    && ledger.Entries[0] == mail && ledger.Revision == acceptedRevision);

// Persist actual bytes, then reconstruct a different ledger from the file.
string evidenceDirectory = Path.GetFullPath(args.FirstOrDefault()
    ?? Path.Combine(".sdvkit", "tests", "core-journal"));
Directory.CreateDirectory(evidenceDirectory);
string savePath = Path.Combine(evidenceDirectory, "journal-roundtrip.json");
File.WriteAllText(savePath, JsonSerializer.Serialize(ledger.Snapshot()));
var reloaded = new GiftLedger();
JournalData persisted = JsonSerializer.Deserialize<JournalData>(File.ReadAllText(savePath))!;
Check("durable JSON roundtrip restores every receipt field and chronological order",
    reloaded.Load(persisted) == 0 && reloaded.Entries.SequenceEqual(ledger.Entries));
Check("receipt deduplication survives a save and reload", !reloaded.Add(mail) && reloaded.Entries.Count == 3);

JournalData detached = reloaded.Snapshot();
detached.SchemaVersion = 999;
detached.Gifts[0] = mail with { ItemName = "Changed outside the ledger", Quantity = 99 };
detached.Gifts.RemoveAt(1);
detached.Gifts.Add(mail with { Id = Guid.NewGuid().ToString("N") });
Check("mutating an exported snapshot cannot alter the live ledger",
    reloaded.Entries.SequenceEqual(new[] { mail, birthday, secondIdenticalMail }));

Check("removing a receipt keeps the other chronological memories", reloaded.Remove(mail.Id)
    && reloaded.Entries.SequenceEqual(new[] { birthday, secondIdenticalMail }));
int removedRevision = reloaded.Revision;
Check("late callbacks cannot resurrect a removed memory", !reloaded.Add(mail)
    && !reloaded.Add(mail with { Quantity = 100 }) && !reloaded.Remove(mail.Id)
    && reloaded.Revision == removedRevision && reloaded.Entries.Count == 2);
var afterRemoval = new GiftLedger();
Check("saving and reloading a deletion does not restore the removed entry",
    afterRemoval.Load(JsonSerializer.Deserialize<JournalData>(JsonSerializer.Serialize(reloaded.Snapshot()))!) == 0
    && afterRemoval.Entries.All(entry => entry.Id != mail.Id));

var invalidEntries = new (string Name, GiftEntry? Entry)[]
{
    ("null receipt", null),
    ("null receipt ID", mail with { Id = null! }),
    ("malformed receipt ID", mail with { Id = "not-a-guid" }),
    ("noncanonical receipt ID", mail with { Id = Guid.Parse(mail.Id).ToString("D") }),
    ("null sender", mail with { SenderId = null! }),
    ("blank sender", mail with { SenderId = " " }),
    ("oversized sender", mail with { SenderId = new string('x', 129) }),
    ("empty source ID", mail with { SourceId = "" }),
    ("null source ID", mail with { SourceId = null! }),
    ("oversized source ID", mail with { SourceId = new string('x', 513) }),
    ("unknown origin", mail with { Origin = "unrecognized" }),
    ("null origin", mail with { Origin = null! }),
    ("empty optional source mod ID", birthday with { SourceModId = "" }),
    ("oversized source mod ID", birthday with { SourceModId = new string('x', 257) }),
    ("zero year", mail with { Year = 0 }),
    ("oversized year", mail with { Year = 10000 }),
    ("zero day", mail with { Day = 0 }),
    ("day beyond the season", mail with { Day = 29 }),
    ("unknown season", mail with { Season = "monsoon" }),
    ("null season", mail with { Season = null! }),
    ("empty item ID", mail with { QualifiedItemId = "" }),
    ("null item ID", mail with { QualifiedItemId = null! }),
    ("oversized item ID", mail with { QualifiedItemId = new string('x', 257) }),
    ("null item name", mail with { ItemName = null! }),
    ("blank item name", mail with { ItemName = " " }),
    ("oversized item name", mail with { ItemName = new string('x', 513) }),
    ("negative quantity", mail with { Quantity = -1 }),
    ("zero quantity", mail with { Quantity = 0 }),
    ("negative quality", mail with { Quality = -1 })
};
foreach ((string name, GiftEntry? entry) in invalidEntries)
{
    // A fresh ledger avoids mistaking duplicate-ID rejection for data validation.
    var empty = new GiftLedger();
    Check("invalid data rejected: " + name, !empty.Add(entry) && empty.Entries.Count == 0 && empty.Revision == 0);
}

var mixed = new JournalData { Gifts = new() { mail, birthday, mail, null!, mail with { Quantity = -1 } } };
var recovered = new GiftLedger();
Check("loading damaged entries retains valid receipts and reports every rejected entry",
    recovered.Load(mixed) == 3 && recovered.Entries.SequenceEqual(new[] { mail, birthday }));
RejectLoadWithoutChangingLedger("unknown schema is rejected before clearing existing history", recovered,
    new JournalData { SchemaVersion = 2, Gifts = new() { secondIdenticalMail } });
RejectLoadWithoutChangingLedger("missing gift list is rejected before clearing existing history", recovered,
    new JournalData { Gifts = null! });
Check("rejected loads preserve receipt deduplication too", !recovered.Add(mail) && recovered.Entries.Count == 2);

var localized = birthday with
{
    Id = Guid.NewGuid().ToString("N"), SenderId = "Renée", SourceId = "Example.Mod/礼物",
    QualifiedItemId = "(O)Example.Mod_礼物", ItemName = "Großmutters 🍪"
};
var localizedLedger = new GiftLedger();
localizedLedger.Add(localized);
var localizedReloaded = new GiftLedger();
Check("localized and modded receipt text survives JSON", localizedReloaded.Load(
    JsonSerializer.Deserialize<JournalData>(JsonSerializer.Serialize(localizedLedger.Snapshot()))!) == 0
    && localizedReloaded.Entries.Single() == localized);

recovered.Clear();
Check("clearing for a new save removes old history", recovered.Entries.Count == 0);
Check("a new save can accept a receipt ID seen in the previous save", recovered.Add(mail)
    && recovered.Entries.SequenceEqual(new[] { mail }));
recovered.Remove(mail.Id);
Check("loading another save resets removed-ID suppression from the previous save",
    recovered.Load(new JournalData { Gifts = new() { mail } }) == 0 && recovered.Entries.SequenceEqual(new[] { mail }));
Check("loading an empty save never leaks the previous save's history",
    recovered.Load(new JournalData()) == 0 && recovered.Entries.Count == 0);

Console.WriteLine($"All {passed} checks passed. Durable roundtrip evidence: {savePath}");
