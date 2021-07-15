using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("SpellReflection")]
    public class SpellReflectionHandler : SpellHandler
    {
        public SpellReflectionHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            GameSpellEffect damnationEffect = FindEffectOnTarget(target, "SpellReflection");
            if (damnationEffect != null)
            {
                if (Caster is GamePlayer player)
                    MessageToCaster(LanguageMgr.GetTranslation(player.Client, "SpellReflection.Target.Resist", target.Name), eChatType.CT_SpellResisted);
                return;
            }
            base.ApplyEffectOnTarget(target, effectiveness);
        }

        public override void OnEffectStart(GameSpellEffect effect)
        {
            GameLiving living = effect.Owner;
            
            GameEventMgr.AddHandler(living, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventHandler));
            if (Caster is GamePlayer casterPlayer)
            {
                MessageToLiving(casterPlayer, LanguageMgr.GetTranslation(casterPlayer.Client, "SpellReflection.Self.Message"), eChatType.CT_Spell);
            }
            SendEffectAnimation(effect.Owner, 0, false, 1);
        }

        public void EventHandler(DOLEvent e, object sender, EventArgs arguments)
        {
            if (!(arguments is AttackedByEnemyEventArgs args))
            {
                return;
            }
            AttackData ad = args.AttackData;

            if (ad.AttackType == AttackData.eAttackType.Spell)
            {
                Spell spellToCast = ad.SpellHandler.Spell.Copy();
                int cost;
                GamePlayer player = ad.Target as GamePlayer;
                if (player != null && player.CharacterClass is Salvage)
                {
                    cost = ((spellToCast.Power * Spell.AmnesiaChance / 100) / 2) / (ad.Target.Level / ad.Attacker.Level);
                    spellToCast.CostPower = false;
                }
                else
                {
                    cost = (spellToCast.Power * Spell.AmnesiaChance / 100) / (ad.Target.Level / ad.Attacker.Level);
                    spellToCast.CostPower = true;
                    if (player.Mana < cost)
                        return;
                }
                spellToCast.Power = cost;

                double absorbPercent = Spell.LifeDrainReturn;

                int damageAbsorbed = (int)(0.01 * absorbPercent * (ad.Damage + ad.CriticalDamage));

                ad.Damage -= damageAbsorbed;
                if (player != null)
                    MessageToLiving(player, LanguageMgr.GetTranslation(player.Client, "SpellReflection.Self.Absorb", damageAbsorbed), eChatType.CT_Spell);
                if (ad.Attacker is GamePlayer attacker)
                    MessageToLiving(attacker, LanguageMgr.GetTranslation(attacker.Client, "SpellReflection.Target.Absorbs", damageAbsorbed), eChatType.CT_Spell);

                
                spellToCast.Damage = spellToCast.Damage * Spell.AmnesiaChance / 100;
                spellToCast.Value = spellToCast.Value * Spell.AmnesiaChance / 100;
                spellToCast.Duration = spellToCast.Duration * Spell.AmnesiaChance / 100;
                spellToCast.CastTime = 0;

                switch(ad.DamageType)
                {
                    case eDamageType.Body:
                        spellToCast.ClientEffect = 6172;
                        break;
                    case eDamageType.Cold:
                        spellToCast.ClientEffect = 6057;
                        break;
                    case eDamageType.Energy:
                        spellToCast.ClientEffect = 6173;
                        break;
                    case eDamageType.Heat:
                        spellToCast.ClientEffect = 6171;
                        break;
                    case eDamageType.Matter:
                        spellToCast.ClientEffect = 6174;
                        break;
                    case eDamageType.Spirit:
                        spellToCast.ClientEffect = 6175;
                        break;
                    default:
                        spellToCast.ClientEffect = 6173;
                        break;
                }

                ad.Target.TargetObject = ad.Attacker;
                ad.Target.CastSpell(spellToCast, m_spellLine);
            }
        }

        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            GameLiving living = effect.Owner;

            GameEventMgr.RemoveHandler(effect.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(EventHandler));
            return base.OnEffectExpires(effect, noMessages);
        }
    }
}
