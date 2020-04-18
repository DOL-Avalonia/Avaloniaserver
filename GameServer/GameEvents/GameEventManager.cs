using DOL.Database;
using DOL.events.server;
using DOL.Events;
using DOL.GS;
using DOLDatabase.Tables;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GameEvents
{
    public class GameEventManager
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static GameEventManager instance;

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
                                newEvent.Coffres.Add(coffre);
                                Instance.PreloadedCoffres.Remove(coffre);
                                coffreCount++;
                            }
                        }
                    }

                    foreach(var mobInfo in objects.Where(o => o.IsMob))
                    {
                        var mob = Instance.PreloadedMobs.FirstOrDefault(c => c.InternalID.Equals(mobInfo.ItemID));

                        if (mob != null)
                        {
                            newEvent.Mobs.Add(mob);
                            Instance.PreloadedMobs.Remove(mob);
                            mobCount++;
                        }
                    }
                }

                Instance.Events.Add(newEvent);
            }
            log.Info(string.Format("{0} Mobs Loaded Into Events", mobCount));
            log.Info(string.Format("{0} Coffre Loaded Into Events", coffreCount));
            log.Info(string.Format("{0} Events Loaded", Instance.Events.Count()));

            CreateMissingRelationObjects();
        }

        /// <summary>
        /// Add Tagged Mob or Coffre with EventID in Database
        /// </summary>
        private static void CreateMissingRelationObjects()
        {
            foreach(var obj in Instance.PreloadedCoffres)
            {
                var newCoffre = new EventsXObjects()
                {
                    EventID = obj.EventID,
                    ItemID = obj.InternalID,
                    IsCoffre = true,
                    Region = obj.CurrentRegionID                  
                };

                GameServer.Database.AddObject(newCoffre);
            }

            Instance.PreloadedCoffres.Clear();

            foreach (var obj in Instance.PreloadedMobs)
            {
                var newCoffre = new EventsXObjects()
                {
                    EventID = obj.EventID,
                    ItemID = obj.InternalID,
                    IsMob = true,
                    Region = obj.CurrentRegionID
                };

                GameServer.Database.AddObject(newCoffre);
            }

            Instance.PreloadedMobs.Clear();
        }

        public List<string> GetPublicEventsInfos()
        {
            List<string> infos = new List<string>();

            foreach (var e in Instance.Events.Where(e => e.ShowEvent && e.EndingStatus == EndingType.NotOver))
            {
                infos.Add(" --  EVENT -- ");
                infos.Add("");
                infos.Add(" -- Name: " + e.EventName);
                infos.Add(" -- EventArea: " + (e.EventArea != null ? string.Join(",", e.EventArea.Split(new char[] { '|' })) : string.Empty));
                infos.Add(" -- EventZone: " + (e.EventZone != null ? string.Join(",", e.EventZone.Split(new char[] { '|' })) : string.Empty));
                infos.Add(" -- Started Time: " + e.StartedTime.ToLocalTime());
                infos.Add(" -- Remaining Time: " + (e.EndingStatus == EndingType.Timer && e.EndTime != DateTimeOffset.MinValue ? e.EndTime.Subtract(DateTimeOffset.UtcNow).ToString() : string.Empty));
                infos.Add("");
                infos.Add("--------------------");
            }

            return infos;
        }
    }
}
