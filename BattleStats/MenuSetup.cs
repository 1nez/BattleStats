using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void ShowMenu()
        {
            bool gameStarted = Campaign.Current.GameStarted;
            if (gameStarted && ((Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl)) && Input.IsKeyPressed(InputKey.SemiColon)))
            {
                System.IO.File.Delete(SaveLoadRecords.file);
                sortedHeroRecords.Clear();
                sortedArmyRecords.Clear();
                BattleStatsBehavior.heroRecords.Clear();
                BattleStatsBehavior.armyRecords.Clear();
            }
            else if (gameStarted && (Input.IsKeyPressed(InputKey.SemiColon)))
            {
                RemoveNonClanMembers();
                SortRecordsByKills();
                ShowMenuPage(1);
            }
        }

        private static bool IsClanMember(string name)
        {
            foreach (Hero hero in Clan.PlayerClan.Heroes)
            {
                if (name.Equals(hero.Name.ToString()) && hero.IsAlive)
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
                Dictionary<string, HeroRecords>.ValueCollection heroRecords = BattleStatsBehavior.heroRecords.Values;
                sortedHeroRecords = heroRecords.ToList();
                sortedHeroRecords.Sort((x, y) => y.Kills.CompareTo(x.Kills));
            }

            if (!BattleStatsBehavior.armyRecords.IsEmpty())
            {
                sortedArmyRecords = new List<ArmyRecords>();
                List<ArmyRecords> tempArmyRecords = new List<ArmyRecords>();
                Dictionary<string, ArmyRecords>.ValueCollection armyRecords = BattleStatsBehavior.armyRecords.Values;
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
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(1), true, true, "Ok", "Next", null, () => ShowMenuPage(2), "", 0f, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(1), true, false, "Ok", "", null, null, "", 0f, null), false);
                        }

                        break;

                    case 2:

                        if (recordsCount > 20)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(2), true, true, "Ok", "Next", null, () => ShowMenuPage(3), "", 0f, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(2), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null), false);
                        }

                        break;

                    case 3:

                        if (recordsCount > 30)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(3), true, true, "Ok", "Next", null, () => ShowMenuPage(4), "", 0f, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(3), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null), false);
                        }

                        break;

                    case 4:

                        if (recordsCount > 40)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(4), true, true, "Ok", "Next", null, () => ShowMenuPage(5), "", 0f, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(4), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null), false);
                        }

                        break;

                    case 5:
                        InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(5), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null), false);
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
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(1), true, true, "Ok", "Next", null, () => ShowMenuPage(2), "", 0f, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(1), true, false, "Ok", "", null, null, "", 0f, null), false);
                        }
                        break;

                    case 2:

                        if (recordsCount > 16)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(2), true, true, "Ok", "Next", null, () => ShowMenuPage(3), "", 0f, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(2), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null), false);
                        }

                        break;

                    case 3:

                        if (recordsCount > 24)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(3), true, true, "Ok", "Next", null, () => ShowMenuPage(4), "", 0f, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(3), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null), false);
                        }
                        break;

                    case 4:

                        if (recordsCount > 32)
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(4), true, true, "Ok", "Next", null, () => ShowMenuPage(5), "", 0f, null), false);
                        }
                        else
                        {
                            InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(4), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null), false);
                        }
                        break;

                    case 5:
                        InformationManager.ShowInquiry(new InquiryData("Battle Stats", StatsView(5), true, true, "Ok", "Back", null, () => ShowMenuPage(1), "", 0f, null), false);
                        break;
                }
            }
        }

        private static string StatsView(int pageNum)
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
                            stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8) + " PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                " K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8) + " Wounds: " + sortedHeroRecords.ElementAt(i).Wounds.ToString().PadRight(8) +
                                "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
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
                            stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8) + "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8) + "Wounds: " + sortedHeroRecords.ElementAt(i).Wounds.ToString().PadRight(8) +
                                "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
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
                            stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8) + "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8) + "Wounds: " + sortedHeroRecords.ElementAt(i).Wounds.ToString().PadRight(8) +
                                "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
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
                            stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8) + "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8) + "Wounds: " + sortedHeroRecords.ElementAt(i).Wounds.ToString().PadRight(8) +
                                "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
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
                            stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8) + "PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                "K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8) + "Wounds: " + sortedHeroRecords.ElementAt(i).Wounds.ToString().PadRight(8) +
                                "Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
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
                            stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " PR: " + sortedHeroRecords.ElementAt(i).PR.ToString() +
                                " K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString() + " Wounds: " + sortedHeroRecords.ElementAt(i).Wounds.ToString() +
                                " Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
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
                            stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString().PadRight(8) + " PR: " + sortedHeroRecords.ElementAt(i).PR.ToString().PadRight(8) +
                                " K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString().PadRight(8) + " Wounds: " + sortedHeroRecords.ElementAt(i).Wounds.ToString().PadRight(8) +
                                " Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
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
                            stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " PR: " + sortedHeroRecords.ElementAt(i).PR.ToString() +
                                " K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString() + " Wounds: " + sortedHeroRecords.ElementAt(i).Wounds.ToString() +
                                " Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
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
                            stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " PR: " + sortedHeroRecords.ElementAt(i).PR.ToString() +
                                " K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString() + " Wounds: " + sortedHeroRecords.ElementAt(i).Wounds.ToString() +
                                " Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
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
                            stats += "Kills: " + sortedHeroRecords.ElementAt(i).Kills.ToString() + " PR: " + sortedHeroRecords.ElementAt(i).PR.ToString() +
                                " K/B: " + sortedHeroRecords.ElementAt(i).KB.ToString() + " Wounds: " + sortedHeroRecords.ElementAt(i).Wounds.ToString() +
                                " Battles: " + sortedHeroRecords.ElementAt(i).Battles.ToString() + "\n";
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
                        if (!formation.Name.Equals("Army Totals"))
                        {
                            stats += "[" + formation.Name + "]\n" + "K: " + formation.Kills.ToString() + "  PR: " + formation.PR.ToString()
                                + "  K/B: " + formation.KB.ToString() + "  W/B: " + formation.WB.ToString()
                                + "  C/B: " + formation.CB.ToString() + "  B: " + formation.Battles.ToString() + "\n";
                        }
                        else if (formation.Name.Equals("Army Totals"))
                        {
                            stats += "[" + formation.Name + "]\n" + "K: " + formation.Kills.ToString() + "  PR: " + formation.PR.ToString()
                                + "  K/B: " + formation.KB.ToString() + "  W/B: " + formation.WB.ToString()
                                + "  C/B: " + formation.CB.ToString() + "  FK: " + formation.FK.ToString() + "  B: " + formation.Battles.ToString() + "\n";
                        }
                    }
                }
            }

            return stats;
        }
    }
}