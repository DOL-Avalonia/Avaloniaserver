using DOL.Database;
using DOL.GS;
using DOL.GS.Commands;
using DOL.MobGroups;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.GameNPC;

namespace DOL.commands.gmcommands
{
	[CmdAttribute(
		  "&GroupMob",
		  ePrivLevel.GM,
		  "Commandes GroupMob",
		  "'/GroupMob add <groupId>' Ajoute le mob en target au group donné (créer le groupe si besoin)",
		  "'/GroupMob remove <groupId>' Supprime le mob en target de son groupe",
		  "'/GroupMob group remove <groupId>' Supprime le groupe et tous les mobs associés à celui-ci",
		  "'/GroupMob info <GroupId>' Affiche les infos sur un GroupMob en fournissant son <GroupId>",
		  "'/GroupMob status <GroupId> set <StatusId> <SlaveGroupId> Affecte un GroupMobStatus<StatusId> à un <GroupId>(master) envers un <GroupId>(slave)'",
		  "'/GroupMob status origin set <StatusId> <GroupId>' Attribut un Status d'origine à un GroupMob en donnant son <GroupdId> et le <StatusId> souhaité",
		  "'/GroupMob status create <SpellId|null>(Effect) <FlagsValue|null>(Flags) <true|false|null>(IsInvicible) <id|null>(Model) <value|null>(VisibleWeapon) <id|null>(Race)' - Créer un GroupStatus et renvoie en sortie <StatusId>)")]

	public class GroupMob
		  : AbstractCommandHandler, ICommandHandler
	{
		public async void OnCommand(GameClient client, string[] args)
		{

			GameNPC target = client.Player.TargetObject as GameNPC;
			string groupId = null;

			if (target == null && args.Length > 3 && args[1].ToLowerInvariant() != "status")
			{
				if (args.Length == 4 && args[1].ToLowerInvariant() == "group" && args[2].ToLowerInvariant() == "remove")
				{
					groupId = args[3];
					bool allRemoved = MobGroups.MobGroupManager.Instance.RemoveGroupsAndMobs(groupId);

					if (allRemoved)
					{
						client.Out.SendMessage($"le groupe {groupId} a été supprimé et les mobs liés à celui-ci enlevés.", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
					}
					else
					{
						client.Out.SendMessage($"Impossible de supprimer le groupe {groupId}", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
					}
				}
				else
				{
					client.Out.SendMessage("La target doit etre un mob", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
					this.DisplaySyntax(client);
				}

				return;
			}

			if (args.Length < 3)
			{
				DisplaySyntax(client);
				return;
			}

			groupId = args[2];

			if (string.IsNullOrEmpty(groupId))
			{
				DisplaySyntax(client);
				return;
			}

			switch (args[1].ToLowerInvariant())
			{
				case "add":

					bool added = MobGroupManager.Instance.AddMobToGroup(target, groupId);
					if (added)
					{
						client.Out.SendMessage($"le mob {target.Name} a été ajouté au groupe {groupId}", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
					}
					else
					{
						client.Out.SendMessage($"Impossible d'ajouter {target.Name} au groupe {groupId}", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
					}
					break;

				case "remove":

					bool removed = MobGroups.MobGroupManager.Instance.RemoveMobFromGroup(target, groupId);
					if (removed)
					{
						client.Out.SendMessage($"le mob {target.Name} a été supprimé du groupe {groupId}", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
					}
					else
					{
						client.Out.SendMessage($"Impossible de supprimer {target.Name} du groupe {groupId}", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
					}
					break;


				case "info":

					if (!MobGroupManager.Instance.Groups.ContainsKey(groupId))
                    {
						client.Out.SendMessage($"Le groupe {groupId} n'existe pas.", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
					}

					IList<string> infos = MobGroupManager.Instance.GetInfos(MobGroupManager.Instance.Groups[groupId]);
					
					if (infos != null)
                    {
						client.Out.SendCustomTextWindow("[ GROUPMOB " + groupId + " ]", infos);
                    }
					break;

				case "status":

					if (args.Length < 6)
                    {
						DisplaySyntax(client);
						return;
					}

					if (args[3].ToLowerInvariant() == "set")
                    {
						string groupStatusId = args[4];
						string slaveGroupId = args[5];
						
						if (args[2].ToLowerInvariant() == "origin")
                        {
							groupId = args[5];

							if (!this.isGroupIdAvailable(groupId, client))
							{
								return;
							}

							var status = GameServer.Database.SelectObjects<GroupMobStatusDb>("GroupStatusId = @GroupStatusId", new QueryParameter("GroupStatusId", groupStatusId))?.FirstOrDefault();

							if (status == null)
							{
								client.Out.SendMessage("Le GroupStatusId: " + groupStatusId + " n'existe pas.", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
								return;
							}

							MobGroupManager.Instance.Groups[groupId].SetGroupInfo(status);
							MobGroupManager.Instance.Groups[groupId].SaveToDabatase();
							client.Out.SendMessage("Le GroupStatus: " + groupStatusId + " a été attribué au MobGroup " + groupId, GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
							return;
						}
                        else
                        {
							if (!this.isGroupIdAvailable(groupId, client))
                            {
								return;
                            }

							if (!MobGroupManager.Instance.Groups.ContainsKey(slaveGroupId))
							{
								client.Out.SendMessage("Le SlaveGroupId : " + slaveGroupId + " n'existe pas.", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
								return;
							}

							var groupInteract = GameServer.Database.SelectObjects<GroupMobStatusDb>("GroupStatusId = @GroupStatusId", new QueryParameter("GroupStatusId", groupStatusId))?.FirstOrDefault();

							if (groupInteract == null)
							{
								client.Out.SendMessage("Le GroupStatusId: " + groupStatusId + " n'existe pas.", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
								return;
							}

							MobGroupManager.Instance.Groups[groupId].SetGroupInteractions(groupInteract);
							MobGroupManager.Instance.Groups[groupId].SlaveGroupId = slaveGroupId;
							MobGroupManager.Instance.Groups[groupId].SaveToDabatase();
							client.Out.SendMessage("Le MobGroup: " + groupId + " a été associé au GroupMobInteract" + groupInteract.GroupStatusId, GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
							return;
						}
					}
					else if(args[2].ToLowerInvariant() == "create")
                    {
						if (args.Length != 9)
                        {
							DisplaySyntax(client);
							return;
						}					

// "'/GroupMob interact <GroupdId> create Effect<SpellId|null> Flag<FlagValue> IsInvicible<true|false|null> Model<id|null> VisibleWeapon<value|null> Race<id|null>'
						ushort? effect = args[3].ToLowerInvariant() == "null" ? (ushort?)null: ushort.TryParse(args[3], out ushort effectVal) ? effectVal : (ushort?)null;
						eFlags? flag = args[4].ToLowerInvariant() == "null" ? (eFlags?)null : Enum.TryParse(args[4], out eFlags flagEnum) ? flagEnum : (eFlags?)null;
						bool? isInvincible = args[5].ToLowerInvariant() == "null" ? (bool?)null : bool.TryParse(args[5], out bool isInvincibleBool) ? isInvincibleBool : (bool?)null;
						string model = args[6].ToLowerInvariant() == "null" ? null : args[6];
						byte? visibleWeapon = args[7].ToLowerInvariant() == "null" ? (byte?)null : byte.TryParse(args[7], out byte wp) ? wp : (byte?)null;
						eRace? race = args[8].ToLowerInvariant() == "null" ? (eRace?)null : Enum.TryParse(args[8], out eRace raceEnum) ? raceEnum : (eRace?)null;

						var groupStatus = new GroupMobStatusDb();
						groupStatus.Effect = effect?.ToString();
						groupStatus.Flag = flag?.ToString();
						groupStatus.GroupStatusId = Guid.NewGuid().ToString().Substring(0,8);
						groupStatus.Model = model;
						groupStatus.Race = race?.ToString();
						groupStatus.SetInvincible = isInvincible?.ToString();
						groupStatus.VisibleSlot = visibleWeapon?.ToString();

                        try
                        {
							GameServer.Database.AddObject(groupStatus);
                        }
                        catch
                        {
							groupStatus.GroupStatusId = Guid.NewGuid().ToString().Substring(0, 8);
							GameServer.Database.AddObject(groupStatus);
						}

						client.Out.SendMessage("Le GroupStatus a été créé avec le GroupStatusId: " + groupStatus.GroupStatusId, GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
						return;
					}

					break;

				default:
					DisplaySyntax(client);
					break;
			}
		}

		private bool isGroupIdAvailable(string groupId, GameClient client)
        {
			if (!MobGroupManager.Instance.Groups.ContainsKey(groupId))
			{
				client.Out.SendMessage("Le GroupId: " + groupId + " n'existe pas.", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
				return false;
			}

			return true;
		}
	}
}