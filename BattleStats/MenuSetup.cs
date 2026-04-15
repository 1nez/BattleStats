using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
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

        private sealed class DisplayRow
        {
            public bool IsHero { get; set; }
            public HeroRecords Hero { get; set; }
            public ArmyRecords Army { get; set; }
        }

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
            else if (gameStarted && Input.IsKeyPressed(hotkey))
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
                List<KeyValuePair<int, HeroRecords>> heroesToRemove = BattleStatsBehavior.heroRecords.Where(x => !IsClanMember(x.Key)).ToList();
                foreach (KeyValuePair<int, HeroRecords> hero in heroesToRemove)
                {
                    BattleStatsBehavior.heroRecords.Remove(hero.Key);
                }
            }
        }

        public static void SortRecordsByKills()
        {
            sortedHeroRecords = BattleStatsBehavior.heroRecords.IsEmpty()
                ? new List<HeroRecords>()
                : BattleStatsBehavior.heroRecords.Values.ToList();
            sortedHeroRecords.Sort((x, y) => y.Kills.CompareTo(x.Kills));

            sortedArmyRecords = new List<ArmyRecords>();
            if (BattleStatsBehavior.armyRecords.IsEmpty())
            {
                return;
            }

            List<ArmyRecords> tempArmyRecords = BattleStatsBehavior.armyRecords.Values.ToList();
            tempArmyRecords.Sort((x, y) => y.Kills.CompareTo(x.Kills));

            ArmyRecords totalsRecord = null;
            for (int i = 0; i < tempArmyRecords.Count; i++)
            {
                ArmyRecords record = tempArmyRecords[i];
                if (record == null)
                {
                    continue;
                }

                if (record.Name != null && record.Name.Equals("Army Totals"))
                {
                    totalsRecord = record;
                }
                else
                {
                    sortedArmyRecords.Add(record);
                }
            }

            if (totalsRecord != null)
            {
                sortedArmyRecords.Add(totalsRecord);
            }
        }

        private static int GetRowsPerPage()
        {
            return changeFormat ? 8 : 10;
        }

        private static int GetTotalRecordCount()
        {
            return sortedHeroRecords.Count + sortedArmyRecords.Count;
        }

        private static int GetTotalPages()
        {
            int rowsPerPage = GetRowsPerPage();
            int totalRecords = GetTotalRecordCount();
            return Math.Max(1, (totalRecords + rowsPerPage - 1) / rowsPerPage);
        }

        private static int ClampPage(int pageNum)
        {
            int totalPages = GetTotalPages();
            if (pageNum < 1)
            {
                return 1;
            }

            if (pageNum > totalPages)
            {
                return totalPages;
            }

            return pageNum;
        }

        private static List<DisplayRow> BuildDisplayRows()
        {
            List<DisplayRow> rows = new List<DisplayRow>(GetTotalRecordCount());

            for (int i = 0; i < sortedHeroRecords.Count; i++)
            {
                rows.Add(new DisplayRow
                {
                    IsHero = true,
                    Hero = sortedHeroRecords[i]
                });
            }

            for (int i = 0; i < sortedArmyRecords.Count; i++)
            {
                rows.Add(new DisplayRow
                {
                    IsHero = false,
                    Army = sortedArmyRecords[i]
                });
            }

            return rows;
        }

        private static void ShowMenuPage(int pageNum)
        {
            int currentPage = ClampPage(pageNum);
            int totalPages = GetTotalPages();
            string affirmativeText = "Close";
            Action affirmativeAction = null;
            string negativeText = string.Empty;
            Action negativeAction = null;
            bool showNegative = false;

            if (totalPages > 1)
            {
                if (currentPage < totalPages)
                {
                    negativeText = "Next";
                    negativeAction = () => ShowMenuPage(currentPage + 1);
                    showNegative = true;
                }
                else
                {
                    affirmativeText = "Back";
                    affirmativeAction = () => ShowMenuPage(1);
                    negativeText = "Close";
                    showNegative = true;
                }
            }

            InformationManager.ShowInquiry(
                new InquiryData("Battle Stats", ViewStats(currentPage), true, showNegative, affirmativeText, negativeText, affirmativeAction, negativeAction, "", 0f, null, null, null),
                false);
        }

        private static string ViewStats(int pageNum)
        {
            List<DisplayRow> allRows = BuildDisplayRows();
            if (allRows.Count == 0)
            {
                return "No battle stats recorded yet.";
            }

            int rowsPerPage = GetRowsPerPage();
            int currentPage = ClampPage(pageNum);
            int startIndex = (currentPage - 1) * rowsPerPage;
            int endIndex = Math.Min(startIndex + rowsPerPage, allRows.Count);
            StringBuilder stats = new StringBuilder();

            for (int i = startIndex; i < endIndex; i++)
            {
                DisplayRow row = allRows[i];
                if (row.IsHero && row.Hero != null)
                {
                    stats.Append(FormatHeroRow(row.Hero));
                }
                else if (!row.IsHero && row.Army != null)
                {
                    stats.Append(FormatArmyRow(row.Army));
                }
            }

            return stats.Length > 0 ? stats.ToString() : "No battle stats recorded yet.";
        }

        private static string FormatHeroRow(HeroRecords hero)
        {
            StringBuilder stats = new StringBuilder();
            int killDiff = 0;
            bool hasDiff = BattleStatsBehavior.statDiff.TryGetValue(hero.Id, out killDiff) && !killDiff.Equals(0);

            stats.Append('[').Append(hero.Name).Append("]\n");
            if (hasDiff)
            {
                stats.Append("Kills: ").Append(hero.Kills).Append(" +").Append(killDiff.ToString().PadRight(8));
            }
            else
            {
                stats.Append("Kills: ").Append(hero.Kills.ToString().PadRight(8));
            }

            stats.Append("PR: ").Append(hero.PR.ToString().PadRight(8))
                .Append("K/B: ").Append(hero.KB.ToString().PadRight(8))
                .Append("Scars: ").Append(hero.Scars.ToString().PadRight(8))
                .Append("Battles: ").Append(hero.Battles)
                .Append('\n');

            return stats.ToString();
        }

        private static string FormatArmyRow(ArmyRecords army)
        {
            StringBuilder stats = new StringBuilder();
            int killDiff = 0;
            bool hasDiff = BattleStatsBehavior.statDiff.TryGetValue(army.Id, out killDiff) && !killDiff.Equals(0);

            stats.Append('[').Append(army.Name).Append("]\n");
            if (hasDiff)
            {
                stats.Append("K: ").Append(army.Kills).Append(" +").Append(killDiff);
            }
            else
            {
                stats.Append("K: ").Append(army.Kills);
            }

            stats.Append("  PR: ").Append(army.PR)
                .Append("  K/B: ").Append(army.KB)
                .Append("  W/B: ").Append(army.WB)
                .Append("  C/B: ").Append(army.CB);

            if (army.Name != null && army.Name.Equals("Army Totals"))
            {
                stats.Append("  FK: ").Append(army.FK);
            }

            stats.Append("  B: ").Append(army.Battles).Append('\n');
            return stats.ToString();
        }
    }
}
