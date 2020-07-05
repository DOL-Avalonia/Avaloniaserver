using System;
using System.Collections.Generic;

namespace DOL.Bonus
{
    public class BonusCondition
    {

        public string BonusName
        {
            get;
            set;
        }

        public int ChampionLevel
        {
            get;
            set;
        }

        public int MlLevel
        {
            get;
            set;
        }

        public bool IsRenaissanceRequired
        {
            get;
            set;
        }

        public static IEnumerable<BonusCondition> LoadFromString(string raw)
        {
            if (raw == null)
            {
                return null;
            }

            try
            {
                var conditions = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<BonusCondition>>(raw);
                return conditions;
            }
            catch
            {
                return null;
            }
        }


        public static string SaveToString(IEnumerable<BonusCondition> conditions)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(conditions, Newtonsoft.Json.Formatting.None);
            }
            catch
            {
                return null;
            }
        }
    }
}
