using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace DearlyKept;

/// <summary>Records Happy Birthday gifts at delivery, without modifying their items.</summary>
internal sealed class HappyBirthdayIntegration
{
    public const string ModId = "Omegasis.HappyBirthday";
    private const string SupportedVersion = "3.21.4";
    private const string HarmonyId = "Nana1873.DearlyKept.HappyBirthday";
    private static HappyBirthdayIntegration? instance;
    [ThreadStatic] private static SenderContext? currentSender;

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly GiftJournal journal;
    private readonly Func<bool> enabled;
    private readonly Dictionary<object, PendingGift> pending = new(ReferenceEqualityComparer.Instance);
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
            if (coreInstanceField?.FieldType != coreType || managerField?.FieldType != managerType
                || giftField?.FieldType != typeof(Item)
                || namedSetter is null || namedSetter.IsStatic || namedSetter.ReturnType != typeof(void)
                || itemSetter is null || itemSetter.IsStatic || itemSetter.ReturnType != typeof(void)
                || close is null || !close.IsStatic || close.ReturnType != typeof(void))
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
        currentSender = null;
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
                integration.journal.Record(__state.Entry);
            else if (ReferenceEquals(integration.giftField!.GetValue(__instance), __state.Item))
                integration.pending[__instance] = new PendingGift(__state.Item, __state.Entry, __state.Player);
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
                integration.journal.Record(gift.Entry);
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
    private sealed record PendingGift(Item Item, GiftEntry Entry, Farmer Player);
    private sealed record ItemDelivery(Item Item, GiftEntry Entry, Farmer Player, GameLocation Location, HashSet<Debris> PreviousDebris);
}
