﻿using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class RenaissanceNPC
        : GameNPC
    {
        private readonly string RENAISSANCE_ITEM_ID = "pierre_philosophale";

        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
            {
                return false;
            }

            TurnTo(player, 5000);
            
            if (player.IsRenaissance)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RenaissanceNPC.DeniedRenaissance"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            if (player.Level >= 40)
            {                
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RenaissanceNPC.AskForRenaissance"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
            }        

            return true;
        }

        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            var player = source as GamePlayer;

            if(item == null || player == null || player.IsRenaissance)
            {
                return base.ReceiveItem(source, item);
            }


            if (item.Id_nb.Equals(RENAISSANCE_ITEM_ID))
            {
                player.ApplyRenaissance();
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "RenaissanceNPC.RenaissanceDone"), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                
                return true;
            }

            return base.ReceiveItem(source, item);  
        }

        public override eQuestIndicator GetQuestIndicator(GamePlayer player)
        {
            if (player.Level >= 40 && !player.IsRenaissance)
            {
                return eQuestIndicator.Lore;
            }

            return base.GetQuestIndicator(player);
        }
    }
}
