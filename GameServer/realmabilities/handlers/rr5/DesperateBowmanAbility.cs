using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;

namespace DOL.GS.RealmAbilities
{
    /// <summary>
    /// Mastery of Concentration RA
    /// </summary>
    public class DesperateBowmanAbility : RR5RealmAbility
    {
        public DesperateBowmanAbility(DBAbility dba, int level) : base(dba, level) { }

        /// <summary>
        /// Action
        /// </summary>
        /// <param name="living"></param>
        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED))
            {
                return;
            }

            GameLiving target = (GameLiving) living.TargetObject;

            if (target == null)
            {
                return;
            }

            if (!GameServer.ServerRules.IsAllowedToAttack(living, target, false))
            {
                return;
            }

            if (!living.IsWithinRadius(target, 1000))
            {
                return;
            }

            if (living.ActiveWeaponSlot != GameLiving.eActiveWeaponSlot.Distance)
            {
                return;
            }

            SendCasterSpellEffectAndCastMessage(living, 7061, true);
            new DesperateBowmanDisarmEffect().Start(living);
            new DesperateBowmanStunEffect().Start(target);
            DamageTarget(target, living);
            DisableSkill(living);
        }

        private void DamageTarget(GameLiving target, GameLiving caster)
        {

            int damage = 300;

            if (caster is GamePlayer player)
            {
                player.Out.SendMessage($"You hit {target.Name} for {damage} points of damage!", eChatType.CT_YouHit, eChatLoc.CL_SystemWindow);
            }

            if (target is GamePlayer targetPlayer)
            {
                if (targetPlayer.IsStealthed)
                {
                    targetPlayer.Stealth(false);
                }
            }

            AttackData ad = new AttackData
            {
                AttackResult = GameLiving.eAttackResult.HitUnstyled,
                Attacker = caster,
                Target = target,
                DamageType = eDamageType.Crush,
                Damage = damage
            };

            target.OnAttackedByEnemy(ad);
            caster.DealDamage(ad);
        }

        public override int GetReUseDelay(int level)
        {
            return 420;
        }

        public override void AddEffectsInfo(IList<string> list)
        {
            list.Add("Stuns your target for 5 seconds and damage it for 300 , but disarms you for 15 seconds! You need a bow in your hand to use this ability!");
            list.Add(string.Empty);
            list.Add("Range: 1000");
            list.Add("Target: Enemy");
            list.Add("Casting time: instant");
        }
    }
}