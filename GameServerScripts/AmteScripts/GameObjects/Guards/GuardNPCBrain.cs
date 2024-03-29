using System;
using AmteScripts.Managers;
using DOL.gameobjects.CustomNPC;
using DOL.GS;
using DOL.GS.Scripts;

namespace DOL.AI.Brain
{
    public class GuardNPCBrain : AmteMobBrain
    {
        public override int AggroLevel
        {
            get { return 100; }
            set { }
        }

        public override bool CanBAF 
        {
            get { return true; }
            set { base.CanBAF = value; }
        }

        public override void Think()
        {
            base.Think();
            if (Body is ITextNPC && !Body.InCombat)
                ((ITextNPC)Body).SayRandomPhrase();
        }

		protected override void CheckPlayerAggro()
		{
			if (Body.AttackState)
				return;
			foreach(GamePlayer pl in Body.GetPlayersInRadius((ushort)AggroRange))
			{
				if (!pl.IsAlive || pl.ObjectState != GameObject.eObjectState.Active || !GameServer.ServerRules.IsAllowedToAttack(Body, pl, true))
					continue;

                if (pl.IsStealthed)
                    pl.Stealth(false);

                //Check Reputation
                if (pl.Reputation < 0)
                {
                    //Full aggression against outlaws
                    AddToAggroList(pl, 1);
                    // Use new BAF system 
                    BringFriends(pl);
                    continue;
                }

				int aggro = CalculateAggroLevelToTarget(pl);
				if (aggro <= 0)
					continue;
				AddToAggroList(pl, aggro);
				if (pl.Level > Body.Level - 20 || (pl.Group != null && pl.Group.MemberCount >= 2))
                    // Use new BAF system
                    BringFriends(pl);
			}
		}

        protected override void CheckNPCAggro()
        {
            if (Body.AttackState)
                return;
            foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)AggroRange, Body.CurrentRegion.IsDungeon ? false : true))
            {
                if (npc is ShadowNPC)
                    continue;
				if (npc.Realm != 0 || (npc.Flags & GameNPC.eFlags.PEACE) != 0 ||
					!npc.IsAlive || npc.ObjectState != GameObject.eObjectState.Active ||
					npc is GameTaxi ||
					m_aggroTable.ContainsKey(npc) ||
					!GameServer.ServerRules.IsAllowedToAttack(Body, npc, true))
					continue;

                int aggro = CalculateAggroLevelToTarget(npc);
                    if (aggro <= 0)
                        continue;
                    AddToAggroList(npc, aggro);
                    if (npc.Level > Body.Level)
                    BringFriends(npc);
            }
        }

        private void BringReinforcements(GameNPC target)
        {
            BringFriends(target);
        }

        public override int CalculateAggroLevelToTarget(GameLiving target)
        {
			if (target is AmtePlayer)
			{
				var player = (AmtePlayer)target;
				if (player.Reputation < 0)
                {
                    return 100;
                }
				return GuardsMgr.CalculateAggro(player);
			}
        	if (target.Realm == 0)
                return Math.Max(100, 200 - target.Level);
            return base.CalculateAggroLevelToTarget(target);
        }
    }
}
