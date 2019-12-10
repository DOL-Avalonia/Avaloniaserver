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
using System.Text;
using System.Collections.Generic;
using System.Linq;

using DOL.Database;

namespace DOL.GS
{	
	public class Spell : Skill, ICustomParamsValuable
	{
		protected readonly int m_range;
        protected readonly int m_power;
        protected readonly int m_casttime;
        protected readonly bool m_minotaurspell;        
        protected ushort m_tooltipId = 0;        

        public Dictionary<string, List<string>> CustomParamsDictionary { get; set; }

        public bool IsPrimary { get; }

        public bool IsSecondary { get; }

        public bool InChamber { get; set; }

        public bool CostPower { get; set; } = true;

        public bool AllowBolt { get; }

        public int OverrideRange { get; set; }

        public ushort ClientEffect { get; }

        public string Description { get; } = string.Empty;

        public int SharedTimerGroup { get; }

        public string Target { get; } = string.Empty;

        public int Range
        {
            get
            {
                if (OverrideRange != 0 && m_range != 0)
                {
                    return OverrideRange;
                }

                return m_range;
            }
        }

        public int Power
        {
            get
            {
                if (!CostPower)
                {
                    return 0;
                }

                return m_power;
            }
        }

        public int CastTime
        {
            get
            {
                if (InChamber)
                {
                    return 0;
                }

                return m_casttime;
            }
        }

        public double Damage { get; set; }

        public eDamageType DamageType { get; } = eDamageType.Natural;

        public string SpellType { get; } = "-";

        public int Duration { get; set; }

        public int Frequency { get; }

        public int Pulse { get; }

        public int PulsePower { get; }

        public int Radius { get; }

        public int RecastDelay { get; }

        public int ResurrectHealth { get; }

        public int ResurrectMana { get; }

        public double Value { get; set; }

        public byte Concentration { get; }

        public int LifeDrainReturn { get; }

        public int AmnesiaChance { get; }

        public string Message1 { get; } = string.Empty;

        public string Message2 { get; } = string.Empty;

        public string Message3 { get; } = string.Empty;

        public string Message4 { get; } = string.Empty;

        public override eSkillPage SkillType => NeedInstrument ? eSkillPage.Songs : eSkillPage.Spells;

        public int InstrumentRequirement { get; }

        public int Group { get; }

        public int EffectGroup { get; }

        public int SubSpellId { get; }

        public bool MoveCast { get; }

        public bool Uninterruptible { get; }

        public bool IsFocus { get; }

        /// <summary>
        /// This spell can be sheared even if cast by self
        /// </summary>
        public bool IsShearable { get; set; } = false;

        /// <summary>
        /// Whether or not this spell is harmful.
        /// </summary>
        public bool IsHarmful
        {
            get
            {
                var target = Target.ToLower();
                return target == "enemy" || target == "area" || target == "cone";
            }
        }
		
		/// <summary>
		/// Returns the string representation of the Spell
		/// </summary>		
		public override string ToString()
		{
			return new StringBuilder(32)
				.Append("Name=").Append(Name)
				.Append(", ID=").Append(ID)
				.Append(", SpellType=").Append(SpellType)
				.ToString();
		}

        public Spell(DBSpell dbspell, int requiredLevel)
            : this(dbspell, requiredLevel, false)
        {
        }

        public Spell(DBSpell dbspell, int requiredLevel, bool minotaur)
			: base(dbspell.Name, dbspell.SpellID, (ushort)dbspell.Icon, requiredLevel, dbspell.TooltipId)
		{
        	Description = dbspell.Description;
            Target = dbspell.Target;
            SpellType = dbspell.Type;
            m_range = dbspell.Range;
            Radius = dbspell.Radius;
            Value = dbspell.Value;
            Damage = dbspell.Damage;
            DamageType = (eDamageType)dbspell.DamageType;
            Concentration = (byte)dbspell.Concentration;
            Duration = dbspell.Duration * 1000;
            Frequency = dbspell.Frequency * 100;
            Pulse = dbspell.Pulse;
            PulsePower = dbspell.PulsePower;
            m_power = dbspell.Power;
            m_casttime = (int)(dbspell.CastTime * 1000);
            RecastDelay = dbspell.RecastDelay * 1000;
            ResurrectHealth = dbspell.ResurrectHealth;
            ResurrectMana = dbspell.ResurrectMana;
            LifeDrainReturn = dbspell.LifeDrainReturn;
            AmnesiaChance = dbspell.AmnesiaChance;
            Message1 = dbspell.Message1;
            Message2 = dbspell.Message2;
            Message3 = dbspell.Message3;
            Message4 = dbspell.Message4;
            ClientEffect = (ushort)dbspell.ClientEffect;
            InstrumentRequirement = dbspell.InstrumentRequirement;
            Group = dbspell.SpellGroup;
            EffectGroup = dbspell.EffectGroup;
            SubSpellId = dbspell.SubSpellID;
            MoveCast = dbspell.MoveCast;
            Uninterruptible = dbspell.Uninterruptible;
            IsFocus = dbspell.IsFocus;

            // warlocks
            IsPrimary = dbspell.IsPrimary;
            IsSecondary = dbspell.IsSecondary;
            AllowBolt = dbspell.AllowBolt;
            SharedTimerGroup = dbspell.SharedTimerGroup;
            m_minotaurspell = minotaur;

            // Params
            this.InitFromCollection<DBSpellXCustomValues>(dbspell.CustomValues, param => param.KeyName, param => param.Value);
		}

		/// <summary>
		/// Make a copy of a spell but change the spell type
		/// Usefull for customization of spells by providing custom spell handelers
		/// </summary>		
		public Spell(Spell spell, string spellType) :
			base(spell.Name, spell.ID, (ushort)spell.Icon, spell.Level, spell.InternalID)
		{
			Description = spell.Description;
            Target = spell.Target;
            SpellType = spellType; // replace SpellType
            m_range = spell.Range;
            Radius = spell.Radius;
            Value = spell.Value;
            Damage = spell.Damage;
            DamageType = spell.DamageType;
            Concentration = spell.Concentration;
            Duration = spell.Duration;
            Frequency = spell.Frequency;
            Pulse = spell.Pulse;
            PulsePower = spell.PulsePower;
            m_power = spell.Power;
            m_casttime = spell.CastTime;
            RecastDelay = spell.RecastDelay;
            ResurrectHealth = spell.ResurrectHealth;
            ResurrectMana = spell.ResurrectMana;
            LifeDrainReturn = spell.LifeDrainReturn;
            AmnesiaChance = spell.AmnesiaChance;
            Message1 = spell.Message1;
            Message2 = spell.Message2;
            Message3 = spell.Message3;
            Message4 = spell.Message4;
            ClientEffect = spell.ClientEffect;
            m_icon = spell.Icon;
            InstrumentRequirement = spell.InstrumentRequirement;
            Group = spell.Group;
            EffectGroup = spell.EffectGroup;
            SubSpellId = spell.SubSpellId;
            MoveCast = spell.MoveCast;
            Uninterruptible = spell.Uninterruptible;
            IsFocus = spell.IsFocus;
            IsPrimary = spell.IsPrimary;
            IsSecondary = spell.IsSecondary;
            AllowBolt = spell.AllowBolt;
            SharedTimerGroup = spell.SharedTimerGroup;
            m_minotaurspell = spell.m_minotaurspell;

            // Params
            CustomParamsDictionary = new Dictionary<string, List<string>>(spell.CustomParamsDictionary);
		}

		/// <summary>
		/// Make a shallow copy of this spell
		/// Always make a copy before attempting to modify any of the spell values
		/// </summary>		
		public virtual Spell Copy()
		{
			return (Spell)MemberwiseClone();
		}

		public override Skill Clone()
		{
			return (Spell)MemberwiseClone();
		}
		
		/// <summary>
		/// Fill in spell delve information.
		/// </summary>
		/// <param name="delve"></param>
		public virtual void Delve(List<String> delve)
		{
			delve.Add($"Function: {Name}");
            delve.Add(string.Empty);
            delve.Add(Description);
            delve.Add(string.Empty);
            DelveEffect(delve);
            DelveTarget(delve);

            if (Range > 0)
            {
                delve.Add($"Range: {Range}");
            }

            if (Duration > 0 && Duration < 65535)
            {
                delve.Add($"Duration: {(Duration >= 60000 ? $"{Duration / 60000}:{Duration % 60000} min" : $"{Duration / 1000} sec")}");
            }

            delve.Add($"Casting time: {(CastTime == 0 ? "instant" : $"{CastTime} sec")}");

            if (Target.ToLower() == "enemy" || Target.ToLower() == "area" || Target.ToLower() == "cone")
            {
                delve.Add($"Damage: {GlobalConstants.DamageTypeToName(DamageType)}");
            }

            delve.Add(string.Empty);
        }

		private void DelveEffect(List<String> delve)
		{
		}

		private void DelveTarget(List<String> delve)
		{
			String target;
			switch (Target)
			{
				case "Enemy":
					target = "Targetted";
					break;
				default:
					target = Target;
					break;
			}

			delve.Add(String.Format("Target: {0}", target));
		}

		/// <summary>
        /// Whether or not the spell is instant cast.
        /// </summary>
        public bool IsInstantCast => CastTime <= 0;

        /// <summary>
        /// Wether or not the spell is Point Blank Area of Effect
        /// </summary>
        public bool IsPBAoE => Range == 0 && IsAoE;

        /// <summary>
        /// Wether or not this spell need Instrument (and is a Song)
        /// </summary>
        public bool NeedInstrument => InstrumentRequirement != 0;

        /// <summary>
        /// Wether or not this spell is an Area of Effect Spell
        /// </summary>
        public bool IsAoE => Radius > 0;

        /// <summary>
        /// Wether this spell Has valid SubSpell
        /// </summary>
        public bool HasSubSpell => SubSpellId > 0 || MultipleSubSpells.Count > 0;

        /// <summary>
        /// Wether this spell has a recast delay (cooldown)
        /// </summary>
        public bool HasRecastDelay => RecastDelay > 0;

        /// <summary>
        /// Wether this spell is concentration based
        /// </summary>
        public bool IsConcentration => Concentration > 0;

        /// <summary>
        /// Wether this spell has power usage.
        /// </summary>
        public bool UsePower => Power != 0;

        /// <summary>
        /// Wether this spell has pulse power usage.
        /// </summary>
        public bool UsePulsePower => PulsePower != 0;

        /// <summary>
        /// Wether this Spell is a pulsing spell (Song/Chant)
        /// </summary>
        public bool IsPulsing => Pulse != 0;

        /// <summary>
        /// Wether this Spell is a Song/Chant
        /// </summary>
        public bool IsChant => Pulse != 0 && !IsFocus;

        /// <summary>
        /// Wether this Spell is a Pulsing Effect (Dot/Hot...)
        /// </summary>
        public bool IsPulsingEffect => Frequency > 0 && !IsPulsing;

        public ushort InternalIconID => this.GetParamValue<ushort>("InternalIconID");

        public IList<int> MultipleSubSpells
        {
            get
            {
                return this.GetParamValues<int>("MultipleSubSpellID").Where(id => id > 0).ToList();
            }
        }

        public bool AllowCoexisting => this.GetParamValue<bool>("AllowCoexisting");
				
		//Eden delve methods that i've added to
		public string GetDelveFunction()
		{
			switch (SpellType)
			{
				case "DummySpell": // test for abilitySpells
				case "RvrResurrectionIllness":
				case "PveResurrectionIllness": return "light";
				
				case "Charm": return "charm";
				case "CureMezz": return "remove_eff";
				case "Lifedrain": return "lifedrain";
				case "PaladinArmorFactorBuff":
				case "ArmorFactorBuff": return "shield";
				case "ArmorAbsorptionBuff": return "absorb";
				case "DirectDamageWithDebuff": return "nresist_dam";
				case "DamageSpeedDecrease":
				case "SpeedDecrease": return "snare";
				case "Bolt": return "bolt";
				
				case "Amnesia": return "amnesia";

				case "QuicknessDebuff":
				case "ConstitutionDebuff":
				case "StrengthDebuff":
				case "DexterityDebuff": return "nstat";

				case "QuicknessBuff":
				case "ConstitutionBuff":
				case "StrengthBuff":
				case "DexterityBuff": return "stat";
				
				case "Disease": return "disease";
				case "DamageOverTime": return "dot";

				case "Confusion":
				case "Mesmerize":
				case "Nearsight":
				case "PetSpeedEnhancement":
				case "SpeedEnhancement":
				case "SpeedOfTheRealm":
				case "CombatSpeedBuff":
				case "CombatSpeedDebuff":
				case "Bladeturn": return "combat";

				case "DirectDamage":
					if (Duration == 1) // change this , field in DB? IsGTAoE bool - Unty
					{
						return "storm";
					}
					return "direct";

				case "AcuityDebuff":
				case "StrengthConstitutionDebuff":
				case "DexterityConstitutionDebuff":
				case "WeaponSkillConstitutionDebuff":
				case "DexterityQuicknessDebuff": return "ntwostat";

				case "AcuityBuff":
				case "StrengthConstitutionBuff":
				case "DexterityConstitutionBuff":
				case "WeaponSkillConstitutionBuff":
				case "DexterityQuicknessBuff": return "twostat";

				case "BodyResistDebuff":
				case "ColdResistDebuff":
				case "EnergyResistDebuff":
				case "HeatResistDebuff":
				case "MatterResistDebuff":
				case "SpiritResistDebuff":
				case "SlashResistDebuff":
				case "ThrustResistDebuff":
				case "CrushResistDebuff":
				case "EssenceSear": return "nresistance";
				
				case "BodySpiritEnergyBuff":
				case "HeatColdMatterBuff":
				case "BodyResistBuff":
				case "ColdResistBuff":
				case "EnergyResistBuff":
				case "HeatResistBuff":
				case "MatterResistBuff":
				case "SpiritResistBuff":
				case "SlashResistBuff":
				case "ThrustResistBuff":
				case "CrushResistBuff": return "resistance";
				
				case "HealthRegenBuff":
				case "EnduranceRegenBuff":
				case "PowerRegenBuff": return "enhancement";

				case "MesmerizeDurationBuff": return "mez_dampen";
				
				case "CombatHeal": // guess for now
				
				case "SubSpellHeal": // new for ability value - Unty
				case "Heal": return "heal";
				
				case "Resurrect": return "raise_dead";
				case "DamageAdd": return "dmg_add";

				case "CureNearsight":
				case "CurePoison":
				case "CureDisease": return "rem_eff_ty";
				case "SpreadHeal": return "spreadheal";

				case "SummonAnimistFnF":
				case "SummonAnimistPet":
				case "SummonCommander":
				case "SummonMinion":
				case "SummonSimulacrum":
				case "SummonDruidPet":
				case "SummonHunterPet":
				case "SummonNecroPet":
				case "SummonUnderhill":
				case "SummonTheurgistPet": return "summon";

				case "StrengthShear":
				case "DexterityShear":
				case "ConstitutionShear":
				case "AcuityShear":
				case "StrengthConstitutionShear":
				case "DexterityQuicknessShear": return "buff_shear";

				case "StyleStun":
				case "StyleBleeding":
				case "StyleSpeedDecrease":
				case "StyleCombatSpeedDebuff": return "add_effect";

				case "SiegeArrow":
				case "ArrowDamageTypes":
				case "Archery": return "archery";

				case "HereticDoTLostOnPulse": return "direct_inc";

				case "DefensiveProc": return "def_proc";
				
				case "OffensiveProcPvE":
				case "OffensiveProc": return "off_proc";
				case "AblativeArmor": return "hit_buffer";

				case "Stun": return "paralyze";
				case "HealOverTime": return "regen";
				case "DamageShield": return "dmg_shield";
				case "Taunt": return "taunt";

                case "MeleeDamageDebuff": return "ndamage";
                case "ArmorAbsorptionDebuff": return "nabsorb";
            }
			return "0";
		}

		public int GetDelveAmountIncrease()
		{
			switch (SpellType)
			{
				case "HereticDoTLostOnPulse": return 50;
			}
			return 0;
		}

		public int GetDelveAbility()
		{
			switch (SpellType)
			{
				case "MesmerizeDurationBuff": return Target == "Self" ? 4 : 3072;
				case "DamageAdd":				
				case "ArmorAbsorptionBuff":
				case "BodyResistBuff":
				case "DefensiveProc":
				case "OffensiveProc":
				case "ColdResistBuff":
				case "EnergyResistBuff":
				case "HeatResistBuff":
				case "MatterResistBuff":
				case "SpiritResistBuff":
				case "SlashResistBuff":
				case "ThrustResistBuff":
				case "CrushResistBuff":
				case "ArmorFactorBuff": return 4;
				case "SiegeArrow": return 1024;
				
				case "BodySpiritEnergyBuff":
				case "HeatColdMatterBuff": return Pulse > 0 ? 2052 : 3076;
				
				case "SubSpellHeal": return 2049;
				case "PowerRegenBuff": 
				case "EnduranceRegenBuff":
				case "HealthRegenBuff":
					if (Pulse > 0)
					{
						return 3072;
					}
					return 4;
				case "AblativeArmor": return 3072;
				case "Archery":
					if (Name.StartsWith("Power Shot"))
					{
						return 1088;
					}	
					return 1024;				
				case "ArcheryDoT": return 1;

                case "ArmorAbsorptionDebuff":
                case "MeleeDamageDebuff": return 8;
            }
			return 0;
		}

		public int GetDelvePowerLevel(int level)
		{
			switch (SpellType)
			{
				case "Confusion": return (int)Value + 100;

				case "SummonAnimistFnF":
				case "SummonAnimistPet":
				case "SummonCommander":
				case "SummonMinion":
				case "SummonSimulacrum":
				case "SummonDruidPet":
				case "SummonHunterPet":
				case "SummonNecroPet":
				case "SummonTheurgistPet":
				case "DamageOverTime": return -(int)Damage;
				
				case "Charm": return Pulse == 1 ? (int)Damage : -(int)Damage;

				case "CombatSpeedBuff": return -(int)(Value * 2);

				case "StyleBleeding": return (int)Damage;
				case "StyleSpeedDecrease": return (int)(100 - Value);
				
				case "CombatSpeedDebuff":
				case "StyleCombatSpeedDebuff": return -(int)Value;
			}
			return level;
		}

		public int GetDelveTargetType()
		{
			switch (Target)
			{
				case "Realm": return 7;
				case "Self": return 0;
				case "Enemy": return 1;
				case "Pet": return 6;
				case "Group": return 3;
				case "Area": return 9;

				case "StrengthShear":
				case "DexterityShear":
				case "ConstitutionShear":
				case "AcuityShear":
				case "StrengthConstitutionShear":
				case "DexterityQuicknessShear": return 10;				
				//case "OffensiveProcPvE": return 14 PvE only -Unty
				default: return 0;
			}
		}

		public int GetDelvePowerCost()
		{
			switch(SpellType)
			{
				//case "ArmorAbsorptionDebuff"
				case "BodySpiritEnergyBuff":
				case "HeatColdMatterBuff": return Pulse > 0 ? -PulsePower : Power;
				case "SiegeArrow":
				case "ArrowDamageTypes":
				case "Archery": return -Power;				
			}
			return Power;
		}

		public int GetDelveLinkEffect()
		{
            if (SubSpellId > 0)
            {
                return (int)SubSpellId;
            }
			switch (SpellType)
			{
				case "StrengthShear":
				case "DexterityShear":
				case "ConstitutionShear":
				case "AcuityShear":
				case "StrengthConstitutionShear":
				case "DexterityQuicknessShear": return IsAoE ? 7312 : 5595;				
			}
			return 0;
		}

		public int GetDelveDurationType()
		{
			//2-seconds,4-conc,5-focus
			switch (SpellType)
			{
				case "HereticDoTLostOnPulse": return 5;
			}
			if (Duration > 0)
			{
				return 2;
			}	
			if (Concentration > 0)
			{
				return 4;
			}
			return 0;
		}

		public int GetDelveDuration()
		{
			return Duration / 1000;
		}

		public int GetDelveDamageType()
		{
			switch (SpellType)
			{				
				case "StyleSpeedDecrease":
				case "StyleCombatSpeedDebuff": return 0;
			}
			switch (DamageType)
			{
				case eDamageType.Slash: return 2;
				case eDamageType.Heat: return 10;
				case eDamageType.Cold: return 12;
				case eDamageType.Matter: return 15;
				case eDamageType.Body: return 16;
				case eDamageType.Spirit: return 17;
				case eDamageType.Energy: return 22;
			}
			return 0;
		}

		public int GetDelveBonus()
		{
			switch (SpellType)
			{
				case "Charm": return (int)Pulse == 1 ? 1 : 0;
				case "SummonAnimistFnF":
				case "SummonAnimistPet":
				case "SummonCommander":
				case "SummonMinion":
				case "SummonSimulacrum":
				case "SummonDruidPet":
				case "SummonHunterPet":
				case "SummonNecroPet":
				case "SummonUnderhill":				
				case "SummonTheurgistPet": return 1;

				case "Lifedrain": return LifeDrainReturn / 10;
				case "DamageSpeedDecrease":
				case "SpeedDecrease": return (int)(100 - Value);
				case "Amnesia": return AmnesiaChance;
				case "QuicknessDebuff":
				case "QuicknessBuff":
				case "ConstitutionDebuff":
				case "ConstitutionBuff":
				case "StrengthDebuff":
				case "StrengthBuff":
				case "DexterityDebuff":
				case "DexterityBuff":
				case "AcuityDebuff":
				case "AcuityBuff":
				case "SpeedOfTheRealm":
				case "SpeedEnhancement":
				case "PetSpeedEnhancement":				
				case "ArmorAbsorptionBuff":
				case "DexterityQuicknessDebuff":
				case "DexterityQuicknessBuff":
				case "StrengthConstitutionDebuff":
				case "StrengthConstitutionBuff":

				case "BodyResistDebuff":
				case "ColdResistDebuff":
				case "EnergyResistDebuff":
				case "HeatResistDebuff":
				case "MatterResistDebuff":
				case "SpiritResistDebuff":
				case "SlashResistDebuff":
				case "ThrustResistDebuff":
				case "CrushResistDebuff":

				case "BodyResistBuff":
				case "ColdResistBuff":
				case "EnergyResistBuff":
				case "HeatResistBuff":
				case "MatterResistBuff":
				case "SpiritResistBuff":
				case "SlashResistBuff":
				case "ThrustResistBuff":
				case "CrushResistBuff":
				case "BodySpiritEnergyBuff":
				case "HeatColdMatterBuff":

				case "StrengthShear":
				case "DexterityShear":
				case "ConstitutionShear":
				case "AcuityShear":
				case "StrengthConstitutionShear":
				case "DexterityQuicknessShear":

				case "EssenceSear":
				case "DirectDamageWithDebuff":
				case "MesmerizeDurationBuff":
				case "PaladinArmorFactorBuff":
				case "Disease": // this is disease strength decrease
				case "ArmorFactorBuff": return (int)Value;

				case "StyleSpeedDecrease": return (int)(100 - Value);
				
				case "Bolt":
				case "SiegeArrow":
				case "ArrowDamageTypes":
				case "Archery": return 20;				
				
				case "OffensiveProcPvE":
				case "DefensiveProc":
				case "OffensiveProc": return (int)Frequency / 100;
				
				case "AblativeArmor": return (int)Damage;
				case "Resurrect": return (int)ResurrectMana;
                case "ArmorAbsorptionDebuff":
                case "MeleeDamageDebuff": return (int)Value * (-1);
            }
			return 0;
		}
		
		
		public int GetDelveLink()
		{
			if (SubSpellId != 0)
			{
				return SubSpellId;
			}
			return 0;
		}
		
		public int GetDelveDamage()
		{
			switch (SpellType)
			{				
				case "Bladeturn": return 51;
				case "DamageAdd":
				case "DamageSpeedDecrease":
				case "DirectDamage":
				case "DirectDamageWithDebuff":
				case "DamageShield":
				case "Bolt":
				case "Lifedrain": return (int)(Damage * 10);

				case "SummonAnimistFnF":
				case "SummonAnimistPet":
				case "SummonCommander":
				case "SummonMinion":
				case "SummonSimulacrum":
				case "SummonDruidPet":
				case "SummonHunterPet":
				case "SummonNecroPet":
				case "SummonTheurgistPet":
				case "SummonUnderhill":
				case "Disease": // this is disease speed decrease
				case "DamageOverTime": return (int)Damage;

				case "StrengthShear":
				case "DexterityShear":
				case "ConstitutionShear":
				case "AcuityShear":
				case "StrengthConstitutionShear":
				case "DexterityQuicknessShear": return 2;

				case "CombatHeal": // guess
				case "SpreadHeal":
				case "SubSpellHeal":
				case "Heal":
				case "Charm":
				case "EnduranceRegenBuff":
				case "HealOverTime":
				case "AblativeArmor":
				case "HealthRegenBuff":
				case "PowerRegenBuff": return (int)Value;
				case "Resurrect": return ResurrectHealth;

				case "StyleBleeding": return (int)Damage;

				case "SiegeArrow":
				case "Archery":	return (int)(Damage * 10);

                case "Taunt": return (int)Value;
			}
			return 0;
		}

		public int GetDelveParm(GameClient client)
		{
			switch (SpellType)
			{
				case "BodySpiritEnergyBuff": return Pulse > 0 ? 98 : 94;
				case "HeatColdMatterBuff": return Pulse > 0 ? 97 : 93;
				
				case "DamageAdd":
				case "ArmorAbsorptionDebuff":
				case "ArmorAbsorptionBuff":
				case "StyleSpeedDecrease":
				case "StyleStun":
				case "StrengthShear":
				case "StrengthDebuff":
				case "StrengthBuff":
				case "StrengthConstitutionShear":
				case "StrengthConstitutionDebuff":
				case "StrengthConstitutionBuff":
				case "Taunt":
				case "Stun":
				case "DamageOverTime":
				case "SpeedDecrease":
				case "DamageSpeedDecrease":
				case "DirectDamage":
				case "Bolt":
				case "HealOverTime":
				case "DamageShield":
				case "AblativeArmor":
				case "HealthRegenBuff":
				case "CombatHeal": // guess
				case "Lifedrain": return 1;
				
				case "CombatSpeedDebuff":
				case "StyleCombatSpeedDebuff":
				case "PowerRegenBuff":
				case "DexterityShear":
				case "DexterityDebuff":
				case "DexterityBuff":
				case "DexterityQuicknessShear":
				case "DexterityQuicknessDebuff":
				case "DexterityQuicknessBuff":
				case "ArmorFactorDebuff":
                case "MeleeDamageDebuff":
                case "ArmorFactorBuff": return 2;

				case "EnduranceRegenBuff":
				case "ConstitutionShear":
				case "ConstitutionBuff":
				case "ConstitutionDebuff":
				case "AcuityShear":
				case "AcuityDebuff":
				case "AcuityBuff": return 3;

				case "Confusion": return 5;

				case "CureMezz":
				case "Mesmerize": return 6;

				case "Bladeturn": return 9;
				
				case "DirectDamageWithDebuff":
				case "HeatResistDebuff":
				case "HeatResistBuff":
				case "SpeedOfTheRealm":
				case "PetSpeedEnhancement":
				case "SpeedEnhancement": return 10;

				case "CombatSpeedBuff": return 11;
				
				case "CureNearsight":
				case "Nearsight":
				case "ColdResistBuff":
				case "ColdResistDebuff": return 12;
				
				case "BodyResistDebuff":
				case "BodyResistBuff":	return 16;			
				
				case "EnergyResistDebuff":
				case "EnergyResistBuff": return 22;
				
				case "SpiritResistDebuff":
				case "SpiritResistBuff": return 17;
				
				case "MatterResistBuff":
				case "MatterResistDebuff": return 15;

				case "SummonAnimistFnF":
				case "SummonAnimistPet":
				case "SummonCommander":
				case "SummonMinion":
				case "SummonSimulacrum":
				case "SummonDruidPet":
				case "SummonHunterPet":
				case "SummonNecroPet":
				case "SummonUnderhill":
				case "SummonTheurgistPet": return 9915;				
				
				case "DefensiveProc":
				case "OffensiveProc":
				case "OffensiveProcPvE":
				{
					if ((int)Value > 0)
					{
						client.Out.SendDelveInfo(DOL.GS.PacketHandler.Client.v168.DetailDisplayHandler.DelveAttachedSpell(client, (int)Value));
					}
					return (int)Value;
				}
				case "StyleBleeding": return 20;
				
				case "ArcheryDoT": return 8;
				case "ArrowDamageTypes": return 2;
				
				case "Charm": return GetCharmParm();
			}
			return 0;
		}

		/// <summary>
        /// Parm id specifically for charm type spells
        /// </summary>
        public int GetCharmParm()
        {
            switch (AmnesiaChance)
            {
                case 1: return 3; // humanoid
                case 2: return 5; // animal
                case 3: return 13; // insect
                case 4: return 50; // human animal
                case 5: return 51; // human animal insect
                case 6: return 52; // human animal insect magical
                case 7: return 53; // human animal insect magical undead
                case 8: return 6; // reptile
                default: return 54; // all
            }
        }
		
		public int GetDelveCastTimer()
		{
			switch (SpellType)
			{
				case "HereticDoTLostOnPulse":
				case "OffensiveProc": return 1;
			}
			if (CastTime == 2000)
			{
				return 1;
			}	
			return CastTime - 2000;
		}

		public int GetDelveInstant()
		{
			switch (SpellType)
			{
				case "Heal":
				case "Charm":
				case "AblativeArmor":
				case "SubSpellHeal":
					if (IsInstantCast)
					{
						return 2;	
					}	
					return 0;
				
				case "StyleBleeding":
				case "StyleSpeedDecrease":
				case "StyleCombatSpeedDebuff": return 0;
			}
			 return IsInstantCast ? 1 : 0;
		}

		public int GetDelveType1()
		{			
			switch (SpellType)
			{
				case "DexterityDebuff": return 2;
				case "StyleBleeding":
				case "CurePoison":				
				case "StrengthDebuff": return 1;			
				
				case "CureDisease": return 18;
				case "Resurrect": return 65;

				case "StyleStun": return 22;
				
				case "CureNearsight":
				case "CureMezz":
				case "StyleCombatSpeedDebuff": return 8;
				case "StyleSpeedDecrease": return 39;
				case "AblativeArmor": return 43;

				case "Archery":
					if (Name.StartsWith("Critical Shot")) return 1752;
					else if (Name.StartsWith("Power Shot")) return 1032;
					else if (Name.StartsWith("Fire Shot") || Name.StartsWith("Cold Shot")) return 4;
					return 0;
			}
			return 0;
			
		}
		
		public int GetDelveFrequency()
		{
			if (Frequency != 0 || SpellType != "DamageOverTime")
			{
				return Frequency;
			}
			return 2490;
		}

		public string GetDelveNoCombat()
		{
			switch (SpellType)
			{
				case "SpeedOfTheRealm":
				case "SpeedEnhancement": return "\u0005";
				case "StyleStun": return " ";
			}
			return null;
		}

		public int GetDelveCostType()
		{
			switch (SpellType)
			{
				case "SiegeArrow":
				case "Archery": return 3;
			}
			return 0;
		}

		public int GetDelveIncreaseCap()
		{
			switch (SpellType)
			{
				case "HereticDoTLostOnPulse": return 150;
			}
			return 0;
		}	
	}	
}
