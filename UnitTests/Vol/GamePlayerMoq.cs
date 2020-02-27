using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.Vol
{
    public class GamePlayerMoq
        : IGamePlayer
    {
        public byte Level { get; set ; }

        public long GetCurrentMoney()
        {
            return 100L;
        }

        public bool HasAbility(string keyName)
        {
            return true;
        }
    }
}
