using DOL.Database;
using DOL.Database.Attributes;


namespace DOLDatabase.Tables
{
    [DataTable(TableName = "Event")]
    public class EventDB : DataObject
    {
        private string m_eventName;
        private string m_eventArea;
        private string m_eventZone;
        private bool m_showEvent;
        private int m_startConditionType;
        private int m_eventChance;
        private string m_DebutText;
        private string m_RandomText;
        private int m_RandTextInterval;
        private string m_remainingTimeText;
        private double m_remainingTimeInterval;
        private string m_endText;
        private int m_endingStatus;
        private int m_endingActionB;
        private int m_endingActionA;
        private long m_endTime;
        private long m_startedTime;
        private double m_eventChanceInterval;

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string EventName
        {
            get
            {
                return m_eventName;
            }

            set
            {
                m_eventName = value;
                Dirty = true;
            }
        }    

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string EventArea
        {
            get
            {
                return m_eventArea;
            }

            set
            {
                m_eventArea = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string EventZone
        {
            get
            {
                return m_eventZone;
            }

            set
            {
                m_eventZone = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public bool ShowEvent
        {
            get
            {
                return m_showEvent;
            }

            set
            {
                m_showEvent = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int StartConditionType
        {
            get
            {
                return m_startConditionType;
            }

            set
            {
                m_startConditionType = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int EventChance
        {
            get
            {
                return m_eventChance;
            }

            set
            {
                m_eventChance = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double EventChanceInterval
        {
            get
            {
                return m_eventChanceInterval;
            }

            set
            {
                m_eventChanceInterval = value;
                Dirty = true;
            }
        }


        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string DebutText
        {
            get
            {
                return m_DebutText;
            }

            set
            {
                m_DebutText = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string RandomText
        {
            get
            {
                return m_RandomText;
            }

            set
            {
                m_RandomText = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int RandTextInterval
        {
            get
            {
                return m_RandTextInterval;
            }

            set
            {
                m_RandTextInterval = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string RemainingTimeText
        {
            get
            {
                return m_remainingTimeText;
            }

            set
            {
                m_remainingTimeText = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public double RemainingTimeInterval
        {
            get
            {
                return m_remainingTimeInterval;
            }

            set
            {
                m_remainingTimeInterval = value;
                Dirty = true;
            }
        }


        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string EndText
        {
            get
            {
                return m_endText;
            }

            set
            {
                m_endText = value;
                Dirty = true;
            }
        }



        [DataElement(AllowDbNull = false)]
        public int EndingStatus
        {
            get
            {
                return m_endingStatus;
            }

            set
            {
                m_endingStatus = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int EndingActionA
        {
            get
            {
                return m_endingActionA;
            }

            set
            {
                m_endingActionA = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int EndingActionB
        {
            get
            {
                return m_endingActionB;
            }

            set
            {
                m_endingActionB = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true)]
        public long EndTime
        {
            get
            {
                return m_endTime;
            }

            set
            {
                m_endTime = value;
                Dirty = true;
            }
        }


        [DataElement(AllowDbNull = true)]
        public long StartedTime
        {
            get
            {
                return m_startedTime;
            }

            set
            {
                m_startedTime = value;
                Dirty = true;
            }
        }


    }
}
