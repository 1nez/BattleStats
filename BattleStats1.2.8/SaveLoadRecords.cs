using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;

namespace BattleStats
{
    public static class SaveLoadRecords
    {
        private static readonly string folderPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SavedStats");
        public static string file;

        public static void SaveRecords()
        {
            string _file;
            TextObject clan = Hero.MainHero.Clan.InformalName;
            _file = Path.Combine(folderPath, clan.ToString() + "_BattleStats.xml");
            file = _file;
            XmlSerializer serializer = new XmlSerializer(typeof(BattleStatsXml));
            XmlWriter writer;

            if (Directory.Exists(Path.Combine(folderPath)))
            {
                Stream output = new FileStream(file, FileMode.Create);
                writer = XmlWriter.Create(output, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "\t",
                    OmitXmlDeclaration = true
                });
            }
            else
            {
                Directory.CreateDirectory(Path.Combine(folderPath));
                Stream output = new FileStream(file, FileMode.Create);
                writer = XmlWriter.Create(output, new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "\t",
                    OmitXmlDeclaration = true
                });
            }

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

            serializer.Serialize(writer, bsx);
            writer.Close();
        }

        private static void LoadRecords()
        {
            BattleStatsBehavior.heroRecords.Clear();
            BattleStatsBehavior.armyRecords.Clear();
            BattleStatsBehavior.statDiff.Clear();
            MenuSetup.sortedHeroRecords.Clear();
            MenuSetup.sortedArmyRecords.Clear();
            MenuSetup.openCount = 0;

            TextObject clan = Hero.MainHero.Clan.InformalName;
            string id = clan.ToString();

            if (!Directory.Exists(Path.Combine(folderPath)))
            {
                Directory.CreateDirectory(Path.Combine(folderPath));
            }

            if (!File.Exists(Path.Combine(folderPath, (id + "_BattleStats.xml"))))
            {
                TextObject name = Hero.MainHero.Name;
                string fileName = name?.ToString() + "_BattleStats.xml";
                string filePath = Path.Combine(folderPath, fileName);

                if (File.Exists(filePath))
                {
                    id = name?.ToString();
                }
                else
                {
                    return;
                }
            }

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(BattleStatsXml));
            FileStream fs = new FileStream(Path.Combine(folderPath, id + "_BattleStats.xml"), FileMode.Open);
            BattleStatsXml bsx = (BattleStatsXml)xmlSerializer.Deserialize(fs);

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

            fs.Close();

            id = clan.ToString();
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
    }
}