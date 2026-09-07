using System;
using HarmonyLib;
using StardewValley;
using StardewValley.Menus;

namespace DearlyKept;

internal static class GiftPatches
{
    private static Func<GiftStamp, string> formatNote = null!;
    private static Action<IClickableMenu> captureMenu = null!;

    public static void Apply(string uniqueId, Func<GiftStamp, string> formatNote, Action<IClickableMenu> captureMenu)
    {
        GiftPatches.formatNote = formatNote;
        GiftPatches.captureMenu = captureMenu;
        Harmony harmony = new(uniqueId);

        harmony.Patch(
            AccessTools.Method(typeof(Item), nameof(Item.canStackWith), new[] { typeof(ISalable) }),
            postfix: new HarmonyMethod(typeof(GiftPatches), nameof(AfterCanStackWith)));

        harmony.Patch(
            AccessTools.Method(typeof(IClickableMenu), nameof(IClickableMenu.drawToolTip)),
            prefix: new HarmonyMethod(typeof(GiftPatches), nameof(BeforeDrawToolTip)));

        harmony.Patch(
            AccessTools.Constructor(typeof(LetterViewerMenu), new[] { typeof(string), typeof(string), typeof(bool) }),
            postfix: new HarmonyMethod(typeof(GiftPatches), nameof(CaptureLetter)));

        harmony.Patch(
            AccessTools.Method(typeof(LetterViewerMenu), nameof(LetterViewerMenu.receiveLeftClick)),
            prefix: new HarmonyMethod(typeof(GiftPatches), nameof(CaptureLetter)));

        harmony.Patch(
            AccessTools.Method(typeof(LetterViewerMenu), "cleanupBeforeExit"),
            prefix: new HarmonyMethod(typeof(GiftPatches), nameof(CaptureLetter)));
    }

    private static void AfterCanStackWith(Item __instance, ISalable other, ref bool __result)
    {
        if (__result && other is Item item && !GiftTagService.CanMerge(__instance, item))
            __result = false;
    }

    private static void BeforeDrawToolTip(Item hoveredItem, ref string hoverText)
    {
        if (hoveredItem is null || !GiftTagService.TryRead(hoveredItem, out GiftStamp stamp))
            return;

        string note = formatNote(stamp);
        if (string.IsNullOrWhiteSpace(note))
            return;

        hoverText = string.IsNullOrWhiteSpace(hoverText)
            ? note
            : hoverText + Environment.NewLine + Environment.NewLine + note;
    }

    private static void CaptureLetter(LetterViewerMenu __instance)
    {
        captureMenu(__instance);
    }
}
