﻿using DOL.Database;
using DOL.events.server;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.MobGroups;
using DOLDatabase.Tables;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DOL.GameEvents
{
    public class GameEventManager
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly int dueTime = 5000;
        private readonly int period = 10000;
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
            Instance.timer.Change(Timeout.Infinite, Timeout.Infinite);
            Console.WriteLine("GameEvent: " + DateTime.Now.ToString());


            foreach(var ev in this.Events.Where(ev => ev.Status == EventStatus.NotOver))
            {
                //End events with timer over
                if (ev.EndTime.HasValue && ev.EndingConditionTypes.Contains(EndingConditionType.Timer) && DateTime.UtcNow >= ev.EndTime.Value.DateTime)
                {
                    await this.StopEvent(ev, EndingConditionType.Timer);
                }       

                //Start Events Timer
                if (!ev.StartedTime.HasValue && ev.StartTriggerTime.HasValue && ev.StartConditionType == StartingConditionType.Timer)
                {
                    if (DateTime.UtcNow >= ev.StartTriggerTime.Value.DateTime)
                    {
                        await this.StartEvent(ev);
                    }
                }
            }

            var chanceEvents = this.Events.Where(e => e.Status == EventStatus.NotOver && e.EventChanceInterval.HasValue && e.EventChance > 0 && !e.StartedTime.HasValue);
            var rand = new Random((int)(DateTimeOffset.Now.ToUnixTimeSeconds() / 10000));


            //Start Event if chance proc
            foreach(var ev in chanceEvents)
            {
                if (!ev.ChanceLastTimeChecked.HasValue)
                {
                    ev.ChanceLastTimeChecked = DateTimeOffset.UtcNow;
                    ev.SaveToDatabase();
                }
                else
                {
                    if (DateTimeOffset.UtcNow - ev.ChanceLastTimeChecked.Value >= ev.EventChanceInterval.Value)
                    {
                        ev.ChanceLastTimeChecked = DateTimeOffset.UtcNow;
                        if (rand.Next(0, 101) <= ev.EventChance)
                        {
                            await this.StartEvent(ev);
                        }
                    }
                }
            }

            Instance.timer.Change(Instance.period, Instance.period);
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
            foreach (var eventdb in eventsFromDb)
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
                                    if (newEvent.StartEffects.ContainsKey(coffreInfo.ItemID))
                                    {
                                        newEvent.StartEffects[coffreInfo.ItemID] = (ushort)coffreInfo.StartEffect;
                                    }
                                    else
                                    {
                                        newEvent.StartEffects.Add(coffreInfo.ItemID, (ushort)coffreInfo.StartEffect);
                                    }
                                }

                                if (coffreInfo.EndEffect > 0)
                                {
                                    if (newEvent.EndEffects.ContainsKey(coffreInfo.ItemID))
                                    {
                                        newEvent.EndEffects[coffreInfo.ItemID] = (ushort)coffreInfo.EndEffect;
                                    }
                                    else
                                    {
                                        newEvent.EndEffects.Add(coffreInfo.ItemID, (ushort)coffreInfo.EndEffect);
                                    }
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
                                mob.ExperienceEventFactor = mobInfo.ExperienceFactor;
                                newEvent.Mobs.Add(mob);
                                Instance.PreloadedMobs.Remove(mob);
                                mobCount++;

                                if (mobInfo.StartEffect > 0)
                                {
                                    if (newEvent.StartEffects.ContainsKey(mobInfo.ItemID))
                                    {
                                        newEvent.StartEffects[mobInfo.ItemID] = (ushort)mobInfo.StartEffect;
                                    }
                                    else
                                    {
                                        newEvent.StartEffects.Add(mobInfo.ItemID, (ushort)mobInfo.StartEffect);
                                    }
                                }

                                if (mobInfo.EndEffect > 0)
                                {
                                    if (newEvent.EndEffects.ContainsKey(mobInfo.ItemID))
                                    {
                                        newEvent.EndEffects[mobInfo.ItemID] = (ushort)mobInfo.EndEffect;
                                    }
                                    else
                                    {
                                        newEvent.EndEffects.Add(mobInfo.ItemID, (ushort)mobInfo.EndEffect);
                                    }
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
            Instance.timer = new System.Threading.Timer(Instance.TimeCheck, Instance, Instance.dueTime, Instance.period);
            GameEventMgr.Notify(GameServerEvent.GameEventLoaded);
        }


        internal IList<string> GetEventInfo(string id)
        {
            List<string> infos = new List<string>();
            var ev = Instance.Events.FirstOrDefault(e => e.ID.Equals(id));

            if (ev == null)
            {
                return null;
            }

            this.GetMainInformations(ev, infos);
            this.GetGMInformations(ev, infos, false);

            return infos;
        }

        /// <summary>
        /// Add Tagged Mob or Coffre with EventID in Database
        /// </summary>
        public static void CreateMissingRelationObjects(IEnumerable<string> eventIds)
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
                        else
                        {
                            ev.Coffres.Add(obj);
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
                    ExperienceFactor = obj.ExperienceEventFactor,
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
                            npc.Delete();
                        }
                        else
                        {
                            ev.Mobs.Add(obj);
                        }
                    }
                }

                GameServer.Database.AddObject(newMob);
            }

            Instance.PreloadedMobs.Clear();
        }

        internal IList<string> GetEventsLightInfos()
        {
            List<string> infos = new List<string>();

            foreach(var e in this.Events.Where(e => e.Status == EventStatus.NotOver))
            {
                GetMainInformations(e, infos);

                GetGMInformations(e, infos, true);

                infos.Add("");
                infos.Add("--------------------");
            }


            return infos;
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
                    this.GetGMInformations(e, infos, false);
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
            
            if (e.EndingConditionTypes.Contains(EndingConditionType.Timer) && e.EndTime.HasValue)
            {
                infos.Add(" -- Remaining Time: " + (e.Status == EventStatus.NotOver ? string.Format(@"{0:dd\:hh\:mm\:ss}", e.EndTime.Value.Subtract(DateTimeOffset.UtcNow)) : "-"));
            }
        }

        private void GetGMInformations(GameEvent e, List<string> infos, bool isLight)
        {
            infos.Add(" -- Status: " + e.Status.ToString());
            infos.Add(" -- StartConditionType: " + e.StartConditionType.ToString());
            infos.Add(" -- StartActionStopEventID: " + e.StartActionStopEventID ?? string.Empty);
            infos.Add(" -- DebutText: " + e.DebutText ?? string.Empty);
            infos.Add(" -- TimerType: " + e.TimerType.ToString());
            infos.Add(" -- ChronoTime: " + e.ChronoTime + " mins");
            infos.Add(" -- EndTime: " + (e.EndTime.HasValue ? e.EndTime.Value.ToLocalTime().ToString() : string.Empty));
            infos.Add(" -- EndingActionA: " + e.EndingActionA.ToString());
            infos.Add(" -- EndingActionB: " + e.EndingActionB.ToString());
            infos.Add(" -- StartTriggerTime: " + (e.StartTriggerTime.HasValue ? e.StartTriggerTime.Value.ToLocalTime().ToString() : string.Empty));
            infos.Add(" -- MobNamesToKill: " + (e.MobNamesToKill != null ? string.Join(",", e.MobNamesToKill) : "-"));
            infos.Add(" -- KillStartingGroupMobId: " + (e.KillStartingGroupMobId ?? "-"));
            infos.Add(" -- ResetEventId: " + (e.ResetEventId ?? "-"));
            infos.Add(" -- ChanceLastTimeChecked: " + (e.ChanceLastTimeChecked.HasValue ? e.ChanceLastTimeChecked.Value.ToLocalTime().ToString() : "-"));
            infos.Add(" -- EndingConditionTypes: ");
            foreach(var t in e.EndingConditionTypes)
            {
                infos.Add("    * " + t.ToString());
            }
         
            infos.Add(" -- EndActionStartEventID: " + (e.EndActionStartEventID ?? string.Empty));
            infos.Add(" -- EndText: " + e.EndText ?? string.Empty);
            infos.Add(" -- EventChance: " + e.EventChance);
            infos.Add(" -- EventChanceInterval: " + (e.EventChanceInterval.HasValue ? (e.EventChanceInterval.Value.TotalMinutes + " mins") : string.Empty));
            infos.Add(" -- RemainingTimeText: " + e.RemainingTimeText ?? string.Empty);
            infos.Add(" -- RemainingTimeInterval: " + (e.RemainingTimeInterval.HasValue ? (e.RemainingTimeInterval.Value.TotalMinutes.ToString() + " mins") : string.Empty));
            infos.Add(" -- ShowEvent: " + e.ShowEvent);        
            infos.Add("");        

            if (!isLight)
            {
                infos.Add(" ------- MOBS ---------- Total ( " + e.Mobs.Count() + " )");
                infos.Add("");
                foreach (var mob in e.Mobs)
                {
                    infos.Add(" * id: " + mob.InternalID);
                    infos.Add(" * Name: " + mob.Name);
                    infos.Add(" * Brain: " + mob.Brain?.GetType()?.FullName ?? string.Empty);
                    infos.Add(string.Format(" * X: {0}, Y: {1}, Z: {2}", mob.X, mob.Y, mob.Z));
                    infos.Add(" * Region: " + mob.CurrentRegionID);
                    infos.Add(" * Zone: " + (mob.CurrentZone != null ? mob.CurrentZone.ID.ToString() : "-"));
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
                    infos.Add(" * Zone: " + (coffre.CurrentZone != null ? coffre.CurrentZone.ID.ToString() : "-"));
                    infos.Add(" * Area: " + (coffre.CurrentAreas != null ? string.Join(",", coffre.CurrentAreas) : string.Empty));
                    infos.Add("");
                }
            }
            else
            {
                infos.Add(" ------- MOBS ---------- Total ( " + e.Mobs.Count() + " )");
                infos.Add(" ------- COFFRES ---------- Total ( " + e.Coffres.Count() + " )");
                infos.Add("");
            }         
        }

        private void ResetEventsFromId(string id)
        {
            Instance.timer.Change(Timeout.Infinite, 0);
            List<string> resetIds = new List<string>();
            var ids = this.GetDependentEventsFromRootEvent(id);

            if (ids == null)
            {
                ids = new string[] { id };
            }
            else
            {
                if (!ids.Contains(id))
                {
                    ids = Enumerable.Concat(ids, new string[] { id });
                }
            }

            foreach (var eventId in ids.OrderBy(i => i))
            {
                var ev = this.Events.FirstOrDefault(e => e.ID.Equals(eventId));

                if (ev == null)
                {
                    break;
                }

                if (ev.TimerType == TimerType.DateType && ev.EndingConditionTypes.Contains(EndingConditionType.Timer) && ev.EndingConditionTypes.Count() == 1)
                {
                    log.Error(string.Format("Cannot Reset Event {0}, Name: {1} with DateType with only Timer as Ending condition", ev.ID, ev.EventName));
                }
                else
                {
                    this.ResetEvent(ev);
                    resetIds.Add(ev.ID);
                }
            }

            if (resetIds.Any())
            {
                log.Info(string.Format("Event Reset called by Event {0}, Reset events are : {1}", id, string.Join(",", resetIds)));
            }
            else
            {
                log.Error(string.Format("Reset called by Event: {0} but not Event Resets", id));
            }

            Instance.timer.Change(Instance.period, Instance.period);
        }

        public void ResetEvent(GameEvent ev)
        {
            ev.StartedTime = (DateTimeOffset?)null;
            ev.EndTime = (DateTimeOffset?)null;
            ev.Status = EventStatus.NotOver;
            ev.WantedMobsCount = 0;

            CleanEvent(ev);

            if (ev.StartConditionType == StartingConditionType.Money)
            {
                //Reset related NPC Money
                var moneyNpcDb = GameServer.Database.SelectObjects<MoneyNpcDb>("`EventID` = @id", new QueryParameter("id", ev.ID))?.FirstOrDefault();

                if (moneyNpcDb != null)
                {
                    var mob = GameServer.Database.FindObjectByKey<Mob>(moneyNpcDb.MobID);

                    if (mob != null)
                    {
                        MoneyEventNPC mobIngame = WorldMgr.Regions[mob.Region].Objects?.FirstOrDefault(o => o?.InternalID?.Equals(mob.ObjectId) == true && o is MoneyEventNPC) as MoneyEventNPC;

                        if (mobIngame != null)
                        {
                            mobIngame.CurrentSilver = 0;
                            mobIngame.CurrentCopper = 0;
                            mobIngame.CurrentGold = 0;
                            mobIngame.CurrentMithril = 0;
                            mobIngame.CurrentPlatinum = 0;
                            mobIngame.SaveIntoDatabase();
                        }
                    }


                    moneyNpcDb.CurrentAmount = 0;
                    GameServer.Database.SaveObject(moneyNpcDb);
                }

            }

            ev.SaveToDatabase();
        }

        public async Task<bool> StartEvent(GameEvent e)
        {
            e.WantedMobsCount = 0;

            if (e.EndingConditionTypes.Contains(EndingConditionType.Timer))
            {
                if (e.TimerType == TimerType.ChronoType)
                {
                    e.EndTime = DateTimeOffset.UtcNow.AddMinutes(e.ChronoTime);
                }
                else
                {
                    //Cannot launch event if Endate is not set in DateType and no other ending exists
                    if (!e.EndTime.HasValue)
                    {
                        if (e.EndingConditionTypes.Count() == 1)
                        {
                            log.Error(string.Format("Cannot Launch Event {0}, Name: {1} with DateType because EndDate is Null", e.ID, e.EventName));
                            return false;
                        }
                        else
                        {
                            log.Warn(string.Format("Event Id: {0}, Name: {1}, started with ending type Timer DateType but Endate is Null, Event Started with other endings", e.ID, e.EventName));
                        }
                    }
                }
            }

            foreach (var mob in e.Mobs)
            {             
                var db = GameServer.Database.FindObjectByKey<Mob>(mob.InternalID);
                mob.LoadFromDatabase(db);

                var groupMob = GameServer.Database.SelectObjects<GroupMobXMobs>("MobID = @mobid", new QueryParameter("mobid", mob.InternalID))?.FirstOrDefault();

                if (groupMob != null)
                {
                    if (MobGroupManager.Instance.Groups.ContainsKey(groupMob.GroupId))
                    {
                        mob.CurrentGroupMob = MobGroupManager.Instance.Groups[groupMob.GroupId];                     
                    }
                    else
                    {
                        var mobgroupDb = GameServer.Database.FindObjectByKey<GroupMobDb>(groupMob.GroupId);
                        if (mobgroupDb != null)
                        {
                            var groupInteraction = mobgroupDb.GroupMobInteract_FK_Id != null ? GameServer.Database.SelectObjects<GroupMobStatusDb>("GroupStatusId = @GroupStatusId", new QueryParameter("GroupStatusId", mobgroupDb.GroupMobInteract_FK_Id))?.FirstOrDefault() : null;
                            var groupOriginStatus = mobgroupDb.GroupMobOrigin_FK_Id != null ? GameServer.Database.SelectObjects<GroupMobStatusDb>("GroupStatusId = @GroupStatusId", new QueryParameter("GroupStatusId", mobgroupDb.GroupMobOrigin_FK_Id))?.FirstOrDefault() : null;
                            mob.CurrentGroupMob = new MobGroup(mobgroupDb, groupInteraction, groupOriginStatus);
                            MobGroupManager.Instance.Groups.Add(groupMob.GroupId, mob.CurrentGroupMob);
                        }
                    }

                    if (!MobGroupManager.Instance.Groups[groupMob.GroupId].NPCs.Contains(mob))
                    {
                        MobGroupManager.Instance.Groups[groupMob.GroupId].NPCs.Add(mob);
                        MobGroupManager.Instance.Groups[groupMob.GroupId].ApplyGroupInfos();
                    }
                }

                if (e.IsKillingEvent && e.MobNamesToKill.Contains(mob.Name))
                {
                    e.WantedMobsCount++;
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
                    log.Error(string.Format("Event ID: {0}, Name {1}: with Kill type has {2} mobs missings, MobNamesToKill column in datatabase and tagged mobs Name should match.", e.ID, e.EventName, delta));
                }
            }

            e.StartedTime = DateTimeOffset.UtcNow;
            e.Status = EventStatus.NotOver;


            if (e.DebutText != null && e.EventZones?.Any() == true)
            {                
                SendEventNotification(e, e.DebutText, (e.Discord == 1 || e.Discord == 3));
            }

            if (e.HasHandomText)
            {
                e.RandomTextTimer.Start();
            }

            if (e.HasRemainingTimeText)
            {
                e.RemainingTimeTimer.Start();
            }

            foreach(var mob in e.Mobs)
            {
                mob.AddToWorld();
            }

            //need give more time to client after addtoworld to perform animation
            await Task.Delay(500);

            foreach (var mob in e.Mobs)
            {
                if (e.StartEffects.ContainsKey(mob.InternalID))
                {
                    this.ApplyEffect(mob, e.StartEffects);
                }
            }

            e.Coffres.ForEach(c => c.AddToWorld());          

            //need give more time to client after addtoworld to perform animation
            await Task.Delay(500);

            foreach (var coffre in e.Coffres)
            {
                if (e.StartEffects.ContainsKey(coffre.InternalID))
                {
                    ApplyEffect(coffre, e.StartEffects);
                }
            }         

            if (!string.IsNullOrEmpty(e.StartActionStopEventID))
            {
                await FinishEventByEventById(e.ID, e.StartActionStopEventID);
            }

            log.Info(string.Format("Event ID: {0}, Name: {1} was Launched At: {2}", e.ID, e.EventName, DateTime.Now.ToLocalTime()));
            e.SaveToDatabase();

            return true;
        }

        private async Task FinishEventByEventById(string originEventId, string startActionStopEventID)
        {
            var ev = this.Events.FirstOrDefault(e => e.ID.Equals(startActionStopEventID));

            if (ev == null)
            {
                log.Error(string.Format("Impossible To Stop Event Id {0} from StartActionStopEventID (Event {1}). Event not found", startActionStopEventID, originEventId));
                return;
            }         

            log.Info(string.Format("Stop Event Id {0} from StartActionStopEventID (Event {1})", startActionStopEventID, originEventId));
            await this.StopEvent(ev, EndingConditionType.StartingEvent);
        }

        private void ApplyEffect(GameObject item, Dictionary<string, ushort> dic)
        {
            foreach (GamePlayer pl in item.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE(item.CurrentRegion)))
            {
                pl.Out.SendSpellEffectAnimation(item, item, dic[item.InternalID], 0, false, 5);


            }
        }

        private void SendEventNotification(GameEvent e, string message, bool sendDiscord)
        {
            NotifyPlayersInEventZones(e.AnnonceType, message, e.EventZones);
            if (Properties.DISCORD_ACTIVE && sendDiscord)
            {
                var hook = new DolWebHook(Properties.DISCORD_WEBHOOK_ID);
                hook.SendMessage(message);
            }
        }

        public async Task StopEvent(GameEvent e, EndingConditionType end)
        {
            e.EndTime = DateTimeOffset.UtcNow;

            if (end == EndingConditionType.Kill && e.IsKillingEvent)
            {
                e.Status = EventStatus.EndedByKill;
                //Allow time to loot
                await Task.Delay(TimeSpan.FromSeconds(15));
                await ShowEndEffects(e);
                CleanEvent(e);
            }
            else if (end == EndingConditionType.StartingEvent)
            {
                e.Status = EventStatus.EndedByEventStarting;
                await ShowEndEffects(e);
                CleanEvent(e);
            }
            else if (end == EndingConditionType.Timer)
            {
                e.Status = EventStatus.EndedByTimer;
                await ShowEndEffects(e);
                CleanEvent(e);
            }

            if (e.EndText != null && e.EventZones?.Any() == true)
            {
                string message = e.EndText;
                if (message.Contains("<guilde>"))
                {
                    if (e.Owner != null && e.Owner.Guild != null)
                    {
                        message = message.Replace("<guilde>", e.Owner.GuildName);
                        SendEventNotification(e, message, (e.Discord == 2 || e.Discord == 3));
                    }         
                }
                else if (message.Contains("<player>"))
                {
                    if (e.Owner != null)
                    {
                        message = message.Replace("<player>", e.Owner.Name);
                        SendEventNotification(e, message, (e.Discord == 2 || e.Discord == 3));

                    }
                }
                else if (message.Contains("<group>"))
                {
                    if (e.Owner != null && e.Owner.Group != null)
                    {
                        message = message.Replace("<group>", e.Owner.Group.Leader.Name);
                        SendEventNotification(e, message, (e.Discord == 2 || e.Discord == 3));

                    }
                } 
                else if (message.Contains("<race>"))
                {
                    if (e.Owner != null)
                    {
                        message = message.Replace("<race>", e.Owner.RaceName);
                        SendEventNotification(e, message, (e.Discord == 2 || e.Discord == 3));

                    }
                }
                else if (message.Contains("<class>"))
                {
                    if (e.Owner != null)
                    {
                        message = message.Replace("<class>", e.Owner.CharacterClass.Name);
                        SendEventNotification(e, message, (e.Discord == 2 || e.Discord == 3));

                    }
                }
                else
                {
                    SendEventNotification(e, message, (e.Discord == 2 || e.Discord == 3));

                }
                //Enjoy the message
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            //Handle Consequences
            //Consequence A
            if (e.EndingConditionTypes.Count() == 1 || (e.EndingConditionTypes.Count() > 1 && e.EndingConditionTypes.First() == end))
            {
                await this.HandleConsequence(e.EndingActionA, e.EventZones, e.EndActionStartEventID, e.ResetEventId);
            }
            else
            {
                //Consequence B
                await this.HandleConsequence(e.EndingActionB, e.EventZones, e.EndActionStartEventID, e.ResetEventId);
            }

            log.Info(string.Format("Event Id: {0}, Name: {1} was stopped At: {2}", e.ID, e.EventName, DateTime.Now.ToString()));

            //Handle Interval Starting Event
            //let a chance to this event to trigger at next interval
            if (e.StartConditionType == StartingConditionType.Interval)
            {
                e.Status = EventStatus.NotOver;
                e.StartedTime = null;
                e.EndTime = null;
                e.ChanceLastTimeChecked = (DateTimeOffset?)null;
            }

            e.SaveToDatabase();
        }

        private async Task ShowEndEffects(GameEvent e)
        {
            foreach (var mob in e.Mobs)
            {
                if (e.EndEffects.ContainsKey(mob.InternalID))
                {
                    this.ApplyEffect(mob, e.EndEffects);
                }
            }

            foreach (var coffre in e.Coffres)
            {
                if (e.EndEffects.ContainsKey(coffre.InternalID))
                {
                    this.ApplyEffect(coffre, e.EndEffects);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        private async Task HandleConsequence(EndingAction action, IEnumerable<string> zones, string startEventId, string resetEventId)
        {
            if (action == EndingAction.BindStone)
            {
                foreach(var cl in WorldMgr.GetAllPlayingClients().Where(c => zones.Contains(c.Player.CurrentZone.ID.ToString())))
                {
                    cl.Player.MoveToBind();
                }

                return;
            }

            if (action == EndingAction.Event && startEventId != null)
            {
                var ev = Instance.Events.FirstOrDefault(e => e.ID.Equals(startEventId));

                if (ev == null)
                {
                    log.Error(string.Format("Ending Consequence Event: Impossible to start Event ID: {0}. Event not found.", startEventId));
                    return;
                }

                if (ev.StartConditionType != StartingConditionType.Event)
                {
                    log.Error(string.Format("Ending Consequence Event: Impossible to start Event ID: {0}. Event is not Event Start type", startEventId));
                }
                else
                {
                    ev.StartedTime = null;
                    await Instance.StartEvent(ev);                   
                }
            }
            else if (action == EndingAction.Reset && resetEventId != null)
            {
                var resetEvent = this.Events.FirstOrDefault(e => e.ID.Equals(resetEventId));

                if (resetEvent == null)
                {
                    log.Error("Impossible to reset Event from resetEventId : " + resetEventId);
                    return;
                }

                if (resetEvent.TimerType == TimerType.DateType && resetEvent.EndingConditionTypes.Contains(EndingConditionType.Timer) && resetEvent.EndingConditionTypes.Count() == 1)
                {
                    log.Error(string.Format("Cannot Reset Event {0}, Name: {1} with DateType with only Timer as Ending condition", resetEvent.ID, resetEvent.EventName));
                }
                else
                {
                    this.ResetEventsFromId(resetEventId);
                }
            }
        }

        public static void NotifyPlayersInEventZones(AnnonceType annonceType, string message, IEnumerable<string> zones)
        {
            eChatType type;
            eChatLoc loc;

            switch (annonceType)
            {
                case AnnonceType.Log:
                    type = eChatType.CT_Merchant;
                    loc = eChatLoc.CL_SystemWindow;
                    break;

                case AnnonceType.Send:
                    type = eChatType.CT_Send;
                    loc = eChatLoc.CL_SystemWindow;
                    break;

                case AnnonceType.Windowed:
                    type = eChatType.CT_System;
                    loc = eChatLoc.CL_PopupWindow;
                    break;

                default:
                    type = eChatType.CT_ScreenCenter;
                    loc = eChatLoc.CL_SystemWindow;
                    break;
            }
         
            foreach (var cl in WorldMgr.GetAllPlayingClients().Where(c => zones.Contains(c.Player.CurrentZone.ID.ToString())))
            {
                if (annonceType == AnnonceType.Confirm)
                {
                    cl.Out.SendDialogBox(eDialogCode.CustomDialog, 0, 0, 0, 0, eDialogType.Ok, true, message);
                }
                else
                {
                    cl.Out.SendMessage(message, type, loc);
                }
            }
        }

        private static void CleanEvent(GameEvent e)
        {
            foreach (var mob in e.Mobs)
            {
                if (mob.ObjectState == GameObject.eObjectState.Active)
                {
                    if (!(mob is MoneyEventNPC))
                        mob.Health = 0;
                    
                    mob.RemoveFromWorld();
                    mob.Delete();
                }
            }

            foreach (var coffre in e.Coffres)
            {
                if (coffre.ObjectState == GameObject.eObjectState.Active)
                    coffre.RemoveFromWorld();
            }

            e.Clean();
        }

        public IEnumerable<string> GetDependentEventsFromRootEvent(string id)
        {
            List<string> ids = new List<string>();
            IEnumerable<string> newIds = Enumerable.Empty<string>();          
            
            var ev = Instance.Events.FirstOrDefault(e => e.ID.Equals(id));

            if (ev != null)
            {
                var deps = this.SearchDependencies(ev.ID);

                if (deps != null)
                {
                    ids = deps.ToList();
                    List<string> loopMem = new List<string>();
                    while (newIds != null)
                    {
                        loopMem.Clear();
                        foreach (var i in ids)
                        {
                            deps = this.SearchDependencies(i);

                            if (deps != null)
                            {
                                newIds = this.Add(deps, ids);
                                if (newIds != null)
                                    loopMem.AddRange(newIds);
                            }                          
                        }

                        if (loopMem.Any())
                        {
                            loopMem.ForEach(i => ids.Add(i));
                        }

                        if (deps == null)
                        {
                            newIds = null;
                        }
                    }
                }

                return ids.Any() ? ids : null;
            }

            return null;
        }


        private IEnumerable<string> Add(IEnumerable<string> deps, List<string> ids)
        {
            List<string> newIds = new List<string>();

            foreach (var id in deps)
            {
                if (!ids.Contains(id))
                {
                    newIds.Add(id);                    
                }
            }

            return (newIds == null || !newIds.Any()) ? null : newIds;
        }

        private IEnumerable<string> SearchDependencies(string id)
        {
           var ev = Instance.Events.Where(e => e.EndActionStartEventID?.Equals(id) == true);

            if (!ev.Any())
            {
                return null;
            }

            return ev.Select(e => e.ID);
        }
    }
}
