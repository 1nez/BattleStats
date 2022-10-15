using HarmonyLib;
using System;
using System.IO;
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
            LoadConfig();
            Harmony harmony = new Harmony("onez.battlestats");
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

        private static void LoadConfig()
        {
            if (File.ReadAllText(ConfigFile).ToLower().Equals("changetextformat = true"))
            {
                MenuSetup.changeFormat = true;
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

        private static readonly string ConfigFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.txt");
    }
}