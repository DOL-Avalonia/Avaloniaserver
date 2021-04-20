using System.Linq;
using DOL.Database;
using DOL.GS;
using DOL.GS.Scripts;

namespace DOL.AI.Brain
{
	public class AreaEffectBrain : APlayerVicinityBrain
	{
		public override int ThinkInterval
		{
			get { return 1000; }
		}

        protected static SpellLine m_mobSpellLine = SkillBase.GetSpellLine(GlobalSpellsLines.Mob_Spells);

        public override void Think()
		{
			if(Body is AreaEffect areaEffect)
            {
                if(areaEffect.SpellID != 0)
                {
                    DBSpell dbspell = GameServer.Database.SelectObjects<DBSpell>("`SpellID` = @SpellID", new QueryParameter("@SpellID", areaEffect.SpellID)).FirstOrDefault();
                    Spell spell = new Spell(dbspell, 0);
                    
                    foreach (GamePlayer player in areaEffect.GetPlayersInRadius((ushort)dbspell.Radius))
                    {
                        if ((spell.Duration == 0 || !player.HasEffect(spell) || spell.SpellType.ToUpper() == "DIRECTDAMAGEWITHDEBUFF"))
                        {
                            Body.TurnTo(player);

                            Body.CastSpellOnOwnerAndPets(player, spell, m_mobSpellLine);
                        }
                    }
                }
                else
                    ((AreaEffect)Body).ApplyEffect();
            }
		}
	}
}
