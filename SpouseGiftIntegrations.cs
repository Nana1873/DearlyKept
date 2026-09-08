using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace DearlyKept;

/// <summary>Observes deliveries from explicitly identified spouse gift producers.</summary>
internal sealed class SpouseGiftIntegrations
{
    private const string MarriageId = "TitanmasterRy.MarriageOverhaul";
    private const string AnniversaryId = "Kantrip.WeddingAnniversaries";
    private static SpouseGiftIntegrations? instance;
    [ThreadStatic] private static ReceiptContext? current;
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly GiftJournal journal;
    private readonly Func<bool> marriageEnabled;
    private readonly Func<bool> anniversaryEnabled;
    private readonly PerScreen<ConditionalWeakTable<Dialogue, ReceiptContext>> screenDialogues = new(() => new());
    private readonly PerScreen<Dictionary<DialogueBox, Transcript>> screenConversations = new(() => new(ReferenceEqualityComparer.Instance));
    private ConditionalWeakTable<Dialogue, ReceiptContext> dialogues { get => screenDialogues.Value; set => screenDialogues.Value = value; }
    private Dictionary<DialogueBox, Transcript> conversations => screenConversations.Value;
    private bool reportedError;

    public SpouseGiftIntegrations(IModHelper helper, IMonitor monitor, GiftJournal journal,
        Func<bool> marriageEnabled, Func<bool> anniversaryEnabled)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.journal = journal;
        this.marriageEnabled = marriageEnabled;
        this.anniversaryEnabled = anniversaryEnabled;
    }

    public void Register()
    {
        instance = this;
        helper.Events.GameLoop.GameLaunched += (_, _) => Install();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) =>
        {
            current = null;
            dialogues = new();
            conversations.Clear();
        };
        helper.Events.GameLoop.UpdateTicked += (_, _) => Observe();
    }

    private bool CanCapture(ReceiptContext context) => journal.CanRecord
        && ReferenceEquals(context.Player, Game1.player)
        && (context.ModId == MarriageId ? marriageEnabled() : anniversaryEnabled());

    private void Install()
    {
        InstallAdapter(MarriageId, "1.7.4", "MarriageOverhaul.ModEntry", (h, type) =>
        {
            Patch(h, Required(type, "Birthday_OnDayStarted", typeof(NPC)), nameof(BeginBirthday), null, nameof(EndContext));
            Patch(h, Required(type, "Requests_DeliverPendingReward", typeof(NPC)), nameof(BeginReward), null, nameof(EndContext));
            Patch(h, Required(type, "GiveItemToPlayerOrFridge", typeof(Item)), nameof(BeforeMarriageItem), nameof(AfterMarriageItem));
            Patch(h, Required(type, "QueueOrShow", typeof(Action)), nameof(WrapDisplay));
        });
        InstallAdapter(AnniversaryId, "2.1.0", "WeddingAnniversaries.ModEntry", (h, type) =>
        {
            Patch(h, Required(type, "PushAnniversaryText", typeof(NPC), typeof(int)), nameof(BeforeAnniversary), nameof(AfterAnniversary));
            Patch(h, Required(typeof(Dialogue), nameof(Dialogue.prepareCurrentDialogueForDisplay)), nameof(BeginDialogue), null, nameof(EndContext));
            Patch(h, Required(typeof(Farmer), nameof(Farmer.addItemToInventoryBool), typeof(Item), typeof(bool)), nameof(BeforeAnniversaryItem), nameof(AfterInventoryItem));
            Patch(h, Required(typeof(Farmer), nameof(Farmer.addItemsByMenuIfNecessary), typeof(List<Item>), typeof(ItemGrabMenu.behaviorOnItemSelect), typeof(bool)), nameof(BeforeQueuedItems), nameof(AfterQueuedItems));
        });
        var observer = new Harmony("Nana1873.DearlyKept.SpouseTranscripts");
        try
        {
            Patch(observer, Required(typeof(DialogueBox), nameof(DialogueBox.receiveLeftClick), typeof(int), typeof(int), typeof(bool)), nameof(ObserveBox), nameof(ObserveBox));
            Patch(observer, Required(typeof(DialogueBox), nameof(DialogueBox.closeDialogue)), nameof(ObserveBox));
        }
        catch (Exception ex) { observer.UnpatchAll(observer.Id); Report(ex); }
    }

    private void InstallAdapter(string id, string version, string typeName, Action<Harmony, Type> install)
    {
        IModInfo? mod = helper.ModRegistry.Get(id);
        if (mod is null) return;
        if (mod.Manifest.Version.ToString() != version)
        {
            monitor.Log($"{mod.Manifest.Name} gift recording supports {version}; installed {mod.Manifest.Version} stays disabled.", LogLevel.Warn);
            return;
        }
        var harmony = new Harmony("Nana1873.DearlyKept." + id);
        try
        {
            Type type = AccessTools.TypeByName(typeName) ?? throw new InvalidOperationException($"Missing {typeName}.");
            if (type.Assembly.GetName().Name != typeName.Split('.')[0])
                throw new InvalidOperationException("Unexpected integration assembly.");
            install(harmony, type);
            monitor.Log($"{mod.Manifest.Name} {version} gift journal integration is active.", LogLevel.Debug);
        }
        catch (Exception ex) { harmony.UnpatchAll(harmony.Id); Report(ex); }
    }

    private static MethodInfo Required(Type type, string name, params Type[] parameters) =>
        AccessTools.DeclaredMethod(type, name, parameters) ?? throw new MissingMethodException(type.FullName, name);

    private static void Patch(Harmony h, MethodInfo method, string? before = null, string? after = null, string? finallyDo = null) =>
        h.Patch(method, before is null ? null : new HarmonyMethod(typeof(SpouseGiftIntegrations), before),
            after is null ? null : new HarmonyMethod(typeof(SpouseGiftIntegrations), after),
            finalizer: finallyDo is null ? null : new HarmonyMethod(typeof(SpouseGiftIntegrations), finallyDo));

    private static void BeginBirthday(NPC spouse, out ReceiptContext? __state) => BeginMarriage(spouse, "birthday", out __state);
    private static void BeginReward(NPC spouse, out ReceiptContext? __state) => BeginMarriage(spouse, "spouse", out __state);
    private static void BeginMarriage(NPC spouse, string origin, out ReceiptContext? previous)
    {
        previous = current;
        current = null;
        try
        {
            if (instance?.journal.CanRecord == true && instance.marriageEnabled() && spouse is not null)
                current = new ReceiptContext(Game1.player, spouse.Name, origin, MarriageId);
        }
        catch (Exception ex) { instance?.Report(ex); }
    }
    private static void EndContext(ReceiptContext? __state) => current = __state;

    private static void BeforeMarriageItem(Item item, out Delivery? __state) => __state = Snapshot(item, MarriageId);
    private static Delivery? Snapshot(Item item, string modId)
    {
        try
        {
            if (item is null || current is not { } context || context.ModId != modId || instance?.CanCapture(context) != true)
                return null;
            if (!context.Items.TryGetValue(item, out GiftEntry? entry))
            {
                entry = instance.journal.CreateEntry(item, context.Sender, context.Origin + ":" + context.Sender, context.Origin, modId);
                if (entry is null) return null;
                context.Items.Add(item, entry);
            }
            return new Delivery(item, entry, context);
        }
        catch (Exception ex) { instance?.Report(ex); return null; }
    }
    private static void AfterMarriageItem(bool __result, Delivery? __state)
    {
        if (__state is null) return;
        // A failed all-or-nothing result can still leave a partial stack in a fridge.
        int quantity = __result ? __state.Entry.Quantity : Math.Max(0, __state.Entry.Quantity - __state.Item.Stack);
        Record(__state, quantity);
    }
    private static void Record(Delivery delivery, int quantity)
    {
        try
        {
            if (quantity > 0 && instance?.CanCapture(delivery.Context) == true)
            {
                GiftEntry entry = delivery.Entry with { Quantity = quantity, MessageText = delivery.Context.Text };
                instance.journal.Record(entry);
                delivery.Context.ReceiptIds.Add(entry.Id);
            }
        }
        catch (Exception ex) { instance?.Report(ex); }
    }

    private static void WrapDisplay(ref Action show)
    {
        if (current is not { ModId: MarriageId } context || instance?.CanCapture(context) != true) return;
        Action original = show;
        show = () =>
        {
            IClickableMenu? before = Game1.activeClickableMenu;
            original();
            try
            {
                if (instance?.CanCapture(context) == true && Game1.activeClickableMenu is DialogueBox box
                    && !ReferenceEquals(before, box) && box.characterDialogue?.speaker?.Name == context.Sender)
                    instance.Attach(box, context);
            }
            catch (Exception ex) { instance?.Report(ex); }
        };
    }

    private static void BeforeAnniversary(NPC npc, out Dialogue? __state) => __state = npc.CurrentDialogue.TryPeek(out Dialogue? d) ? d : null;
    private static void AfterAnniversary(NPC npc, Dialogue? __state)
    {
        try
        {
            if (instance?.journal.CanRecord == true && instance.anniversaryEnabled()
                && npc.CurrentDialogue.TryPeek(out Dialogue? dialogue) && !ReferenceEquals(dialogue, __state))
                instance.dialogues.Add(dialogue, new ReceiptContext(Game1.player, npc.Name, "anniversary", AnniversaryId));
        }
        catch (Exception ex) { instance?.Report(ex); }
    }
    private static void BeginDialogue(Dialogue __instance, out ReceiptContext? __state)
    {
        __state = current;
        current = null;
        if (instance?.dialogues.TryGetValue(__instance, out ReceiptContext? context) == true && instance.CanCapture(context))
            current = context;
    }
    private static void BeforeAnniversaryItem(Farmer __instance, Item item, out Delivery? __state) =>
        __state = ReferenceEquals(__instance, Game1.player) ? Snapshot(item, AnniversaryId) : null;
    private static void AfterInventoryItem(bool __result, Delivery? __state)
    {
        if (__result && __state is not null) Record(__state, __state.Entry.Quantity);
    }
    private static void BeforeQueuedItems(Farmer __instance, List<Item> itemsToAdd, out Delivery[] __state)
    {
        __state = ReferenceEquals(__instance, Game1.player) && itemsToAdd is not null
            ? itemsToAdd.Select(item => Snapshot(item, AnniversaryId)).OfType<Delivery>().ToArray()
            : Array.Empty<Delivery>();
    }
    private static void AfterQueuedItems(Delivery[] __state)
    {
        foreach (Delivery delivery in __state) Record(delivery, delivery.Entry.Quantity);
    }

    private void Attach(DialogueBox box, ReceiptContext context)
    {
        if (!conversations.ContainsKey(box)) conversations.Add(box, new Transcript(context));
        ObserveBox(box);
    }
    private void Observe()
    {
        if (Game1.activeClickableMenu is DialogueBox box) ObserveBox(box);
        foreach (DialogueBox closed in conversations.Keys.Where(b => !ReferenceEquals(b, Game1.activeClickableMenu)).ToArray())
            conversations.Remove(closed);
    }
    private static void ObserveBox(DialogueBox __instance)
    {
        try
        {
            if (instance is null || !ReferenceEquals(Game1.activeClickableMenu, __instance)
                || __instance.characterDialogue is not { } dialogue || __instance.characterDialoguesBrokenUp.Count == 0) return;
            if (!instance.conversations.TryGetValue(__instance, out Transcript? transcript))
            {
                if (!instance.dialogues.TryGetValue(dialogue, out ReceiptContext? context)) return;
                instance.conversations.Add(__instance, transcript = new Transcript(context));
            }
            if (!instance.CanCapture(transcript.Context) || transcript.Full) return;
            string? page = GiftMessage.Normalize(__instance.getCurrentString());
            if (page is null) return;
            string[] remaining = __instance.characterDialoguesBrokenUp.ToArray();
            if (transcript.Index == dialogue.currentDialogueIndex && transcript.Page == page && transcript.Remaining.SequenceEqual(remaining)) return;
            transcript.Index = dialogue.currentDialogueIndex;
            transcript.Page = page;
            transcript.Remaining = remaining;
            string? text = GiftMessage.Normalize(transcript.Context.Text is null ? page : transcript.Context.Text + "\n\n" + page);
            if (text is null) { transcript.Full = true; return; }
            transcript.Context.Text = text;
            foreach (string id in transcript.Context.ReceiptIds) instance.journal.UpdateMessage(id, text);
        }
        catch (Exception ex) { instance?.Report(ex); }
    }
    private void Report(Exception ex)
    {
        if (reportedError) return;
        reportedError = true;
        monitor.Log($"Couldn't observe a spouse gift: {ex.Message}", LogLevel.Warn);
    }
    private sealed record Delivery(Item Item, GiftEntry Entry, ReceiptContext Context);
    private sealed record ReceiptContext(Farmer Player, string Sender, string Origin, string ModId)
    {
        public Dictionary<Item, GiftEntry> Items { get; } = new(ReferenceEqualityComparer.Instance);
        public HashSet<string> ReceiptIds { get; } = new(StringComparer.Ordinal);
        public string? Text { get; set; }
    }
    private sealed class Transcript
    {
        public Transcript(ReceiptContext context) => Context = context;
        public ReceiptContext Context { get; }
        public int Index { get; set; } = -1;
        public string? Page { get; set; }
        public string[] Remaining { get; set; } = Array.Empty<string>();
        public bool Full { get; set; }
    }
}
