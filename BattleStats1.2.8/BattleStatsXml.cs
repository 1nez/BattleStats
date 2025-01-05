using System.Collections.Generic;
using System.Xml.Serialization;

namespace BattleStats
{
    [XmlRoot("BattleStats")]
    public class BattleStatsXml
    {
        [XmlArrayItem("Hero")]
        public List<HeroRecords> Clan;

        [XmlArrayItem("Formation")]
        public List<ArmyRecords> Army;
    }
}