using DOL.Database;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Determination
    /// </summary>
    public class DeterminationAbility : RAPropertyEnhancer
    {
        private static readonly eProperty[] Properties =
        {
            eProperty.MesmerizeDurationReduction,
            eProperty.StunDurationReduction,
            eProperty.SpeedDecreaseDurationReduction,
        };

        public DeterminationAbility(DBAbility dba, int level) : base(dba, level, Properties) { }

        protected override string ValueUnit => "%";

        public override int GetAmountForLevel(int level)
        {
            if (level < 1)
            {
                return 0;
            }

            if (ServerProperties.Properties.USE_NEW_PASSIVES_RAS_SCALING)
            {
                switch (level)
                {
                        case 1: return 4;
                        case 2: return 8;
                        case 3: return 12;
                        case 4: return 17;
                        case 5: return 23;
                        case 6: return 30;
                        case 7: return 38;
                        case 8: return 46;
                        case 9: return 55;
                        default: return 55;
                }
            }

            int amount = 0;
            if (level >= 1)
            {
                amount += 4;
            }

            if (level >= 2)
            {
                amount += 4;
            }

            if (level >= 3)
            {
                amount += 12;
            }

            if (level >= 4)
            {
                amount += 18;
            }

            if (level >= 5)
            {
                amount += 17;
            }

            return amount;
        }
    }
}