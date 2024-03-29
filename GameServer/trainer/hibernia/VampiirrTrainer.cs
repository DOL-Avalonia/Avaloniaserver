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
    /// Vampiir Trainer
    /// </summary>
    [NPCGuildScript("Vampiir Trainer", eRealm.Hibernia)] // this attribute instructs DOL to use this script for all "Vampiir Trainer" NPC's in Albion (multiple guilds are possible for one script)
    public class VampiirTrainer : GameTrainer
    {
        public override eCharacterClass TrainedClass => eCharacterClass.Vampiir;

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
                player.Out.SendMessage(Name + " says, \"Do you wish to learn some more, " + player.Name + "? Step up and receive your training!\"", eChatType.CT_Say, eChatLoc.CL_ChatWindow);
            }
            else
            {
                // perhaps player can be promoted
                if (CanPromotePlayer(player))
                {
                    player.Out.SendMessage(Name + " says, \"" + player.Name + ", do you choose the Path of Affinity, and life as a [Vampiir]?\"", eChatType.CT_System, eChatLoc.CL_PopupWindow);
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

            switch (text)
            {
                case "Vampiir":
                    // promote player to other class
                    if (CanPromotePlayer(player))
                    {
                        PromotePlayer(player, (int)eCharacterClass.Vampiir, "Very well, " + source.GetName(0, false) + ". I gladly take your training into my hands. Congratulations, from this day forth, you are a Vampiir. Here, take this gift to aid you.", null);
                        foreach (GamePlayer plr in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE(player.CurrentRegion))) // inform nearest clients about this player now is vampire (can fly)
                        {
                            plr?.Out.SendVampireEffect(player, true);
                        }
                    }

                    break;
            }

            return true;
        }
    }
}
