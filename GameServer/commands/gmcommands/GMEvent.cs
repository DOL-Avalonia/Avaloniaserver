using DOL.GS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&GMEvent",
		ePrivLevel.GM,
		"Gestion des Events",
		"Commands.Players.Friend.Usage")]
	public class GMEvent
		: AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			DisplaySyntax(client);
			client.Out.SendMessage("Messages aux GMS..", PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_PopupWindow);
		}
	}
}
