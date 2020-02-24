using DOL;
using DOL.GS;
using System;
using DOL.Database;
using System.Timers;
using System.Collections;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
	[NPCGuildScript("Douanier")]
    public class Douanier : Scripts.TeleportNPC
	{
		private long Price1 = Money.GetMoney(0,0,50,0,0);//M/P/G/S/C Amount of gold used for RemoveMoney

		public Douanier()
		{
			LoadedFromScript = false;
		}
		
		public override bool Interact(GamePlayer player)
		{
			if (!base.Interact(player))
				return false;

            player.Out.SendMessage("Et toi ? Tu choisis quoi ? Tu veux ramasser tout mon clan dans ta face ou [payer 50 pièces d'or pour les éviter] ?", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
				
			return true;
		}
        private void SendReply(GamePlayer target, string msg)
        {
            target.Client.Out.SendMessage(
               msg,
               eChatType.CT_Say, eChatLoc.CL_PopupWindow);

        }
		public override bool WhisperReceive(GameLiving source, string str)
		{
			if (!(source is GamePlayer player))
				return false;	

			switch (str)
			{
                case "payer 50 pièces d'or pour les éviter":
					if (player.GetCurrentMoney()<Price1)
					{
						foreach(GamePlayer emoteplayer in this.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						emoteplayer.Out.SendEmoteAnimation(this, eEmote.Laugh);
						Say("Tchakkkk, tu passe pas ! t'es fauché !");
						break;
					}
					else
					{
						if (m_Occupe)
						{
							player.Out.SendMessage("Je suis occupé pour le moment.", eChatType.CT_System, eChatLoc.CL_PopupWindow);
							return true;
						}

						player.RemoveMoney(Price1, null);
						player.Out.SendMessage("Vous donnez "+Money.GetString(Price1)+" au douanier.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						Say("Bien, tu peux passer toi !");	
						
						//TeleportClass tc = new TeleportClass(this, player, 393786,616663,9025,3501, 163);		

						RegionTimer TimerTL = new RegionTimer(this, Teleportation);
						TimerTL.Properties.setProperty("TP", new JumpPos("Drium Ligen",393786,616663,9025,3501,163));
						TimerTL.Properties.setProperty("player", player);
						TimerTL.Start(3000);
						foreach (GamePlayer players in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
						{
							players.Out.SendSpellCastAnimation(this, 1, 20);
							players.Out.SendEmoteAnimation(player, eEmote.Bind);
						}
						m_Occupe = true;
					}
					break;
			}
			return true;
		}
	}
}