using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.Area;

namespace DOL.Territory
{
    public class Territory
    {
        public Territory(IArea area, string name, ushort regionId, ushort zoneId, string groupId, string bossId)
        {
            this.Area = area;
            this.RegionId = regionId;
            this.Name = name;
            this.ZoneId = zoneId;
            this.ID = Guid.NewGuid().ToString();
            this.GroupId = groupId;
            this.BossId = bossId;
        }

        public string ID
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public string GuildOwner
        {
            get;
            set;
        }

        public ushort RegionId
        {
            get;
            set;
        }

        public ushort ZoneId
        {
            get;
            set;
        }

        public IArea Area
        {
            get;
        }

        public string BossId
        {
            get;
            set;
        }

        public string GroupId
        {
            get;
            set;
        }

        public GameNPC Boss
        {
            get;
            set;
        }
    }
}
