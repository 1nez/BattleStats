using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

namespace BattleStats
{
    [HarmonyPatch(typeof(ScoreboardBaseVM), "UpdateQuitText")]
    public static class BattleStatsPatch_UpdateQuitText
    {
        [HarmonyPostfix]
        public static void PostFix(ScoreboardBaseVM __instance)
        {
            BattleStatsBehavior.HandleScoreboardEvent(__instance, "UpdateQuitText");
        }
    }

    [HarmonyPatch(typeof(ScoreboardBaseVM), "OnFinalize")]
    public static class BattleStatsPatch_OnFinalize
    {
        [HarmonyPostfix]
        public static void PostFix(ScoreboardBaseVM __instance)
        {
            BattleStatsBehavior.HandleScoreboardEvent(__instance, "OnFinalize");
        }
    }

    [HarmonyPatch(typeof(ScoreboardBaseVM), "Tick")]
    public static class BattleStatsPatch_Tick
    {
        [HarmonyPostfix]
        public static void PostFix(ScoreboardBaseVM __instance)
        {
            BattleStatsBehavior.HandleScoreboardEvent(__instance, "Tick");
        }
    }

    public class BattleStatsBehavior
    {
        public static readonly bool CaptureLoggingEnabled = false;
        public static Dictionary<int, HeroRecords> heroRecords = new Dictionary<int, HeroRecords>();
        public static Dictionary<int, ArmyRecords> armyRecords = new Dictionary<int, ArmyRecords>();
        public static Dictionary<int, int> statDiff = new Dictionary<int, int>();

        private static readonly string CaptureLogFallbackFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Mount and Blade II Bannerlord",
            "Configs",
            "BattleStats_capture.log");

        private static readonly string CaptureLogFile = ResolveCaptureLogFile();
        private static ScoreboardBaseVM lastCapturedScoreboard;
        private static readonly List<ScoreboardBaseVM> seenScoreboards = new List<ScoreboardBaseVM>();

        public static void WriteDiagnostic(string message)
        {
            LogCapture(message);
        }

        public static void HandleScoreboardEvent(ScoreboardBaseVM __instance, string source)
        {
            if (__instance == null)
            {
                LogCapture("Scoreboard event fired with null instance. Source=" + source);
                return;
            }

            int scoreboardId = GetScoreboardInstanceLabel(__instance, out bool firstSeen);
            if (firstSeen || !string.Equals(source, "Tick", StringComparison.Ordinal))
            {
                LogCapture("Scoreboard event. Source=" + source +
                    ", IsOver=" + __instance.IsOver +
                    ", ShowScoreboard=" + __instance.ShowScoreboard +
                    ", AttackerParties=" + GetPartyCount(__instance.Attackers) +
                    ", DefenderParties=" + GetPartyCount(__instance.Defenders) +
                    ", ScoreboardId=" + scoreboardId);
            }

            if (object.ReferenceEquals(__instance, lastCapturedScoreboard))
            {
                return;
            }

            bool readyToCapture = __instance.IsOver ||
                string.Equals(source, "UpdateQuitText", StringComparison.Ordinal) ||
                string.Equals(source, "OnFinalize", StringComparison.Ordinal);

            if (!readyToCapture)
            {
                if (firstSeen)
                {
                    LogCapture("Capture deferred. Scoreboard not ready yet for ScoreboardId=" + scoreboardId);
                }

                return;
            }

            statDiff.Clear();
            MenuSetup.openCount = 0;

            LogCapture("Capture starting. Source=" + source + ", ScoreboardId=" + scoreboardId);
            bool captureCompleted = GetStatsFromBattle(__instance);
            if (captureCompleted)
            {
                lastCapturedScoreboard = __instance;
                LogCapture("Capture completed. Source=" + source + ", ScoreboardId=" + scoreboardId);
            }
            else
            {
                LogCapture("Capture failed. Source=" + source + ", ScoreboardId=" + scoreboardId);
            }
        }

        private static bool GetStatsFromBattle(ScoreboardBaseVM scoreboard)
        {
            List<SPScoreboardUnitVM> clanHeroes = new List<SPScoreboardUnitVM>();
            List<SPScoreboardUnitVM> infantry = new List<SPScoreboardUnitVM>();
            List<SPScoreboardUnitVM> ranged = new List<SPScoreboardUnitVM>();
            List<SPScoreboardUnitVM> cavalry = new List<SPScoreboardUnitVM>();
            List<SPScoreboardUnitVM> horseArchers = new List<SPScoreboardUnitVM>();
            Dictionary<string, List<SPScoreboardUnitVM>> formations = new Dictionary<string, List<SPScoreboardUnitVM>>();
            int enemyKills = 0;
            int allyCasualties = 0;

            SPScoreboardSideVM playerSideVm;
            SPScoreboardSideVM enemySideVm;
            BattleSideEnum playerSide;
            if (!TryResolveScoreboardSides(scoreboard, out playerSideVm, out enemySideVm, out playerSide))
            {
                LogCapture("Capture aborted: player side unresolved.");
                return false;
            }

            int sidePartyCount = (playerSideVm != null && playerSideVm.Parties != null) ? playerSideVm.Parties.Count : 0;
            LogCapture("PlayerSide=" + playerSide + ", SidePartyCount=" + sidePartyCount);

            SPScoreboardPartyVM playerParty;
            if (playerSideVm == null || playerSideVm.Parties == null || !TryGetPlayerScoreboardParty(playerSideVm.Parties, out playerParty))
            {
                LogCapture("Capture aborted: player scoreboard party not found.");
                return false;
            }

            enemyKills = (enemySideVm != null && enemySideVm.Score != null) ? enemySideVm.Score.Kill : 0;

            foreach (SPScoreboardPartyVM party in playerSideVm.Parties)
            {
                if (party == null || object.ReferenceEquals(party, playerParty) || party.Score == null)
                {
                    continue;
                }

                allyCasualties += party.Score.Dead + party.Score.Wounded;
            }

            int playerPartyMemberCount = playerParty.Members != null ? playerParty.Members.Count : 0;
            LogCapture("PlayerPartyFound=true, MemberCount=" + playerPartyMemberCount + ", EnemyKills=" + enemyKills + ", AllyCasualties=" + allyCasualties);

            if (playerParty.Members != null)
            {
                foreach (SPScoreboardUnitVM troop in playerParty.Members)
                {
                    if (troop == null)
                    {
                        continue;
                    }

                    if (troop.IsHero)
                    {
                        clanHeroes.Add(troop);
                    }
                    else if (troop.Character != null)
                    {
                        if (troop.Character.IsMounted && troop.Character.IsRanged)
                        {
                            horseArchers.Add(troop);
                        }
                        else if (troop.Character.IsMounted && !troop.Character.IsRanged)
                        {
                            cavalry.Add(troop);
                        }
                        else if (troop.Character.IsRanged && !troop.Character.IsMounted)
                        {
                            ranged.Add(troop);
                        }
                        else if (troop.Character.IsInfantry)
                        {
                            infantry.Add(troop);
                        }
                    }
                }
            }

            if (!infantry.IsEmpty())
            {
                formations.Add("Infantry", infantry);
            }
            if (!ranged.IsEmpty())
            {
                formations.Add("Ranged", ranged);
            }
            if (!cavalry.IsEmpty())
            {
                formations.Add("Cavalry", cavalry);
            }
            if (!horseArchers.IsEmpty())
            {
                formations.Add("Horse Archers", horseArchers);
            }

            bool heroRecordsUpdated = false;
            bool armyRecordsUpdated = false;

            if (!clanHeroes.IsEmpty())
            {
                UpdateHeroRecords(clanHeroes);
                heroRecordsUpdated = true;
            }
            if (!formations.IsEmpty())
            {
                UpdateArmyRecords(formations, enemyKills, allyCasualties);
                armyRecordsUpdated = true;
            }

            LogCapture("CaptureResult: Heroes=" + clanHeroes.Count +
                ", Infantry=" + infantry.Count +
                ", Ranged=" + ranged.Count +
                ", Cavalry=" + cavalry.Count +
                ", HorseArchers=" + horseArchers.Count +
                ", HeroRecordsUpdated=" + heroRecordsUpdated +
                ", ArmyRecordsUpdated=" + armyRecordsUpdated +
                ", HeroRecordsTotal=" + heroRecords.Count +
                ", ArmyRecordsTotal=" + armyRecords.Count);

            return true;
        }

        private static bool TryResolveScoreboardSides(ScoreboardBaseVM scoreboard, out SPScoreboardSideVM playerSideVm, out SPScoreboardSideVM enemySideVm, out BattleSideEnum playerSide)
        {
            playerSideVm = null;
            enemySideVm = null;
            playerSide = BattleSideEnum.None;

            if (scoreboard == null || PartyBase.MainParty == null)
            {
                return false;
            }

            playerSide = PartyBase.MainParty.Side;
            if (playerSide == BattleSideEnum.Attacker)
            {
                playerSideVm = scoreboard.Attackers;
                enemySideVm = scoreboard.Defenders;
                return true;
            }

            if (playerSide == BattleSideEnum.Defender)
            {
                playerSideVm = scoreboard.Defenders;
                enemySideVm = scoreboard.Attackers;
                return true;
            }

            return false;
        }

        private static bool TryGetPlayerScoreboardParty(IEnumerable<SPScoreboardPartyVM> parties, out SPScoreboardPartyVM playerParty)
        {
            playerParty = null;
            if (parties == null)
            {
                return false;
            }

            PartyBase mainParty = PartyBase.MainParty;
            Hero mainHero = Hero.MainHero;
            Clan mainClan = mainHero != null ? mainHero.Clan : null;

            // Prefer direct party identity checks when available.
            foreach (SPScoreboardPartyVM party in parties)
            {
                PartyBase partyBase = party != null ? party.BattleCombatant as PartyBase : null;
                if (partyBase == null || mainParty == null)
                {
                    continue;
                }

                if (object.ReferenceEquals(partyBase, mainParty))
                {
                    playerParty = party;
                    return true;
                }

                if (partyBase.MobileParty != null && partyBase.MobileParty.IsMainParty)
                {
                    playerParty = party;
                    return true;
                }

                if (mainParty.MobileParty != null && object.ReferenceEquals(partyBase.MobileParty, mainParty.MobileParty))
                {
                    playerParty = party;
                    return true;
                }
            }

            // Fallback to hero/clan ownership when direct identity isn't exposed.
            foreach (SPScoreboardPartyVM party in parties)
            {
                PartyBase partyBase = party != null ? party.BattleCombatant as PartyBase : null;
                if (partyBase == null)
                {
                    continue;
                }

                if (mainHero != null && (partyBase.LeaderHero == mainHero || partyBase.Owner == mainHero))
                {
                    playerParty = party;
                    return true;
                }

                if (mainClan != null &&
                    ((partyBase.LeaderHero != null && partyBase.LeaderHero.Clan == mainClan) ||
                     (partyBase.Owner != null && partyBase.Owner.Clan == mainClan)))
                {
                    playerParty = party;
                    return true;
                }
            }

            // Semantic fallback by faction + party name.
            foreach (SPScoreboardPartyVM party in parties)
            {
                PartyBase partyBase = party != null ? party.BattleCombatant as PartyBase : null;
                if (partyBase == null || mainParty == null || partyBase.MapFaction == null || mainParty.MapFaction == null)
                {
                    continue;
                }

                if (partyBase.MapFaction == mainParty.MapFaction)
                {
                    string partyName = partyBase.Name != null ? partyBase.Name.ToString() : string.Empty;
                    string mainPartyName = mainParty.Name != null ? mainParty.Name.ToString() : string.Empty;
                    if (!string.IsNullOrWhiteSpace(partyName) && partyName.Equals(mainPartyName, StringComparison.Ordinal))
                    {
                        playerParty = party;
                        return true;
                    }
                }
            }

            // Last-resort fallback keeps the previous banner match behavior as backup only.
            foreach (SPScoreboardPartyVM party in parties)
            {
                if (party != null && party.BattleCombatant != null && mainHero != null && party.BattleCombatant.Banner == mainHero.ClanBanner)
                {
                    playerParty = party;
                    return true;
                }
            }

            return false;
        }

        private static int GetPartyCount(SPScoreboardSideVM side)
        {
            return side != null && side.Parties != null ? side.Parties.Count : 0;
        }

        private static int GetScoreboardInstanceLabel(ScoreboardBaseVM scoreboard, out bool firstSeen)
        {
            for (int i = 0; i < seenScoreboards.Count; i++)
            {
                if (object.ReferenceEquals(seenScoreboards[i], scoreboard))
                {
                    firstSeen = false;
                    return i + 1;
                }
            }

            seenScoreboards.Add(scoreboard);
            firstSeen = true;
            return seenScoreboards.Count;
        }

        private static string ResolveCaptureLogFile()
        {
            try
            {
                string moduleDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (!string.IsNullOrWhiteSpace(moduleDirectory))
                {
                    return Path.Combine(moduleDirectory, "BattleStats_capture.log");
                }
            }
            catch
            {
            }

            return CaptureLogFallbackFile;
        }

        private static void LogCapture(string message)
        {
            if (!CaptureLoggingEnabled)
            {
                return;
            }

            string line = "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " + message + Environment.NewLine;
            if (TryAppendLine(CaptureLogFile, line))
            {
                return;
            }

            TryAppendLine(CaptureLogFallbackFile, line);
        }

        private static bool TryAppendLine(string path, string line)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.AppendAllText(path, line);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static void UpdateHeroRecords(List<SPScoreboardUnitVM> clanHeros)
        {
            foreach (SPScoreboardUnitVM hero in clanHeros)
            {
                int heroID = StableIds.GetHeroId(hero.Character);
                if (heroID == 0)
                {
                    continue;
                }

                HeroRecords newStats = new HeroRecords()
                {
                    Name = hero.Score.NameText,
                    Kills = hero.Score.Kill,
                    PR = hero.Score.Kill,
                    KB = (double)hero.Score.Kill,
                    Scars = hero.Score.Wounded,
                    Battles = 1,
                    Id = heroID
                };

                if (newStats.Kills > 0)
                {
                    statDiff[heroID] = newStats.Kills;
                }

                if (!heroRecords.IsEmpty())
                {
                    if (heroRecords.ContainsKey(heroID))
                    {
                        heroRecords[heroID].Name = newStats.Name;

                        heroRecords[heroID].Kills += newStats.Kills;
                        if (heroRecords[heroID].PR < newStats.Kills)
                        {
                            heroRecords[heroID].PR = newStats.Kills;
                        }

                        heroRecords[heroID].Scars += newStats.Scars;
                        heroRecords[heroID].Battles++;
                        heroRecords[heroID].KB = Math.Round((double)heroRecords[heroID].Kills / heroRecords[heroID].Battles, 2);
                    }
                    else
                    {
                        heroRecords.Add(heroID, newStats);
                    }
                }
                else
                {
                    heroRecords.Add(heroID, newStats);
                }
            }
        }

        private static void UpdateArmyRecords(Dictionary<string, List<SPScoreboardUnitVM>> formations, int enemyKills, int allyCasualties)
        {
            int totalKills = 0;
            int totalWounded = 0;
            int totalCasualties = 0;
            int friendlyKills;

            foreach (KeyValuePair<string, List<SPScoreboardUnitVM>> formation in formations)
            {
                string formationName = formation.Key;
                int formationKills = 0;
                int formationWounded = 0;
                int formationCasualties = 0;
                int formationID = StableIds.GetArmyFormationId(formationName);
                if (formationID == 0)
                {
                    continue;
                }

                foreach (SPScoreboardUnitVM troop in formation.Value)
                {
                    formationKills += troop.Score.Kill;
                    formationWounded += troop.Score.Wounded;
                    formationCasualties += troop.Score.Dead;
                }

                totalKills += formationKills;
                totalWounded += formationWounded;
                totalCasualties += formationCasualties;

                ArmyRecords newStats = new ArmyRecords()
                {
                    Name = formationName,
                    Kills = formationKills,
                    PR = formationKills,
                    KB = (double)formationKills,
                    Wounded = formationWounded,
                    Casualties = formationCasualties,
                    WB = (double)formationWounded,
                    CB = (double)formationCasualties,
                    Battles = 1,
                    Id = formationID
                };

                if (formationKills > 0)
                {
                    statDiff[formationID] = formationKills;
                }

                if (!armyRecords.IsEmpty())
                {
                    if (armyRecords.ContainsKey(formationID) && !formationName.Equals("Army Totals"))
                    {
                        armyRecords[formationID].Kills += formationKills;

                        if (armyRecords[formationID].PR < formationKills)
                        {
                            armyRecords[formationID].PR = formationKills;
                        }

                        armyRecords[formationID].Wounded += formationWounded;
                        armyRecords[formationID].Casualties += formationCasualties;
                        armyRecords[formationID].Battles++;
                        armyRecords[formationID].KB = Math.Round((double)armyRecords[formationID].Kills / armyRecords[formationID].Battles, 2);
                        armyRecords[formationID].WB = Math.Round((double)armyRecords[formationID].Wounded / armyRecords[formationID].Battles, 2);
                        armyRecords[formationID].CB = Math.Round((double)armyRecords[formationID].Casualties / armyRecords[formationID].Battles, 2);
                    }
                    else
                    {
                        armyRecords.Add(formationID, newStats);
                    }
                }
                else
                {
                    armyRecords.Add(formationID, newStats);
                }
            }

            int totalsID = StableIds.GetArmyTotalsId();

            if (totalKills > 0)
            {
                statDiff[totalsID] = totalKills;
            }

            if (armyRecords.ContainsKey(totalsID))
            {
                armyRecords[totalsID].Battles++;
            }
            else if (!armyRecords.ContainsKey(totalsID))
            {
                ArmyRecords totals = new ArmyRecords()
                {
                    Name = "Army Totals",
                    Battles = 1,
                    Id = totalsID
                };
                armyRecords.Add(totalsID, totals);
            }

            armyRecords[totalsID].Kills += totalKills;
            if (armyRecords[totalsID].PR < totalKills)
            {
                armyRecords[totalsID].PR = totalKills;
            }

            armyRecords[totalsID].Wounded += totalWounded;
            armyRecords[totalsID].Casualties += totalCasualties;
            armyRecords[totalsID].KB = Math.Round((double)armyRecords[totalsID].Kills / armyRecords[totalsID].Battles, 2);
            armyRecords[totalsID].WB = Math.Round((double)armyRecords[totalsID].Wounded / armyRecords[totalsID].Battles, 2);
            armyRecords[totalsID].CB = Math.Round((double)armyRecords[totalsID].Casualties / armyRecords[totalsID].Battles, 2);
            if (totalCasualties + totalWounded + allyCasualties > enemyKills)
            {
                friendlyKills = totalCasualties + totalWounded - (enemyKills - allyCasualties);
                armyRecords[totalsID].FK += friendlyKills;
            }
        }
    }
}
