using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using DOL.Database;
using DOL.GS.PacketHandler;

namespace DOL.GS.Scripts
{
	public class GameCoffre : GameStaticItem
	{
		public const string CROCHET = "Crochet"; //Id_nb des crochets
		public const int	UNLOCK_TIME = 10; //Temps pour crocheter une serrure en secondes
	    private GamePlayer m_interactPlayer;
	    private DateTime m_lastInteract;
		

		#region Variables - Constructeur
		private DBCoffre Coffre;
		private IList<CoffreItem> m_Items;
		public IList<CoffreItem> Items
		{
			get { return m_Items; }
		}

		public Timer RespawnTimer
		{
			get;
			set;
		}

        public static List<GameCoffre> Coffres;


		private int m_ItemChance;
		/// <summary>
		/// Pourcentage de chance de trouver un item dans le coffre (si 0 alors 50% de chance)
		/// </summary>
		public int ItemChance
		{
			get { return m_ItemChance; }
			set
			{
				if(value > 100)
					m_ItemChance = 100;
				else if(value < 0)
					m_ItemChance = 0;
				else
					m_ItemChance = value;
			}
		}

		public int TrapRate
		{
			get;
			set;
		}

		public string NpctemplateId
		{
			get;
			set;
		}
		
		public int TpX
		{
			get;
			set;			
		}

		public int TpY
		{
			get;
			set;			
		}
	
		public int TpZ
		{
			get;
			set;		
		}

		public bool IsTeleporter
		{
			get;
			set;
		}
		
		public int TpLevelRequirement
		{
			get;
			set;
		}

		public bool TpIsRenaissance
		{
			get;
			set;
		}

		public int TpEffect
		{
			get;
			set;
		}

		public int TpRegion
		{
			get;
			set;
		}

		public int	AllChance
		{
			get 
			{
				return  m_Items.Sum(item => item.Chance);
			}
		}
		public DateTime LastOpen;
		/// <summary>
		/// Temps de réapparition d'un item (en minutes)
		/// </summary>
		public int ItemInterval;

		public string KeyItem = "";
		public int LockDifficult;

		public GameCoffre()
		{
			LastOpen = DateTime.MinValue;
			ItemInterval = 5;
			m_Items = new List<CoffreItem>();
		}

		public GameCoffre(IList<CoffreItem> Items)
		{
			LastOpen = DateTime.MinValue;
			ItemInterval = 5;
			m_Items = Items;
		}
		#endregion

		public override bool AddToWorld()
		{
			base.AddToWorld();

			if (Coffres == null)
			{
				Coffres = new List<GameCoffre>();
			}

			Coffres.Add(this);

			return true;
		}

		public void RespawnCoffre()
		{
			base.AddToWorld();
		}

		#region Interact - GetRandomItem
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact (player) || !player.IsAlive) return false;

			////Coffre vide
			//if ((LastOpen.Ticks / 600000000 + ItemInterval) > DateTime.Now.Ticks / 600000000)
			//{
   //             player.Out.SendMessage("Quelqu'un a déjà regardé par ici... Revenez-plus tard.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			//	return true;
			//}

			if (LockDifficult > 0 || KeyItem != "")
			{
                if (m_interactPlayer != null && player != m_interactPlayer && (m_lastInteract.Ticks + 200000000) > DateTime.Now.Ticks)
                {
                    player.Out.SendMessage("Quelqu'un a déjà regardé par ici... Revenez-plus tard.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                    return true;
                }
			    if (KeyItem != "" && player.Inventory.GetFirstItemByID(KeyItem, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) != null)
			    {
			        if(!KeyItem.StartsWith("oneuse"))
			            player.Out.SendMessage("Vous avez utilisé " + player.Inventory.GetFirstItemByID(KeyItem, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack).Name + ".", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			        else
			        {
			            InventoryItem it = player.Inventory.GetFirstItemByID(KeyItem, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
			            player.TempProperties.setProperty("CoffreItem", it);
			            player.Out.SendCustomDialog("Voulez-vous utiliser \""+it.Name+"\" ?", OneUseOpen);
			            m_interactPlayer = player;
			            m_lastInteract = DateTime.Now;
			            return true;
			        }
			    }
			    else if (LockDifficult > 0 && player.Inventory.GetFirstItemByID(CROCHET, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) != null)
			    {
			        player.Out.SendCustomDialog("Voulez-vous crocheter la serrure ?", Unlock);
			        m_interactPlayer = player;
                    m_lastInteract = DateTime.Now;
			        return true;
			    }
			    else
			    {
			        if (LockDifficult == 0 && KeyItem != "")
			            player.Out.SendMessage("Vous avez besoin d'un objet.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			        else if (LockDifficult > 0 && KeyItem == "")
			            player.Out.SendMessage("Vous avez besoin d'un crochet.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			        else
			            player.Out.SendMessage("Vous avez besoin d'un objet ou d'un crochet.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			        return true;
			    }
			}

		    return InteractEnd(player);
		}

		private void OneUseOpen(GamePlayer player, byte response)
		{
			if (response == 0x00) return;
			InventoryItem it = player.TempProperties.getProperty<InventoryItem>("CoffreItem", null);
			player.TempProperties.removeProperty("CoffreItem");
			if (it == null || m_interactPlayer != player || !player.Inventory.RemoveCountFromStack(it, 1))
			{
                if (m_interactPlayer == player)
                {
                    m_interactPlayer = null;
                    m_lastInteract = DateTime.MinValue;
                }
			    player.Out.SendMessage("Un problème est survenue.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Other, it.Template);
			InteractEnd(player);
		}

		private bool InteractEnd(GamePlayer player)
		{
			if (!IsTeleporter)
			{
				CoffreItem coffre = GetRandomItem();
				if (coffre.Id_nb == "" && coffre.Chance == 0)
					player.Out.SendMessage("Vous ne trouvez rien d'intéressant.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				else
				{
					ItemTemplate item = GameServer.Database.SelectObject<ItemTemplate>("Id_nb = '" + GameServer.Database.Escape(coffre.Id_nb) + "'");
					if (item == null)
					{
						player.Out.SendMessage("Vous ne trouvez rien d'intéressant. (Erreur de donnée, veuillez le signaliser à un GameMaster)", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						coffre.Chance = 0;
					}
					else
					{
						if (player.Inventory.AddTemplate(GameInventoryItem.Create(item), 1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
						{
							player.Out.SendMessage("Vous récupérez un objet!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							//GameServer.Instance.LogTradeAction("[COFFRE] "+Name+" ("+ToString()+") -> " + player.Name + " (" + player.Client.Account.Name + "): [ITEM] 1 '" + item.Id_nb + "' (" + item.ObjectId + ")", 2);
							InventoryLogging.LogInventoryAction(this, player, eInventoryActionType.Loot, item);
						}
						else
							player.Out.SendMessage("Vous récupérez un objet mais votre sac-à-dos est plein.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

						HandlePopMob();
					}
				}
			}
			else
			{
				bool failed = false;
				if (TpLevelRequirement > 0 && player.Level < TpLevelRequirement)
				{
					failed = true;					
				}
				
				if (TpIsRenaissance && !player.IsRenaissance)
				{					
					failed = true;					
				}

				if (failed)
				{
					player.Out.SendMessage("Ce coffre ne parvient pas à vous téléporter. Vous devriez revenir plus tard.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				}
				else
				{
					HandleTeleporter(player);
				}
			}
		
            m_interactPlayer = null;
            m_lastInteract = DateTime.MinValue;
            LastOpen = DateTime.Now;
			RemoveFromWorld(ItemInterval * 60);
			RespawnTimer.Start();

			SaveIntoDatabase();
			return true;
		}

		private void HandleTeleporter(GamePlayer player)
		{
			if (TpX > 0 && TpY > 0 && TpZ > 0)
			{
				if (TpEffect > 0)
				{
					//handle effect
					player.Out.SendSpellCastAnimation(player, (ushort)TpEffect, 20);
				}

				RegionTimer TimerTL = new RegionTimer(this, Teleportation);
				TimerTL.Properties.setProperty("TP", new GameLocation("Coffre Location", (ushort)TpRegion, TpX, TpY, TpZ, player.Heading));
				TimerTL.Properties.setProperty("player", player);
				TimerTL.Start(3000);

				foreach (GamePlayer players in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{			
					players.Out.SendEmoteAnimation(player, eEmote.Bind);
				}
			}
		}

		private int Teleportation(RegionTimer timer)
		{
			GameLocation pos = timer.Properties.getProperty<GameLocation>("TP", null);
			GamePlayer player = timer.Properties.getProperty<GamePlayer>("player", null);

			player.MoveTo(pos.RegionID, pos.X, pos.Y, pos.Z, pos.Heading);		
			player.Bind(true);

			return 0;
		}

		private void HandlePopMob()
		{
			if (TrapRate > 0 && NpctemplateId != null)
			{
				var rand = new Random(DateTime.Now.Millisecond);
				if (rand.Next(1,TrapRate + 1) <= TrapRate)
				{
					var template = GameServer.Database.FindObjectByKey<DBNpcTemplate>(NpctemplateId);
					if (template != null)
					{
						var mob = new AmteMob(new NpcTemplate(template))
						{
							X = X,
							Y = Y,
							Z = Z,						
							Heading = Heading,
							CurrentRegionID = CurrentRegionID,
							CurrentRegion = CurrentRegion,
							Size = 50,
							Name = "Gardien du Coffre"
						};

						mob.AddToWorld();						
					}
				}				
			}
		}

		private void Repop_Elapsed(object sender, ElapsedEventArgs e)
		{
			AddToWorld();
		}
		#endregion

		#region Serrure
		private void Unlock(GamePlayer player, byte response)
		{
            if (response == 0x00)
            {
                m_interactPlayer = null;
                return;
            }

			RegionTimer timer = new RegionTimer(player, UnlockCallback);
			timer.Properties.setProperty("X", player.X);
			timer.Properties.setProperty("Y", player.Y);
			timer.Properties.setProperty("Z", player.Z);
			timer.Properties.setProperty("Head", (int)player.Heading);
			timer.Properties.setProperty("player", player);
			timer.Start(500);
			player.Out.SendTimerWindow("Crochetage", UNLOCK_TIME);
		}

		private int UnlockCallback(RegionTimer timer)
		{
            m_interactPlayer = null;
			GamePlayer player = timer.Properties.getProperty<GamePlayer>("player", null);
			int Xpos = timer.Properties.getProperty("X",0);
			int Ypos = timer.Properties.getProperty("Y",0);
			int Zpos = timer.Properties.getProperty("Z",0);
			int Head = timer.Properties.getProperty("Head",0);
			if (player == null)
				return 0;
			if (Xpos != player.X || Ypos != player.Y || Zpos != player.Z || Head != player.Heading || player.InCombat)
			{
				player.Out.SendCloseTimerWindow();
				return 0;
			}

			int time = timer.Properties.getProperty("time", 0)+500;
			timer.Properties.setProperty("time", time);
			if (time < UNLOCK_TIME*1000)
				return 500;
			player.Out.SendCloseTimerWindow();

			int Chance = 100-LockDifficult;

			//Dexterité
			float dextChance = (float)(player.Dexterity)/125;
			if (dextChance > 1.0f)
				dextChance = 1.0f;
			if (dextChance < 0.1f)
				dextChance = 0.1f;
			Chance = (int)(dextChance*Chance); 

			//Races
			switch (player.RaceName)
			{
				case "Half Ogre":
				case "Troll":
					Chance -= 2;
					break;

				case "Highlander":
				case "Firbolg":
				case "Dwarf":
				case "Norseman":
					Chance -= 1;
					break;

				case "Elf":
					Chance += 1;
					break;
			}

			//Classes
			switch (player.CharacterClass.ID)
			{
				case (int)eCharacterClass.AlbionRogue:
				case (int)eCharacterClass.Stalker:	
				case (int)eCharacterClass.MidgardRogue:
					Chance += 1;
					break;

				case (int)eCharacterClass.Infiltrator:
				case (int)eCharacterClass.Minstrel:
				case (int)eCharacterClass.Scout:
				case (int)eCharacterClass.Hunter:
				case (int)eCharacterClass.Shadowblade:
				case (int)eCharacterClass.Nightshade:
				case (int)eCharacterClass.Ranger:
					Chance += 2;
					break;
			}

			if (Chance >= 100)
				Chance = 99;

			if (player.Client.Account.PrivLevel >= 2)
				player.Out.SendMessage("Chance de votre personnage: "+Chance+"/100", eChatType.CT_Important, eChatLoc.CL_SystemWindow);

			if (Chance > 0 && Util.Chance(Chance))
			{
				player.Out.SendMessage("Vous crochetez le coffre avec succès !", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				InteractEnd(player);
			}
			else
			{
				player.Out.SendMessage("Vous n'avez pas réussi à crocheter le coffre et vous cassez un crochet !", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
				player.Inventory.RemoveTemplate(CROCHET, 1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
			}
			return 0;
		}
		#endregion

		#region Item aléatoire
		private CoffreItem GetRandomItem()
		{
			if(!Util.Chance(ItemChance))
				return new CoffreItem("",0);

			int num = Util.Random(1, AllChance);
			int i = 0;
			foreach(CoffreItem item in m_Items)
			{
				i += item.Chance;
				if(i >= num)
					return item;
			}
			return new CoffreItem("",0);
		}
		#endregion

		#region Gestion des items
		/// <summary>
		/// Ajoute ou modifie la chance d'apparition d'un item
		/// </summary>
		/// <param name="Id_nb">Id_nb de l'item à modifier ou ajouter</param>
		/// <param name="chance">Nombre de chance d'apparition de l'item</param>
		/// <returns>Retourne false si l'item n'existe pas dans la base de donné ItemTemplate</returns>
		public bool ModifyItemList(string Id_nb, int chance)
		{
			ItemTemplate item = GameServer.Database.SelectObject<ItemTemplate>("Id_nb = '" + GameServer.Database.Escape(Id_nb) + "'");
			if(item == null) 
				return false;

			foreach(CoffreItem it in m_Items)
			{
				if(it.Id_nb == Id_nb)
				{
					it.Chance = chance;
					return true;
				}
			}

			m_Items.Add(new CoffreItem(Id_nb, chance));
			return true;
		}

		/// <summary>
		/// Supprime un item de la liste des items
		/// </summary>
		/// <param name="Id_nb">item à supprimer</param>
		/// <returns>Retourne true si l'item est supprimé</returns>
		public bool DeleteItemFromItemList(string Id_nb)
		{
			foreach(CoffreItem item in m_Items)
			{
				if(item.Id_nb == Id_nb)
				{
					m_Items.Remove(item);
					return true;
				}
			}
			return false;
		}

		#endregion

		#region Database
        public override void LoadFromDatabase(DataObject obj)
        {
            DBCoffre coffre = obj as DBCoffre;
            if (coffre == null) return;
            Name = coffre.Name;
            X = coffre.X;
            Y = coffre.Y;
            Z = coffre.Z;
            Heading = (ushort) (coffre.Heading & 0xFFF);
            CurrentRegionID = coffre.Region;
            Model = coffre.Model;
            LastOpen = coffre.LastOpen;
            ItemInterval = coffre.ItemInterval;
            InternalID = coffre.ObjectId;
            ItemChance = coffre.ItemChance;
            KeyItem = coffre.KeyItem;
            LockDifficult = coffre.LockDifficult;
            Coffre = coffre;
			TrapRate = coffre.TrapRate;
			NpctemplateId = coffre.NpctemplateId;
			TpX = coffre.TpX;
			TpY = coffre.TpY;
			TpZ = coffre.TpZ;
			TpLevelRequirement = coffre.TpLevelRequirement;
			TpIsRenaissance = coffre.TpIsRenaissance;
			IsTeleporter = coffre.IsTeleporter;
			TpEffect = coffre.TpEffect;
			TpRegion = coffre.TpRegion;

			double interval = (double)ItemInterval * 60D * 1000D;
			if (interval > int.MaxValue)
			{
				interval = (double)int.MaxValue;
			}
			RespawnTimer = new Timer(interval);
			RespawnTimer.Elapsed += Repop_Elapsed;

			m_Items = new List<CoffreItem>();
            if (coffre.ItemList != "")
                foreach (string item in coffre.ItemList.Split(';'))
                    m_Items.Add(new CoffreItem(item));
        }

	    public override void SaveIntoDatabase()
		{
			if(Coffre == null)
				Coffre = new DBCoffre();

			Coffre.Name = Name;
			Coffre.X = X;
			Coffre.Y = Y;
			Coffre.Z = Z;
			Coffre.Heading = Heading;
			Coffre.Region = CurrentRegionID;
			Coffre.Model = Model;
			Coffre.LastOpen = LastOpen;
			Coffre.ItemInterval = ItemInterval;
			Coffre.ItemChance = ItemChance;
			Coffre.KeyItem = KeyItem;
			Coffre.LockDifficult = LockDifficult;
			Coffre.TrapRate = TrapRate;
			Coffre.NpctemplateId = NpctemplateId;
			Coffre.TpX = TpX;
			Coffre.TpY = TpY;
			Coffre.TpZ = TpZ;
			Coffre.TpLevelRequirement = TpLevelRequirement;
			Coffre.TpIsRenaissance = TpIsRenaissance;
			Coffre.IsTeleporter = IsTeleporter;
			Coffre.TpEffect = TpEffect;
			Coffre.TpRegion = TpRegion;

			if (Items != null)
			{
				string list = "";
				foreach(CoffreItem item in m_Items)
				{
					if(list.Length > 0)
						list += ";";
					list += item.Id_nb + "|" + item.Chance;
				}
				Coffre.ItemList = list;
			}

			if(InternalID == null)
			{
				GameServer.Database.AddObject(Coffre);
				InternalID = Coffre.ObjectId;
			}
			else
				GameServer.Database.SaveObject(Coffre);
		}

		public override void DeleteFromDatabase()
		{
			if(Coffre == null)
				return;
			GameServer.Database.DeleteObject(Coffre);
		}
		#endregion

		#region CoffreItem
		public class CoffreItem
		{
			public string Id_nb;
			public int Chance;

			public CoffreItem (string id_nb, int chance)
			{
				Id_nb = id_nb;
				Chance = chance;
			}

			public CoffreItem (string item)
			{
				string[] values = item.Split('|');
				if(values.Length < 2)
					throw new Exception("Pas de caractère séparateur pour l'item \""+item+"\"");
				Id_nb = values[0];
				try
				{
					Chance = int.Parse(values[1]);
				}
				catch
				{
					Chance = 0;
				}
			}
		}
		#endregion

		public IList<string> DelveInfo()
		{
			List<string> text = new List<string>
				{
					" + OID: " + ObjectID,
					" + Class: " + GetType(),
					" + Position: X=" + X + " Y=" + Y + " Z=" + Z + " Heading=" + Heading,
					" + Realm: " + Realm,
					" + Model: " + Model,
					"",
					"-- Coffre --",
					" + Chance d'apparition d'un item: " + ItemChance + "%",
					" + Interval d'apparition d'un item: " + ItemInterval + " minutes",
					" + Dernière fois que le coffre a été ouvert: " + LastOpen.ToShortDateString() + " " + LastOpen.ToShortTimeString()
				};
			if (LockDifficult > 0)
				text.Add(" + Difficulté pour crocheter le coffre: " + LockDifficult + "%");
			else
				text.Add(" + Ce coffre ne peut pas être crocheté");
			if (KeyItem != "")
				text.Add(" + Id_nb de la clef: " + KeyItem);
			else
				text.Add(" + Le coffre n'a pas besoin de clef");
			text.Add("");
			text.Add(" + Listes des items (" + Items.Count + " items):");
			int i = 0;
			int TotalChance = 0;
			foreach (CoffreItem item in Items)
			{
				i++;
				TotalChance += item.Chance;
				text.Add("  " + i + ". " + item.Id_nb + " - " + item.Chance);
			}
			text.Add("Total des chances: " + TotalChance);
			return text;
		}
	}
}
