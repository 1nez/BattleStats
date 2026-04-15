using HarmonyLib;
using System;
using System.IO;
using System.Linq;
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

            if (BattleStatsBehavior.CaptureLoggingEnabled)
            {
                BattleStatsBehavior.WriteDiagnostic("SubModule load start.");
                BattleStatsBehavior.WriteDiagnostic("Harmony patch applied.");
                try
                {
                    var patchedScoreboardMethods = Harmony.GetAllPatchedMethods()
                        .Where(m => m.DeclaringType == typeof(TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard.ScoreboardBaseVM))
                        .Where(m => m.Name == "Tick" || m.Name == "OnFinalize" || m.Name == "UpdateQuitText")
                        .Select(m => m.Name)
                        .Distinct()
                        .OrderBy(n => n)
                        .ToArray();

                    BattleStatsBehavior.WriteDiagnostic("Patched ScoreboardBaseVM methods: " + string.Join(", ", patchedScoreboardMethods));
                }
                catch (Exception ex)
                {
                    BattleStatsBehavior.WriteDiagnostic("Failed to inspect patched methods: " + ex.GetType().Name);
                }
            }
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
            if (!File.Exists(SubModule.ConfigFile))
            {
                return;
            }

            try
            {
                using (StreamReader streamReader = new StreamReader(SubModule.ConfigFile))
                {
                    string configLine;
                    while ((configLine = streamReader.ReadLine()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(configLine))
                        {
                            continue;
                        }

                        string trimmedLine = configLine.Trim();
                        if (trimmedLine.StartsWith("#") || trimmedLine.StartsWith(";") || trimmedLine.StartsWith("//"))
                        {
                            continue;
                        }

                        string[] parts = trimmedLine.Split(new[] { '=' }, 2, StringSplitOptions.None);
                        if (parts.Length != 2)
                        {
                            continue;
                        }

                        string setting = parts[0].Trim();
                        string value = parts[1].Trim();

                        if (setting.Equals("hotkey", StringComparison.OrdinalIgnoreCase))
                        {
                            if (Enum.TryParse<InputKey>(value, true, out InputKey inputKey) &&
                                (Enum.IsDefined(typeof(InputKey), inputKey) || value.Contains(",")))
                            {
                                MenuSetup.hotkey = inputKey;
                            }
                        }
                        else if (setting.Equals("changetextformat", StringComparison.OrdinalIgnoreCase) &&
                                 bool.TryParse(value, out bool changeTextFormat))
                        {
                            MenuSetup.changeFormat = changeTextFormat;
                        }
                        else if (setting.Equals("maxtrackedheroes", StringComparison.OrdinalIgnoreCase) &&
                                 int.TryParse(value, out int maxTrackedHeroes) &&
                                 maxTrackedHeroes > 0)
                        {
                            BattleStatsBehavior.SetMaxTrackedHeroes(maxTrackedHeroes);
                        }
                    }
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
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

        public static readonly string ConfigFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "config.txt");
    }
}
