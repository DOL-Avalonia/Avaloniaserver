using DOL.AI.Brain;

namespace DOL.GS
{
    public class MobChieftain : GameNPC
    {
        public static ushort LINK_DISTANCE = 1000;

        public MobChieftain()
            : base()
        {
            LoadedFromScript = false;
            this.SetOwnBrain(new AmteMobBrain());
        }

        public override void StartAttack(GameObject attackTarget)
        {
            //We leave if this attacker is already handled by the chieftain (meaning he has already called his minions) 
            if (AttackState)
            {
                base.StartAttack(attackTarget);
                return;
            }
            base.StartAttack(attackTarget);
            bool yell = false;
            foreach (GameNPC npc in GetNPCsInRadius(LINK_DISTANCE))
            {
                if (npc is GameNPC && this.Name.EndsWith(npc.GuildName) && !npc.InCombat)
                {
                    npc.StartAttack(attackTarget);
                    yell = true;
                }
            }
            if (yell)
            {
                Yell("Venez à moi, mes serviteurs.");
            }
        }
    }
}
