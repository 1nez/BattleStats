using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security;
using System.Xml;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace BattleStats
{
    public static class SaveLoadRecords
    {
        private static readonly string folderPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory, "SavedStats");
        public static string file;

        public static void SaveRecords()
        {
            if (!TryGetCurrentClanId(out string clanId))
            {
                return;
            }

            file = Path.Combine(folderPath, clanId + "_BattleStats.xml");
            if (!EnsureStatsDirectory())
            {
                return;
            }

            XmlSerializer serializer = new XmlSerializer(typeof(BattleStatsXml));
            BattleStatsXml bsx = new BattleStatsXml
            {
                Clan = new List<HeroRecords>(),
                Army = new List<ArmyRecords>()
            };

            MenuSetup.SortRecordsByKills();

            foreach (var hero in MenuSetup.sortedHeroRecords)
            {
                bsx.Clan.Add(hero);
            }

            foreach (var formation in MenuSetup.sortedArmyRecords)
            {
                bsx.Army.Add(formation);
            }

            try
            {
                using (Stream output = new FileStream(file, FileMode.Create))
                using (XmlWriter writer = XmlWriter.Create(output, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "\t",
                    OmitXmlDeclaration = true
                }))
                {
                    serializer.Serialize(writer, bsx);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (SecurityException)
            {
            }
        }

        private static void LoadRecords()
        {
            BattleStatsBehavior.heroRecords.Clear();
            BattleStatsBehavior.armyRecords.Clear();
            BattleStatsBehavior.statDiff.Clear();
            MenuSetup.sortedHeroRecords.Clear();
            MenuSetup.sortedArmyRecords.Clear();
            MenuSetup.openCount = 0;

            if (!TryGetCurrentClanId(out string id))
            {
                return;
            }

            if (!EnsureStatsDirectory())
            {
                return;
            }

            string statsFilePath = Path.Combine(folderPath, id + "_BattleStats.xml");
            if (!File.Exists(statsFilePath))
            {
                string nameId = Hero.MainHero?.Name?.ToString();
                if (string.IsNullOrWhiteSpace(nameId))
                {
                    return;
                }

                string filePath = Path.Combine(folderPath, nameId + "_BattleStats.xml");
                if (File.Exists(filePath))
                {
                    id = nameId;
                    statsFilePath = Path.Combine(folderPath, id + "_BattleStats.xml");
                }
                else
                {
                    return;
                }
            }

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(BattleStatsXml));
            BattleStatsXml bsx;
            try
            {
                using (FileStream fs = new FileStream(statsFilePath, FileMode.Open))
                {
                    bsx = (BattleStatsXml)xmlSerializer.Deserialize(fs);
                }
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
            catch (SecurityException)
            {
                return;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (bsx?.Clan != null)
            {
                foreach (HeroRecords record in bsx.Clan)
                {
                    int resolvedId = ResolveClanHeroRecordId(record);
                    if (resolvedId == 0)
                    {
                        continue;
                    }

                    record.Id = resolvedId;
                    if (!BattleStatsBehavior.heroRecords.ContainsKey(resolvedId))
                    {
                        BattleStatsBehavior.heroRecords.Add(resolvedId, record);
                    }
                    else
                    {
                        MergeHeroRecords(BattleStatsBehavior.heroRecords[resolvedId], record);
                    }
                }
            }

            if (bsx?.Army != null)
            {
                foreach (ArmyRecords record in bsx.Army)
                {
                    int resolvedId = StableIds.GetArmyFormationId(record.Name);
                    if (resolvedId == 0)
                    {
                        resolvedId = record.Id;
                    }

                    if (resolvedId == 0)
                    {
                        continue;
                    }

                    record.Id = resolvedId;
                    if (!BattleStatsBehavior.armyRecords.ContainsKey(resolvedId))
                    {
                        BattleStatsBehavior.armyRecords.Add(resolvedId, record);
                    }
                    else
                    {
                        MergeArmyRecords(BattleStatsBehavior.armyRecords[resolvedId], record);
                    }
                }
            }

            file = Path.Combine(folderPath, id + "_BattleStats.xml");
        }

        public static void LoadRecords(CampaignGameStarter cgs)
        {
            if (cgs is null)
            {
                throw new ArgumentNullException(nameof(cgs));
            }

            LoadRecords();
        }

        private static int ResolveClanHeroRecordId(HeroRecords record)
        {
            if (record == null)
            {
                return 0;
            }

            if (record.Id != 0 && MenuSetup.IsClanMember(record.Id))
            {
                return record.Id;
            }

            Hero mainHero = Hero.MainHero;
            Clan clan = mainHero != null ? mainHero.Clan : null;
            if (clan?.Heroes == null)
            {
                return 0;
            }

            Hero member = clan.Heroes.Find(c => c != null && c.CharacterObject != null && c.Name.ToString() == record.Name);
            int resolvedId = StableIds.GetHeroId(member);
            if (resolvedId != 0 && MenuSetup.IsClanMember(resolvedId))
            {
                return resolvedId;
            }

            return 0;
        }

        private static void MergeHeroRecords(HeroRecords existing, HeroRecords incoming)
        {
            if (existing == null || incoming == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(incoming.Name))
            {
                existing.Name = incoming.Name;
            }

            existing.Kills += incoming.Kills;
            if (existing.PR < incoming.PR)
            {
                existing.PR = incoming.PR;
            }

            existing.Scars += incoming.Scars;
            existing.Battles += incoming.Battles;
            if (existing.Battles > 0)
            {
                existing.KB = Math.Round((double)existing.Kills / existing.Battles, 2);
            }
        }

        private static void MergeArmyRecords(ArmyRecords existing, ArmyRecords incoming)
        {
            if (existing == null || incoming == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(incoming.Name))
            {
                existing.Name = incoming.Name;
            }

            existing.Kills += incoming.Kills;
            if (existing.PR < incoming.PR)
            {
                existing.PR = incoming.PR;
            }

            existing.Wounded += incoming.Wounded;
            existing.Casualties += incoming.Casualties;
            existing.FK += incoming.FK;
            existing.Battles += incoming.Battles;

            if (existing.Battles > 0)
            {
                existing.KB = Math.Round((double)existing.Kills / existing.Battles, 2);
                existing.WB = Math.Round((double)existing.Wounded / existing.Battles, 2);
                existing.CB = Math.Round((double)existing.Casualties / existing.Battles, 2);
            }
        }

        private static bool TryGetCurrentClanId(out string clanId)
        {
            clanId = Hero.MainHero?.Clan?.InformalName?.ToString();
            return !string.IsNullOrWhiteSpace(clanId);
        }

        private static bool EnsureStatsDirectory()
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

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
            catch (SecurityException)
            {
                return false;
            }
        }
    }
}
