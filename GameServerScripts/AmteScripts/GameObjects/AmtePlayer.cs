using System;
using AmteScripts.Managers;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;

namespace DOL.GS
{
	public class AmtePlayer : GamePlayer
	{
		public DateTime LastDeath = DateTime.MinValue;
		public ItemTemplate HeadTemplate;

		public DateTime LastActivity = DateTime.Now;
		public Point3D LastPosition = new Point3D(0, 0, 0);

		public AmtePlayer(GameClient client, DOLCharacters dbChar) : base(client, dbChar) 
		{
			HeadTemplate = GameServer.Database.FindObjectByKey<ItemTemplate>("head_blacklist") ?? new ItemTemplate();
            if(RvrManager.WinnerRealm == Realm)
            {
                BaseBuffBonusCategory[eProperty.MythicalCoin] += 5;
                BaseBuffBonusCategory[eProperty.XpPoints] += 10;
                BaseBuffBonusCategory[eProperty.RealmPoints] += 5;
            }
		}

		public override int BountyPointsValue
		{
			get
			{
				if (RvrManager.Instance.IsInRvr(this))
					return (int)(1 + Level * 0.6);
				return 0;
			}
		}

        public override void Die(GameObject killer)
		{
			base.Die(killer);

			if (RvrManager.Instance.IsInRvr(this))
            {
                GamePlayer killerAsPlayer = killer as GamePlayer;
                if (killerAsPlayer != null && killerAsPlayer.IsInRvR)
                {
                    if (RvrManager.Instance.Kills.ContainsKey(killerAsPlayer))
                        RvrManager.Instance.Kills[killerAsPlayer]++;
                    else
                        RvrManager.Instance.Kills.Add(killerAsPlayer, 1);
                }
                return;
            }

			if (killer is AmtePlayer)
			{
				this.PlayerKilledByPlayer(this, (AmtePlayer)killer);
			}		
		}

		public void SendMessage(string message, eChatType type = eChatType.CT_System, eChatLoc loc = eChatLoc.CL_SystemWindow)
		{
			Out.SendMessage(message, type, loc);
		}

		#region DB
		public override void LoadFromDatabase(DataObject obj)
		{
			base.LoadFromDatabase(obj);
		}

		public override void SaveIntoDatabase()
		{
			base.SaveIntoDatabase();
		}

		public void PlayerKilledByPlayer(AmtePlayer victim, AmtePlayer killer)
		{
			if (victim == killer)
				return;
			//old system
			//var inBL = IsBlacklisted(victim);

			if (victim.isInBG || victim.IsInPvP || victim.IsInRvR || Territory.TerritoryManager.Instance.IsTerritoryArea(victim.CurrentAreas))
			{
				return;
			}

			if (victim.Reputation < 0)
			{
				ItemUnique iu = new ItemUnique(HeadTemplate)
				{
					Name = "T�te de " + victim.Name,
					MessageArticle = victim.InternalID,
					CanDropAsLoot = true,
					MaxCondition = (int)DateTime.Now.Subtract(new DateTime(2000, 1, 1)).TotalSeconds
				};
				GameServer.Database.AddObject(iu);
				if (killer.Inventory.AddItem(eInventorySlot.FirstEmptyBackpack, GameInventoryItem.Create(iu)))
					killer.SendMessage("Vous avez r�cup�rer la t�te de " + victim.Name + ".", eChatType.CT_Loot);
				else
					killer.SendMessage("Vous n'avez pas pu r�cup�rer la t�te de " + victim.Name + ", votre inventaire est plein !", eChatType.CT_Loot);
			}
		}

		public override void DeleteFromDatabase()
		{
			base.DeleteFromDatabase();
		}
		#endregion

		public override void CraftItem(ushort itemID)
		{
			if (JailMgr.IsPrisoner(this))
				return;

			LastActivity = DateTime.Now;
			base.CraftItem(itemID);
		}

		public override void UseSlot(int slot, int type)
		{
			if (JailMgr.IsPrisoner(this))
				return;
			base.UseSlot(slot, type);
		}
	}
}
