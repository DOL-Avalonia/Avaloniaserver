/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using DOL.Database;

namespace DOL.GS
{
    /// <summary>
    /// Caer Sidhi Hydra.
    /// </summary>
    /// <author>Kassar</author>
    public class Hydra : GameDragon
    {
        /// <summary>
        /// Spawn adds that will despawn again after 30 seconds.
        /// For Golestandt, these will be level 57-60 GameNPCs and
        /// their numbers will depend on the number of players inside
        /// the lair.
        /// </summary>
        /// <returns>Whether or not any adds were spawned.</returns>
        public override bool CheckAddSpawns()
        {
            base.CheckAddSpawns();  // In order to reset HealthPercentOld.

            int numAdds = Math.Max(1, 2* PlayersInLair);
            for (int add = 1; add <= numAdds; ++add)
            {
                SpawnTimedAdd(1110300, Util.Random(57, 60), X + Util.Random(300, 600), Y + Util.Random(300, 600), 30, false);   // serpent de fiel
            }

            return true;
        }


        /// <summary>
        /// The Breath spell.
        /// </summary>
        protected override Spell Breath
        {
            get
            {
                if (m_breathSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Uninterruptible = true;
                    spell.ClientEffect = 13108;
                    spell.Description = "Nuke";
                    spell.Name = "Hydra's Nuke";
                    spell.Range = 600;
                    spell.Radius = 600;
                    spell.Damage = 1500 * DragonDifficulty / 100;
                    spell.DamageType = (int)eDamageType.Matter;
                    spell.SpellID = 6002;
                    spell.Target = "Enemy";
                    spell.Type = "DirectDamage";
                    m_breathSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_breathSpell);
                }

                return m_breathSpell;
            }
        }

        /// <summary>
        /// The resist debuff spell.
        /// </summary>
        protected override Spell ResistDebuff
        {
            get
            {
                if (m_resistDebuffSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Uninterruptible = true;
                    spell.ClientEffect = 10640;
                    spell.Icon = 10640;
                    spell.Description = "Hydra Matter Resist Debuff";
                    spell.Name = "Dissolve Armor";
                    spell.Range = 600;
                    spell.Radius = 600;
                    spell.Value = 30 * DragonDifficulty / 100;
                    spell.Duration = 30;
                    spell.Damage = 0;
                    spell.DamageType = (int)eDamageType.Matter;
                    spell.SpellID = 6003;
                    spell.Target = "Enemy";
                    spell.Type = "HeatResistDebuff";
                    spell.Message1 = "You feel more vulnerable to heat!";
                    spell.Message2 = "{0} seems vulnerable to heat!";
                    m_resistDebuffSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_resistDebuffSpell);
                }

                return m_resistDebuffSpell;
            }
        }

        /// <summary>
        /// The melee debuff spell.
        /// </summary>
        protected override Spell MeleeDebuff
        {
            get
            {
                if (m_meleeDebuffSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Uninterruptible = true;
                    spell.ClientEffect = 11415;
                    spell.Icon = 11450;
                    spell.Description = "Melee Damage Debuff";
                    spell.Name = "Hydra's Trepidation";
                    spell.Range = 600;
                    spell.Radius = 600;
                    spell.Value = 35;
                    spell.Duration = 90 * DragonDifficulty / 100;
                    spell.Damage = 0;
                    spell.DamageType = (int)eDamageType.Matter;
                    spell.SpellID = 6003;
                    spell.Target = "Enemy";
                    spell.Type = "MeleeDamageDebuff";
                    m_meleeDebuffSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_meleeDebuffSpell);
                }

                return m_meleeDebuffSpell;
            }
        }

        /// <summary>
        /// The ranged debuff spell.
        /// </summary>
        protected override Spell RangedDebuff
        {
            get
            {
                if (m_rangedDebuffSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.Uninterruptible = true;
                    spell.ClientEffect = 2734;
                    spell.Icon = 2734;
                    spell.Description = "Nearsight";
                    spell.Name = "Hydra's Cloud of Blindness";
                    spell.Range = 600;
                    spell.Radius = 600;
                    spell.Value = 75;
                    spell.Duration = 90 * DragonDifficulty / 100;
                    spell.Damage = 0;
                    spell.DamageType = (int)eDamageType.Matter;
                    spell.SpellID = 6003;
                    spell.Target = "Enemy";
                    spell.Type = "Nearsight";
                    spell.Message1 = "You are blinded!";
                    spell.Message2 = "{0} is blinded!";
                    m_rangedDebuffSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_rangedDebuffSpell);
                }

                return m_rangedDebuffSpell;
            }
        }

        protected override Spell Glare
        {
            get
            {
                if (m_glareSpell == null)
                {
                    DBSpell spell = new DBSpell();
                    spell.AllowAdd = false;
                    spell.CastTime = 0;
                    spell.ClientEffect = 5703;
                    spell.Icon = 5703;
                    spell.Description = "Glare";
                    spell.Name = "Dragon Glare";
                    spell.Range = 2500;
                    spell.Radius = 700;
                    spell.Damage = 2000 * DragonDifficulty / 100;
                    spell.RecastDelay = 10;
                    spell.Value = 75;
                    spell.DamageType = (int)eDamageType.Matter;
                    spell.SpellID = 60001;
                    spell.Target = "Enemy";
                    spell.Type = "DirectDamage";
                    m_glareSpell = new Spell(spell, 70);
                    SkillBase.AddScriptedSpell(GlobalSpellsLines.Mob_Spells, m_glareSpell);
                }

                return m_glareSpell;
            }
        }
    }
}
