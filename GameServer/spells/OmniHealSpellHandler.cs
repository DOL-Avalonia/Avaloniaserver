using DOL.GS;
using DOL.GS.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.spells
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
            int min, max;
            CalculateHealVariance(out min, out max);
            int amount = Util.Random(min, max);
            target.ChangeEndurance(Caster, GameLiving.eEnduranceChangeType.Spell, amount);
            target.ChangeMana(Caster, GameLiving.eManaChangeType.Spell, amount);
            return true;
        }
    }
}
