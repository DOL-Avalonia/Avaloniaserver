using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using DOL.spells.negative;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.spells
{
    [SpellHandler("Unpetrify")]
    public class UnpetrifySpellHandler : SpellHandler
    {
        public UnpetrifySpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }

        public override int CalculateSpellResistChance(GameLiving target)
        {
            return 0;
        }

        public override void OnDirectEffect(GameLiving target, double effectiveness)
        {
            foreach (GameSpellEffect activeEffect in target.EffectList.ToList())
            {
                if (activeEffect.SpellHandler is PetrifySpellHandler)
                { 
                    activeEffect.Cancel(false);
                }
            }
            base.OnDirectEffect(target, effectiveness);
        }
    }
}
