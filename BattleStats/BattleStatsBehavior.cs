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
        public static Dictionary<string, HeroRecords> heroRecords = new Dictionary<string, HeroRecords>();
        public static Dictionary<string, ArmyRecords> armyRecords = new Dictionary<string, ArmyRecords>();

        [HarmonyPostfix]
        public static void PostFix(ScoreboardBaseVM __instance)
        {
            bool battleIsOver = __instance.IsOver;

            if (battleIsOver)
            {
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
                string heroName = hero.Score.NameText;
                HeroRecords newStats = new HeroRecords()
                {
                    Name = heroName,
                    Kills = hero.Score.Kill,
                    PR = hero.Score.Kill,
                    KB = (double)hero.Score.Kill,
                    Wounds = hero.Score.Wounded,
                    Battles = 1
                };

                if (!heroRecords.IsEmpty())
                {
                    if (heroRecords.ContainsKey(heroName))
                    {
                        heroRecords[heroName].Kills += newStats.Kills;
                        if (heroRecords[heroName].PR < newStats.Kills)
                        {
                            heroRecords[heroName].PR = newStats.Kills;
                        }

                        heroRecords[heroName].Wounds += newStats.Wounds;
                        heroRecords[heroName].Battles++;
                        heroRecords[heroName].KB = Math.Round((double)heroRecords[heroName].Kills / heroRecords[heroName].Battles, 2);
                    }
                    else
                    {
                        heroRecords.Add(heroName, newStats);
                    }
                }
                else
                {
                    heroRecords.Add(heroName, newStats);
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
                    Battles = 1
                };

                if (!armyRecords.IsEmpty())
                {
                    if (armyRecords.ContainsKey(formationName) && !formationName.Equals("Army Totals"))
                    {
                        armyRecords[formationName].Kills += formationKills;

                        if (armyRecords[formationName].PR < formationKills)
                        {
                            armyRecords[formationName].PR = formationKills;
                        }

                        armyRecords[formationName].Wounded += formationWounded;
                        armyRecords[formationName].Casualties += formationCasualties;
                        armyRecords[formationName].Battles++;
                        armyRecords[formationName].KB = Math.Round((double)armyRecords[formationName].Kills / armyRecords[formationName].Battles, 2);
                        armyRecords[formationName].WB = Math.Round((double)armyRecords[formationName].Wounded / armyRecords[formationName].Battles, 2);
                        armyRecords[formationName].CB = Math.Round((double)armyRecords[formationName].Casualties / armyRecords[formationName].Battles, 2);
                    }
                    else
                    {
                        armyRecords.Add(formationName, newStats);
                    }
                }
                else
                {
                    armyRecords.Add(formationName, newStats);
                }
            }

            if (armyRecords.ContainsKey("Army Totals"))
            {
                armyRecords["Army Totals"].Battles++;
            }
            else
            {
                ArmyRecords totals = new ArmyRecords()
                {
                    Name = "Army Totals",
                    Battles = 1
                };
                armyRecords.Add("Army Totals", totals);
            }

            armyRecords["Army Totals"].Kills += totalKills;
            if (armyRecords["Army Totals"].PR < totalKills)
            {
                armyRecords["Army Totals"].PR = totalKills;
            }
            armyRecords["Army Totals"].Wounded += totalWounded;
            armyRecords["Army Totals"].Casualties += totalCasualties;
            armyRecords["Army Totals"].KB = Math.Round((double)armyRecords["Army Totals"].Kills / armyRecords["Army Totals"].Battles, 2);
            armyRecords["Army Totals"].WB = Math.Round((double)armyRecords["Army Totals"].Wounded / armyRecords["Army Totals"].Battles, 2);
            armyRecords["Army Totals"].CB = Math.Round((double)armyRecords["Army Totals"].Casualties / armyRecords["Army Totals"].Battles, 2);
            if (totalCasualties + totalWounded + allyCasualties > enemyKills)
            {
                friendlyKills = totalCasualties + totalWounded - (enemyKills - allyCasualties);
                armyRecords["Army Totals"].FK += friendlyKills;
            }
        }
    }
}