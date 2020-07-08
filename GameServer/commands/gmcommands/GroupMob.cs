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
		  "'/GroupMob interact <GroupId> set <GroupInteractId> Affect un GroupInteract à un <GroudId>'",
		  "'/GroupMob interact <GroupdId> create Effect<SpellId|null> Flag<FlagValue> IsInvicible<true|false|null> Model<id|null> VisibleWeapon<value|null> Race<id|null>' - Créer un GroupInteract et l'affecte au groupe")]

	public class GroupMob
		  : AbstractCommandHandler, ICommandHandler
	{
		public async void OnCommand(GameClient client, string[] args)
		{

			GameNPC target = client.Player.TargetObject as GameNPC;
			string groupId = null;

			if (target == null && args.Length > 3 && args[1].ToLowerInvariant() != "interact")
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

				case "interact":

					if (args.Length < 5)
                    {
						DisplaySyntax(client);
						return;
					}

					if (!MobGroupManager.Instance.Groups.ContainsKey(groupId))
					{
						client.Out.SendMessage("Le GroupId: " + groupId + " n'existe pas.", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
						return;
					}

					if (args[3].ToLowerInvariant() == "set")
                    {
						string groupInteractId = args[4];
						var groupInteract = GameServer.Database.SelectObjects<GroupMobInteract>("InteractId = @InteractId", new QueryParameter("InteractId", groupInteractId))?.FirstOrDefault();

						if (groupInteract == null)
                        {
							client.Out.SendMessage("Le GroupMobInteract Id: " + groupInteractId + " n'existe pas.", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
							return;
                        }

						MobGroupManager.Instance.Groups[groupId].SetGroupInteractions(groupInteract);
						MobGroupManager.Instance.Groups[groupId].SaveToDabatase();
						client.Out.SendMessage("Le GroupId: " + groupId + " a été mis à jour.", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
						return;

					}
					else if(args[3].ToLowerInvariant() == "create")
                    {
						if (args.Length != 10)
                        {
							DisplaySyntax(client);
							return;
						}
// "'/GroupMob interact <GroupdId> create Effect<SpellId|null> Flag<FlagValue> IsInvicible<true|false|null> Model<id|null> VisibleWeapon<value|null> Race<id|null>'
						ushort? effect = args[4].ToLowerInvariant() == "null" ? (ushort?)null: ushort.TryParse(args[4], out ushort effectVal) ? effectVal : (ushort?)null;
						eFlags? flag = args[5].ToLowerInvariant() == "null" ? (eFlags?)null : Enum.TryParse(args[5], out eFlags flagEnum) ? flagEnum : (eFlags?)null;
						bool? isInvincible = args[6].ToLowerInvariant() == "null" ? (bool?)null : bool.TryParse(args[6], out bool isInvincibleBool) ? isInvincibleBool : (bool?)null;
						string model = args[7].ToLowerInvariant() == "null" ? null : args[7];
						byte? visibleWeapon = args[8].ToLowerInvariant() == "null" ? (byte?)null : byte.TryParse(args[8], out byte wp) ? wp : (byte?)null;
						eRace? race = args[9].ToLowerInvariant() == "null" ? (eRace?)null : Enum.TryParse(args[9], out eRace raceEnum) ? raceEnum : (eRace?)null;

						var groupInteract = new GroupMobInteract();
						groupInteract.Effect = effect?.ToString();
						groupInteract.Flag = flag?.ToString();
						groupInteract.InteractId = Guid.NewGuid().ToString().Substring(0,8);
						groupInteract.Model = model;
						groupInteract.Race = race?.ToString();
						groupInteract.SetInvincible = isInvincible.ToString();
						groupInteract.VisibleSlot = visibleWeapon?.ToString();
						GameServer.Database.AddObject(groupInteract);
						MobGroupManager.Instance.Groups[groupId].SetGroupInteractions(groupInteract);

						client.Out.SendMessage("Le Groupe " + groupId + " a été mis à jour et le GroupMobInteract a été créé et associé.", GS.PacketHandler.eChatType.CT_System, GS.PacketHandler.eChatLoc.CL_ChatWindow);
						return;
					}

					break;

				default:
					DisplaySyntax(client);
					break;
			}
		}
	}
}