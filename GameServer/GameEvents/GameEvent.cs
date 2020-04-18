using DOL.Database;
using DOL.GS;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GameEvents
{
    public class GameEvent
    {
        private object _db;

        public GameEvent(EventDB db)
        {
            _db = db.Clone();
            ID = db.ObjectId;
            EventArea = db.EventArea;
            EventChance = db.EventChance;
            EventName = db.EventName;
            EventZone = db.EventZone;
            ShowEvent = db.ShowEvent;
            StartConditionType = (ConditionType)db.StartConditionType;
            EventChanceInterval = TimeSpan.FromMinutes(db.EventChanceInterval);
            DebutText = db.DebutText;
            EndText = db.EndText;
            StartedTime = DateTimeOffset.FromUnixTimeSeconds(db.StartedTime);
            EndTime = db.EndTime != 0 ? DateTimeOffset.FromUnixTimeSeconds(db.EndTime) : DateTimeOffset.MinValue;
            RandomText = db.RandomText;
            RandTextInterval = TimeSpan.FromHours(db.RandTextInterval);
            RemainingTimeInterval = TimeSpan.FromMinutes(db.RemainingTimeInterval);
            RemainingTimeText = db.RemainingTimeText;
            EndingActionA = (EndingAction)db.EndingActionA;
            EndingActionB = (EndingAction)db.EndingActionB;
            this.Coffres = new List<GameStaticItem>();
            this.Mobs = new List<GameNPC>();
        }


        public string ID
        {
            get;
            set;
        }

        public string EventName
        {
            get;
            set;
        }
        
        public string EventArea
        {
            get;
            set;
        }

        public string EventZone
        {
            get;
            set;
        }
        
        public bool ShowEvent
        {
            get;
            set;
        }
        
        public ConditionType StartConditionType
        {
            get;
            set;
        }
        
        public int EventChance
        {
            get;
            set;
        }
        
        public TimeSpan EventChanceInterval
        {
            get;
            set;
        }
        
        public string DebutText
        {
            get;
            set;
        }
      
        public string RandomText
        {
            get;
            set;
        }
       
        public TimeSpan RandTextInterval
        {
            get;
            set;
        }
     
        public string RemainingTimeText
        {
            get;
            set;
        }
              
        public TimeSpan RemainingTimeInterval
        {
            get;
            set;
        }
        
        public string EndText
        {
            get;
            set;
        }
     
        public EndingType EndingStatus
        {
            get;
            set;
        }

        public EndingAction EndingActionA
        {
            get;
            set;
        }

     
        public EndingAction EndingActionB
        {
            get;
            set;
        }
     
        public DateTimeOffset EndTime
        {
            get;
            set;
        }
      
        public DateTimeOffset StartedTime
        {
            get;
            set;
        }

        public List<GameNPC> Mobs
        {
            get;
        }

        public List<GameStaticItem> Coffres
        {
            get;
        }
        



        public void SaveToDatabase()
        {
            var db = _db as EventDB;
            bool needClone = false;
            
            if (db == null)
            {
                db = new EventDB();
                needClone = true;
            }

            db.EventArea = EventArea;
            db.EventChance = EventChance;
            db.EventName = EventName;
            db.EventZone = EventZone;
            db.ShowEvent = ShowEvent;
            db.StartConditionType = (int)StartConditionType;
            db.EventChanceInterval = EventChanceInterval.TotalMinutes;
            db.DebutText = DebutText;
            db.EndText = EndText;
            db.StartedTime = StartedTime.ToUnixTimeSeconds();
            db.EndTime = EndingStatus == EndingType.NotOver ? 0 : EndTime.ToUnixTimeSeconds();
            db.RandomText = RandomText;
            db.RandTextInterval = RandTextInterval.Hours;
            db.RemainingTimeInterval = RemainingTimeInterval.TotalMinutes;
            db.RemainingTimeText = RemainingTimeText;
            db.EndingActionA = (int)EndingActionA;
            db.EndingActionB = (int)EndingActionB;

            if (ID == null)
            {
                GameServer.Database.AddObject(db);
                ID = db.ObjectId;
            }
            else
            {
                GameServer.Database.SaveObject(db);
            }

            if (needClone)
                _db = db.Clone();
        }
    }
}
