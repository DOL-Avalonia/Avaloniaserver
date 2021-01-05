using DOL.GS;
using DOL.GS.PacketHandler;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Spells
{
    [SpellHandler("OmniHeal")]
    public class OmniHealSpellHandler : HealSpellHandler
    {
        public OmniHealSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }

        public override bool StartSpell(GameLiving target)
        {
            if (!base.StartSpell(target))
                return false;
            var targets = SelectTargets(target);
            int min, max;
            CalculateHealVariance(out min, out max);
            foreach (GameLiving healTarget in targets)
            {
                int amount = Util.Random(min, max);
                if (SpellLine.KeyName == GlobalSpellsLines.Item_Effects)
                {
                    amount = max;
                }
                int endurance  = healTarget.ChangeEndurance(Caster, GameLiving.eEnduranceChangeType.Spell, amount);
                int power = healTarget.ChangeMana(Caster, GameLiving.eManaChangeType.Spell, amount);
                if (m_caster == healTarget)
                {
                    MessageToCaster("You gain for " + endurance + " endurance and " + power + " power points.", eChatType.CT_Spell);
                }
                else
                {
                    MessageToCaster("You heal " + target.GetName(0, false) + " for " + endurance + " endurance and " + power + " power points.", eChatType.CT_Spell);
                    MessageToLiving(target, "You are healed by " + m_caster.GetName(0, false) + " for " + endurance + " endurance and " + power + " power points.", eChatType.CT_Spell);
                }
            }
                
            return true;
        }
    }
}
