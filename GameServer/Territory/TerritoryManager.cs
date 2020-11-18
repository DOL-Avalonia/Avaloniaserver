﻿using DOL.Database;
using DOL.events.server;
using DOL.Events;
using DOL.GameEvents;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.MobGroups;
using DOLDatabase.Tables;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.Area;
using static DOL.GS.GameObject;

namespace DOL.Territory
{
    public class TerritoryManager
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static TerritoryManager instance;
        public static readonly ushort NEUTRAL_EMBLEM = 256;
        private readonly string BOSS_CLASS = "DOL.GS.Scripts.TerritoryBoss";    
        private static readonly int DAILY_TAX = GS.ServerProperties.Properties.DAILY_TAX;
        private static readonly int TERRITORY_BANNER_PERCENT_OFF = GS.ServerProperties.Properties.TERRITORY_BANNER_PERCENT_OFF;
        private static readonly int DAILY_MERIT_POINTS = GS.ServerProperties.Properties.DAILY_MERIT_POINTS;

        public static TerritoryManager Instance => instance ?? (instance = new TerritoryManager());

        public List<Territory> Territories
        {
            get;
        }

        private TerritoryManager()
        {
            this.Territories = new List<Territory>();
        }

        public bool Init()
        {
            return true;
        }


        [GameEventLoaded]
        public static void LoadTerritories(DOLEvent e, object sender, EventArgs arguments)
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
                            var areaDb = GameServer.Database.SelectObjects<DBArea>("area_id = @id", new QueryParameter("id", territoryDb.AreaId))?.FirstOrDefault();

                            if (areaDb == null)
                            {
                                log.Error($"Cannot find Area in Database with ID: { territoryDb.AreaId }");
                                continue;
                            }

                            var area = zone.GetAreasOfSpot(new Point3D(territoryDb.AreaX, territoryDb.AreaY, 0), false)?.FirstOrDefault(a => ((AbstractArea)a).Description.Equals(areaDb.Description));

                            if (area != null)
                            {
                                var mobinfo = Instance.FindBossFromGroupId(territoryDb.GroupId);

                                if (mobinfo.Error == null)
                                {
                                    if (!territoryDb.BossMobId.Equals(mobinfo.Mob.InternalID))
                                    {
                                        log.Error($"Boss Id does not match from GroupId { territoryDb.GroupId } and Found Bossid from groupId (event search) { mobinfo.Mob.InternalID } , {territoryDb.BossMobId} identified in database");
                                        continue;
                                    }

                                    Instance.Territories.Add(new Territory(area, territoryDb.AreaId, territoryDb.RegionId, territoryDb.ZoneId, territoryDb.GroupId, mobinfo.Mob, bonus: territoryDb.Bonus, id: territoryDb.ObjectId));
                                    count++;
                                }
                                else
                                {
                                    log.Error(mobinfo.Error);
                                }
                            }
                        }
                    }
                }
            }

            GuildMgr.GetAllGuilds().ForEach(g => g.LoadTerritories());
            log.Info(count + " Territoires Chargés");
        }

        public bool IsTerritoryArea(IEnumerable<IArea> areas)
        {
            foreach (var item in areas)
            {
                bool matched = this.Territories.Any(t => t.Area.ID.Equals(item.ID));

                if (matched)
                {
                    return true;
                }
            }

            return false;
        }

        public bool DoesPlayerOwnsTerritory(GamePlayer player)
        {
            foreach (var item in player.CurrentAreas)
            {
                var matched = this.Territories.FirstOrDefault(t => t.Area.ID.Equals(item.ID));

                if (matched != null)
                {
                    return true;
                }
            }

            return false;
        }

        public Territory GetCurrentTerritory(IEnumerable<IArea> areas)
        {
            foreach (var item in areas)
            {
                var matched = this.Territories.FirstOrDefault(t => t.Area.ID.Equals(item.ID));

                if (matched != null)
                {
                    return matched;
                }
            }

            return null;
        }

        public void ChangeGuildOwner(Guild guild, Territory territory)
        {
            if (guild == null || territory == null)
            {
                return;
            }

            this.ApplyTerritoryChange(guild, territory, false);
        }
            
        public void ChangeGuildOwner(GameNPC mob, Guild guild)
        {
            if (guild == null || mob == null || string.IsNullOrEmpty(mob.InternalID))
            {
                return;
            }

            Territory territory = this.Territories.FirstOrDefault(t => t.BossId.Equals(mob.InternalID));
            //For Boss change also Guild for Mob linked in same Event
            if (mob.EventID != null)
            {
                var gameEvent = GameEventManager.Instance.Events.FirstOrDefault(e => e.ID.Equals(mob.EventID));

                if (gameEvent?.Mobs?.Any() == true)
                {
                    gameEvent.Mobs.ForEach(m => m.GuildName = guild.Name);
                }
            }   

            if (territory == null || territory.Mobs == null)
            {
                log.Error("Cannot get Territory from MobId: " + mob.InternalID);
                return;
            }

            territory.IsBannerSummoned = false;
            this.ApplyTerritoryChange(guild, territory, true);        
        }        

        public void RestoreTerritoryGuildNames(Territory territory)
        {
            if (territory == null || territory.Mobs == null || territory.Boss == null)
            {
                log.Error($"Impossible to clear territory. One Value is Null: Territory: {territory == null }, Mobs: {territory?.Mobs == null }, Boss: { territory?.Boss == null }");
                return;
            }

            if (territory.Boss != null)
            {
                var gameEvents = GameEventManager.Instance.Events.FirstOrDefault(e => e.ID.Equals(territory.Boss.EventID));

                if (gameEvents?.Mobs?.Any() == true)
                {
                    gameEvents.Mobs.ForEach(m =>
                    {
                        if (territory.OriginalGuilds.ContainsKey(m.InternalID))
                        {
                            m.GuildName = territory.OriginalGuilds[m.InternalID];
                        }
                        else
                        {
                            m.GuildName = null;
                        }
                    });
                }
            }

            if (territory.GuildOwner != null)
            {
                Guild oldOwner = GuildMgr.GetGuildByName(territory.GuildOwner);

                if (oldOwner != null)
                {
                    oldOwner.RemoveTerritory(territory.AreaId);
                }
            }

            territory.GuildOwner = null;
            territory.Boss.RestoreOriginalGuildName();
            territory.SaveIntoDatabase();
        }

        public static void ClearEmblem(Territory territory, GameNPC initNpc = null)
        {
            territory.IsBannerSummoned = false;
            foreach (var mob in territory.Mobs)
            {
                if (territory.OriginalGuilds.ContainsKey(mob.InternalID))
                {
                    mob.GuildName = territory.OriginalGuilds[mob.InternalID];
                }
                else
                {
                    mob.GuildName = null;
                }
                RestoreOriginalEmblem(mob);
            }

            var firstMob = initNpc ?? territory.Mobs.FirstOrDefault() ?? territory.Boss;
            foreach (GameObject item in firstMob.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.ITEM, firstMob.X, firstMob.Y, firstMob.Z, WorldMgr.VISIBILITY_DISTANCE, new System.Collections.ArrayList(), true))
            {
                if (item is TerritoryBanner ban)
                {
                    ban.Emblem = ban.OriginalEmblem;
                }
            }
        }

        private static void ApplyNewEmblem(string guildName, GameNPC mob)
        {
            if (string.IsNullOrWhiteSpace(guildName) || mob.ObjectState != eObjectState.Active || mob.CurrentRegion == null || mob.Inventory == null || mob.Inventory.VisibleItems == null)
                return;
            var guild = GuildMgr.GetGuildByName(guildName);
            if (guild == null)
                return;
            foreach (var item in mob.Inventory.VisibleItems.Where(i => i.SlotPosition == 26 || i.SlotPosition == 11))
            {
                item.Emblem = guild.Emblem;
            }
        }

        private static void RestoreOriginalEmblem(GameNPC mob)
        {
            if (mob.ObjectState != eObjectState.Active || mob.CurrentRegion == null || mob.Inventory == null || mob.Inventory.VisibleItems == null)
                return;

            foreach (var item in mob.Inventory.VisibleItems.Where(i => i.SlotPosition == 11 || i.SlotPosition == 26))
            {
                var equipment = GameServer.Database.SelectObjects<NPCEquipment>("TemplateID = @TemplateID AND Slot = @Slot",
                    new QueryParameter[]{
                        new QueryParameter("TemplateID", mob.EquipmentTemplateID),
                        new QueryParameter("Slot", item.SlotPosition)
                    })?.FirstOrDefault();

                if (equipment != null)
                {
                    item.Emblem = equipment.Emblem;
                }               
            }
            
            mob.BroadcastLivingEquipmentUpdate();
        }

        public MobInfo FindBossFromGroupId(string groupId)
        {
            var bossEvent = GameEventManager.Instance.Events.FirstOrDefault(e => e.KillStartingGroupMobId?.Equals(groupId) == true);

            if (bossEvent == null)
            {
                return new MobInfo()
                {
                    Error = "Impossible de trouver l'event lié au GroupId: " + groupId
                };
            } 

            var boss = bossEvent.Mobs.FirstOrDefault(m => m.GetType().FullName.Equals(BOSS_CLASS));

            if (boss == null)
            {
                return new MobInfo()
                {
                    Error = $"Aucun mob avec la classe { BOSS_CLASS } a été trouvé dans l'Event {bossEvent.ID}"
                };
            }

            return new MobInfo()
            {
                Mob = boss
            };
        }

        public IList<string> GetTerritoriesInformations()
        {
            List<string> infos = new List<string>();
            
            foreach (var territory in this.Territories)
            {   
                string line = (((AbstractArea)territory.Area).Description + " / ");

                var zone = WorldMgr.Regions[territory.RegionId].Zones.FirstOrDefault(z => z.ID.Equals(territory.ZoneId));

                if (zone != null)
                {
                    line += zone.Description + " / ";
                }

                line += territory.GuildOwner ?? "Neutre";
                infos.Add(line);
                infos.Add("");
            }

            return infos;
        }    


        public static Territory GetTerritoryFromMobId(string mobId)
        {
            foreach (var territory in Instance.Territories)
            {
                if (territory.Mobs.Any(m => m.InternalID.Equals(mobId)))
                    return territory;
            }

            return null;
        }

        public bool AddTerritory(IArea area, string areaId, ushort regionId, string groupId, GameNPC boss)
        {
            if (!WorldMgr.Regions.ContainsKey(regionId) || groupId == null || boss == null || areaId == null)
            {
                return false;
            }

            var coords = GetCoordinates(area);
            
            if (coords == null)
            {
                return false;
            }

            var zone = WorldMgr.Regions[regionId].GetZone(coords.X, coords.Y);

            if (zone == null)
            {
                return false;
            }

            var territory = new Territory(area, areaId, regionId, zone.ID, groupId, boss);
            this.Territories.Add(territory);

            try
            {
                territory.SaveIntoDatabase();
            }
            catch(Exception e)
            {
                log.Error(e.Message);
                return false;
            }

            return true;
        }

        private void ApplyTerritoryChange(Guild guild, Territory territory, bool saveChange)
        {
            //remove Territory from old Guild if any
            if (territory.GuildOwner != null)
            {
                var oldGuild = GuildMgr.GetGuildByName(territory.GuildOwner);

                if (oldGuild != null)
                {
                    oldGuild.RemoveTerritory(territory.AreaId);
                }
            }

            guild.AddTerritory(territory.AreaId, saveChange);
            territory.GuildOwner = guild.Name;         

            territory.Mobs.ForEach(m => m.GuildName = guild.Name);
            territory.Boss.GuildName = guild.Name;

            if (saveChange)
                territory.SaveIntoDatabase();
        }

        public static void ApplyEmblemToTerritory(Territory territory, Guild guild, GameNPC initSearchNPC = null)
        {
            territory.IsBannerSummoned = true;
            var cls = WorldMgr.GetAllPlayingClients().Where(c => c.Player.CurrentZone.ID.Equals(territory.ZoneId));

            foreach (var mob in territory.Mobs)
            {
                ApplyNewEmblem(guild.Name, mob);
                cls.ForEach(c => c.Out.SendLivingEquipmentUpdate(mob));
            }

            var firstMob = initSearchNPC ?? territory.Mobs.FirstOrDefault();
            foreach (GameObject item in firstMob.CurrentZone.GetObjectsInRadius(Zone.eGameObjectType.ITEM, firstMob.X, firstMob.Y, firstMob.Z, WorldMgr.VISIBILITY_DISTANCE, new System.Collections.ArrayList(), true))
            {
                if (item is TerritoryBanner ban)
                {
                    ban.Emblem = guild.Emblem;
                }
            }
        }


        public static AreaCoordinate GetCoordinates(IArea area)
        {
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
                return null;
            }

            return new AreaCoordinate()
            {
                X = x,
                Y = y
            };
        }

        public void ProceedPayments()
        {
            foreach (var guildGroup in this.Territories.GroupBy(t => t.GuildOwner))
            {
                var guildName = guildGroup.Key;
                if (guildName != null)
                {
                    var guild = GuildMgr.GetGuildByName(guildName);

                    if (guild != null)
                    {
                        int count = guildGroup.Count();
                        bool shouldRemoveTerritories = false;
                        var players = guild.GetListOfOnlineMembers();

                        if (count < 6)
                        {
                            int sum = guildGroup.Sum(g => g.IsBannerSummoned ? (int)Math.Round((DAILY_TAX - (DAILY_TAX * TERRITORY_BANNER_PERCENT_OFF / 100D))) : DAILY_TAX);

                            if (guild.TryPayTerritoryTax(Money.GetMoney(0,0,sum,0,0)))
                            {
                                players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryPaid", sum),
                                              eChatType.CT_Guild, eChatLoc.CL_SystemWindow));
                            }
                            else
                            {
                                players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryNoMoney"),
                                                                            eChatType.CT_Guild, eChatLoc.CL_SystemWindow));
                                shouldRemoveTerritories = true;
                            }
                        }
                        else
                        {
                            int total = 0;
                            int counter = 0;
                            foreach (var territory in guildGroup)
                            {
                                counter++;
                                if (counter < 6)
                                {
                                    total += territory.IsBannerSummoned ? (int)Math.Round((DAILY_TAX - (DAILY_TAX * TERRITORY_BANNER_PERCENT_OFF / 100D))) : DAILY_TAX;
                                }
                                else
                                {
                                    int dailyOverCost = DAILY_TAX + 10;
                                    total += territory.IsBannerSummoned ? (dailyOverCost - (dailyOverCost * TERRITORY_BANNER_PERCENT_OFF / 100)) : dailyOverCost;
                                }
                            }

                            if (guild.TryPayTerritoryTax(Money.GetMoney(0, 0, total, 0, 0)))
                            {
                                players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryPaid", total),
                                              eChatType.CT_Guild, eChatLoc.CL_SystemWindow));
                            }
                            else
                            {
                                players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryNoMoney"),
                                              eChatType.CT_Guild, eChatLoc.CL_SystemWindow));                             
                                shouldRemoveTerritories = true;
                            }
                        }


                        if (shouldRemoveTerritories)
                        {
                            foreach (var territory in guildGroup)
                            {
                                this.RestoreTerritoryGuildNames(territory);
                                ClearEmblem(territory);
                            }
                        }
                        else
                        {
                            int mp = count * DAILY_MERIT_POINTS;
                            guild.GainMeritPoints(mp);
                            players.Foreach(p => p.Out.SendMessage(Language.LanguageMgr.GetTranslation(p.Client.Account.Language, "Commands.Players.Guild.TerritoryMeritPoints", mp),
                                             eChatType.CT_Guild, eChatLoc.CL_SystemWindow));
                        }
                    }
                }
            }
        }
    }

    public class MobInfo
    {
        public GameNPC Mob
        {
            get;
            set;
        }

        public string Error
        {
            get;
            set;
        }
    }
}
