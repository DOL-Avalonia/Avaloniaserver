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
// Respec script Version 1.0 by Echostorm
/* Ver 1.0 Notes:
 * With changes to the core the respec system adds a new (allowed null) field to the DOL character file called RespecAllSkill that contains an integer.
 * All characters with 1 or more in their RespecAllSkill field, who are targeting their trainer will be able to Respec, or reset their spec
 *		points to their full amount and return their specs to 1 clearing their style and spell lists.  One respec is deducted each time.
 * Characters recieve 1 respec upon creation, and 2 more at 20th and 40th levels.  Respecs are currently cumulative due to the high
 *		demand.
 * Respec stones have been added to default item template to prevent confustion with item databases.  They can be created via the /item command
 *		by typing /item create respec_full | respec_single | respec_realm.
 * Respec stones may be turned in to trainers for respecs.
 * 
 * TODO: include autotrains in the formula
 * TODO: realm respec
 * 
 * Suncheck:
 * 	Added: /respec buy
 */


using System.Collections;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&respec",
		ePrivLevel.Player,
		"Commands.Players.Respec.Description",
		"Commands.Players.Respec.Usage")]
	public class RespecCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		const string RA_RESPEC = "realm_respec";
		const string ALL_RESPEC = "all_respec";
		const string LINE_RESPEC = "line_respec";
		const string DOL_RESPEC = "dol_respec";
		const string BUY_RESPEC = "buy_respec";
		const string CHAMP_RESPEC = "champion_respec";
		
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				if (ServerProperties.Properties.FREE_RESPEC)
				{
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Help.Trainer"));
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Help.All"));
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Help.Line"));
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Help.Realm"));
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Help.Champion"));
					return;
				}
				
				// Check for respecs.
				if (client.Player.RespecAmountAllSkill < 1
					&& client.Player.RespecAmountSingleSkill < 1
					&& client.Player.RespecAmountDOL <1
					&& client.Player.RespecAmountRealmSkill < 1)
				{
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Help.NotAvailable"));
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Help.NotAvailable.Buy"));
					return;
				}

				if (client.Player.RespecAmountAllSkill > 0)
				{
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Available.All",
							client.Player.RespecAmountAllSkill));
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Available.All.Use"));
				}
				if (client.Player.RespecAmountSingleSkill > 0)
				{
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Available.Line",
							client.Player.RespecAmountSingleSkill));
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Available.Line.Use"));
				}
				if (client.Player.RespecAmountRealmSkill > 0)
				{
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Available.Realm",
							client.Player.RespecAmountRealmSkill));
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Available.Realm.Use"));
				}
				if (client.Player.RespecAmountDOL > 0)
				{
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Available.DOL",
							client.Player.RespecAmountDOL));
					DisplayMessage(
						client,
						LanguageMgr.GetTranslation(
							client.Account.Language,
							"Commands.Players.Respec.Available.DOL.Use"));
				}
				DisplayMessage(
					client,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Respec.NotAvailable.Buy"));
				return;
			}

			GameTrainer trainer = client.Player.TargetObject as GameTrainer;
			// Player must be speaking with trainer to respec.  (Thus have trainer targeted.) Prevents losing points out in the wild.
			if (args[1].ToLower() != "buy" && (trainer == null || !trainer.CanTrain(client.Player)))
			{
				DisplayMessage(
					client,
					LanguageMgr.GetTranslation(
						client.Account.Language,
						"Commands.Players.Respec.NeedTrainer"));
				return;
			}

			switch (args[1].ToLower())
			{
				case "buy":
					{
						if (ServerProperties.Properties.FREE_RESPEC)
							return;

						// Buy respec
						if (client.Player.CanBuyRespec == false || client.Player.RespecCost < 0)
						{
							DisplayMessage(
								client,
								LanguageMgr.GetTranslation(
									client.Account.Language,
									"Commands.Players.Respec.NotAvailable.Level"));
							return;
						}

						long mgold = client.Player.RespecCost;
						if ((client.Player.Gold + 1000 * client.Player.Platinum) < mgold)
						{
							DisplayMessage(
								client,
								LanguageMgr.GetTranslation(
									client.Account.Language,
									"Commands.Players.Respec.NotAvailable.Gold", // No entry for Money, it replace by Gold
									mgold));
							return;
						}
						client.Out.SendCustomDialog(
							LanguageMgr.GetTranslation(
								client.Account.Language,
								"Commands.Players.Respec.Confirm.Buy",
								mgold),
							new CustomDialogResponse(RespecDialogResponse));
						client.Player.TempProperties.setProperty(BUY_RESPEC, true);
						break;
					}
				case "all":
					{
						// Check for full respecs.
						if (client.Player.RespecAmountAllSkill < 1
							&& !ServerProperties.Properties.FREE_RESPEC)
						{
							DisplayMessage(
								client,
								LanguageMgr.GetTranslation(
									client.Account.Language,
									"Commands.Players.Respec.Missing.All"));
							return;
						}
						client.Out.SendCustomDialog(
							LanguageMgr.GetTranslation(
								client.Account.Language,
								"Commands.Players.Respec.Caution"),
							new CustomDialogResponse(RespecDialogResponse));
						client.Player.TempProperties.setProperty(ALL_RESPEC, true);
						break;
					}
				case "dol":
					{
						// Check for DOL respecs.
						if (client.Player.RespecAmountDOL < 1
							&& !ServerProperties.Properties.FREE_RESPEC)
						{
							DisplayMessage(
								client,
								LanguageMgr.GetTranslation(
									client.Account.Language,
									"Commands.Players.Respec.Missing.DOL"));
							return;
						}
						client.Out.SendCustomDialog(
							LanguageMgr.GetTranslation(
								client.Account.Language,
								"Commands.Players.Respec.Caution"),
							new CustomDialogResponse(RespecDialogResponse));
						client.Player.TempProperties.setProperty(DOL_RESPEC, true);
						break;
					}
				case "realm":
					{
						if (client.Player.RespecAmountRealmSkill < 1
							&& !ServerProperties.Properties.FREE_RESPEC)
						{
							DisplayMessage(
								client,
								LanguageMgr.GetTranslation(
									client.Account.Language,
									"Commands.Players.Respec.Missing.Realm"));
							return;
						}
						client.Out.SendCustomDialog(
							LanguageMgr.GetTranslation(
								client.Account.Language,
								"Commands.Players.Respec.Caution"),
							new CustomDialogResponse(RespecDialogResponse));
						client.Player.TempProperties.setProperty(RA_RESPEC, true);
						break;
					}
				case "champion":
					{
						if (ServerProperties.Properties.FREE_RESPEC)
						{
							client.Out.SendCustomDialog(
								LanguageMgr.GetTranslation(
									client.Account.Language,
									"Commands.Players.Respec.Caution"),
								new CustomDialogResponse(RespecDialogResponse));
							client.Player.TempProperties.setProperty(CHAMP_RESPEC, true);
							break;
						}
						return;
					}
				default:
					{
						// Check for single-line respecs.
						if (client.Player.RespecAmountSingleSkill < 1
							&& !ServerProperties.Properties.FREE_RESPEC)
						{
							DisplayMessage(
								client,
								LanguageMgr.GetTranslation(
									client.Account.Language,
									"Commands.Players.Respec.Missing.Line"));
							return;
						}

						string lineName = string.Join(" ", args, 1, args.Length - 1);
						Specialization specLine = client.Player.GetSpecializationByName(lineName, false);
						if (specLine == null)
						{
							DisplayMessage(
								client,
								LanguageMgr.GetTranslation(
									client.Account.Language,
									"Commands.Players.Respec.Line.NotFound",
									lineName));
							return;
						}
						if (specLine.Level < 2)
						{
							DisplayMessage(
								client,
								LanguageMgr.GetTranslation(
									client.Account.Language,
									"Commands.Players.Respec.Line.Low",
									specLine.Name));
							return;
						}
						client.Out.SendCustomDialog(
								LanguageMgr.GetTranslation(
									client.Account.Language,
									"Commands.Players.Respec.Caution"),
								new CustomDialogResponse(RespecDialogResponse));
						client.Player.TempProperties.setProperty(LINE_RESPEC, specLine);
						break;
					}
			}
		}
		

		protected void RespecDialogResponse(GamePlayer player, byte response)
		{

			if (response != 0x01)
			{
				// declined
				// clean all properties
				player.TempProperties.removeProperty(ALL_RESPEC);
				player.TempProperties.removeProperty(DOL_RESPEC);
				player.TempProperties.removeProperty(RA_RESPEC);
				player.TempProperties.removeProperty(CHAMP_RESPEC);
				player.TempProperties.removeProperty(LINE_RESPEC);
				player.TempProperties.removeProperty(BUY_RESPEC);
				return;
			}

			int specPoints = player.SkillSpecialtyPoints;
			int realmSpecPoints = player.RealmSpecialtyPoints;

			if (player.TempProperties.getProperty(ALL_RESPEC, false))
			{
				player.RespecAll();
				player.TempProperties.removeProperty(ALL_RESPEC);
			}
			if (player.TempProperties.getProperty(DOL_RESPEC, false))
			{
				player.RespecDOL();
				player.TempProperties.removeProperty(DOL_RESPEC);
			}
			if (player.TempProperties.getProperty(RA_RESPEC, false))
			{
				player.RespecRealm();
				player.TempProperties.removeProperty(RA_RESPEC);
			}
			if (player.TempProperties.getProperty(CHAMP_RESPEC, false))
			{
				player.RespecChampionSkills();
				player.TempProperties.removeProperty(CHAMP_RESPEC);
			}
			if (player.TempProperties.getProperty<object>(LINE_RESPEC, null) != null)
			{
				Specialization specLine = (Specialization)player.TempProperties.getProperty<object>(LINE_RESPEC, null);
				player.RespecSingle(specLine);
				player.TempProperties.removeProperty(LINE_RESPEC);
			}
			if (player.TempProperties.getProperty(BUY_RESPEC, false))
			{
				player.TempProperties.removeProperty(BUY_RESPEC);
				if (player.RespecCost >= 0 && player.RemoveMoney(player.RespecCost * 10000))
				{
                    InventoryLogging.LogInventoryAction(player, "(respec)", eInventoryActionType.Merchant, player.RespecCost * 10000);
					player.RespecAmountSingleSkill++;
					player.RespecBought++;
					DisplayMessage(
						player,
						LanguageMgr.GetTranslation(
							player.Client.Account.Language,
							"Commands.Players.Respec.Buy.Line"));
				}
				player.Out.SendUpdateMoney();
			}			
			// Assign full points returned
			if (player.SkillSpecialtyPoints > specPoints)
			{
				player.RemoveAllStyles(); // Kill styles
				DisplayMessage(
						player,
						LanguageMgr.GetTranslation(
							player.Client.Account.Language,
							"Commands.Players.Respec.Regain.Spec",
							(player.SkillSpecialtyPoints - specPoints)));
			}
			if (player.RealmSpecialtyPoints > realmSpecPoints)
			{
				DisplayMessage(
						player,
						LanguageMgr.GetTranslation(
							player.Client.Account.Language,
							"Commands.Players.Respec.Regain.Realm",
							(player.RealmSpecialtyPoints - realmSpecPoints)));
			}
			player.RefreshSpecDependantSkills(false);
			// Notify Player of points
			player.Out.SendUpdatePlayerSkills();
			player.Out.SendUpdatePoints();
			player.Out.SendUpdatePlayer();
			player.SendTrainerWindow();
			player.SaveIntoDatabase();
		}
	}
}
