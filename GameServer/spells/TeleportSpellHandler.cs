using DOL.gameobjects.CustomNPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Spells
{
    [SpellHandler("Teleport")]
    public class TeleportSpellHandler : SpellHandler
    {
        public TeleportSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {

        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            if (target is ShadowNPC)
                return;
            if (Spell.LifeDrainReturn > 0)
            {
                TPPoint tPPoint = TeleportMgr.LoadTP((ushort)Spell.LifeDrainReturn);
                switch(tPPoint.Type)
                {
                    case Database.eTPPointType.Random:
                        tPPoint = tPPoint.GetNextTPPoint();
                        break;
                    case Database.eTPPointType.Loop:
                        if(target.TPPoint != null)
                        {
                            tPPoint = target.TPPoint.GetNextTPPoint();
                        }
                        target.TPPoint = tPPoint;
                        break;
                    case Database.eTPPointType.Smart:
                        tPPoint = tPPoint.GetSmarttNextPoint();
                        break;
                }
                target.MoveTo(tPPoint.Region, tPPoint.X, tPPoint.Y, tPPoint.Z, tPPoint.GetHeading(tPPoint));
            }
        }
    }
}
