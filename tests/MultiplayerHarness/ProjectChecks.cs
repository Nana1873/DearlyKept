using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace DearlyKept.MultiplayerHarness;

internal sealed partial class ModEntry
{
    private int projectDueDay;
    private void ProjectCheck(string action)
    {
        NoMenu();
        object mod = ModInstance("TitanmasterRy.MarriageOverhaul");
        object config = mod.GetType().GetProperty("Config")!.GetValue(mod)!;
        object data = mod.GetType().GetProperty("Data")!.GetValue(mod)!;
        if (action == "prepare")
        {
            giftSender = Context.IsMainPlayer ? "Emily" : "Sebastian";
            Game1.player.spouse = giftSender;
            Game1.player.friendshipData[giftSender] = new Friendship(3000)
            {
                Status = FriendshipStatus.Married,
                WeddingDate = new WorldDate(Game1.Date) { TotalDays = Math.Max(0, Game1.Date.TotalDays - 20) }
            };
            Game1.player.HouseUpgradeLevel = 1;
            ((FarmHouse)Utility.getHomeOfFarmer(Game1.player)).setMapForUpgradeLevel(1);
            Property(config, "EnableSpouseRequests", true);
            Property(config, "EnableSpouseRequestQuest", true);
            Property(config, "EnableBirthdaySystem", false);
            Property(config, "EnableMilestones", false);
            Property(data, "PendingRewardItem", "");
            Property(data, "RequestActive", true);
            Property(data, "RequestId", Context.IsMainPlayer ? "emily_cloth" : "seb_part");
            Property(data, "RequestStartDay", Game1.Date.TotalDays + 1);
            Property(data, "LastRequestDay", Game1.Date.TotalDays);
            Game1.player.CurrentToolIndex = 0;
            Game1.player.Items[0] = ItemRegistry.Create(Context.IsMainPlayer ? "(O)428" : "(O)338");
            giftBefore = null;
            Monitor.Log("DKPROJECT prepared an original authored request and its ordinary required item. Sleep to create the real tracked quest, then project give.", LogLevel.Info);
        }
        else if (action == "give")
        {
            Check(Game1.player.questLog.Any(q => q.id.Value == "MO.SpouseRequest"), "original day-start handler created the tracked spouse request");
            NPC spouse = Game1.getCharacterFromName(giftSender);
            Check(spouse.tryToReceiveActiveObject(Game1.player), "native NPC interaction accepted the requested gift");
            Check(Game1.player.Items[0] is null, "native gift interaction consumed the real required item");
            Check(!(bool)data.GetType().GetProperty("RequestActive")!.GetValue(data)!, "the original request completion handler cleared the active request");
            projectDueDay = (int)data.GetType().GetProperty("PendingRewardDay")!.GetValue(data)!;
            Check(projectDueDay == Game1.Date.TotalDays + 3, "the original producer scheduled its reward three days later");
            Monitor.Log("DKPROJECT real request fulfilled; close the response, project wait, then sleep three normal days.", LogLevel.Info);
        }
        else if (action == "wait")
        {
            giftBefore = Journal; giftItems = Quantities(); giftPages.Clear(); giftPageKey = null;
            giftProvider = "TitanmasterRy.MarriageOverhaul"; giftOrigin = "spouse";
        }
        else if (action == "assert")
        {
            Check(Game1.Date.TotalDays >= projectDueDay, "three normal days elapsed before reward acceptance");
            AssertGift();
            Monitor.Log("DKPROJECT PASS: original quest, native item handover, real delay, original reward and archived text.", LogLevel.Info);
        }
        else throw new InvalidOperationException("Use project prepare|give|wait|assert.");
    }
}
