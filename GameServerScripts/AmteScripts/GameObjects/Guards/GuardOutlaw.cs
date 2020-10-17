using DOL.AI.Brain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Scripts
{
    public class GuardOutlaw
        : AmteMob
    {
        public GuardOutlaw()
            : base()
        {
            this.SetOwnBrain(new GuardNPCBrain());
        }

        public GuardOutlaw(INpcTemplate template)
           : base(template)
        {
            this.SetOwnBrain(new GuardNPCBrain());
        }
    }
}
