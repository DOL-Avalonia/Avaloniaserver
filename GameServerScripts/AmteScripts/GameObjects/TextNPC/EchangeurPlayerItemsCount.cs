using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServerScripts.Amtescripts.GameObjects.TextNPC
{
    public class EchangeurPlayerItemsCount
    {
        public Dictionary<string, int> Items
        {
            get;
            set;
        }

        public bool HasAllRequiredItems
        {
            get;
            set;
        }
    }
}
