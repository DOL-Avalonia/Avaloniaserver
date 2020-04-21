using DOL.Database;
using DOL.Database.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOLDatabase.Tables
{
    [DataTable(TableName ="eventXMoneyNpc")]
    public class MoneyNpcDb
        : DataObject
    {
        private string eventId;
        private long currentAmount;
        private long requiredMoney;
        private string mobId;
        private string mobName;
        private string needMoreMoneyText;
        private string validateText;
        private string interactText;

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string MobID
        {
            get => mobId;

            set
            {
                mobId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string MobName
        {
            get => mobName;

            set
            {
                mobName = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false, Varchar = 255)]
        public string EventID
        {
            get => eventId;

            set
            {
                eventId = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public long RequiredMoney
        {
            get => requiredMoney;

            set
            {
                requiredMoney = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string NeedMoreMoneyText
        {
            get => needMoreMoneyText;

            set
            {
                needMoreMoneyText = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string ValidateText
        {
            get => validateText;

            set
            {
                validateText = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = false)]
        public long CurrentAmount
        {
            get => currentAmount;

            set
            {
                currentAmount = value;
                Dirty = true;
            }
        }

        [DataElement(AllowDbNull = true, Varchar = 255)]
        public string InteractText
        {
            get => interactText;

            set
            {
                interactText = value;
                Dirty = true;
            }
        }
    }
}
