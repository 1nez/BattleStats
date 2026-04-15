using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace BattleStats
{
    public static class MenuSetup
    {
        public static List<HeroRecords> sortedHeroRecords = new List<HeroRecords>();
        public static List<ArmyRecords> sortedArmyRecords = new List<ArmyRecords>();
        public static bool changeFormat;
        public static InputKey hotkey = (InputKey)39;
        public static int openCount = 0;

        public static void ShowMenu()
        {
            bool gameStarted = Campaign.Current.GameStarted;

            if (gameStarted && ((Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl)) && Input.IsKeyPressed(hotkey)))
            {
                if (!string.IsNullOrWhiteSpace(SaveLoadRecords.file) && System.IO.File.Exists(SaveLoadRecords.file))
                {
                    try
                    {
                        System.IO.File.Delete(SaveLoadRecords.file);
                    }
                    catch (System.IO.IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                    catch (SecurityException)
                    {
                    }
                }

                sortedHeroRecords.Clear();
                sortedArmyRecords.Clear();
                BattleStatsBehavior.heroRecords.Clear();
                BattleStatsBehavior.armyRecords.Clear();
            }
            else if (gameStarted && (Input.IsKeyPressed(hotkey)))
            {
                if (!BattleStatsBehavior.statDiff.IsEmpty())
                {
                    openCount++;
                }

                if (openCount > 2)
                {
                    BattleStatsBehavior.statDiff.Clear();
                    openCount = 0;
                }
                RemoveNonClanMembers();
                SortRecordsByKills();
                ShowMenuPage(1);
            }
        }

        public static bool IsClanMember(int id)
        {
            Clan playerClan = Clan.PlayerClan;
            if (playerClan == null || playerClan.Heroes == null)
            {
                return false;
            }

            foreach (Hero hero in playerClan.Heroes)
            {
                int heroId = StableIds.GetHeroId(hero);
                if (hero.IsAlive && heroId != 0 && id == heroId)
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveNonClanMembers()
        {
            if (!BattleStatsBehavior.heroRecords.IsEmpty())
            {
                var herosToRemove = BattleStatsBehavior.heroRecords.Where(x => !IsClanMember(x.Key)).ToList();
                foreach (var hero in herosToRemove)
                {
                    BattleStatsBehavior.heroRecords.Remove(hero.Key);
                }
            }
        }

        public static void SortRecordsByKills()
        {
            if (!BattleStatsBehavior.heroRecords.IsEmpty())
            {
                Dictionary<int, HeroRecords>.ValueCollection heroRecords = BattleStatsBehavior.heroRecords.Values;
                sortedHeroRecords = heroRecords.ToList();
                sortedHeroRecords.Sort((x, y) => y.Kills.CompareTo(x.Kills));
            }

            if (!BattleStatsBehavior.armyRecords.IsEmpty())
            {
                sortedArmyRecords = new List<ArmyRecords>();
                List<ArmyRecords> tempArmyRecords = new List<ArmyRecords>();
                Dictionary<int, ArmyRecords>.ValueCollection armyRecords = BattleStatsBehavior.armyRecords.Values;
                tempArmyRecords = armyRecords.ToList();
                tempArmyRecords.Sort((x, y) => y.Kills.CompareTo(x.Kills));

                for (int i = 0; i < tempArmyRecords.Count; i++)
                {
                    if (!tempArmyRecords.ElementAt(i).Name.Equals("Army Totals"))
                    {
                        sortedArmyRecords.Add(tempArmyRecords.ElementAt(i));
                    }
                }

                sortedArmyRecords.Add(tempArmyRecords.Find(x => x.Name.Equals("Army Totals")));
            }
        }

        private static void ShowMenuPage(int pageNum)
        {
            int recordsCount = sortedHeroRecords.Count + sortedArmyRecords.Count;

            if (!changeFormat)
            {
                switch (pageNum)
                {
                    case 1:
                        if (recordsCount > 10)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(1), true, true, "Ok", "Next", null, () => ShowMenuPage(2), "", 0f, null, null, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(1), true, false, "Ok", "", null, null, "", 0f, null, null, null), false);
                        }

                        break;

                    case 2:

                        if (recordsCount > 20)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(2), true, true, "Ok", "Next", null, () => ShowMenuPage(3), "", 0f, null, null, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(2), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null, null, null), false);
                        }

                        break;

                    case 3:

                        if (recordsCount > 30)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(3), true, true, "Ok", "Next", null, () => ShowMenuPage(4), "", 0f, null, null, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(3), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null, null, null), false);
                        }

                        break;

                    case 4:

                        if (recordsCount > 40)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(4), true, true, "Ok", "Next", null, () => ShowMenuPage(5), "", 0f, null, null, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(4), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null, null, null), false);
                        }

                        break;

                    case 5:
                        InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(5), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null, null, null), false);
                        break;
                }
            }
            else if (changeFormat)
            {
                switch (pageNum)
                {
                    case 1:
                        if (recordsCount > 8)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(1), true, true, "Ok", "Next", null, () => ShowMenuPage(2), "", 0f, null, null, null), false);
                            openCount++;
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(1), true, false, "Ok", "", null, null, "", 0f, null, null, null), false);
                            openCount++;
                        }
                        break;

                    case 2:

                        if (recordsCount > 16)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(2), true, true, "Ok", "Next", null, () => ShowMenuPage(3), "", 0f, null, null, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(2), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null, null, null), false);
                        }

                        break;

                    case 3:

                        if (recordsCount > 24)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(3), true, true, "Ok", "Next", null, () => ShowMenuPage(4), "", 0f, null, null, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(3), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null, null, null), false);
                        }
                        break;

                    case 4:

                        if (recordsCount > 32)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(4), true, true, "Ok", "Next", null, () => ShowMenuPage(5), "", 0f, null, null, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(4), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null, null, null), false);
                        }
                        break;

                    case 5:
                        InformationManager.ShowInquiry(new InquiryData("Battle Stats", ViewStats(5), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null, null, null), false);
                        break;
                }
            }
        }

        private static string ViewStats(int pageNum)
        {
            String stats = String.Empty;
            int recordsCount = sortedHeroRecords.Count + sortedArmyRecords.Count;
            int heroCount = sortedHeroRecords.Count;
            bool showTotals = false;

            if (!changeFormat)
            {
                switch (pageNum)
                {
                    case 1:

                        for (int i = 0; i < 10 && i < heroCount; i++)
                        {
                            stats += "[" + sortedHeroRecords.ElementAt(i).Name + "]\n";
                            if (BattleStatsBehavior.statDiff.ContainsKey(sortedHeroRecords.ElementAt(i).Id) && !BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].Equals(0))
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " +" + BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].ToString().PadRight(8);
                            }
                            else
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8);
                            }

                            stats += "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8);
                            stats += "Scars: " + sortedHeroRecords.ElementAt(i).Scars.ToString().PadRight(8);
                            stats += "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
                        }

                        if (recordsCount <= 10)
                        {
                            showTotals = true;
                        }

                        break;

                    case 2:

                        for (int i = 10; i < 20 && i < heroCount; i++)
                        {
                            stats += "[" + sortedHeroRecords.ElementAt(i).Name + "]\n";
                            if (BattleStatsBehavior.statDiff.ContainsKey(sortedHeroRecords.ElementAt(i).Id) && !BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].Equals(0))
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " +" + BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].ToString().PadRight(8);
                            }
                            else
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8);
                            }

                            stats += "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8);
                            stats += "Scars: " + sortedHeroRecords.ElementAt(i).Scars.ToString().PadRight(8);
                            stats += "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
                        }

                        if (recordsCount <= 20)
                        {
                            showTotals = true;
                        }

                        break;

                    case 3:

                        for (int i = 20; i < 30 && i < heroCount; i++)
                        {
                            stats += "[" + sortedHeroRecords.ElementAt(i).Name + "]\n";
                            if (BattleStatsBehavior.statDiff.ContainsKey(sortedHeroRecords.ElementAt(i).Id) && !BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].Equals(0))
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " +" + BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].ToString().PadRight(8);
                            }
                            else
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8);
                            }

                            stats += "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8);
                            stats += "Scars: " + sortedHeroRecords.ElementAt(i).Scars.ToString().PadRight(8);
                            stats += "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
                        }

                        if (recordsCount <= 30)
                        {
                            showTotals = true;
                        }

                        break;

                    case 4:

                        for (int i = 30; i < 40 && i < heroCount; i++)
                        {
                            stats += "[" + sortedHeroRecords.ElementAt(i).Name + "]\n";
                            if (BattleStatsBehavior.statDiff.ContainsKey(sortedHeroRecords.ElementAt(i).Id) && !BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].Equals(0))
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " +" + BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].ToString().PadRight(8);
                            }
                            else
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8);
                            }

                            stats += "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8);
                            stats += "Scars: " + sortedHeroRecords.ElementAt(i).Scars.ToString().PadRight(8);
                            stats += "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
                        }

                        if (recordsCount <= 40)
                        {
                            showTotals = true;
                        }

                        break;

                    case 5:

                        for (int i = 40; i < recordsCount; i++)
                        {
                            stats += "[" + sortedHeroRecords.ElementAt(i).Name + "]\n";
                            if (BattleStatsBehavior.statDiff.ContainsKey(sortedHeroRecords.ElementAt(i).Id) && !BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].Equals(0))
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " +" + BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].ToString().PadRight(8);
                            }
                            else
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8);
                            }

                            stats += "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8);
                            stats += "Scars: " + sortedHeroRecords.ElementAt(i).Scars.ToString().PadRight(8);
                            stats += "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
                        }

                        showTotals = true;

                        break;
                }
            }
            else if (changeFormat)
            {
                switch (pageNum)
                {
                    case 1:
                        for (int i = 0; i < 8 && i < heroCount; i++)
                        {
                            stats += "[" + sortedHeroRecords.ElementAt(i).Name + "]\n";
                            if (BattleStatsBehavior.statDiff.ContainsKey(sortedHeroRecords.ElementAt(i).Id) && !BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].Equals(0))
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " +" + BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].ToString().PadRight(8);
                            }
                            else
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8);
                            }

                            stats += "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8);
                            stats += "Scars: " + sortedHeroRecords.ElementAt(i).Scars.ToString().PadRight(8);
                            stats += "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
                        }

                        if (recordsCount <= 8)
                        {
                            showTotals = true;
                        }
                        break;

                    case 2:

                        for (int i = 8; i < 16 && i < heroCount; i++)
                        {
                            stats += "[" + sortedHeroRecords.ElementAt(i).Name + "]\n";
                            if (BattleStatsBehavior.statDiff.ContainsKey(sortedHeroRecords.ElementAt(i).Id) && !BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].Equals(0))
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " +" + BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].ToString().PadRight(8);
                            }
                            else
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8);
                            }

                            stats += "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8);
                            stats += "Scars: " + sortedHeroRecords.ElementAt(i).Scars.ToString().PadRight(8);
                            stats += "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
                        }

                        if (recordsCount <= 16)
                        {
                            showTotals = true;
                        }

                        break;

                    case 3:

                        for (int i = 16; i < 24 && i < heroCount; i++)
                        {
                            stats += "[" + sortedHeroRecords.ElementAt(i).Name + "]\n";
                            if (BattleStatsBehavior.statDiff.ContainsKey(sortedHeroRecords.ElementAt(i).Id) && !BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].Equals(0))
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " +" + BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].ToString().PadRight(8);
                            }
                            else
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8);
                            }

                            stats += "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8);
                            stats += "Scars: " + sortedHeroRecords.ElementAt(i).Scars.ToString().PadRight(8);
                            stats += "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
                        }

                        if (recordsCount <= 24)
                        {
                            showTotals = true;
                        }

                        break;

                    case 4:

                        for (int i = 24; i < 32 && i < heroCount; i++)
                        {
                            stats += "[" + sortedHeroRecords.ElementAt(i).Name + "]\n";
                            if (BattleStatsBehavior.statDiff.ContainsKey(sortedHeroRecords.ElementAt(i).Id) && !BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].Equals(0))
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " +" + BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].ToString().PadRight(8);
                            }
                            else
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8);
                            }

                            stats += "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8);
                            stats += "Scars: " + sortedHeroRecords.ElementAt(i).Scars.ToString().PadRight(8);
                            stats += "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
                        }

                        if (recordsCount <= 32)
                        {
                            showTotals = true;
                        }

                        break;

                    case 5:

                        for (int i = 32; i < recordsCount; i++)
                        {
                            stats += "[" + sortedHeroRecords.ElementAt(i).Name + "]\n";
                            if (BattleStatsBehavior.statDiff.ContainsKey(sortedHeroRecords.ElementAt(i).Id) && !BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].Equals(0))
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " +" + BattleStatsBehavior.statDiff[sortedHeroRecords.ElementAt(i).Id].ToString().PadRight(8);
                            }
                            else
                            {
                                stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8);
                            }

                            stats += "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8);
                            stats += "Scars: " + sortedHeroRecords.ElementAt(i).Scars.ToString().PadRight(8);
                            stats += "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
                        }

                        showTotals = true;

                        break;
                }
            }

            if (showTotals)
            {
                if (!sortedArmyRecords.IsEmpty())
                {
                    foreach (ArmyRecords formation in sortedArmyRecords)
                    {
                        stats += "[" + formation.Name + "]\n";
                        if (BattleStatsBehavior.statDiff.ContainsKey(formation.Id) && !BattleStatsBehavior.statDiff[formation.Id].Equals(0))
                        {
                            stats += "K: " + formation.Kills.ToString() + " +" + BattleStatsBehavior.statDiff[formation.Id].ToString();
                        }
                        else
                        {
                            stats += "K: " + formation.Kills.ToString();
                        }
                        stats += "  PR: " + formation.PR.ToString()
                            + "  K/B: " + formation.KB.ToString() + "  W/B: " + formation.WB.ToString()
                            + "  C/B: " + formation.CB.ToString();

                        if (formation.Name.Equals("Army Totals"))
                        {
                            stats += "  FK: " + formation.FK;
                        }
                        stats += "  B: " + formation.Battles.ToString() + "\n";
                    }
                }
            }
            return stats;
        }
    }
}

