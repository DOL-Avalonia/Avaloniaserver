/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Trainer
{
    /// <summary>
    /// Mauler Trainer
    /// </summary>
    [NPCGuildScript("Mauler Trainer", eRealm.Hibernia)]
    public class HiberniaMaulerTrainer : GameTrainer
    {
        public override eCharacterClass TrainedClass => eCharacterClass.MaulerHib;

        private const string WeaponId1 = "maulerhib_item_staff";
        private const string WeaponId2 = "maulerhib_item_fist";

        /// <summary>
        /// Interact with trainer
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public override bool Interact(GamePlayer player)
        {
            if (!base.Interact(player))
            {
                return false;
            }

            // check if class matches.
            if (player.CharacterClass.ID == (int)TrainedClass)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.Interact.Text2", Name), eChatType.CT_System, eChatLoc.CL_ChatWindow);
            }
            else
            {
                // perhaps player can be promoted
                if (CanPromotePlayer(player))
                {
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.Interact.Text1", Name), eChatType.CT_Say, eChatLoc.CL_PopupWindow);

                    if (!player.IsLevelRespecUsed)
                    {
                        OfferRespecialize(player);
                    }
                }
                else
                {
                    CheckChampionTraining(player);
                }
            }

            return true;
        }

        /// <summary>
        /// Talk to trainer
        /// </summary>
        /// <param name="source"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public override bool WhisperReceive(GameLiving source, string text)
        {
            if (!base.WhisperReceive(source, text))
            {
                return false;
            }

            if (!(source is GamePlayer player))
            {
                return false;
            }

            string lowerCase = text.ToLower();
            if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.WhisperReceiveCase.Text1"))
            {
                // promote player to other class
                if (CanPromotePlayer(player))
                {
                    // Mauler_Hib = 62
                    PromotePlayer(player, (int)eCharacterClass.MaulerHib, LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.WhisperReceive.Text1"), null);
                }
            }
            else if ((player.Inventory.GetFirstItemByID(WeaponId1, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) == null) &&
                     (player.Inventory.GetFirstItemByID(WeaponId2, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack) == null))
            {
                if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.WhisperReceiveCase.Text2"))
                {
                    player.ReceiveItem(this, WeaponId1);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.WhisperReceive.Text2"), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
                else if (lowerCase == LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.WhisperReceiveCase.Text3"))
                {
                    player.ReceiveItem(this, WeaponId2);
                    player.ReceiveItem(this, WeaponId2);
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "MaulerHibTrainer.WhisperReceive.Text2"), eChatType.CT_Say, eChatLoc.CL_PopupWindow);
                }
            }

            return true;
        }
    }
}
