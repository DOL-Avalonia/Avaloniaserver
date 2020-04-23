using DOL.Database;
using DOL.GameEvents;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOLDatabase.Tables;
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
		"'/GMEvent info [id]' Affiche les informations sur les Events, [id] pour un event sinon tous sont affichés",
		"'/GMEvent infolight' Affiche les informations sur les Events, plus succin",
		"'/GMEvent start <id>' Lance l'evenement avec son <id>",
		"'/GMEvent add <eventId>' Ajoute la cible (mob ou coffre) à l'event et le fait disparaitre du monde",
		"'/GMEvent add <mob|coffre> <name> <region> <eventId>'Ajoute un <mob|coffre> par son nom et sa region à un event",	
		"'/GMEvent respawn <mob|coffre> <name> <eventId> <true|false>'Change la valeur de CanRespawn du <mob|coffre> par son <name> dans un event par son <eventId> <true|false>",		
		"'/GMEvent starteffect <mob|coffre> <name> <eventId> <spellId>'Change la valeur starteffectId du <mob|coffre> par son <name> dans un event <eventId> en spécifiant le <spellId>",
		"'/GMEvent endeffect <mob|coffre> <name> <eventId> <spellId>'Change la valeur endeffectId du <mob|coffre> par son <name> dans un event <eventId> en spécifiant le <spellId>",
		"'GMEvent refresh region <regionid>'Cherche les mobs et les coffres de la région <regionid> avec des EventID récemment ajoutés et les ajoute à cet évent'")]
	
	public class GMEvent
		: AbstractCommandHandler, ICommandHandler
	{
		public async void OnCommand(GameClient client, string[] args)
		{
			string id = null;
			string name = null;

			if (args.Length == 1)
				DisplaySyntax(client);
			

			if (args.Length > 1)
			{				
				if (args.Length > 2 && !string.IsNullOrEmpty(args[2]))
				{
					id = args[2];
				}

				switch (args[1].ToLower())
				{
					case "info":

						if (args.Length == 2)
						{
							ShowEvents(client);
						}
						else if (args.Length == 3 && id != null)
						{
							if (!ShowEvent(client, id))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.EventNotFound", id), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
						}
						else
						{
							DisplaySyntax(client);
						}
						break;

					case "infolight":
						ShowLightEvents(client);
						break;

					case "add":

						if (args.Length == 6) 
						{
							name = args[3];
							ushort region = 0;						
								
							if (!ushort.TryParse(args[4], out region) || string.IsNullOrEmpty(name))
							{
								DisplaySyntax(client);
								return;
							}

							id = args[5];

							if (string.IsNullOrEmpty(id))
							{
								DisplaySyntax(client);
								return;
							}

							if (args[2] == "mob")
							{
								TryToAddMobToEvent(client, name, region, id, true);
							}
							else if (args[2] == "coffre")
							{
								TryToAddMobToEvent(client, name, region, id, false);
							}
							else
							{
								DisplaySyntax(client);
							}
						}
						else
						{
							if (client.Player.TargetObject is GameNPC npc && id != null)
							{
								if (!AddItemToEvent(npc, id, false))
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.EventNotFound", id), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
								}
								else
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemAdded", npc.Name, id), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
								}
							}
							else if (client.Player.TargetObject is GameStaticItem st && st.IsCoffre && id != null)
							{
								if (!AddItemToEvent(st, id, true))
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.EventNotFound", id), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
								}
								else
								{
									client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemAdded", st.Name, id), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
								}
							}
							else
							{
								DisplaySyntax(client);
							}
						}

						break;

					case "respawn":
						
						if (args.Length != 6)
						{
							DisplaySyntax(client);
							return;
						}

						name = args[3];
						id = args[4];
						bool canRespawn = false;
						if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id) || !bool.TryParse(args[5], out canRespawn))
						{
							DisplaySyntax(client);
							return;
						}					

						if (args[2] == "mob")
						{
							if (!ChangeRespawnValue(client, name, id, canRespawn, true))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemNotFound", name), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemModified", name), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
						}
						else if (args[2] == "coffre")
						{
							if (!ChangeRespawnValue(client, name, id, canRespawn, false))
							{								
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemNotFound", name), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemModified", name), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
						}
						else
						{
							DisplaySyntax(client);
						}
						break;

					case "starteffect":

						if (args.Length != 6)
						{
							DisplaySyntax(client);
							return;
						}

						name = args[3];
						id = args[4];
						int startEffect = 0;
						if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id))
						{
							DisplaySyntax(client);
							return;
						}

						if (!int.TryParse(args[5], out startEffect) || startEffect == 0)
						{
							DisplaySyntax(client);
							return;
						}

						if (args[2] == "mob")
						{
							if (!ChangeStartEffectValue(client, name, id, startEffect, true))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemNotFound", name), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemModified", name), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
						}
						else if (args[2] == "coffre")
						{
							if (!ChangeStartEffectValue(client, name, id, startEffect, false))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemNotFound", name), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemModified", name), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
						}
						else
						{
							DisplaySyntax(client);
						}
						break;

					case "endeffect":

						if (args.Length != 6)
						{
							DisplaySyntax(client);
							return;
						}

						name = args[3];
						id = args[4];
						int endEffect = 0;
						if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id))
						{
							DisplaySyntax(client);
							return;
						}

						if (!int.TryParse(args[5], out endEffect) || endEffect == 0)
						{
							DisplaySyntax(client);
							return;
						}

						if (args[2] == "mob")
						{
							if (!ChangeEndEffectValue(client, name, id, endEffect, true))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemNotFound", name), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemModified", name), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
						}
						else if (args[2] == "coffre")
						{
							if (!ChangeEndEffectValue(client, name, id, endEffect, false))
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemNotFound", name), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
							else
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemModified", name), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
							}
						}
						else
						{
							DisplaySyntax(client);
						}
						break;

					case "start":

						if (id != null)
						{
							var ev = GameEventManager.Instance.Events.FirstOrDefault(e => e.ID.Equals(id));

							if (ev == null)
							{
								client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.EventNotFound", id), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
								return;
							}

							await GameEventManager.Instance.StartEvent(ev);
							client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.EventStarted", id), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
						}
						else
						{
							DisplaySyntax(client);
						}

						break;


					case "refresh":
						if (args.Length != 4 || args[2] != "region")
						{
							DisplaySyntax(client);
							return;
						}
						
						if (ushort.TryParse(args[3], out ushort refreshRegion))
						{
							RefreshRegion(client, refreshRegion);
						}
						else
						{
							DisplaySyntax(client);
						}

						break;


					default:
						DisplaySyntax(client);
						break;
				}
			}

		}

		private void RefreshRegion(GameClient client, ushort region)
		{
			if (!WorldMgr.Regions.ContainsKey(region))
			{
				client.Out.SendMessage("La region n'existe pas", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			bool hasNewitems = false;
			List<string> ids = new List<string>();

			var mobs = GameServer.Database.SelectObjects<Mob>("`EventID` IS NOT NULL AND `region` = @region", new QueryParameter("region", region));

			if (mobs == null)
			{
				client.Out.SendMessage("Aucun mobs de trouvés dans la region" + region, eChatType.CT_System, eChatLoc.CL_SystemWindow);
			}
			else
			{
				foreach (var mob in mobs)
				{
					GameNPC mobInRegion = WorldMgr.Regions[region].Objects.FirstOrDefault(o => o != null && o is GameNPC npc && npc.InternalID.Equals(mob.ObjectId)) as GameNPC;
					
					if (mobInRegion != null)
					{
						mobInRegion.EventID = mob.EventID;
						mobInRegion.RemoveFromWorld();
						GameEventManager.Instance.PreloadedMobs.Add(mobInRegion);
						hasNewitems = true;

						if (!ids.Contains(mobInRegion.EventID))
						{
							ids.Add(mobInRegion.EventID);
						}
					}
				}
			}

			 var scr = ScriptMgr.Scripts.FirstOrDefault(s => s.FullName.Contains("GameServerScripts"));

			if (scr != null)
			{
				GameStaticItem item = null;
				try
				{
					item = scr.CreateInstance("DOL.GS.Scripts.GameCoffre") as GameStaticItem;
				}
				catch { }
			
				if (item != null)
				{
					var coffres = item.GetCoffresUsedInEventsInDb(region);

					if (coffres == null)
					{
						client.Out.SendMessage("Aucun Coffres ont été trouvés dans la region" + region, eChatType.CT_System, eChatLoc.CL_SystemWindow);
					}
					else
					{
						foreach (var coffreInfo in coffres)
						{
							coffreInfo.Item1.EventID = coffreInfo.Item2;
							coffreInfo.Item1.RemoveFromWorld();
							GameEventManager.Instance.PreloadedCoffres.Add(coffreInfo.Item1);
							hasNewitems = true;

							if (!ids.Contains(coffreInfo.Item1.EventID))
							{
								ids.Add(coffreInfo.Item1.EventID);
							}
						}
					}
				}
			}

			if (hasNewitems && ids.Any())
			{
				GameEventManager.CreateMissingRelationObjects(ids);
			}
		}

		private void ShowLightEvents(GameClient client)
		{
			client.Out.SendCustomTextWindow("[ EVENTS ]", GameEventManager.Instance.GetEventsLightInfos());
		}

		private bool ChangeStartEffectValue(GameClient client, string name, string id, int effect, bool isMob)
		{
			string itemID = null;
			var ev = GetEventById(client, id);

			if (ev == null)
			{
				return false;
			}

			if (isMob)
			{
				var mobInfo = GetMobInfo(ev, name, client);

				if (mobInfo == null)
				{
					return false;
				}

				itemID = mobInfo.Mob.InternalID;
				mobInfo.Db.StartEffect = effect;
				GameServer.Database.SaveObject(mobInfo.Db);
			}
			else
			{
				var coffreInfo = GetCoffreInfo(ev, name, client);

				if (coffreInfo == null)
				{
					return false;
				}

				itemID = coffreInfo.Item.InternalID;
				coffreInfo.Db.StartEffect = effect;
				GameServer.Database.SaveObject(coffreInfo.Db);
			}

			if (ev.StartEffects.ContainsKey(itemID))
			{
				ev.StartEffects[itemID] = (ushort)effect;
			}
			else
			{
				ev.StartEffects.Add(itemID, (ushort)effect);
			}

			return true;
		}

		private MobInfo GetMobInfo(GameEvent ev, string name, GameClient client)
		{
			var mob = ev.Mobs.FirstOrDefault(c => c.Name.Equals(name));

			if (mob == null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Target.NotFound", name), eChatType.CT_System, eChatLoc.CL_SystemWindow); ;
				return null;
			}

			var mobDb = GameServer.Database.SelectObjects<EventsXObjects>("`ItemID` = @ItemID", new Database.QueryParameter("ItemID", mob.InternalID))?.FirstOrDefault();

			if (mobDb == null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Target.NotFound", name), eChatType.CT_System, eChatLoc.CL_SystemWindow); ;
				return null;
			}

			return new MobInfo()
			{
				Db = mobDb,
				Mob = mob
			};
		}


		private CoffreInfo GetCoffreInfo(GameEvent ev, string name, GameClient client)
		{
			var coffre = ev.Coffres.FirstOrDefault(c => c.Name.Equals(name));

			if (coffre == null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Target.NotFound", name), eChatType.CT_System, eChatLoc.CL_SystemWindow); ;
				return null;
			}

			var coffreDb = GameServer.Database.SelectObjects<EventsXObjects>("`ItemID` = @ItemID", new Database.QueryParameter("ItemID", coffre.InternalID))?.FirstOrDefault();

			if (coffreDb == null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Target.NotFound", name), eChatType.CT_System, eChatLoc.CL_SystemWindow); ;
				return null;
			}

			return new CoffreInfo()
			{
				Db = coffreDb,
				Item = coffre
			};
		}


		private GameEvent GetEventById(GameClient client, string id)
		{
			var ev = GameEventManager.Instance.Events.FirstOrDefault(e => e.ID.Equals(id));

			if (ev == null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.EventNotFound", id), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
				return null;
			}

			return ev;
		}		

		private bool ChangeEndEffectValue(GameClient client, string name, string id, int effect, bool isMob)
		{
			string itemID = null;
			var ev = GetEventById(client, id);

			if (ev == null)
			{
				return false;
			}

			if (isMob)
			{
				var mobInfo = GetMobInfo(ev, name, client);

				if (mobInfo == null)
				{
					return false;
				}

				itemID = mobInfo.Mob.InternalID;
				mobInfo.Db.EndEffect = effect;
				GameServer.Database.SaveObject(mobInfo.Db);
			}
			else
			{
				var coffreInfo = GetCoffreInfo(ev, name, client);

				if (coffreInfo == null)
				{
					return false;
				}

				itemID = coffreInfo.Item.InternalID;
				coffreInfo.Db.EndEffect = effect;
				GameServer.Database.SaveObject(coffreInfo.Db);
			}

			if (ev.EndEffects.ContainsKey(itemID))
			{
				ev.EndEffects[itemID] = (ushort)effect;
			}
			else
			{
				ev.EndEffects.Add(itemID, (ushort)effect);
			}

			return true;
		}

		private bool ChangeRespawnValue(GameClient client, string name, string id, bool canRespawn, bool isMob)
		{
			var ev = GameEventManager.Instance.Events.FirstOrDefault(e => e.ID.Equals(id));

			if (ev == null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.EventNotFound", id), eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
				return false;
			}
		
			if (isMob)
			{
				var mob = ev.Mobs.FirstOrDefault(m => m.Name.Equals(name));

				if (mob == null)
				{
					client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Target.NotFound", name), eChatType.CT_System, eChatLoc.CL_SystemWindow); ;
					return false;
				}

				var mobDb = GameServer.Database.SelectObjects<EventsXObjects>("`ItemID` = @ItemID", new Database.QueryParameter("ItemID", mob.InternalID))?.FirstOrDefault();


				if (mobDb == null)
				{
					client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Target.NotFound", name), eChatType.CT_System, eChatLoc.CL_SystemWindow); ;
					return false;
				}

				mobDb.CanRespawn = canRespawn;
				mob.CanRespawnWithinEvent = canRespawn;
				GameServer.Database.SaveObject(mobDb);
			}
			else
			{
				var coffre = ev.Coffres.FirstOrDefault(c => c.Name.Equals(name));

				if (coffre == null)
				{
					client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Target.NotFound", name), eChatType.CT_System, eChatLoc.CL_SystemWindow); ;
					return false;
				}

				var coffreDb = GameServer.Database.SelectObjects<EventsXObjects>("`ItemID` = @ItemID", new Database.QueryParameter("ItemID", coffre.InternalID))?.FirstOrDefault();

				if (coffreDb == null)
				{
					client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Target.NotFound", name), eChatType.CT_System, eChatLoc.CL_SystemWindow); ;
					return false;
				}

				coffreDb.CanRespawn = canRespawn;
				coffre.CanRespawnWithinEvent = canRespawn;
				GameServer.Database.SaveObject(coffreDb);
			}

			return true;
		}

		private void TryToAddMobToEvent(GameClient client, string name, ushort region, string id, bool isMob)
		{
			var ev = GetEventById(client, id);

			if (ev == null)
			{
				return;
			}

			if (!WorldMgr.Regions.ContainsKey(region))
			{
				DisplaySyntax(client);
				return;
			}

			var obj = WorldMgr.Regions[region].Objects.FirstOrDefault(o => o?.Name.Equals(name) == true);

			if (obj == null)
			{
				client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Target.NotFound", name), eChatType.CT_System, eChatLoc.CL_SystemWindow); ;
				return;
			}

			if (isMob && obj is GameNPC npc)
			{
				if (npc == null)
				{
					client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Target.NotFound", name), eChatType.CT_System, eChatLoc.CL_SystemWindow); ;
					return;
				}

				npc.EventID = id;
				GameEventManager.Instance.PreloadedMobs.Add(npc);
			}
			else
			{				
				GameStaticItem coffre = obj as GameStaticItem;

				if (coffre == null || !coffre.IsCoffre)
				{
					client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Target.NotFound", name), eChatType.CT_System, eChatLoc.CL_SystemWindow); ;
					return;
				}

				coffre.EventID = id;
				GameEventManager.Instance.PreloadedCoffres.Add(coffre);
			}
			
			obj.RemoveFromWorld();
			obj.SaveIntoDatabase();
		
			GameEventManager.CreateMissingRelationObjects(new string[] { id });
			client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "Commands.Players.Event.ItemAdded", name, id), eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}

		private bool AddItemToEvent(GameObject item, string id, bool isCoffre)
		{
			var ev = GameEventManager.Instance.Events.FirstOrDefault(e => e.ID.Equals(id));

			if (ev == null)
			{
				return false;
			}			

			if (isCoffre)
			{
				var coffre = (GameStaticItem)item;
				coffre.EventID = id;
				coffre.SaveIntoDatabase();

				if (!ev.Coffres.Contains(coffre))
				{
					GameEventManager.Instance.PreloadedCoffres.Add(coffre);
					GameEventManager.CreateMissingRelationObjects(new string[] { id });
					return true;
				}
			}
			else
			{
				var mob = (GameNPC)item;
				mob.EventID = id;
				mob.SaveIntoDatabase();

				if (!ev.Mobs.Contains(mob))
				{
					GameEventManager.Instance.PreloadedMobs.Add(mob);
					GameEventManager.CreateMissingRelationObjects(new string[] { id });
					return true;
				}					
			}
			
			return false;
		}

		private bool ShowEvent(GameClient client, string id)
		{
			var infos = GameEventManager.Instance.GetEventInfo(id);
			if (infos == null)
			{
				return false;
			}

			client.Out.SendCustomTextWindow("[ EVENT "+ id +" ]", infos);
			return true;
		}

		private void ShowEvents(GameClient client)
		{			
			client.Out.SendCustomTextWindow("[ EVENTS ]", GameEventManager.Instance.GetEventsInfos(false, true));
		}
	}


	public class CoffreInfo
	{
		public GameStaticItem Item
		{
			get;
			set;
		}

		public EventsXObjects Db
		{
			get;
			set;
		}
	}

	public class MobInfo
	{
		public GameNPC Mob
		{
			get;
			set;
		}

		public EventsXObjects Db
		{
			get;
			set;
		}
	}
}
