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
using DOL.GS.PacketHandler;

namespace DOL.GS.Trainer
{
    /// <summary>
    /// Sorcerer Trainer
    /// </summary>
    [NPCGuildScript("Sorcerer Trainer", eRealm.Albion)] // this attribute instructs DOL to use this script for all "Sorcerer Trainer" NPC's in Albion (multiple guilds are possible for one script)
    public class SorcererTrainer : GameTrainer
    {
        public override eCharacterClass TrainedClass => eCharacterClass.Sorcerer;

        private const string WeaponId = "sorcerer_item";

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
                OfferTraining(player);
            }
            else
            {
                // perhaps player can be promoted
                if (CanPromotePlayer(player))
                {
                    player.Out.SendMessage(Name + " says, \"Is it your wish to [join the Academy] and lend us your power as a Sorcerer?\"",eChatType.CT_Say,eChatLoc.CL_PopupWindow);
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

            switch (text) {
                case "join the Academy":
                    // promote player to other class
                    if (CanPromotePlayer(player)) {
                        PromotePlayer(player, (int)eCharacterClass.Sorcerer, "You are now part of our shadow! You shall forever have a place among us! Here too is your guild weapon, a Staff of Focus!", null);
                        player.ReceiveItem(this,WeaponId);
                    }

                    break;
            }

            return true;
        }
    }
}
