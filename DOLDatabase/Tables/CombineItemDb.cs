﻿using DOL.Database;
using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOLDatabase.Tables
{
    [DataTable(TableName = "combineitem")]
    public class CombineItemDb
        : DataObject
    {
        private string m_itemsIds;
        private int m_spellEffect;
        private string m_itemTemplateId;

        [DataElement(AllowDbNull = false)]
        public string ItemsIds
        {
            get => m_itemsIds;

            set
            {
                m_itemsIds = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public int SpellEffect
        {
            get => m_spellEffect;

            set
            {
                m_spellEffect = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string ItemTemplateId
        {
            get => m_itemTemplateId;

            set
            {
                m_itemTemplateId = value;
                Dirty = true;
            }
        }
    }
}
