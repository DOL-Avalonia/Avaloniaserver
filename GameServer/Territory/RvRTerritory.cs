﻿using DOL.GS;
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
            : base(area, areaId, regionId, zoneId, null, boss, false)
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

        public void Reset()
        {
            this.Boss.RestoreOriginalGuildName();
            this.Boss.Realm = eRealm.None;
            foreach (var mob in this.Mobs)
            {
                mob.Realm = eRealm.None;
                if (this.OriginalGuilds.ContainsKey(mob.InternalID))
                {
                    mob.GuildName = this.OriginalGuilds[mob.InternalID];
                }
                else
                {
                    mob.GuildName = null;
                }
            }
        }
    }
}
