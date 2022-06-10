using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace BattleStats
{
    public static class SaveLoadRecords
    {
        private static readonly string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Mount and Blade II Bannerlord");
        private static readonly string path2 = "BattleStats";
        public static string file;
        public static void SaveRecords()
        {
            TextObject name = Hero.MainHero.Name;
            string _file = Path.Combine(folderPath, path2, (name?.ToString()) + "_BattleStats.xml");
            file = _file;
            XmlSerializer serializer = new XmlSerializer(typeof(BattleStatsXml));
            XmlWriter writer;

            if (Directory.Exists(Path.Combine(folderPath, path2)))
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
                InformationManager.DisplayMessage(new InformationMessage(new TextObject("New Battle Stats at \\Documents\\Mount and Blade II Bannerlord", null).ToString(), Colors.Blue));
                Directory.CreateDirectory(Path.Combine(folderPath, path2));
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
            MenuSetup.sortedHeroRecords.Clear();
            MenuSetup.sortedArmyRecords.Clear();
            TextObject name = Hero.MainHero.Name;

            if (!Directory.Exists(Path.Combine(folderPath, path2)))
            {
                Directory.CreateDirectory(Path.Combine(folderPath, path2));
            }

            if (!File.Exists(Path.Combine(folderPath, path2, (name?.ToString()) + "_BattleStats.xml")))
            {
                return;
            }

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(BattleStatsXml));
            FileStream fs = new FileStream(Path.Combine(folderPath, path2, (name?.ToString()) + "_BattleStats.xml"), FileMode.Open);
            BattleStatsXml bsx = (BattleStatsXml)xmlSerializer.Deserialize(fs);

            foreach (HeroRecords record in bsx.Clan)
            {
                if (!BattleStatsBehavior.heroRecords.ContainsKey(record.Name))
                {
                    BattleStatsBehavior.heroRecords.Add(record.Name, record);
                }
            }

            foreach (ArmyRecords record in bsx.Army)
            {
                if (!BattleStatsBehavior.armyRecords.ContainsKey(record.Name))
                {
                    BattleStatsBehavior.armyRecords.Add(record.Name, record);
                }
            }

            fs.Close();
            file = Path.Combine(folderPath, path2, (name?.ToString()) + "_BattleStats.xml");
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