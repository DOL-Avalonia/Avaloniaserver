﻿using DOL.AI.Brain;

namespace DOL.GS.Spells
{
    [SpellHandler("Quarter")]
    public class QuarterSpellHandler : SpellHandler
    {
        public QuarterSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            AttackData ad = new AttackData();
            ad.Attacker = Caster;
            ad.Target = target;
            ad.AttackType = AttackData.eAttackType.Spell;
            ad.SpellHandler = this;
            ad.AttackResult = GameLiving.eAttackResult.HitUnstyled;
            ad.IsSpellResisted = false;
            if (target.HealthPercent > 25)
            {
                ad.Damage = (target.MaxHealth / 4)*3 - (target.MaxHealth - target.Health);

                m_lastAttackData = ad;
                SendDamageMessages(ad);
                target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
            }
            else
            {
                // Treat non-damaging effects as attacks to trigger an immediate response and BAF
                m_lastAttackData = ad;
                IOldAggressiveBrain aggroBrain = ((GameNPC)ad.Target).Brain as IOldAggressiveBrain;
                if (aggroBrain != null)
                    aggroBrain.AddToAggroList(Caster, 1);
            }
            DamageTarget(ad, true);
        }
    }
}