using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BattleStats
{
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            Harmony harmony = new Harmony("jzeno9.battlestats");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            if (game.GameType is Campaign)
            {
                CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, new Action(SaveLoadRecords.SaveRecords));
                CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(SaveLoadRecords.LoadRecords));
            }
        }

        protected override void OnApplicationTick(float dt)
        {
            base.OnApplicationTick(dt);
            bool flag = Campaign.Current != null;
            if (flag)
            {
                MenuSetup.ShowMenu();
            }
        }
    }
}