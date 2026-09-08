using System.Reflection;
using StardewModdingAPI;
using StardewValley;

namespace DearlyKept.LiveHarness;

internal sealed partial class ModEntry
{
    // Run has verified the registered single-player fixture and exact process.
    private void CheckPlayerStorage()
    {
        RequireNoMenu();
        const string playerKey = "Nana1873.DearlyKept/GiftJournal";
        const string legacyKey = "gift-journal";
        object metadata = Helper.ModRegistry.Get("Nana1873.DearlyKept")!;
        IMod mod = (IMod)metadata.GetType().GetProperty("Mod")!.GetValue(metadata)!;
        object journal = mod.GetType().GetField("journal", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mod)!;
        Type journalType = journal.GetType();
        MethodInfo load = journalType.GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic)!;
        MethodInfo save = journalType.GetMethod("Save", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object ledger = journalType.GetProperty("ledger", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(journal)!;
        object snapshot = ledger.GetType().GetMethod("Snapshot")!.Invoke(ledger, null)!;
        MethodInfo readLegacy = typeof(IDataHelper).GetMethod("ReadSaveData")!.MakeGenericMethod(snapshot.GetType());
        MethodInfo writeLegacy = typeof(IDataHelper).GetMethod("WriteSaveData")!.MakeGenericMethod(snapshot.GetType());
        object? previousLegacy = readLegacy.Invoke(mod.Helper.Data, new object[] { legacyKey });
        string raw = Game1.player.modData[playerKey];
        JournalSnapshot before = CaptureJournal();
        Check(before.Count > 0, "storage regression uses actual received gift history");
        try
        {
            writeLegacy.Invoke(mod.Helper.Data, new[] { legacyKey, snapshot });
            Game1.player.modData.Remove(playerKey);
            load.Invoke(journal, null);
            Check(CaptureJournal() == before, "the host imports the legacy archive without changing receipts or text");
            save.Invoke(journal, null);
            Check(Game1.player.modData[playerKey] == raw, "legacy import serializes the complete archive to this farmer");
            foreach (string invalid in new[] { "{broken", "null", "{\"SchemaVersion\":999,\"Gifts\":[]}" })
            {
                Game1.player.modData[playerKey] = invalid;
                load.Invoke(journal, null);
                Check(!(bool)journalType.GetProperty("CanRecord")!.GetValue(journal)!, "invalid player data disables recording instead of importing legacy data");
                save.Invoke(journal, null);
                Check(Game1.player.modData[playerKey] == invalid, "invalid raw player data remains byte-for-byte untouched");
            }
            Monitor.Log("DKQA STORAGE PASS: actual production loader/saver preserved legacy receipts and malformed player data.", LogLevel.Info);
        }
        finally
        {
            writeLegacy.Invoke(mod.Helper.Data, new[] { legacyKey, previousLegacy });
            Game1.player.modData[playerKey] = raw;
            load.Invoke(journal, null);
        }
    }
}
