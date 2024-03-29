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
using System.Collections.Generic;
using System.Linq;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Spells
{

    // Main class for savage buffs
    public abstract class AbstractSavageBuff : PropertyChangingSpell
    {
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.BaseBuff;

        /// <summary>
        /// When an applied effect starts
        /// duration spells only
        /// </summary>
        /// <param name="effect"></param>
        public override void OnEffectStart(GameSpellEffect effect)
        {
            base.OnEffectStart(effect);
            SendUpdates(effect.Owner);
        }

        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>();
                list.Add(Spell.Description);

                if (Spell.InstrumentRequirement != 0)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.InstrumentRequire", GlobalConstants.InstrumentTypeToName(Spell.InstrumentRequirement)));
                }

                if (Spell.Damage != 0)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.Damage", Spell.Damage.ToString("0.###;0.###'%'")));
                }

                if (Spell.LifeDrainReturn != 0)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.HealthReturned", Spell.LifeDrainReturn));
                }
                else if (Spell.Value != 0)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.Value", Spell.Value.ToString("0.###;0.###'%'")));
                }

                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.Target", Spell.Target));
                if (Spell.Range != 0)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.Range", Spell.Range));
                }

                if (Spell.Duration >= ushort.MaxValue * 1000)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.Duration") + " Permanent.");
                }
                else if (Spell.Duration > 60000)
                {
                    list.Add($"{LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.Duration")}{Spell.Duration / 60000}:{Spell.Duration % 60000 / 1000:00} min");
                }
                else if (Spell.Duration != 0)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.Duration") + (Spell.Duration / 1000).ToString("0' sec';'Permanent.';'Permanent.'"));
                }

                if (Spell.Frequency != 0)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.Frequency", (Spell.Frequency * 0.001).ToString("0.0")));
                }

                if (Spell.Power != 0)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.HealthCost", Spell.Power.ToString("0;0'%'")));
                }

                list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.CastingTime", (Spell.CastTime * 0.001).ToString("0.0## sec;-0.0## sec;'instant'")));
                if (Spell.RecastDelay > 60000)
                {
                    list.Add(
                        $"{LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.RecastTime")}{Spell.RecastDelay / 60000}:{Spell.RecastDelay % 60000 / 1000:00} min");
                }
                else if (Spell.RecastDelay > 0)
                {
                    list.Add($"{LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.RecastTime")}{Spell.RecastDelay / 1000} sec");
                }

                if (Spell.Concentration != 0)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.ConcentrationCost", Spell.Concentration));
                }

                if (Spell.Radius != 0)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.Radius", Spell.Radius));
                }

                if (Spell.DamageType != eDamageType.Natural)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.Damage", GlobalConstants.DamageTypeToName(Spell.DamageType)));
                }

                if (Spell.IsFocus)
                {
                    list.Add(LanguageMgr.GetTranslation((Caster as GamePlayer)?.Client, "DelveInfo.Focus"));
                }

                return list;
            }
        }

        /// <summary>
		/// Return the given Delve Writer with added keyvalue pairs.
        /// Add cost type equal 2 to replace power cost by health cost 
		/// </summary>
		/// <param name="dw"></param>
		/// <param name="id"></param>
		public override void TooltipDelve(ref MiniDelveWriter dw, int id, GameClient client)
        {
            if (dw == null)
                return;

            int level = Spell.Level;
            int spellID = Spell.ID;

            foreach (SpellLine line in client.Player.GetSpellLines())
            {
                Spell s = SkillBase.GetSpellList(line.KeyName).Where(o => o.ID == spellID).FirstOrDefault();
                if (s != null)
                {
                    level = s.Level;
                    break;
                }
            }

            dw.AddKeyValuePair("Function", "light"); 

            dw.AddKeyValuePair("Index", unchecked((short)id));
            dw.AddKeyValuePair("Name", Spell.Name);

            if (Spell.CastTime > 2000)
                dw.AddKeyValuePair("cast_timer", Spell.CastTime - 2000); 
            else if (!Spell.IsInstantCast)
                dw.AddKeyValuePair("cast_timer", 0); 

            if (Spell.IsInstantCast)
                dw.AddKeyValuePair("instant", "1");

            if ((int)Spell.DamageType > 0)
                dw.AddKeyValuePair("damage_type", Spell.GetDelveDamageType()); 

            if (Spell.Level > 0)
                dw.AddKeyValuePair("level", level);
            if (Spell.CostPower)
            {
                dw.AddKeyValuePair("power_cost", Spell.Power);
                dw.AddKeyValuePair("cost_type", 2);
            }
                
                
            if (Spell.Range > 0)
                dw.AddKeyValuePair("range", Spell.Range);
            if (Spell.Duration > 0)
                dw.AddKeyValuePair("duration", Spell.Duration / 1000); 
            if (GetDurationType() > 0)
                dw.AddKeyValuePair("dur_type", GetDurationType());

            if (Spell.HasRecastDelay)
                dw.AddKeyValuePair("timer_value", Spell.RecastDelay / 1000);

            if (GetSpellTargetType() > 0)
                dw.AddKeyValuePair("target", GetSpellTargetType());

            string description = string.Empty;
            if (!string.IsNullOrEmpty(Spell.Description))
                description = Spell.Description;

            if (Spell.Damage > 0)
            {
                description += string.Format(" Value: ({0})", Spell.Damage);
            }
            else if (Spell.Value > 0)
            {
                description += string.Format(" Value: ({0})", Spell.Value);
            }


            dw.AddKeyValuePair("description_string", description);
            if (Spell.IsAoE)
                dw.AddKeyValuePair("radius", Spell.Radius);
            if (Spell.IsConcentration)
                dw.AddKeyValuePair("concentration_points", Spell.Concentration);
        }

        /// <summary>
        /// When an applied effect expires.
        /// Duration spells only.
        /// </summary>
        /// <param name="effect">The expired effect</param>
        /// <param name="noMessages">true, when no messages should be sent to player and surrounding</param>
        public override int OnEffectExpires(GameSpellEffect effect, bool noMessages)
        {
            base.OnEffectExpires(effect, noMessages);

            if (Spell.Power != 0)
            {
                int cost;
                if (Spell.Power < 0)
                {
                    cost = (int)(Caster.MaxHealth * Math.Abs(Spell.Power) * 0.01);
                }
                else
                {
                    cost = Spell.Power;
                }

                if (effect.Owner.Health > cost)
                {
                    effect.Owner.ChangeHealth(effect.Owner, GameLiving.eHealthChangeType.Spell, -cost);
                }
            }

            SendUpdates(effect.Owner);
            return 0;
        }

        // constructor
        public AbstractSavageBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    public abstract class AbstractSavageStatBuff : AbstractSavageBuff
    {
        /// <summary>
        /// Sends needed updates on start/stop
        /// </summary>
        /// <param name="target"></param>
        protected override void SendUpdates(GameLiving target)
        {
            if (target is GamePlayer player)
            {
                player.Out.SendCharStatsUpdate();
                player.Out.SendUpdateWeaponAndArmorStats();
                player.UpdateEncumberance();
                player.UpdatePlayerStatus();
            }
        }

        // constructor
        public AbstractSavageStatBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    public abstract class AbstractSavageResistBuff : AbstractSavageBuff
    {
        /// <summary>
        /// Sends needed updates on start/stop
        /// </summary>
        /// <param name="target"></param>
        protected override void SendUpdates(GameLiving target)
        {
            if (target is GamePlayer player)
            {
                player.Out.SendCharResistsUpdate();
                player.UpdatePlayerStatus();
            }
        }

        // constructor
        public AbstractSavageResistBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    [SpellHandler("SavageParryBuff")]
    public class SavageParryBuff : AbstractSavageStatBuff
    {
        public override eProperty Property1 => eProperty.ParryChance;

        // constructor
        public SavageParryBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    [SpellHandler("SavageEvadeBuff")]
    public class SavageEvadeBuff : AbstractSavageStatBuff
    {
        public override eProperty Property1 => eProperty.EvadeChance;

        // constructor
        public SavageEvadeBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    [SpellHandler("SavageCombatSpeedBuff")]
    public class SavageCombatSpeedBuff : AbstractSavageStatBuff
    {
        public override eProperty Property1 => eProperty.MeleeSpeed;

        // constructor
        public SavageCombatSpeedBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    [SpellHandler("SavageDPSBuff")]
    public class SavageDPSBuff : AbstractSavageStatBuff
    {
        public override eProperty Property1 => eProperty.MeleeDamage;

        // constructor
        public SavageDPSBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    [SpellHandler("SavageSlashResistanceBuff")]
    public class SavageSlashResistanceBuff : AbstractSavageResistBuff
    {
        public override eProperty Property1 => eProperty.Resist_Slash;

        // constructor
        public SavageSlashResistanceBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    [SpellHandler("SavageThrustResistanceBuff")]
    public class SavageThrustResistanceBuff : AbstractSavageResistBuff
    {
        public override eProperty Property1 => eProperty.Resist_Thrust;

        // constructor
        public SavageThrustResistanceBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }

    [SpellHandler("SavageCrushResistanceBuff")]
    public class SavageCrushResistanceBuff : AbstractSavageResistBuff
    {
        public override eProperty Property1 => eProperty.Resist_Crush;

        // constructor
        public SavageCrushResistanceBuff(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
}


