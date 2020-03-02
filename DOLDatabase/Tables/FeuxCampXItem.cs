using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.Database
{
    [DataTable(TableName = "FeuxCampXItem")]
    public class FeuxCampXItem : DataObject
    {
        private string m_feuxCampXItemId_nb;
        private int m_radius;
        private int m_power;
        private int m_lifetime;
        private bool m_isHealthType;
        private bool m_isManaType;
        private bool m_isTrapType;
        private int m_trapDamagePercent;

        public FeuxCampXItem()
        {
            AllowAdd = true;
        }

        [PrimaryKey(AutoIncrement = true)]
        public string FeuxCampXItem_ID
        {
            get;
            set;
        }

        /// <summary>
        /// the index
        /// </summary>
        [DataElement(AllowDbNull = false, Index = true)]
        public string FeuxCampItemId_nb
        {
            get
            {
                return m_feuxCampXItemId_nb;
            }

            set
            {
                Dirty = true;
                m_feuxCampXItemId_nb = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int Radius
        {
            get
            {
                return m_radius;
            }

            set
            {
                Dirty = true;
                m_radius = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int Power
        {
            get
            {
                return m_power;
            }

            set
            {
                Dirty = true;
                m_power = value;
            }
        }


        [DataElement(AllowDbNull = false)]
        public int Lifetime
        {
            get
            {
                return m_lifetime;
            }

            set
            {
                Dirty = true;
                m_lifetime = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public bool IsHealthType
        {
            get
            {
                return m_isHealthType;
            }

            set
            {
                Dirty = true;
                m_isHealthType = value;
            }
        }


        [DataElement(AllowDbNull = false)]
        public bool IsManaType
        {
            get
            {
                return m_isManaType;
            }

            set
            {
                Dirty = true;
                m_isManaType = value;
            }
        }

        [DataElement(AllowDbNull = false)]
        public bool IsTrapType
        {
            get
            {
                return m_isTrapType;
            }

            set
            {
                Dirty = true;
                m_isTrapType = value;
            }
        }

        [DataElement(AllowDbNull = true)]
        public int TrapDamagePercent
        {
            get
            {
                return m_trapDamagePercent;
            }

            set
            {
                Dirty = true;
                m_trapDamagePercent = value;
            }
        }
    }
}
