using System;
using System.Collections.Generic;
using System.Linq;
using DOL.gameobjects.CustomNPC;
using DOL.GS;
using DOL.GS.Effects;
using DOL.GS.RealmAbilities;

namespace DOL.AI.Brain
{
    public class AmteMobBrain : StandardMobBrain
    {
    	public int AggroLink { get; set; }

        public AmteMobBrain()
        {
        	AggroLink = 0;
        }

        public AmteMobBrain(ABrain brain)
        {
            if (!(brain is IOldAggressiveBrain))
                return;
            var old = (IOldAggressiveBrain)brain;
            m_aggroLevel = old.AggroLevel;
            m_aggroMaxRange = old.AggroRange;
        }

        // Remove BAF customisation to keep the new one implemented

		#region RandomWalk
		public override IPoint3D CalcRandomWalkTarget()
        {
            var roamingRadius = Body.RoamingRange > 0 ? Util.Random(0, Body.RoamingRange) : (Body.CurrentRegion.IsDungeon ? 100 : 500);
            var angle = Util.Random(0, 360) / (2 * Math.PI);
            var targetX = Body.SpawnPoint.X + Math.Cos(angle) * roamingRadius;
            var targetY = Body.SpawnPoint.Y + Math.Sin(angle) * roamingRadius;
            return new Point3D((int)targetX, (int)targetY, Body.SpawnPoint.Z);
        }
		#endregion

		#region Defensive Spells
		/// <summary>
		/// Checks defensive spells.  Handles buffs, heals, etc.
		/// </summary>
		protected override bool TryCastDefensiveSpell(Spell spell)
		{
			if (spell == null) return false;
			if (Body.GetSkillDisabledDuration(spell) > 0) return false;
			GameObject lastTarget = Body.TargetObject;
			Body.TargetObject = null;
			switch (spell.SpellType)
			{
				#region Buffs
				case "StrengthConstitutionBuff":
				case "DexterityQuicknessBuff":
				case "StrengthBuff":
				case "DexterityBuff":
				case "ConstitutionBuff":
				case "ArmorFactorBuff":
				case "ArmorAbsorptionBuff":
				case "CombatSpeedBuff":
				case "MeleeDamageBuff":
				case "AcuityBuff":
				case "HealthRegenBuff":
				case "DamageAdd":
				case "DamageShield":
				case "BodyResistBuff":
				case "ColdResistBuff":
				case "EnergyResistBuff":
				case "HeatResistBuff":
				case "MatterResistBuff":
				case "SpiritResistBuff":
				case "BodySpiritEnergyBuff":
				case "HeatColdMatterBuff":
				case "CrushSlashThrustBuff":
				case "AllMagicResistsBuff":
				case "AllMeleeResistsBuff":
				case "AllResistsBuff":
				case "OffensiveProc":
				case "DefensiveProc":
				case "Bladeturn":
				case "ToHitBuff":
					{
						// Buff self, if not in melee, but not each and every mob
						// at the same time, because it looks silly.
						if (!LivingHasEffect(Body, spell) && !Body.AttackState && Util.Chance(80))
						{
							Body.TargetObject = Body;
							break;
						}
						if (!Body.InCombat && spell.Target == "realm")
						{
							foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)Math.Max(spell.Radius, spell.Range)))
								if (Body.IsFriend(npc) && !LivingHasEffect(npc, spell))
								{
									Body.TargetObject = npc;
									break;
								}
						}
						break;
					}
				#endregion Buffs

				#region Disease Cure/Poison Cure/Summon
				case "CureDisease":
					if (Body.IsDiseased)
					{
						Body.TargetObject = Body;
						break;
					}
					if (!Body.InCombat && spell.Target == "realm")
					{
						foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)Math.Max(spell.Radius, spell.Range)))
							if (Body.IsFriend(npc) && npc.IsDiseased && Util.Chance(60))
							{
								Body.TargetObject = npc;
								break;
							}
					}
					break;
				case "CurePoison":
					if (LivingIsPoisoned(Body))
					{
						Body.TargetObject = Body;
						break;
					}
					if (!Body.InCombat && spell.Target == "realm")
					{
						foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)Math.Max(spell.Radius, spell.Range)))
							if (Body.IsFriend(npc) && LivingIsPoisoned(npc) && Util.Chance(60))
							{
								Body.TargetObject = npc;
								break;
							}
					}
					break;
				case "SummonAnimistFnF":
				case "SummonAnimistPet":
				case "SummonTheurgistPet":
					break;
				case "Summon":
				case "SummonCommander":
				case "SummonDruidPet":
				case "SummonHunterPet":
				case "SummonMastery":
				case "SummonMercenary":
				case "SummonMonster":
				case "SummonNoveltyPet":
				case "SummonSalamander":
				case "SummonSiegeWeapon":
				case "SummonSimulacrum":
				case "SummonSpiritFighter":
				case "SummonTitan":
				case "SummonUnderhill":
				case "SummonWarcrystal":
				case "SummonWood":
					//Body.TargetObject = Body;
					break;
				case "SummonMinion":
					//If the list is null, lets make sure it gets initialized!
					if (Body.ControlledNpcList == null)
						Body.InitControlledBrainArray(2);
					else
					{
						//Let's check to see if the list is full - if it is, we can't cast another minion.
						//If it isn't, let them cast.
						IControlledBrain[] icb = Body.ControlledNpcList;
						int numberofpets = icb.Count(t => t != null);
						if (numberofpets >= icb.Length)
							break;
					}
					Body.TargetObject = Body;
					break;
				#endregion

				#region Heals
				case "Heal":
					// Chance to heal self when dropping below 30%, do NOT spam it.

					if (Body.HealthPercent < 70 && Util.Chance(80))
					{
						Body.TargetObject = Body;
						break;
					}

					if (!Body.InCombat && spell.Target == "realm")
					{
						foreach (GameNPC npc in Body.GetNPCsInRadius((ushort)Math.Max(spell.Radius, spell.Range)))
							if (Body.IsFriend(npc) && npc.HealthPercent < 70)
							{
								Body.TargetObject = npc;
								break;
							}
					}

					break;
				#endregion
			}

			if (Body.TargetObject != null)
			{
                ShadowNPC shadow = Body.TargetObject as ShadowNPC;
                if (shadow != null)
                    return false;

                if (spell.Duration > 0 && LivingHasEffect(Body.TargetObject as GameLiving, spell))
                    return false;

                if (Body.IsMoving && spell.CastTime > 0)
					Body.StopFollowing();

				if (Body.TargetObject != Body && spell.CastTime > 0)
					Body.TurnTo(Body.TargetObject);

				Body.CastSpell(spell, m_mobSpellLine);

				Body.TargetObject = lastTarget;
				return true;
			}

			Body.TargetObject = lastTarget;

			return false;
		}
		#endregion

		public override int CalculateAggroLevelToTarget(GameLiving target)
		{
			if (GameServer.ServerRules.IsSameRealm(Body, target, true))
				return 0;

			// related to the pet owner if applicable
			if (target is GamePet)
			{
				GamePlayer thisLiving = ((IControlledBrain)((GamePet)target).Brain).GetPlayerOwner();
				if (thisLiving != null && thisLiving.IsObjectGreyCon(Body))
					return 0;
			}

			if (target.IsObjectGreyCon(Body))
				return 0;	// only attack if green+ to target

			if (Body.Faction != null && target is GamePlayer)
			{
				GamePlayer player = (GamePlayer)target;
				AggroLevel = Body.Faction.GetAggroToFaction(player);
			}
			if (AggroLevel >= 100)
				return 100;
			return AggroLevel;
		}

		public override void CheckAbilities()
		{
			////load up abilities
			if (Body.Abilities != null && Body.Abilities.Count > 0)
			{
				foreach (Ability ab in Body.Abilities.Values)
				{
					switch (ab.KeyName)
					{
						case Abilities.ChargeAbility:
							{
								if (Body.TargetObject is GameLiving
									&& !Body.IsWithinRadius(Body.TargetObject, 500)
									&& GameServer.ServerRules.IsAllowedToAttack(Body, Body.TargetObject as GameLiving, true))
								{
									ChargeAbility charge = Body.GetAbility<ChargeAbility>();
									if (charge != null && Body.GetSkillDisabledDuration(charge) <= 0)
									{
										charge.Execute(Body);
									}
								}
								break;
							}
					}
				}
			}
		}

		protected override void AttackMostWanted()
		{
			base.AttackMostWanted();
			if (!Body.IsCasting)
				CheckAbilities();
		}
	}
}
