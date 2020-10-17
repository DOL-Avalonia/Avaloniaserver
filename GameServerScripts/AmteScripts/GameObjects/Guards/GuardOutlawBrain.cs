using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.AI.Brain
{
    public class GuardOutlawBrain
        : GuardNPCBrain
    {

        protected override void CheckPlayerAggro()
        {
            if (Body.AttackState)
                return;
            foreach (GamePlayer pl in Body.GetPlayersInRadius((ushort)AggroRange))
            {
                if (!pl.IsAlive || pl.ObjectState != GameObject.eObjectState.Active || !GameServer.ServerRules.IsAllowedToAttack(Body, pl, true))
                    continue;


                if (pl.IsStealthed)
                    pl.Stealth(false);

                //Check Reputation
                if (pl.Reputation >= 0)
                {
                    //Full aggression against Non outlaws
                    AddToAggroList(pl, 1);
                    BringReinforcements(pl);
                    continue;
                }

                int aggro = CalculateAggroLevelToTarget(pl);
                if (aggro <= 0)
                    continue;
                AddToAggroList(pl, aggro);
                if (pl.Level > Body.Level - 20 || (pl.Group != null && pl.Group.MemberCount >= 2))
                    BringReinforcements(pl);
            }
        }
    }
}
