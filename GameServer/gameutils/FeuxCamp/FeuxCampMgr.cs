using DOL.Database;
using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerScripts.Utils
{
    public class FeuxCampMgr
    {
        private static FeuxCampMgr instance;

        public Dictionary<string, FeuDeCamp> m_firecamps;

        public static FeuxCampMgr Instance => instance ?? (instance = new FeuxCampMgr());   

        public bool Init()
        {
            var firecamps = GameServer.Database.SelectAllObjects<FeuxCampXItem>();
            m_firecamps = new Dictionary<string, FeuDeCamp>();
            foreach (var firecampItem in firecamps)
            {
                var template = GameServer.Database.FindObjectByKey<ItemTemplate>(firecampItem.FeuxCampItemId_nb);

                if (template != null)
                {
                    var firecamp = new FeuDeCamp(firecampItem.Radius,
                        firecampItem.Lifetime, firecampItem.Power,
                        firecampItem.IsHealthType,
                        firecampItem.IsManaType,
                        firecampItem.IsTrapType,
                        firecampItem.TrapDamagePercent);
                   
                    m_firecamps.Add(firecampItem.FeuxCampXItem_ID, firecamp);
                }             
            }

            return m_firecamps.Count > 0;
        }


    }
}
