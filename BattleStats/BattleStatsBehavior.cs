using HarmonyLib;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.ViewModelCollection.Scoreboard;

namespace BattleStats
{
    [HarmonyPatch(typeof(ScoreboardBaseVM))]
    [HarmonyPatch("UpdateQuitText")]
    public class BattleStatsBehavior
    {
        public static Dictionary<int, HeroRecords> heroRecords = new Dictionary<int, HeroRecords>();
        public static Dictionary<int, ArmyRecords> armyRecords = new Dictionary<int, ArmyRecords>();
        public static Dictionary<int, int> statDiff = new Dictionary<int, int>();

        [HarmonyPostfix]
        public static void PostFix(ScoreboardBaseVM __instance)
        {
            bool battleIsOver = __instance.IsOver;

            if (battleIsOver)
            {
                statDiff.Clear();
                MenuSetup.openCount = 0;
                GetStatsFromBattle(__instance);
            }
        }

        private static void GetStatsFromBattle(ScoreboardBaseVM scoreboard)
        {
            List<SPScoreboardUnitVM> clanHeroes = new List<SPScoreboardUnitVM>();
            List<SPScoreboardUnitVM> infantry = new List<SPScoreboardUnitVM>();
            List<SPScoreboardUnitVM> ranged = new List<SPScoreboardUnitVM>();
            List<SPScoreboardUnitVM> cavalry = new List<SPScoreboardUnitVM>();
            List<SPScoreboardUnitVM> horseArchers = new List<SPScoreboardUnitVM>();
            Dictionary<string, List<SPScoreboardUnitVM>> formations = new Dictionary<string, List<SPScoreboardUnitVM>>();
            int enemyKills = 0;
            int allyCasualties = 0;

            if (PartyBase.MainParty.Side.ToString().Equals("Attacker"))
            {
                foreach (SPScoreboardPartyVM party in scoreboard.Attackers.Parties)
                {
                    if (party.BattleCombatant.Banner == Hero.MainHero.ClanBanner)
                    {
                        foreach (SPScoreboardUnitVM troop in party.Members)
                        {
                            if (troop.IsHero)
                            {
                                clanHeroes.Add(troop);
                            }
                            else
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
                    else
                    {
                        allyCasualties += party.Score.Dead + party.Score.Wounded;
                    }
                }
                enemyKills = scoreboard.Defenders.Score.Kill;
            }
            else if (PartyBase.MainParty.Side.ToString().Equals("Defender"))
            {
                foreach (SPScoreboardPartyVM party in scoreboard.Defenders.Parties)
                {
                    if (party.BattleCombatant.Banner == Hero.MainHero.ClanBanner)
                    {
                        foreach (SPScoreboardUnitVM troop in party.Members)
                        {
                            if (troop.IsHero)
                            {
                                clanHeroes.Add(troop);
                            }
                            else
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
                    else
                    {
                        allyCasualties += party.Score.Dead + party.Score.Wounded;
                    }
                }
                enemyKills = scoreboard.Attackers.Score.Kill;
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

            if (!clanHeroes.IsEmpty())
            {
                UpdateHeroRecords(clanHeroes);
            }
            if (!formations.IsEmpty())
            {
                UpdateArmyRecords(formations, enemyKills, allyCasualties);
            }
        }

        private static void UpdateHeroRecords(List<SPScoreboardUnitVM> clanHeros)
        {
            foreach (SPScoreboardUnitVM hero in clanHeros)
            {
                int heroID = hero.Character.GetHashCode();
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
                int formationID = formationName.GetHashCode();

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

            int totalsID = "Army Totals".GetHashCode();

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