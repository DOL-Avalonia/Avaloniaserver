using DOL.GS;
using DOL.GS.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.commands.gmcommands
{
	[CmdAttribute(
		  "&GroupMob",
		  ePrivLevel.GM,
		  "Commandes GroupMob",
		  "'/GroupMob add <groupId>' Ajoute le mob en target au group donné (créer le groupe si besoin)",
		  "'/GroupMob remove <groupId>' Supprime le mob en target de son groupe",
		  "'/GroupMob group remove <groupId>' Supprime le groupe et tous les mobs associés à celui-ci")]

	public class GroupMob
		  : AbstractCommandHandler, ICommandHandler
	{
		public async void OnCommand(GameClient client, string[] args)
		{

			GameNPC target = client.Player.TargetObject as GameNPC;
			string groupId = null;

			if (target == null)
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

					bool added = MobGroups.MobGroupManager.Instance.AddMobToGroup(target, groupId);
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

				default:
					DisplaySyntax(client);
					break;
			}
		}
	}
}