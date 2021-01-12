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
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.Events;
using DOL.GS.ServerProperties;
using DOL.GS.Spells;
using DOL.GS.Styles;
using DOL.AI;

namespace DOL.GS
{
	public class GamePet : GameNPC
	{
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public GamePet(INpcTemplate template) : base(template)
		{
			if (Inventory != null)
			{
				if (Inventory.GetItem(eInventorySlot.DistanceWeapon) != null)
					SwitchWeapon(eActiveWeaponSlot.Distance);
				else if (Inventory.GetItem(eInventorySlot.RightHandWeapon) != null)
					SwitchWeapon(eActiveWeaponSlot.Standard);
				else if (Inventory.GetItem(eInventorySlot.TwoHandWeapon) != null)
					SwitchWeapon(eActiveWeaponSlot.TwoHanded);
			}
			AddStatsToWeapon();
			BroadcastLivingEquipmentUpdate();
		}

        public GamePet(ABrain brain) : base(brain)
        {

        }

		public GameLiving Owner
		{
			get
			{
				if (Brain is IControlledBrain)
				{
					return (Brain as IControlledBrain).Owner;
				}

				return null;
			}
		}

		public override int Mana { get => 5000; set => base.Mana = value; }
		public override int MaxMana => 5000;


		#region Inventory

		/// <summary>
		/// Load equipment for the pet.
		/// </summary>
		/// <param name="templateID">Equipment Template ID.</param>
		/// <returns>True on success, else false.</returns>
		protected virtual void AddStatsToWeapon()
		{
			if (Inventory != null)
			{
				InventoryItem item;
				if ((item = Inventory.GetItem(eInventorySlot.TwoHandWeapon)) != null)
				{
					item.DPS_AF = (int)(Level * 3.3);
					item.SPD_ABS = 50;
				}
				if ((item = Inventory.GetItem(eInventorySlot.RightHandWeapon)) != null)
				{
					item.DPS_AF = (int)(Level * 3.3);
					item.SPD_ABS = 37;
				}
				if ((item = Inventory.GetItem(eInventorySlot.LeftHandWeapon)) != null)
				{
					item.DPS_AF = (int)(Level * 3.3);
					item.SPD_ABS = 50;
				}
				if ((item = Inventory.GetItem(eInventorySlot.DistanceWeapon)) != null)
				{
					item.DPS_AF = (int)(Level * 3.3);
					item.SPD_ABS = 50;
					SwitchWeapon(eActiveWeaponSlot.Distance);
					BroadcastLivingEquipmentUpdate();
				}
			}
		}

		#endregion

		#region Shared Melee & Spells

		/// <summary>
		/// Multiplier for melee and magic.
		/// </summary>
		public override double Effectiveness
		{
			get 
            {
                GameLiving gl = (Brain as IControlledBrain).GetLivingOwner();
                if (gl != null)
                    return gl.Effectiveness;

                return 1.0;
            }
		}

		/// <summary>
		/// Specialisation level including item bonuses and RR.
		/// </summary>
		/// <param name="keyName">The specialisation line.</param>
		/// <returns>The specialisation level.</returns>
		public override int GetModifiedSpecLevel(string keyName)
		{
			int spec = (Brain as IControlledBrain).GetLivingOwner().GetModifiedSpecLevel(keyName);

			if (spec <= 0)
				return Level;

			return spec;
		}

		#endregion

		#region Spells

		/// <summary>
		/// Called when spell has finished casting.
		/// </summary>
		/// <param name="handler"></param>
		public override void OnAfterSpellCastSequence(ISpellHandler handler)
		{
			base.OnAfterSpellCastSequence(handler);
			Brain.Notify(GameNPCEvent.CastFinished, this, new CastingEventArgs(handler));
		}

        /// <summary>
        /// Scale the passed spell according to PET_SCALE_SPELL_MAX_LEVEL
        /// </summary>
        /// <param name="spell">The spell to scale</param>
        /// <returns>The scaled spell</returns>
        public Spell ScalePetSpell(Spell spell)
        {
            if (ServerProperties.Properties.PET_SCALE_SPELL_MAX_LEVEL <= 0)
                return spell;

            Spell scaledSpell = spell;
            double CasterLevel = Level;

            // Cap the level we scale BD minions' spell effects to the player's modified spec for the spec line the pet is from
            if (this is BDSubPet subpet && subpet.Owner is CommanderPet commander && commander.Owner is GamePlayer player)
                CasterLevel = Math.Min(subpet.Level, player.GetModifiedSpecLevel(subpet.PetSpecLine));

            switch (spell.SpellType.ToString().ToLower())
            {
                // Scale Damage
                case "damageovertime":
                case "damageshield":
                case "damageadd":
                case "directdamage":
                case "directdamagewithdebuff":
                case "lifedrain":
                case "damagespeeddecrease":
                case "stylebleeding": // Style Effect
                    scaledSpell.Damage *= CasterLevel / Properties.PET_SCALE_SPELL_MAX_LEVEL;
                    break;
                // Scale Value
                case "enduranceregenbuff":
                case "enduranceheal":
                case "endurancedrain":
                case "powerregenbuff":
                case "powerheal":
                case "powerdrain":
                case "powerhealthenduranceregenbuff":
                case "combatspeedbuff":
                case "hastebuff":
                case "celeritybuff":
                case "combatspeeddebuff":
                case "hastedebuff":
                case "heal":
                case "combatheal":
                case "healthregenbuff":
                case "healovertime":
                case "constitutionbuff":
                case "dexteritybuff":
                case "strengthbuff":
                case "constitutiondebuff":
                case "dexteritydebuff":
                case "strengthdebuff":
                case "armorfactordebuff":
                case "armorfactorbuff":
                case "armorabsorptionbuff":
                case "armorabsorptiondebuff":
                case "dexterityquicknessbuff":
                case "strengthconstitutionbuff":
                case "dexterityquicknessdebuff":
                case "strengthconstitutiondebuff":
                case "taunt":
                case "unbreakablespeeddecrease":
                case "speeddecrease":
                case "stylecombatspeeddebuff": // Style Effect
                case "stylespeeddecrease": // Style Effect
                                           //case "styletaunt":  Taunt styles already scale with damage, leave their values alone.
                    scaledSpell.Value *= CasterLevel / ServerProperties.Properties.PET_SCALE_SPELL_MAX_LEVEL;
                    break;
                // Scale Duration
                case "disease":
                case "stun":
                case "unrresistablenonimunitystun":
                case "mesmerize":
                case "stylestun": // Style Effect
                    scaledSpell.Duration = (int)Math.Ceiling(spell.Duration * CasterLevel / ServerProperties.Properties.PET_SCALE_SPELL_MAX_LEVEL);
                    break;
                default: break; // Don't mess with types we don't know
            } // switch (m_spell.SpellType.ToString().ToLower())

            return scaledSpell;
        }
        #endregion

        #region Stats
        /// <summary>
        /// Set stats according to PET_AUTOSET values, then scale them according to the values in the DB
        /// </summary>
        public override void AutoSetStats()
		{
			if (NPCTemplate == null || NPCTemplate.Strength < 1)
				Strength = (short)Math.Max(1, Properties.PET_AUTOSET_STR_BASE + (Level - 1) * Properties.PET_AUTOSET_STR_MULTIPLIER);
			else
				Strength = (short)NPCTemplate.Strength;

			if (NPCTemplate == null || NPCTemplate.Constitution < 1)
				Constitution = (short)Math.Max(1, Properties.PET_AUTOSET_CON_BASE + (Level - 1) * Properties.PET_AUTOSET_CON_MULTIPLIER);
			else
				Constitution = (short)NPCTemplate.Constitution;

			if (NPCTemplate == null || NPCTemplate.Quickness < 1)
				Quickness = (short)Math.Max(1, Properties.PET_AUTOSET_QUI_BASE + (Level - 1) * Properties.PET_AUTOSET_QUI_MULTIPLIER);
			else
				Quickness = (short)NPCTemplate.Quickness;

			if (NPCTemplate == null || NPCTemplate.Dexterity < 1)
				Dexterity = (short)Math.Max(1, Properties.PET_AUTOSET_DEX_BASE + (Level - 1) * Properties.PET_AUTOSET_DEX_MULTIPLIER);
			else
				Dexterity = (short)NPCTemplate.Dexterity;

			if (NPCTemplate == null || NPCTemplate.Intelligence < 1)
				Intelligence = (short)Math.Max(1, Properties.PET_AUTOSET_INT_BASE + (Level - 1) * Properties.PET_AUTOSET_INT_MULTIPLIER);
			else
				Intelligence = (short)NPCTemplate.Intelligence;

			if (NPCTemplate == null || NPCTemplate.Empathy < 1)
				Empathy = (short)(29 + Level);
			else
				Empathy = (short)NPCTemplate.Empathy;

			if (NPCTemplate == null || NPCTemplate.Piety < 1)
				Piety = (short)(29 + Level);
			else
				Piety = (short)NPCTemplate.Piety;

			if (NPCTemplate == null || NPCTemplate.Charisma < 1)
				Charisma = (short)(29 + Level);
			else
				Charisma = (short)NPCTemplate.Charisma;

			if (NPCTemplate == null || NPCTemplate.WeaponDps < 1)
				WeaponDps = (int)((1.4 + 0.3 * Level + Level * Level * 0.002) * 10);
			else
				WeaponDps = NPCTemplate.WeaponDps;
			if (NPCTemplate == null || NPCTemplate.WeaponSpd < 1)
				WeaponSpd = 30;
			else
				WeaponSpd = NPCTemplate.WeaponSpd;

			if (NPCTemplate == null || NPCTemplate.ArmorFactor < 1)
				ArmorFactor = (int)((1.0 + (Level / 100.0)) * Level * 1.8);
			else
				ArmorFactor = NPCTemplate.ArmorFactor;
			if (NPCTemplate == null || NPCTemplate.ArmorAbsorb < 1)
				ArmorAbsorb = (int)((Level - 10) * 0.5 - (Level - 60) * Level * 0.0015).Clamp(0, 75);
			else
				ArmorAbsorb = NPCTemplate.ArmorAbsorb;
		}
		#endregion

		#region Melee

		/// <summary>
		/// The type of damage the currently active weapon does.
		/// </summary>
		/// <param name="weapon"></param>
		/// <returns></returns>
		public override eDamageType AttackDamageType(InventoryItem weapon)
		{
			if (weapon != null)
			{
				switch ((eWeaponDamageType)weapon.Type_Damage)
				{
						case eWeaponDamageType.Crush: return eDamageType.Crush;
						case eWeaponDamageType.Slash: return eDamageType.Slash;
				}
			}

			return eDamageType.Crush;
		}

		/// <summary>
		/// Get melee speed in milliseconds.
		/// </summary>
		/// <param name="weapons"></param>
		/// <returns></returns>
		public override int AttackSpeed(params InventoryItem[] weapons)
		{
			double weaponSpeed = 0.0;

			if (weapons != null)
			{
				foreach (InventoryItem item in weapons)
				{
					if (item != null)
					{
						weaponSpeed += item.SPD_ABS;
					}
					else
					{
						weaponSpeed += 34;
					}
				}

				weaponSpeed = (weapons.Length > 0) ? weaponSpeed / weapons.Length : 34.0;
			}
			else
			{
				weaponSpeed = 34.0;
			}

			double speed = 100 * weaponSpeed * (1.0 - (GetModified(eProperty.Quickness) - 60) / 500.0);
			return (int)Math.Max(500.0, (speed * (double)GetModified(eProperty.MeleeSpeed) * 0.01)); // no bonus is 100%, opposite how players work
		}
		#endregion

		public override void Die(GameObject killer)
		{
			StripOwnerBuffs(Owner);
		
			GameEventMgr.Notify(GameLivingEvent.PetReleased, this);
			base.Die(killer);
			CurrentRegion = null;
		}
		
		/// <summary>
		/// Strips any buffs this pet cast on owner
		/// </summary>
		/// <param name="owner">
		/// The target to strip buffs off of.
		/// </param>
		public virtual void StripOwnerBuffs(GameLiving owner)
		{
            if (owner == null)
                return;
            if (owner.Group is Group group)
                // Strip all buffs from this pet off the group, and off other pets
                foreach (GamePlayer player in group.GetPlayersInTheGroup())
                {
                    if (player.EffectList != null)
                        foreach (IGameEffect effect in player.EffectList)
                            if (effect is GameSpellEffect spellEffect && spellEffect.SpellHandler != null && spellEffect.SpellHandler.Caster != null && spellEffect.SpellHandler.Caster == this)
                                effect.Cancel(false);
                }
            else if (owner.EffectList != null)
                // Owner not in a group, only strip buffs from the owner
                foreach (IGameEffect effect in owner.EffectList)
                    if (effect is GameSpellEffect spellEffect && spellEffect.SpellHandler != null && spellEffect.SpellHandler.Caster != null && spellEffect.SpellHandler.Caster == this)
                        effect.Cancel(false);
		}
		
		/// <summary>
		/// Spawn texts are in database
		/// </summary>
		protected override void BuildAmbientTexts()
		{
			base.BuildAmbientTexts();
			
			// also add the pet specific ambient texts if none found
			if (ambientTexts.Count == 0)
				ambientTexts = GameServer.Instance.NpcManager.AmbientBehaviour["pet"];
		}

		public override bool IsObjectGreyCon(GameObject obj)
		{
			GameObject tempobj = obj;
			if (Brain is IControlledBrain)
			{
                GameLiving player = (Brain as IControlledBrain).GetLivingOwner();
				if (player != null)
					tempobj = player;
			}
			return base.IsObjectGreyCon(tempobj);
		}
	}
}
