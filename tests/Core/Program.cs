using DearlyKept;

int passed = 0;
void Check(string name, bool result)
{
    if (!result)
        throw new Exception("FAIL: " + name);
    Console.WriteLine("PASS: " + name);
    passed++;
}

var gift = new GiftStamp("Evelyn", "Evelyn", 2, "spring", 8);
string encoded = StampCodec.Encode(gift);
Check("complete provenance survives serialization", StampCodec.TryDecode(encoded, out var decoded) && decoded == gift);
Check("translated and modded identifiers survive", StampCodec.TryDecode(StampCodec.Encode(gift with { SenderId = "Renée", MailId = "Example.Mod/礼物" }), out var translated) && translated.SenderId == "Renée");
Check("maximum escaped identifiers survive", StampCodec.TryDecode(StampCodec.Encode(gift with { SenderId = new string('礼', 128), MailId = new string('物', 256) }), out _));
foreach (string? bad in new[] { null, "", " ", "null", "{}", "[]", "{", "{\"SenderId\":42}", new string('x', 4097) })
    Check("invalid stored data is ignored: " + (bad?.Length.ToString() ?? "null"), !StampCodec.TryDecode(bad, out _));

foreach (GiftStamp invalid in new[]
{
    gift with { SenderId = null! }, gift with { SenderId = " " }, gift with { SenderId = new string('x', 129) },
    gift with { MailId = "" }, gift with { Year = 0 }, gift with { Year = 10000 },
    gift with { Day = 0 }, gift with { Day = 29 }, gift with { Season = "monsoon" }
})
{
    bool rejected = false;
    try { StampCodec.Encode(invalid); }
    catch (ArgumentException) { rejected = true; }
    Check("invalid provenance cannot be written", rejected);
}

Check("ordinary stacks remain compatible", StampCodec.CanMerge(null, null));
Check("same received gift may be split and rejoined", StampCodec.CanMerge(encoded, encoded));
Check("gift cannot contaminate an ordinary stack", !StampCodec.CanMerge(encoded, null) && !StampCodec.CanMerge(null, encoded));
Check("different senders remain separate", !StampCodec.CanMerge(encoded, StampCodec.Encode(gift with { SenderId = "Mom" })));
Check("different dates remain separate", !StampCodec.CanMerge(encoded, StampCodec.Encode(gift with { Day = 9 })));
Check("different letters remain separate", !StampCodec.CanMerge(encoded, StampCodec.Encode(gift with { MailId = "other" })));
Check("unknown future metadata is preserved", !StampCodec.CanMerge("future-format", null));
Check("damaged metadata is not silently discarded", !StampCodec.CanMerge("{", encoded));
Console.WriteLine($"All {passed} checks passed.");
