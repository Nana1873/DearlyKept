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

const string messageText = "Dear farmer,\r\n\r\nHere's a little gift.\nWith love, Renée 🍪\n  P.S. Enjoy it!  ";
var mail = new GiftEntry("320d7d25e6a2447d87710644b070a00e", "Mom", "mom1", "mail", null,
    1, "spring", 8, "(O)223", "Cookies", 1, 0, messageText);
var birthday = mail with
{
    Id = "83b93f4cd8264c0e99d3e4158eeb5a44",
    SenderId = "Evelyn",
    SourceId = "birthday:Evelyn",
    Origin = "birthday",
    SourceModId = "Omegasis.HappyBirthday",
    Quantity = 3,
    Quality = 2,
    MessageText = "Happy birthday, farmer!\nMay your year be wonderful."
};
var secondIdenticalMail = mail with { Id = "5a485f1790c64ec4b742b3f379c71558" };
var ledger = new GiftLedger();
Check("valid mail and birthday receipts are accepted", ledger.Add(mail) && ledger.Add(birthday));
Check("identical gifts received separately keep separate journal entries", ledger.Add(secondIdenticalMail)
    && ledger.Entries.Count == 3 && ledger.Entries[0] == mail && ledger.Entries[2] == secondIdenticalMail);

int acceptedRevision = ledger.Revision;
Check("a repeated callback for the same receipt is ignored", !ledger.Add(mail)
    && !ledger.Add(mail with { Quantity = 99, MessageText = "A later callback must not replace the archived text." }) && ledger.Entries.Count == 3
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
Check("archived message preserves Unicode, mixed newlines, spacing and the original receipt date exactly",
    reloaded.Entries[0].MessageText == messageText && reloaded.Entries[0].Year == 1
    && reloaded.Entries[0].Season == "spring" && reloaded.Entries[0].Day == 8);
Check("receipt deduplication survives a save and reload", !reloaded.Add(mail) && reloaded.Entries.Count == 3);

JournalData detached = reloaded.Snapshot();
detached.SchemaVersion = 999;
detached.Gifts[0] = mail with { ItemName = "Changed outside the ledger", Quantity = 99, MessageText = "Replaced in the exported snapshot." };
detached.Gifts.RemoveAt(1);
detached.Gifts.Add(mail with { Id = Guid.NewGuid().ToString("N") });
Check("mutating an exported snapshot cannot alter the live ledger",
    reloaded.Entries.SequenceEqual(new[] { mail, birthday, secondIdenticalMail }));

// This is the pre-archive schema-1 shape: MessageText did not exist at all.
const string legacyJson = """
    {"SchemaVersion":1,"Gifts":[{"Id":"320d7d25e6a2447d87710644b070a00e","SenderId":"Mom","SourceId":"mom1","Origin":"mail","SourceModId":null,"Year":1,"Season":"spring","Day":8,"QualifiedItemId":"(O)223","ItemName":"Cookies","Quantity":1,"Quality":0}]}
    """;
string legacyPath = Path.Combine(evidenceDirectory, "journal-schema1-before-text.json");
File.WriteAllText(legacyPath, legacyJson);
var legacy = new GiftLedger();
Check("old schema-1 receipts with no message field still load without invented text",
    legacy.Load(JsonSerializer.Deserialize<JournalData>(File.ReadAllText(legacyPath))!) == 0
    && legacy.Entries.Single() == mail with { MessageText = null });
var legacyResaved = new GiftLedger();
Check("resaving an old receipt preserves its absent text and original fields under schema 1",
    legacyResaved.Load(JsonSerializer.Deserialize<JournalData>(JsonSerializer.Serialize(legacy.Snapshot()))!) == 0
    && legacyResaved.Snapshot().SchemaVersion == 1 && legacyResaved.Entries.Single() == legacy.Entries.Single());
var textBoundaries = new GiftLedger();
Check("an actual empty message is preserved without fabricating replacement text",
    textBoundaries.Add(mail with { MessageText = "" }) && textBoundaries.Entries.Single().MessageText == "");
var longestMessage = mail with { Id = Guid.NewGuid().ToString("N"), MessageText = new string('文', 15999) + "\n" };
Check("a 16000-character original message is accepted without truncation", textBoundaries.Add(longestMessage)
    && textBoundaries.Entries[1].MessageText == longestMessage.MessageText);
var textReloaded = new GiftLedger();
Check("empty and maximum-length messages survive serialization unchanged",
    textReloaded.Load(JsonSerializer.Deserialize<JournalData>(JsonSerializer.Serialize(textBoundaries.Snapshot()))!) == 0
    && textReloaded.Entries.SequenceEqual(textBoundaries.Entries));

// A delivered birthday gift can acquire further text as its real dialogue advances.
var conversation = new GiftLedger();
conversation.Add(mail);
conversation.Add(birthday);
JournalData beforeEnrichment = conversation.Snapshot();
int conversationRevision = conversation.Revision;
string completedMessage = birthday.MessageText + "\n\nA second page for you, Renée 🍪.\n  With love.  ";
GiftEntry enrichedBirthday = birthday with { MessageText = completedMessage };
Check("later dialogue pages update only the existing receipt's message and increment revision once",
    conversation.UpdateMessage(birthday.Id, completedMessage)
    && conversation.Entries.SequenceEqual(new[] { mail, enrichedBirthday })
    && conversation.Revision == conversationRevision + 1);
Check("message enrichment cannot mutate a previously exported snapshot",
    beforeEnrichment.Gifts.SequenceEqual(new[] { mail, birthday }));

conversationRevision = conversation.Revision;
Check("sampling the same dialogue text again is a no-op without a revision change",
    !conversation.UpdateMessage(birthday.Id, completedMessage)
    && conversation.Revision == conversationRevision && conversation.Entries[1] == enrichedBirthday);
Check("message enrichment cannot create an unknown receipt",
    !conversation.UpdateMessage("21326c4924ab417f874f21c2f49d10b2", completedMessage)
    && conversation.Revision == conversationRevision
    && conversation.Entries.SequenceEqual(new[] { mail, enrichedBirthday }));
Check("oversized enrichment is rejected without replacing the archived message",
    !conversation.UpdateMessage(birthday.Id, new string('x', 16001))
    && conversation.Revision == conversationRevision && conversation.Entries[1] == enrichedBirthday);
Check("a missing enrichment message cannot erase archived text",
    !conversation.UpdateMessage(birthday.Id, null!)
    && conversation.Revision == conversationRevision && conversation.Entries[1] == enrichedBirthday);
Check("a repeated receipt callback cannot undo enriched text or change gift quantity",
    !conversation.Add(birthday) && !conversation.Add(birthday with { Quantity = 99, MessageText = "Wrong text" })
    && conversation.Revision == conversationRevision
    && conversation.Entries.SequenceEqual(new[] { mail, enrichedBirthday }));

string enrichedSavePath = Path.Combine(evidenceDirectory, "journal-enriched-roundtrip.json");
File.WriteAllText(enrichedSavePath, JsonSerializer.Serialize(conversation.Snapshot()));
var enrichedReloaded = new GiftLedger();
Check("enriched dialogue text and all original receipt fields survive durable schema-1 persistence",
    enrichedReloaded.Load(JsonSerializer.Deserialize<JournalData>(File.ReadAllText(enrichedSavePath))!) == 0
    && enrichedReloaded.Snapshot().SchemaVersion == 1
    && enrichedReloaded.Entries.SequenceEqual(new[] { mail, enrichedBirthday }));
Check("receipt deduplication survives enrichment and reload",
    !enrichedReloaded.Add(birthday) && enrichedReloaded.Entries.SequenceEqual(new[] { mail, enrichedBirthday }));
string maximumEnrichment = new string('文', 15999) + "\n";
Check("message enrichment accepts exactly 16000 characters without truncating them",
    enrichedReloaded.UpdateMessage(birthday.Id, maximumEnrichment)
    && enrichedReloaded.Entries[1] == birthday with { MessageText = maximumEnrichment });

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
    ("negative quality", mail with { Quality = -1 }),
    ("message beyond archive limit", mail with { MessageText = new string('x', 16001) })
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

var additionalOrigins = new GiftLedger();
var spouseGift = mail with { Id = Guid.NewGuid().ToString("N"), SenderId = "Leah", SourceId = "spouse:Leah", Origin = "spouse" };
var otherGift = mail with { Id = Guid.NewGuid().ToString("N"), SourceId = "Example.Mod/gift", Origin = "other", SourceModId = "Example.Mod", MessageText = null };
Check("spouse and other gift categories are accepted for archive filters", additionalOrigins.Add(spouseGift) && additionalOrigins.Add(otherGift));
var originsReloaded = new GiftLedger();
Check("additional categories and optional message text survive persistence",
    originsReloaded.Load(JsonSerializer.Deserialize<JournalData>(JsonSerializer.Serialize(additionalOrigins.Snapshot()))!) == 0
    && originsReloaded.Entries.SequenceEqual(new[] { spouseGift, otherGift }));

recovered.Clear();
Check("clearing for a new save removes old history", recovered.Entries.Count == 0);
Check("a new save can accept a receipt ID seen in the previous save", recovered.Add(mail)
    && recovered.Entries.SequenceEqual(new[] { mail }));
Check("loading another save resets receipt deduplication from the previous save",
    recovered.Load(new JournalData { Gifts = new() { mail } }) == 0 && recovered.Entries.SequenceEqual(new[] { mail }));
Check("loading an empty save never leaks the previous save's history",
    recovered.Load(new JournalData()) == 0 && recovered.Entries.Count == 0);

Console.WriteLine($"All {passed} checks passed. Durable roundtrip evidence: {savePath}");
