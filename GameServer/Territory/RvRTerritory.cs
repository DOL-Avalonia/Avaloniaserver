using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.Territory
{
    public class RvRTerritory
        : Territory
    {
        public RvRTerritory(IArea area, string areaId, ushort regionId, ushort zoneId, GameNPC boss)
            : base(area, areaId, regionId, zoneId, null, boss)
        {
            //add new area to region
            //only in memory
            if (WorldMgr.Regions.ContainsKey(this.RegionId))
            {
                WorldMgr.Regions[this.RegionId].AddArea(this.Area);
            }
        }
     
        protected override void SaveOriginalGuilds()
        {
            if (this.Mobs != null)
            {
                this.Mobs.ForEach(m => this.SaveMobOriginalGuildname(m));
            }
        }

        public override void SaveIntoDatabase()
        {
            //In memory RvR
            //No save allowed
        }
    }
}
