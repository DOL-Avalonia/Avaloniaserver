using DOL.Database;
using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOLDatabase.Tables
{
    [DataTable(TableName = "groupMobInteract")]
    public class GroupMobInteract
        : DataObject
    {
        private string m_IsInvincible;
        private string m_flag;
        private string m_visibleSlot;
        private string m_race;
        private string m_model;
        private string m_effect;
        private string m_interactId;

        [DataElement(AllowDbNull = false, Index = true)]
        public string InteractId
        {
            get => m_interactId;
            set { Dirty = true; m_interactId = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 5)]
        public string SetInvincible
        {
            get => m_IsInvincible;
            set { Dirty = true; m_IsInvincible = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Flag
        {
            get => m_flag;
            set { Dirty = true; m_flag = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string VisibleSlot
        {
            get => m_visibleSlot;
            set { Dirty = true; m_visibleSlot = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Race
        {
            get => m_race;
            set { Dirty = true; m_race = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Model
        {
            get => m_model;
            set { Dirty = true; m_model = value; }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string Effect
        {
            get => m_effect;
            set { Dirty = true; m_effect = value; }
        }
    }
}
