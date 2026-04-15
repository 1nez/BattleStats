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
                    if (record.Id == 0 | !MenuSetup.IsClanMember(record.Id))
                    {
                        Clan clan_ = Hero.MainHero.Clan;
                        if (clan_ != null)
                        {
                            Hero member = clan_.Heroes.Find(c => c.CharacterObject != null && c.Name.ToString() == record.Name);
                            if (member != null)
                            {
                                record.Id = member.CharacterObject.GetHashCode();
                            }
                        }
                    }

                    if (!BattleStatsBehavior.heroRecords.ContainsKey(record.Id) && MenuSetup.IsClanMember(record.Id))
                    {
                        BattleStatsBehavior.heroRecords.Add(record.Id, record);
                    }
                }
            }

            if (bsx?.Army != null)
            {
                foreach (ArmyRecords record in bsx.Army)
                {
                    if (record.Id == 0)
                    {
                        record.Id = record.Name.GetHashCode();
                    }
                    if (!BattleStatsBehavior.armyRecords.ContainsKey(record.Id))
                    {
                        BattleStatsBehavior.armyRecords.Add(record.Id, record);
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
