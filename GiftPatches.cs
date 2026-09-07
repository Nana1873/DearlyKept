using HarmonyLib;
using StardewValley.Menus;

namespace DearlyKept;

internal static class GiftPatches
{
    private static GiftCapture capture = null!;

    public static void Apply(string uniqueId, GiftCapture capture)
    {
        GiftPatches.capture = capture;
        Harmony harmony = new(uniqueId);
        foreach (string method in new[] { nameof(LetterViewerMenu.receiveLeftClick), "cleanupBeforeExit" })
        {
            harmony.Patch(AccessTools.Method(typeof(LetterViewerMenu), method),
                prefix: new HarmonyMethod(typeof(GiftPatches), nameof(BeforeTransfer)),
                postfix: new HarmonyMethod(typeof(GiftPatches), nameof(AfterTransfer)));
        }
    }

    private static void BeforeTransfer(LetterViewerMenu __instance, out GiftCapture.LetterReceipt? __state)
        => __state = capture.Begin(__instance);

    private static void AfterTransfer(LetterViewerMenu __instance, GiftCapture.LetterReceipt? __state)
        => capture.Complete(__instance, __state);
}
