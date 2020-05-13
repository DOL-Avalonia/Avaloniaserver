using DOL.GS;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.Area;

namespace DOL.Territory
{
    public class Territory
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public Territory(IArea area, string areaId, ushort regionId, ushort zoneId, string groupId, GameNPC boss)
        {
            this.Area = area;
            this.RegionId = regionId;
            this.Name = ((AbstractArea)area).Description;
            this.ZoneId = zoneId;
            this.AreaId = areaId;
            this.GroupId = groupId;
            this.BossId = boss.InternalID;
            this.Boss = boss;
            this.Coordinates = TerritoryManager.GetCoordinates(area);
            this.Radius = this.GetRadius();
            this.OriginalGuilds = new Dictionary<string, string>();
            this.Mobs = this.GetMobsInTerritory();
            this.SaveOriginalGuilds();
        }

        public Dictionary<string, string> OriginalGuilds
        {
            get;
        }

        public string AreaId
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

        public IEnumerable<GameNPC> Mobs
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

        public AreaCoordinate Coordinates
        {
            get;
            set;
        }

        public ushort Radius
        {
            get;
            set;
        }


        private void SaveOriginalGuilds()
        {
            if (this.Mobs != null)
            {
                foreach (var mob in this.Mobs)
                {
                    if (!this.OriginalGuilds.ContainsKey(mob.InternalID))
                    {
                        this.OriginalGuilds.Add(mob.InternalID, mob.GuildName ?? string.Empty);
                    }
                }
            }           
        }


        public IEnumerable<GameNPC> GetMobsInTerritory()
        {
            List<GameNPC> mobs = new List<GameNPC>();
            if (this.Coordinates == null)
            {
                log.Error($"Impossible to get mobs from territory { this.Name } because Area with ID: {this.Area.ID } is not supported");
                return null;
            }
            
            if (Radius == 0)
            {
                return null;
            }

            var items = WorldMgr.Regions[this.RegionId].GetNPCsInRadius(this.Coordinates.X, this.Coordinates.Y, 0, this.Radius, false, true);

            foreach (var item in items.Cast<GameObject>())
            {
                if (item is GameNPC mob && (mob.Flags & GameNPC.eFlags.CANTTARGET) == 0)
                {
                    mobs.Add(mob);
                }
            }

            return mobs;
        }

        private ushort GetRadius()
        {
            if (this.Area is Circle circle)
            {
                return (ushort)circle.Radius;
            }
            else if (this.Area is Square sq)
            {
                if (sq.Height <= 0 || sq.Width <= 0)
                {
                    return 0;
                }

                if (sq.Height > sq.Width)
                {
                    return (ushort)Math.Ceiling(sq.Height / 2D);
                }
                else
                {
                    return (ushort)Math.Ceiling(sq.Width / 2D);
                }
            }
            else if (this.Area is Polygon poly)
            {
                return (ushort)poly.Radius;
            }
            else
            {               
                log.Error($"Territory initialisation failed, cannot determine radius from Area. Area ID: { Area.ID } not supported ");
                return 0;
            }
        }

        public IList<string> GetInformations()
        {
            return new string[]
            {
                " Area Id: " + this.AreaId,
                " Boss Id: " + this.BossId, 
                " Boss Name: " + this.Boss.Name,
                " Group Id: " + this.GroupId,
                " Region: " + this.RegionId,
                " Zone: " + this.ZoneId,
                " Guild Owner: " + (this.GuildOwner ?? "None"),
                " Mobs -- Count(" + this.Mobs.Count() + " )",
                 string.Join("\n", this.Mobs.Select(m => "Name: " + m.Name + "\n Id: " + m.InternalID))
            };            

        }
    }
}
