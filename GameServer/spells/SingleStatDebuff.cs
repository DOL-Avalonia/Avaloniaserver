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
using DOL.AI.Brain;
using DOL.GS.Effects;
using DOL.GS.PlayerClass;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Debuffs a single stat
    /// </summary>
    public abstract class SingleStatDebuff : SingleStatBuff
    {
        // bonus category
        public override eBuffBonusCategory BonusCategory1 => eBuffBonusCategory.Debuff;

        /// <summary>
        /// Apply effect on target or do spell action if non duration spell
        /// </summary>
        /// <param name="target">target that gets the effect</param>
        /// <param name="effectiveness">factor from 0..1 (0%-100%)</param>
        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            base.ApplyEffectOnTarget(target, effectiveness);

            if (target.Realm == 0 || Caster.Realm == 0)
            {
                target.LastAttackedByEnemyTickPvE = target.CurrentRegion.Time;
                Caster.LastAttackTickPvE = Caster.CurrentRegion.Time;
            }
            else
            {
                target.LastAttackedByEnemyTickPvP = target.CurrentRegion.Time;
                Caster.LastAttackTickPvP = Caster.CurrentRegion.Time;
            }

            if (target is GameNPC npc && npc.Brain is IOldAggressiveBrain aggroBrain)
            {
                aggroBrain.AddToAggroList(Caster, (int)Spell.Value);
            }
        }

        /// <summary>
        /// Calculates the effect duration in milliseconds
        /// </summary>
        /// <param name="target">The effect target</param>
        /// <param name="effectiveness">The effect effectiveness</param>
        /// <returns>The effect duration in milliseconds</returns>
        protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
        {
            double duration = Spell.Duration;
            duration *= 1.0 + Caster.GetModified(eProperty.SpellDuration) * 0.01;
            duration -= duration * target.GetResist(Spell.DamageType) * 0.01;

            if (duration < 1)
            {
                duration = 1;
            }
            else if (duration > (Spell.Duration * 4))
            {
                duration = Spell.Duration * 4;
            }

            return (int)duration;
        }

        /// <summary>
        /// Calculates chance of spell getting resisted
        /// </summary>
        /// <param name="target">the target of the spell</param>
        /// <returns>chance that spell will be resisted for specific target</returns>
        public override int CalculateSpellResistChance(GameLiving target)
        {
            int basechance = base.CalculateSpellResistChance(target);
            GameSpellEffect rampage = FindEffectOnTarget(target, "Rampage");
            if (rampage != null)
            {
                basechance += (int)rampage.Spell.Value;
            }

            return Math.Min(100, basechance);
        }

        // constructor
        public SingleStatDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Str stat baseline debuff
    /// </summary>
    [SpellHandler("StrengthDebuff")]
    public class StrengthDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.Strength;

        // constructor
        public StrengthDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Dex stat baseline debuff
    /// </summary>
    [SpellHandler("DexterityDebuff")]
    public class DexterityDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.Dexterity;

        // constructor
        public DexterityDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Con stat baseline debuff
    /// </summary>
    [SpellHandler("ConstitutionDebuff")]
    public class ConstitutionDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.Constitution;

        // constructor
        public ConstitutionDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Armor factor debuff
    /// </summary>
    [SpellHandler("ArmorFactorDebuff")]
    public class ArmorFactorDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.ArmorFactor;

        // constructor
        public ArmorFactorDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Armor Absorption debuff
    /// </summary>
    [SpellHandler("ArmorAbsorptionDebuff")]
    public class ArmorAbsorptionDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.ArmorAbsorption;

        /// <summary>
        /// send updates about the changes
        /// </summary>
        /// <param name="target"></param>
        protected override void SendUpdates(GameLiving target)
        {
        }

        // constructor
        public ArmorAbsorptionDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

        /// <summary>
        /// If Caster is Reaver and Target is NPC Gard and the gard is not aggro,
        /// dont target it
        /// </summary>
        /// <param name="castTarget"></param>
        /// <returns></returns>
        public override IList<GameLiving> SelectTargets(GameObject castTarget)
        {
            IList<GameLiving> result = base.SelectTargets(castTarget);
            List<GameLiving> targetToRemove = new List<GameLiving>();
            result.Foreach((target) =>
            {
                long amount;
                if (Caster is GamePlayer player && player.CharacterClass is ClassReaver && target.GetType().Name == "GuardNPC" && target is GameNPC guard && player.Reputation >= 0 &&
                !((StandardMobBrain)guard.Brain).AggroTable.TryGetValue(Caster, out amount))
                    targetToRemove.Add(target);
            });
            targetToRemove.Foreach((target) => result.Remove(target));
            return result;
        }
    }

    /// <summary>
    /// Combat Speed debuff
    /// </summary>
    [SpellHandler("CombatSpeedDebuff")]
    public class CombatSpeedDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.MeleeSpeed;

        /// <summary>
        /// send updates about the changes
        /// </summary>
        /// <param name="target"></param>
        protected override void SendUpdates(GameLiving target)
        {
        }

        // constructor
        public CombatSpeedDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Melee damage debuff
    /// </summary>
    [SpellHandler("MeleeDamageDebuff")]
    public class MeleeDamageDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.MeleeDamage;

        /// <summary>
        /// send updates about the changes
        /// </summary>
        /// <param name="target"></param>
        protected override void SendUpdates(GameLiving target)
        {
        }

        // constructor
        public MeleeDamageDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }


        /// <summary>
        /// If Caster is Reaver and Target is NPC Gard and the gard is not aggro,
        /// dont target it
        /// </summary>
        /// <param name="castTarget"></param>
        /// <returns></returns>
        public override IList<GameLiving> SelectTargets(GameObject castTarget)
        {
            IList<GameLiving> result = base.SelectTargets(castTarget);
            List<GameLiving> targetToRemove = new List<GameLiving>();
            result.Foreach((target) =>
            {
                long amount;
                if (Caster is GamePlayer player && player.CharacterClass is ClassReaver && target.GetType().Name == "GuardNPC" && target is GameNPC guard && player.Reputation >= 0 &&
                !((StandardMobBrain)guard.Brain).AggroTable.TryGetValue(Caster, out amount))
                    targetToRemove.Add(target);
            });
            targetToRemove.Foreach((target) => result.Remove(target));
            return result;
        }
    }

    /// <summary>
    /// Fatigue reduction debuff
    /// </summary>
    [SpellHandler("FatigueConsumptionDebuff")]
    public class FatigueConsumptionDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.FatigueConsumption;

        /// <summary>
        /// send updates about the changes
        /// </summary>
        /// <param name="target"></param>
        protected override void SendUpdates(GameLiving target)
        {
        }

        // constructor
        public FatigueConsumptionDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Fumble chance debuff
    /// </summary>
    [SpellHandler("FumbleChanceDebuff")]
    public class FumbleChanceDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.FumbleChance;

        /// <summary>
        /// send updates about the changes
        /// </summary>
        /// <param name="target"></param>
        protected override void SendUpdates(GameLiving target)
        {
        }

        // constructor
        public FumbleChanceDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// DPS debuff
    /// </summary>
    [SpellHandler("DPSDebuff")]
    public class DPSDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.DPS;

        // constructor
        public DPSDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Skills Debuff
    /// </summary>
    [SpellHandler("SkillsDebuff")]
    public class SkillsDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.AllSkills;

        // constructor
        public SkillsDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Acuity stat baseline debuff
    /// </summary>
    [SpellHandler("AcuityDebuff")]
    public class AcuityDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.Acuity;

        // constructor
        public AcuityDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// Quickness stat baseline debuff
    /// </summary>
    [SpellHandler("QuicknessDebuff")]
    public class QuicknessDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.Quickness;

        // constructor
        public QuicknessDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }

    /// <summary>
    /// ToHit Skill debuff
    /// </summary>
    [SpellHandler("ToHitDebuff")]
    public class ToHitSkillDebuff : SingleStatDebuff
    {
        public override eProperty Property1 => eProperty.ToHitBonus;

        // constructor
        public ToHitSkillDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
 }
