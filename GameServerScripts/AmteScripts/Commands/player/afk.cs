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

using DOL.GS.SkillHandler;

namespace DOL.GS.Commands
{
    [Cmd(
        "&afk",
        ePrivLevel.Player,
         "Activer ou désactiver l'absence. Se désactive au déplacement du personnage.",
          "/afk <text> (AFk par défaut)")]
    public class AFKCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if ((client.Player.PlayerAfkMessage != null) && args.Length == 1)
            {
                client.Player.PlayerAfkMessage = null;
                client.Player.DisableSkill(SkillBase.GetAbility(Abilities.Vol),
                    VolAbilityHandler.DISABLE_DURATION);
            }
            else
            {
                if (args.Length > 1)
                {
                    client.Player.PlayerAfkMessage = string.Join(" ",
                        args, 1, args.Length - 1);
                    client.Out.SendMessage("Vous etes désormais afk avec le message: " + client.Player.PlayerAfkMessage, PacketHandler.eChatType.CT_Chat, PacketHandler.eChatLoc.CL_SystemWindow);
                }
                else
                {
                    client.Player.PlayerAfkMessage = "AFK";
                    client.Out.SendMessage("Vous êtes en mode AFK, déplacez-vous à nouveau pour le désactiver.", PacketHandler.eChatType.CT_Chat, PacketHandler.eChatLoc.CL_SystemWindow);
                }
            }
        }
    }
}
