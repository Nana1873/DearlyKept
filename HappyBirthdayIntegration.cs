using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace DearlyKept;

/// <summary>Records Happy Birthday gifts at delivery, without modifying their items.</summary>
internal sealed class HappyBirthdayIntegration
{
    public const string ModId = "Omegasis.HappyBirthday";
    private const string SupportedVersion = "3.21.4";
    private const string HarmonyId = "Nana1873.DearlyKept.HappyBirthday";
    private static HappyBirthdayIntegration? instance;
    [ThreadStatic] private static SenderContext? currentSender;
    [ThreadStatic] private static GreetingContext? currentGreeting;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly GiftJournal journal;
    private readonly Func<bool> enabled;
    private readonly PerScreen<Dictionary<object, PendingGift>> screenPending = new(() => new(ReferenceEqualityComparer.Instance));
    private readonly PerScreen<Dictionary<DialogueBox, Conversation>> screenConversations = new(() => new(ReferenceEqualityComparer.Instance));
    private Dictionary<object, PendingGift> pending => screenPending.Value;
    private Dictionary<DialogueBox, Conversation> conversations => screenConversations.Value;
    private FieldInfo? coreInstanceField;
    private FieldInfo? managerField;
    private FieldInfo? giftField;
    private bool registered;
    private bool reportedError;

    public bool IsActive { get; private set; }

    public HappyBirthdayIntegration(IModHelper helper, IMonitor monitor, GiftJournal journal, Func<bool> enabled)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.journal = journal;
        this.enabled = enabled;
    }

    public void Register()
    {
        if (registered)
            return;
        registered = true;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => ClearPending();
        helper.Events.GameLoop.UpdateTicked += (_, _) => ObserveActiveConversation();
        helper.Events.Display.MenuChanged += OnMenuChanged;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        ClearPending();
        IModInfo? mod = helper.ModRegistry.Get(ModId);
        if (mod is null)
            return;
        if (mod.Manifest.Version.ToString() != SupportedVersion)
        {
            monitor.Log($"Happy Birthday {mod.Manifest.Version} is installed; birthday recording supports {SupportedVersion} and stays disabled for this version.", LogLevel.Warn);
            return;
        }

        var harmony = new Harmony(HarmonyId);
        try
        {
            Type? coreType = AccessTools.TypeByName("Omegasis.HappyBirthday.HappyBirthdayModCore");
            Type? managerType = AccessTools.TypeByName("Omegasis.HappyBirthday.GiftManager");
            Type? menuType = AccessTools.TypeByName("Omegasis.HappyBirthday.Framework.Utilities.MenuUtilities");
            if (coreType is null || managerType is null || menuType is null
                || coreType.Assembly.GetName().Name != "HappyBirthday"
                || managerType.Assembly != coreType.Assembly || menuType.Assembly != coreType.Assembly)
                throw new InvalidOperationException("Expected Happy Birthday types are unavailable.");

            coreInstanceField = coreType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
            managerField = coreType.GetField("giftManager", BindingFlags.Public | BindingFlags.Instance);
            giftField = managerType.GetField("BirthdayGiftToReceive", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo? namedSetter = AccessTools.DeclaredMethod(managerType, "setNextBirthdayGift", new[] { typeof(string) });
            MethodInfo? itemSetter = AccessTools.DeclaredMethod(managerType, "setNextBirthdayGift", new[] { typeof(Item) });
            MethodInfo? close = AccessTools.DeclaredMethod(menuType, "OnActiveMenuChangedToNull", new[] { typeof(MenuChangedEventArgs) });
            MethodInfo? greeting = AccessTools.DeclaredMethod(menuType, "OnMenuChangedToDialogueBox", Type.EmptyTypes);
            if (coreInstanceField?.FieldType != coreType || managerField?.FieldType != managerType
                || giftField?.FieldType != typeof(Item)
                || namedSetter is null || namedSetter.IsStatic || namedSetter.ReturnType != typeof(void)
                || itemSetter is null || itemSetter.IsStatic || itemSetter.ReturnType != typeof(void)
                || close is null || !close.IsStatic || close.ReturnType != typeof(void)
                || greeting is null || !greeting.IsStatic || greeting.ReturnType != typeof(void))
                throw new InvalidOperationException("Expected Happy Birthday delivery signatures are unavailable.");

            instance = this;
            harmony.Patch(namedSetter,
                prefix: new HarmonyMethod(typeof(HappyBirthdayIntegration), nameof(BeginSender)),
                finalizer: new HarmonyMethod(typeof(HappyBirthdayIntegration), nameof(EndSender)));
            harmony.Patch(itemSetter,
                prefix: new HarmonyMethod(typeof(HappyBirthdayIntegration), nameof(BeforeSetItem)),
                postfix: new HarmonyMethod(typeof(HappyBirthdayIntegration), nameof(AfterSetItem)));
            harmony.Patch(close,
                prefix: new HarmonyMethod(typeof(HappyBirthdayIntegration), nameof(BeforeClose)),
                postfix: new HarmonyMethod(typeof(HappyBirthdayIntegration), nameof(AfterClose)));
            harmony.Patch(greeting,
                prefix: new HarmonyMethod(typeof(HappyBirthdayIntegration), nameof(BeginGreeting)),
                finalizer: new HarmonyMethod(typeof(HappyBirthdayIntegration), nameof(EndGreeting)));
            // Observe only a dialogue already associated with a real delivery, before it advances.
            harmony.Patch(AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.receiveLeftClick)),
                prefix: new HarmonyMethod(typeof(HappyBirthdayIntegration), nameof(ObserveDialogue)),
                postfix: new HarmonyMethod(typeof(HappyBirthdayIntegration), nameof(ObserveDialogue)));
            harmony.Patch(AccessTools.Method(typeof(DialogueBox), nameof(DialogueBox.closeDialogue)),
                prefix: new HarmonyMethod(typeof(HappyBirthdayIntegration), nameof(ObserveDialogue)));
            IsActive = true;
            monitor.Log($"Happy Birthday {SupportedVersion} gift journal integration is active.", LogLevel.Debug);
        }
        catch (Exception ex)
        {
            IsActive = false;
            harmony.UnpatchAll(HarmonyId);
            Report(ex);
        }
    }

    private bool CanCapture => IsActive && enabled() && journal.CanRecord;

    private object? CurrentManager()
    {
        object? core = coreInstanceField?.GetValue(null);
        return core is null ? null : managerField?.GetValue(core);
    }

    private void ClearPending()
    {
        pending.Clear();
        conversations.Clear();
        currentSender = null;
        currentGreeting = null;
    }

    private static void BeginGreeting(out GreetingContext? __state)
    {
        __state = null;
        try
        {
            if (instance?.CanCapture == true && Game1.currentSpeaker is { } speaker)
            {
                __state = new GreetingContext(currentGreeting, Game1.player, speaker.Name, Game1.activeClickableMenu);
                currentGreeting = __state;
            }
        }
        catch (Exception ex) { instance?.Report(ex); }
    }

    private static Exception? EndGreeting(Exception? __exception, GreetingContext? __state)
    {
        if (__state is null)
            return __exception;
        try
        {
            HappyBirthdayIntegration? integration = instance;
            if (integration?.CanCapture != true || !ReferenceEquals(__state.Player, Game1.player))
                return __exception;

            Conversation? conversation = null;
            if (__exception is null && Game1.activeClickableMenu is DialogueBox box
                && !ReferenceEquals(box, __state.PreviousMenu)
                && box.characterDialogue?.speaker?.Name == __state.SenderId && __state.Deliveries.Count > 0)
            {
                conversation = new Conversation(box);
                integration.conversations[box] = conversation;
                try { integration.Observe(conversation); }
                catch (Exception ex) { integration.Report(ex); }
            }

            foreach (GreetingDelivery delivery in __state.Deliveries)
            {
                if (delivery.Dropped)
                {
                    GiftEntry entry = delivery.Entry with { MessageText = conversation?.Text };
                    integration.journal.Record(entry);
                    conversation?.ReceiptIds.Add(entry.Id);
                }
                else if (integration.pending.TryGetValue(delivery.Manager, out PendingGift? gift)
                    && gift.Entry.Id == delivery.Entry.Id)
                {
                    integration.pending[delivery.Manager] = gift with { Conversation = conversation };
                }
            }
        }
        catch (Exception ex) { instance?.Report(ex); }
        finally { currentGreeting = __state.Previous; }
        return __exception;
    }

    private static void ObserveDialogue(DialogueBox __instance)
    {
        try
        {
            if (instance?.CanCapture == true && ReferenceEquals(Game1.activeClickableMenu, __instance)
                && instance.conversations.TryGetValue(__instance, out Conversation? conversation))
                instance.Observe(conversation);
        }
        catch (Exception ex) { instance?.Report(ex); }
    }

    private void ObserveActiveConversation()
    {
        if (Game1.activeClickableMenu is DialogueBox box)
            ObserveDialogue(box);
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.OldMenu is DialogueBox old && !ReferenceEquals(old, Game1.activeClickableMenu))
            conversations.Remove(old);
    }

    private void Observe(Conversation conversation)
    {
        DialogueBox box = conversation.Box;
        if (conversation.AtLengthLimit || box.characterDialogue is null || box.characterDialoguesBrokenUp.Count == 0)
            return;
        string? page = GiftMessage.Normalize(box.getCurrentString());
        if (page is null)
            return;
        int index = box.characterDialogue.currentDialogueIndex;
        string[] remaining = box.characterDialoguesBrokenUp.ToArray();
        if (conversation.LastIndex == index && conversation.LastPage == page
            && conversation.Remaining.SequenceEqual(remaining, StringComparer.Ordinal))
            return;
        conversation.LastIndex = index;
        conversation.LastPage = page;
        conversation.Remaining = remaining;
        string? transcript = GiftMessage.Normalize(conversation.Text is null ? page : conversation.Text + "\n\n" + page);
        if (transcript is null)
        {
            conversation.AtLengthLimit = true;
            return;
        }
        conversation.Text = transcript;
        foreach (string id in conversation.ReceiptIds)
            journal.UpdateMessage(id, transcript);
    }

    private static void BeginSender(object __instance, string name, out SenderContext? __state)
    {
        __state = currentSender;
        currentSender = null;
        try
        {
            if (instance?.CanCapture == true && ReferenceEquals(instance.CurrentManager(), __instance)
                && Game1.getCharacterFromName(name, false) is { } npc && npc.Name == name)
                currentSender = new SenderContext(__instance, name, Game1.player);
        }
        catch (Exception ex) { instance?.Report(ex); }
    }

    private static Exception? EndSender(Exception? __exception, SenderContext? __state)
    {
        currentSender = __state;
        return __exception;
    }

    private static void BeforeSetItem(object __instance, Item gift, out ItemDelivery? __state)
    {
        __state = null;
        try
        {
            HappyBirthdayIntegration? integration = instance;
            if (integration?.CanCapture != true || currentSender is not { } context
                || !ReferenceEquals(context.Manager, __instance) || !ReferenceEquals(context.Player, Game1.player)
                || gift is null || gift.HasBeenInInventory || Game1.player.Items.Any(item => ReferenceEquals(item, gift)))
                return;
            GiftEntry? entry = integration.journal.CreateEntry(gift, context.SenderId,
                "birthday:" + context.SenderId, "birthday", ModId);
            if (entry is not null)
                __state = new ItemDelivery(gift, entry, context.Player, Game1.currentLocation,
                    new HashSet<Debris>(Game1.currentLocation.debris, ReferenceEqualityComparer.Instance));
        }
        catch (Exception ex) { instance?.Report(ex); }
    }

    private static void AfterSetItem(object __instance, ItemDelivery? __state)
    {
        try
        {
            HappyBirthdayIntegration? integration = instance;
            if (integration?.CanCapture != true || __state is null || !ReferenceEquals(__state.Player, Game1.player))
                return;

            // A full backpack makes Happy Birthday provide the gift as debris immediately.
            bool dropped = __state.Location.debris.Any(debris => !__state.PreviousDebris.Contains(debris)
                && ReferenceEquals(debris.item, __state.Item));
            if (dropped)
            {
                if (currentGreeting is { } greeting && ReferenceEquals(greeting.Player, __state.Player)
                    && greeting.SenderId == __state.Entry.SenderId)
                    greeting.Deliveries.Add(new GreetingDelivery(__instance, __state.Entry, true));
                else
                    integration.journal.Record(__state.Entry);
            }
            else if (ReferenceEquals(integration.giftField!.GetValue(__instance), __state.Item))
            {
                // A close-time reroll retains the conversation which accompanied this gift.
                Conversation? conversation = integration.pending.TryGetValue(__instance, out PendingGift? previous)
                    && ReferenceEquals(previous.Player, __state.Player) && previous.Entry.SenderId == __state.Entry.SenderId
                    ? previous.Conversation : null;
                integration.pending[__instance] = new PendingGift(__state.Item, __state.Entry, __state.Player, conversation);
                if (currentGreeting is { } greeting && ReferenceEquals(greeting.Player, __state.Player)
                    && greeting.SenderId == __state.Entry.SenderId)
                    greeting.Deliveries.Add(new GreetingDelivery(__instance, __state.Entry, false));
            }
        }
        catch (Exception ex) { instance?.Report(ex); }
    }

    private static void BeforeClose(out object? __state)
    {
        __state = null;
        try
        {
            HappyBirthdayIntegration? integration = instance;
            if (integration?.CanCapture != true)
                return;
            // The manager is initialized by Happy Birthday after GameLaunched, so resolve it here.
            object? manager = integration.CurrentManager();
            if (manager is not null && integration.pending.TryGetValue(manager, out PendingGift? gift)
                && ReferenceEquals(gift.Player, Game1.player)
                && ReferenceEquals(integration.giftField!.GetValue(manager), gift.Item))
                __state = manager;
        }
        catch (Exception ex) { instance?.Report(ex); }
    }

    private static void AfterClose(object? __state)
    {
        try
        {
            HappyBirthdayIntegration? integration = instance;
            if (integration is null || __state is null
                || integration.giftField!.GetValue(__state) is not null
                || !integration.pending.Remove(__state, out PendingGift? gift))
                return;

            // Successful close clears the queued reference only after the game's handoff.
            // A rejected gift may have been rerolled during close; use the newest tracked snapshot.
            if (integration.CanCapture && ReferenceEquals(gift.Player, Game1.player))
                integration.journal.Record(gift.Entry with { MessageText = gift.Conversation?.Text });
        }
        catch (Exception ex) { instance?.Report(ex); }
    }

    private void Report(Exception ex)
    {
        if (reportedError)
            return;
        reportedError = true;
        try
        {
            monitor.Log($"Couldn't record a Happy Birthday gift; its normal delivery is unchanged: {ex.Message}", LogLevel.Warn);
        }
        catch { /* A journal diagnostic must not interrupt the other mod's delivery. */ }
    }

    private sealed record SenderContext(object Manager, string SenderId, Farmer Player);
    private sealed record PendingGift(Item Item, GiftEntry Entry, Farmer Player, Conversation? Conversation);
    private sealed record ItemDelivery(Item Item, GiftEntry Entry, Farmer Player, GameLocation Location, HashSet<Debris> PreviousDebris);
    private sealed record GreetingDelivery(object Manager, GiftEntry Entry, bool Dropped);
    private sealed record GreetingContext(GreetingContext? Previous, Farmer Player, string SenderId, IClickableMenu? PreviousMenu)
    {
        public List<GreetingDelivery> Deliveries { get; } = new();
    }

    private sealed class Conversation
    {
        public DialogueBox Box { get; }
        public Conversation(DialogueBox box) => Box = box;
        public HashSet<string> ReceiptIds { get; } = new(StringComparer.Ordinal);
        public string? Text { get; set; }
        public bool AtLengthLimit { get; set; }
        public int LastIndex { get; set; } = -1;
        public string? LastPage { get; set; }
        public string[] Remaining { get; set; } = Array.Empty<string>();
    }
}
