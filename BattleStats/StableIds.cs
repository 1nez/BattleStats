using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace BattleStats
{
    internal static class StableIds
    {
        // Fixed IDs for known synthetic formation buckets.
        private const int InfantryId = unchecked((int)0xA11A0001);
        private const int RangedId = unchecked((int)0xA11A0002);
        private const int CavalryId = unchecked((int)0xA11A0003);
        private const int HorseArchersId = unchecked((int)0xA11A0004);
        private const int ArmyTotalsId = unchecked((int)0xA11A00FF);

        public static int GetHeroId(BasicCharacterObject character)
        {
            if (character != null && !string.IsNullOrWhiteSpace(character.StringId))
            {
                return GetDeterministicHash32("hero:" + character.StringId);
            }

            string fallbackName = character?.Name?.ToString();
            if (!string.IsNullOrWhiteSpace(fallbackName))
            {
                return GetDeterministicHash32("hero-name:" + fallbackName);
            }

            return 0;
        }

        public static int GetHeroId(Hero hero)
        {
            return hero != null ? GetHeroId(hero.CharacterObject) : 0;
        }

        public static int GetArmyFormationId(string formationName)
        {
            if (string.IsNullOrWhiteSpace(formationName))
            {
                return 0;
            }

            switch (formationName.Trim())
            {
                case "Infantry":
                    return InfantryId;
                case "Ranged":
                    return RangedId;
                case "Cavalry":
                    return CavalryId;
                case "Horse Archers":
                    return HorseArchersId;
                case "Army Totals":
                    return ArmyTotalsId;
                default:
                    return GetDeterministicHash32("army:" + formationName.Trim());
            }
        }

        public static int GetArmyTotalsId()
        {
            return ArmyTotalsId;
        }

        private static int GetDeterministicHash32(string value)
        {
            unchecked
            {
                const int fnvOffset = unchecked((int)2166136261);
                const int fnvPrime = 16777619;

                int hash = fnvOffset;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= fnvPrime;
                }

                return hash == 0 ? int.MinValue : hash;
            }
        }
    }
}
