using DOL.Database;
using DOL.GS;
using DOLDatabase.Tables;
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
    public class TerritoryManager
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static TerritoryManager instance;

        public static TerritoryManager Instance => instance ?? (instance = new TerritoryManager());

        public List<Territory> Territories
        {
            get;
        }

        private TerritoryManager()
        {
            this.Territories = new List<Territory>();
        }


        public bool LoadTerritories()
        {
            var values = GameServer.Database.SelectAllObjects<TerritoryDb>();
            int count = 0;
            
            if (values != null)
            {
                foreach (var territoryDb in values)
                {
                    if (WorldMgr.Regions.ContainsKey(territoryDb.RegionId))
                    {
                        var zone = WorldMgr.Regions[territoryDb.RegionId].Zones.FirstOrDefault(z => z.ID.Equals(territoryDb.ZoneId));

                        if (zone != null)
                        {
                            var area = zone.GetAreasOfSpot(new Point3D(territoryDb.AreaX, territoryDb.AreaY, 0), false)?.FirstOrDefault(a => a.ID.Equals(territoryDb.AreaId));

                            if (area != null)
                            {
                                this.Territories.Add(new Territory(area, territoryDb.Name, territoryDb.RegionId, territoryDb.ZoneId, territoryDb.GroupId, territoryDb.BossMobId));
                                count++;
                            }
                        }
                    }
                }
            }

            log.Info(count + " Territoires Chargés");
            return true;
        }


        public bool AddTerritory(IArea area, ushort regionId, string name, string groupId, string bossId)
        {
            if (!WorldMgr.Regions.ContainsKey(regionId) || groupId == null || bossId == null)
            {
                return false;
            }

            int x, y;

            if (area is Circle circle)
            {
                x = circle.X;
                y = circle.Y; 
            }
            else if (area is Square sq)
            {
                x = sq.X;
                y = sq.Y;
            }
            else if (area is Polygon poly)
            {
                x = poly.X;
                y = poly.Y;
            }
            else
            {
                return false;
            }


            var zone = WorldMgr.Regions[regionId].GetZone(x, y);

            if (zone == null)
            {
                return false;
            }

            var territory = new Territory(area, name, regionId, zone.ID, groupId, bossId);

            this.Territories.Add(territory);
            return true;
        }
    }
}
