using DOL.Database;
using DOL.events.server;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOLDatabase.Tables;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.GameTimer;

namespace DOL.GameEvents
{
    public class GameEventManager
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static GameEventManager instance;
        private System.Threading.Timer timer;

        public static GameEventManager Instance => instance ?? (instance = new GameEventManager());

        public List<GameNPC> PreloadedMobs { get; }

        public List<GameStaticItem> PreloadedCoffres { get; }

        private GameEventManager() 
        {
            Events = new List<GameEvent>();
            PreloadedCoffres = new List<GameStaticItem>();
            PreloadedMobs = new List<GameNPC>();            
        }

        public List<GameEvent> Events { get; set; }

        public bool Init()
        {
            return true;
        }

        private async void TimeCheck(object o)
        {
            Console.WriteLine("GameEvent: " + DateTime.Now.ToString());


            foreach(var ev in this.Events.Where(ev => ev.Status == EventStatus.NotOver))
            {
                //End events with timer over
                if (ev.EndTime.HasValue && ev.EndingConditionTypes.Contains(EndingConditionType.Timer) && DateTime.UtcNow > ev.EndTime.Value.DateTime)
                {
                    await this.StopEvent(ev, EndingConditionType.Timer);
                }

                //Start Events Timer
                if (!ev.StartedTime.HasValue && ev.StartTriggerTime.HasValue && ev.StartConditionType == StartingConditionType.Timer)
                {
                    if (DateTime.UtcNow >= ev.StartTriggerTime.Value.DateTime)
                    {
                        this.StartEvent(ev);
                    }
                }
            }

            var chanceEvents = this.Events.Where(e => e.Status == EventStatus.NotOver && e.EventChanceInterval.HasValue && e.EventChance > 0 && !e.StartedTime.HasValue);
            var rand = new Random(DateTime.Now.Millisecond);


            //Start Event if chance proc
            foreach(var ev in chanceEvents)
            {
                if (!ev.ChanceLastTimeChecked.HasValue)
                {
                    ev.ChanceLastTimeChecked = DateTime.UtcNow;
                }
                else
                {
                    if (DateTime.UtcNow - ev.ChanceLastTimeChecked.Value >= ev.EventChanceInterval.Value)
                    {
                        ev.ChanceLastTimeChecked = DateTime.UtcNow;
                        if (rand.Next(0, 101) >= ev.EventChance)
                        {
                            this.StartEvent(ev);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Init Events Objects after Coffre loaded event
        /// </summary>
        /// <returns></returns>
        [GameServerCoffreLoaded]
        public static void LoadObjects(DOLEvent e, object sender, EventArgs arguments)
        {
            int mobCount = 0;
            int coffreCount = 0;
            var eventsFromDb = GameServer.Database.SelectAllObjects<EventDB>();

            if (eventsFromDb == null)
            {
                return;
            }

            //Load Only Not Over Events
            foreach (var eventdb in eventsFromDb.Where(ev => ev.Status == 0))
            {
                GameEvent newEvent = new GameEvent(eventdb);

                var objects = GameServer.Database.SelectObjects<EventsXObjects>("EventID = @ObjectId", new QueryParameter("@ObjectId", eventdb.ObjectId));

                if (objects != null)
                {
                    foreach(var coffreInfo in objects.Where(o => o.IsCoffre))
                    {
                        if (coffreInfo.ItemID != null)
                        {
                            var coffre = Instance.PreloadedCoffres.FirstOrDefault(c => c.InternalID.Equals(coffreInfo.ItemID));

                            if (coffre != null)
                            {
                                coffre.CanRespawnWithinEvent = coffreInfo.CanRespawn;
                                newEvent.Coffres.Add(coffre);
                                Instance.PreloadedCoffres.Remove(coffre);
                                coffreCount++;

                                if (coffreInfo.StartEffect > 0)
                                {
                                    newEvent.StartEffects.Add(coffreInfo.ItemID, (ushort)coffreInfo.StartEffect);
                                }

                                if (coffreInfo.EndEffect > 0)
                                {
                                    newEvent.StartEffects.Add(coffreInfo.ItemID, (ushort)coffreInfo.EndEffect);
                                }
                            }
                        }
                    }

                    foreach(var mobInfo in objects.Where(o => o.IsMob))
                    {
                        if (mobInfo.ItemID != null)
                        {
                            var mob = Instance.PreloadedMobs.FirstOrDefault(c => c.InternalID.Equals(mobInfo.ItemID));

                            if (mob != null)
                            {
                                mob.CanRespawnWithinEvent = mobInfo.CanRespawn;
                                newEvent.Mobs.Add(mob);
                                Instance.PreloadedMobs.Remove(mob);
                                mobCount++;

                                if (mobInfo.StartEffect > 0)
                                {
                                    newEvent.StartEffects.Add(mobInfo.ItemID, (ushort)mobInfo.StartEffect);
                                }

                                if (mobInfo.EndEffect > 0)
                                {
                                    newEvent.StartEffects.Add(mobInfo.ItemID, (ushort)mobInfo.EndEffect);
                                }
                            }
                        }                      
                    }
                }

                Instance.Events.Add(newEvent);
            }
            log.Info(string.Format("{0} Mobs Loaded Into Events", mobCount));
            log.Info(string.Format("{0} Coffre Loaded Into Events", coffreCount));
            log.Info(string.Format("{0} Events Loaded", Instance.Events.Count()));

            CreateMissingRelationObjects(Instance.Events.Select(ev => ev.ID));
            Instance.timer = new System.Threading.Timer(Instance.TimeCheck, Instance, 5000, 10000);
        }

        /// <summary>
        /// Add Tagged Mob or Coffre with EventID in Database
        /// </summary>
        private static void CreateMissingRelationObjects(IEnumerable<string> eventIds)
        {
            foreach(var obj in Instance.PreloadedCoffres.Where(pc => eventIds.Contains(pc.EventID)))
            {
                var newCoffre = new EventsXObjects()
                {
                    EventID = obj.EventID,
                    ItemID = obj.InternalID,
                    IsCoffre = true,
                    Name = obj.Name,
                    Region = obj.CurrentRegionID,
                    CanRespawn = true
                };

                //Try to Add Coffre from Region in case of update and remove it from world
                var ev = Instance.Events.FirstOrDefault(e => e.ID.Equals(obj.EventID));

                if (ev != null)
                {
                    var region = WorldMgr.GetRegion(obj.CurrentRegionID);

                    if (region != null)
                    {
                        var coffreObj = region.Objects.FirstOrDefault(o => o?.InternalID?.Equals(obj.InternalID) == true);

                        if (coffreObj != null && coffreObj is GameStaticItem coffre)
                        {
                            ev.Coffres.Add(coffre);
                            coffre.RemoveFromWorld();
                        }
                    }
                }

                GameServer.Database.AddObject(newCoffre);
            }

            Instance.PreloadedCoffres.Clear();

            foreach (var obj in Instance.PreloadedMobs.Where(pb => eventIds.Contains(pb.EventID)))
            {
                var newMob = new EventsXObjects()
                {
                    EventID = obj.EventID,
                    ItemID = obj.InternalID,
                    Name = obj.Name,
                    IsMob = true,
                    Region = obj.CurrentRegionID,
                    CanRespawn = true
                };

                var ev = Instance.Events.FirstOrDefault(e => e.ID.Equals(obj.EventID));

                //Try to Add Mob from Region in case of update and remove it from world
                if (ev != null)
                {
                    var region = WorldMgr.GetRegion(obj.CurrentRegionID);

                    if (region != null)
                    {
                        var mobObj = region.Objects.FirstOrDefault(o => o?.InternalID?.Equals(obj.InternalID) == true);

                        if (mobObj != null && mobObj is GameNPC npc)
                        {
                            ev.Mobs.Add(npc);
                            npc.RemoveFromWorld();
                        }
                    }
                }

                GameServer.Database.AddObject(newMob);
            }

            Instance.PreloadedMobs.Clear();
        }

        /// <summary>
        /// Show Events Infos depending IsPlayer or not, GM see everything. ShowAllEvents for showing even finished events
        /// </summary>
        /// <param name="isPlayer"></param>
        /// <param name="showAllEvents"></param>
        /// <returns></returns>
        public List<string> GetEventsInfos(bool isPlayer, bool showAllEvents)
        {
            List<string> infos = new List<string>();            

            IEnumerable<GameEvent> events = Instance.Events.Where(e => e.Status == EventStatus.NotOver);

            if (showAllEvents)
            {
                events = events.OrderBy(e => (int)e.Status);
            }
            else
            {
                events = events.Where(e => e.StartedTime.HasValue);
            }
          

            if (isPlayer)
            {
                events = events.Where(e => e.ShowEvent);
            }

            infos.Add(" --  EVENT -- ");

            foreach(var e in events)
            {
                infos.Add("");
                if (!isPlayer)
                    infos.Add(" -- ID: " + e.ID);

                this.GetMainInformations(e, infos);             

                if (!isPlayer)
                {
                    this.GetGMInformations(e, infos);
                }

                infos.Add("");
                infos.Add("--------------------");
            }

            return infos;
        }



        private void GetMainInformations(GameEvent e, List<string> infos)
        {                    
            infos.Add(" -- Name: " + e.EventName);
            infos.Add(" -- EventArea: " + (e.EventAreas != null ? string.Join(",", e.EventAreas) : string.Empty));
            infos.Add(" -- EventZone: " + (e.EventZones != null ? string.Join(",", e.EventZones) : string.Empty));
            infos.Add(" -- Started Time: " + (e.StartedTime.HasValue ? e.StartedTime.Value.ToLocalTime().ToString() : string.Empty));
            infos.Add(" -- Remaining Time: " + (e.EndingConditionTypes.Contains(EndingConditionType.Timer) && e.EndTime.HasValue ? string.Format(@"{0:dd\:hh\:mm\:ss}", e.EndTime.Value.Subtract(DateTimeOffset.UtcNow)) : string.Empty));
        }

        private void GetGMInformations(GameEvent e, List<string> infos)
        {
            infos.Add(" -- EndTime: " + e.EndTime?.ToLocalTime() ?? string.Empty);
            infos.Add(" -- DebutText: " + e.DebutText ?? string.Empty);
            infos.Add(" -- EndingActionA: " + e.EndingActionA.ToString());
            infos.Add(" -- EndingActionB: " + e.EndingActionB.ToString());
            infos.Add(" -- EndingConditionTypes: ");
            foreach(var t in e.EndingConditionTypes)
            {
                infos.Add("    * " + t.ToString());
            }
            infos.Add(" -- Status: " + e.Status.ToString());
            infos.Add(" -- EndText: " + e.EndText ?? string.Empty);
            infos.Add(" -- EventChance: " + e.EventChance);
            infos.Add(" -- EventChanceInterval: " + (e.EventChanceInterval.HasValue ? (e.EventChanceInterval.Value.TotalMinutes + " mins") : string.Empty));
            infos.Add(" -- RemainingTimeText: " + e.RemainingTimeText ?? string.Empty);
            infos.Add(" -- RemainingTimeInterval: " + (e.RemainingTimeInterval.HasValue ? (e.RemainingTimeInterval.Value.TotalMinutes.ToString() + " mins") : string.Empty));
            infos.Add(" -- ShowEvent: " + e.ShowEvent);
            infos.Add(" -- StartConditionType: " + e.StartConditionType.ToString());
            infos.Add("");
            infos.Add(" ------- MOBS ---------- Total ( " +  e.Mobs.Count() + " )");
            infos.Add("");
            foreach (var mob in e.Mobs)
            {
                infos.Add(" * id: " + mob.InternalID);
                infos.Add(" * Name: " + mob.Name);
                infos.Add(" * Brain: " + mob.Brain?.GetType()?.FullName ?? string.Empty);
                infos.Add(string.Format(" * X: {0}, Y: {1}, Z: {2}", mob.X, mob.Y, mob.Z));
                infos.Add(" * Region: " + mob.CurrentRegionID);
                infos.Add(" * Zone: " + mob.CurrentZone.ID);
                infos.Add(" * Area: " + (mob.CurrentAreas != null ? string.Join(",", mob.CurrentAreas) : string.Empty));
                infos.Add("");
            }
            infos.Add(" ------- COFFRES ---------- Total ( " + e.Coffres.Count() + " )");
            infos.Add("");
            foreach (var coffre in e.Coffres)
            {
                infos.Add(" * id: " + coffre.InternalID);
                infos.Add(" * Name: " + coffre.Name);
                infos.Add(string.Format(" * X: {0}, Y: {1}, Z: {2}", coffre.X, coffre.Y, coffre.Z));
                infos.Add(" * Region: " + coffre.CurrentRegionID);
                infos.Add(" * Zone: " + coffre.CurrentZone.ID);
                infos.Add(" * Area: " + (coffre.CurrentAreas != null ? string.Join(",", coffre.CurrentAreas) : string.Empty));
                infos.Add("");
            }
        }


        public bool StartEvent(GameEvent e)
        {
            e.StartedTime = DateTimeOffset.UtcNow;
            e.Status = EventStatus.NotOver;
            WorldMgr.GetAllPlayingClients().Foreach(c => c.Out.SendMessage(e.DebutText, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow));
         
            if (e.HasHandomText)
            {
                e.RandomTextTimer.Start();
            }

            if (e.HasRemainingTimeText)
            {
                e.RemainingTimeTimer.Start();
            }

            e.WantedMobsCount = 0;           

            foreach(var mob in e.Mobs)
            {
                mob.AddToWorld();

                if (e.IsKillingEvent && e.MobNamesToKill.Contains(mob.Name))
                {
                    e.WantedMobsCount++;
                }

                if (e.StartEffects.ContainsKey(mob.InternalID))
                {
                    this.ApplyEffect(mob, e.StartEffects);
                }
            }

            if (e.IsKillingEvent)
            {
                int delta = e.MobNamesToKill.Count() - e.WantedMobsCount;
            
                if (e.WantedMobsCount == 0 && e.EndingConditionTypes.Where(ed => ed != EndingConditionType.Kill).Count() == 0)
                {
                    log.Error(string.Format("Event ID: {0}, Name: {1}, cannot be start because No Mobs found for Killing Type ending and no other ending type set", e.ID, e.EventName));
                    return false;
                }
                else if (delta > 0)
                {
                    log.Error(string.Format("Event ID: {0}, Name {1}: with Kill type has {2} mobs missings, MobNamesToKill column in datatabase and tagged mobs Name should match", e.ID, e.EventName, delta));
                }
            }
        

            foreach (var coffre in e.Coffres)
            {
                coffre.AddToWorld();

                if (e.StartEffects.ContainsKey(coffre.InternalID))
                {
                    ApplyEffect(coffre, e.StartEffects);
                }
            }

            log.Info(string.Format("Event ID: {0}, Name: {1} was Launched At: {2}", e.ID, e.EventName, DateTime.Now.ToLocalTime()));

            e.SaveToDatabase();

            return true;
        }

        private void ApplyEffect(GameObject item, Dictionary<string, ushort> dic)
        {
            foreach (GamePlayer pl in item.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                pl.Out.SendSpellEffectAnimation(item, item, dic[item.InternalID], 0, false, 5);
            }
        }

        public async Task StopEvent(GameEvent e, EndingConditionType end)
        {
            e.EndTime = DateTimeOffset.Now;


            foreach(var mob in e.Mobs)
            {
                if (e.EndEffects.ContainsKey(mob.InternalID))
                {
                    this.ApplyEffect(mob, e.EndEffects);
                }
            }

            foreach(var coffre in e.Coffres)
            {
                if (e.EndEffects.ContainsKey(coffre.InternalID))
                {
                    this.ApplyEffect(coffre, e.EndEffects);
                }
            }

            if (end == EndingConditionType.Kill && e.IsKillingEvent)
            {
                e.Status = EventStatus.EndedByKill;
                //Allow time to loot
                await Task.Delay(TimeSpan.FromSeconds(20));
                CleanEvent(e);
            }
            else if (end == EndingConditionType.StartingEvent)
            {
                e.Status = EventStatus.EndedByEventStarting;
                CleanEvent(e);
            }
            else if (end == EndingConditionType.Timer)
            {
                e.Status = EventStatus.EndedByTimer;
                CleanEvent(e);
            }

            if (e.EndText != null && e.EventZones?.Any() == true)
            {
                NotifyPlayersInEventZones(e.EndText, e.EventZones);
                //Enjoy the message
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            //Handle Consequences
            //Consequence A
            if (e.EndingConditionTypes.Count() == 1 || (e.EndingConditionTypes.Count() > 1 && e.EndingConditionTypes.First() == end))
            {
                this.HandleConsequence(e.EndingActionA, e.EventZones, e.EndingActionEventID);
            }
            else
            {          
                //Consequence B
                this.HandleConsequence(e.EndingActionB, e.EventZones, e.EndingActionEventID);     
            }
 
            e.SaveToDatabase();
        }

        private void HandleConsequence(EndingAction action, IEnumerable<string> zones, string eventId)
        {
            if (action == EndingAction.BindStone)
            {
                foreach(var cl in WorldMgr.GetAllPlayingClients().Where(c => zones.Contains(c.Player.CurrentZone.ID.ToString())))
                {
                    cl.Player.MoveToBind();
                }

                return;
            }
            
            if (action == EndingAction.Event && eventId != null)
            {
                var ev = Instance.Events.FirstOrDefault(e => e.ID.Equals(eventId));

                if (ev == null)
                {
                    log.Error(string.Format("Ending Consequence Event: Impossible to start Event ID: {0}. Event not found.", eventId));
                }

                if (ev.StartConditionType != StartingConditionType.Event)
                {
                    log.Error(string.Format("Ending Consequence Event: Impossible to start Event ID: {0}. Event is not Event Start type", eventId));
                }
                else
                {
                    Instance.StartEvent(ev);                   
                }   
            }
        }

        public static void NotifyPlayersInEventZones(string message, IEnumerable<string> zones)
        {
            foreach (var cl in WorldMgr.GetAllPlayingClients().Where(c => zones.Contains(c.Player.CurrentZone.ID.ToString())))
            {
                cl.Out.SendMessage(message, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
            }
        }


        private static void CleanEvent(GameEvent e)
        {
            foreach (var mob in e.Mobs)
            {
                mob.RemoveFromWorld();
            }

            foreach (var coffre in e.Coffres)
            {
                coffre.RemoveFromWorld();
            }

            e.Clean();

            log.Info(string.Format("Event ID: {0}, Name: {1} was Stopped At: {2}", e.ID, e.EventName, DateTime.Now.ToLocalTime()));
        }
    }
}
