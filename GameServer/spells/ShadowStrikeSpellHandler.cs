using DOL.Database;
using DOL.GS.Styles;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("ShadowStrike")]
    public class ShadowStrikeSpellHandler : SpellHandler
    {

        public override void FinishSpellCast(GameLiving player)
        {

            GameLiving target = (GameLiving)m_caster.TargetObject;
            int xrange = 0;
            int yrange = 0;
            double angle = 0.00153248422;
            m_caster.MoveTo(player.CurrentRegionID, (int)(target.X - ((xrange + 10) * Math.Sin(angle * target.Heading))), (int)(target.Y + ((yrange + 10) * Math.Cos(angle * target.Heading))), target.Z, m_caster.Heading);

            base.FinishSpellCast(player);

        }

        /// <summary>
		/// Apply effect on target or do spell action if non duration spell
		/// </summary>
		/// <param name="target">target that gets the effect</param>
		/// <param name="effectiveness">factor from 0..1 (0%-100%)</param>
		public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            OnDirectEffect(target, effectiveness);

            if (!HasPositiveEffect)
            {
                AttackData ad = new AttackData();
                ad.Attacker = Caster;
                ad.Target = target;
                ad.Style = new Style(GameServer.Database.SelectObjects<DBStyle>("`StyleID` = @StyleID", new QueryParameter("@StyleID", 968))[0]);

                m_lastAttackData = ad;
            }
        }

        public ShadowStrikeSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}