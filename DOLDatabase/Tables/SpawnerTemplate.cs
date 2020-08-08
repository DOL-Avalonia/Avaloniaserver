using DOL.Database;
using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOLDatabase.Tables
{
    [DataTable(TableName = "SpawnerTemplate")]
    public class SpawnerTemplate
        : DataObject
    {
        private string m_mobID;
        private int m_npcTemplate1;
        private int m_npcTemplate2;
        private int m_npcTemplate3;
        private int m_npcTemplate4;
        private bool m_isAggroType;
        private int m_percentLifeAddsActivity;

        [DataElement(AllowDbNull = false, Varchar = 255, Index = true)]
        public string MobID
        {
            get => m_mobID;
            set { Dirty = true; m_mobID = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int NpcTemplate1
        {
            get => m_npcTemplate1;
            set { Dirty = true; m_npcTemplate1 = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int NpcTemplate2
        {
            get => m_npcTemplate2;
            set { Dirty = true; m_npcTemplate2 = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int NpcTemplate3
        {
            get => m_npcTemplate3;
            set { Dirty = true; m_npcTemplate3 = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int NpcTemplate4
        {
            get => m_npcTemplate4;
            set { Dirty = true; m_npcTemplate4 = value; }
        }

        [DataElement(AllowDbNull = false)]
        public bool IsAggroType
        {
            get => m_isAggroType;
            set { Dirty = true; m_isAggroType = value; }
        }

        [DataElement(AllowDbNull = false)]
        public int PercentLifeAddsActivity
        {
            get => m_percentLifeAddsActivity;
            set { Dirty = true; m_percentLifeAddsActivity = value; }
        }       
    }
}
