using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
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
            StreamReader streamReader = new StreamReader(SubModule.ConfigFile);
            string configLine;
            while ((configLine = streamReader.ReadLine()) != null)
            {
                int num = configLine.IndexOf('=');

                if (num != -1)
                {
                    string setting = configLine.Substring(0, num - 1);
                    string value = configLine.Substring(num + 2);

                    if (setting.ToLower().Equals("hotkey"))
                    {
                        if (Enum.TryParse<InputKey>(value, true, out InputKey inputKey) && (Enum.IsDefined(typeof(InputKey), inputKey) | inputKey.ToString().Contains(",")))
                        {
                            MenuSetup.hotkey = inputKey;
                        }
                    }
                }

                if (configLine.ToLower().Equals("changetextformat = true"))
                {
                    MenuSetup.changeFormat = true;
                }
            }
            streamReader.Close();
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

        public static readonly string ConfigFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.txt");
    }
}