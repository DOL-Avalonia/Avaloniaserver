﻿using DOL.GameEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Commands
{

	[CmdAttribute(
		"&event",
		ePrivLevel.Player,
		"Commands.Players.Event.Description",
		"Commands.Players.Friend.Usage")]
	public class Event :
		AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			ShowEvents(client);
		}	

		private void ShowEvents(GameClient client)
		{
			client.Out.SendCustomTextWindow("[ EVENTS ]",  GameEventManager.Instance.GetEventsInfos(true, false));
		}
	}
}
