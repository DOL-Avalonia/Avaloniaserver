using DOL.GameEvents;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
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
		"Commands.Players.Event.Description",
		"'/GMEvent info' Affiche les informations sur les Events",
		"'/GMEvent start <id>' Lance l'evenement avec son <id>")]
	public class GMEvent
		: AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{

			if (args.Length == 1)
				DisplaySyntax(client);

			if (args.Length > 1)
			{
				switch (args[1].ToLower())
				{

					case "info":
						ShowEvents(client);
						break;

					case "start":

						if (args.Length == 3)
						{
							string id = args[2];

							var ev = GameEventManager.Instance.Events.FirstOrDefault(e => e.ID.Equals(id));

							if (ev == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.EventNotFound", id), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
								return;
							}

							GameEventManager.Instance.StartEvent(ev);
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.EventStarted", id), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
						}

						break;


					default:
						DisplaySyntax(client);
						break;
				}
			}

		}

		private void ShowEvents(GameClient client)
		{			
			client.Out.SendCustomTextWindow("[ EVENTS ]", GameEventManager.Instance.GetEventsInfos(false, true));
		}
	}
}
