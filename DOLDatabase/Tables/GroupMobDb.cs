using DOL.Database;
using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOLDatabase.Tables
{
    [DataTable(TableName ="groupmob")]
    public class GroupMobDb
        : DataObject
    {
        private string m_groupId;


        [DataElement(AllowDbNull = false, Varchar = 255, Unique = true)]
        public string GroupId
        {
            get
            {
                return m_groupId;
            }

            set
            {
                m_groupId = value;
                Dirty = true;
            }
        }

    }
}
