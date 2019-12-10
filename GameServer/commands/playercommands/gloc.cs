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

namespace DOL.GS.Commands
{
    [Cmd(
        "&gloc", // command to handle
        ePrivLevel.Player, // minimum privelege level
        "Show the current local zone and global region coordinates", // command description
        "/gloc")] // command usage
    public class GlocCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "gloc"))
            {
                return;
            }

            double degHeading = (client.Player.Heading + 2048) / 11.38;
            // same data sent by client built in /loc command
            DisplayMessage(client, string.Format("{0}: loc={1},{2},{3} dir={4}",
                client.Player.CurrentZone.Description, client.Player.X - client.Player.CurrentZone.XOffset, client.Player.Y - client.Player.CurrentZone.YOffset, client.Player.Z, (int)degHeading > 359 ? (int)degHeading - 360 : (int)degHeading));
            // global location in a region
            DisplayMessage(client, string.Format("Global Location is X:{0} Y:{1} Z:{2} Heading:{3} Region:{4} Zone:{5}",
				client.Player.X, client.Player.Y, client.Player.Z, client.Player.Heading, client.Player.CurrentRegionID, client.Player.CurrentZone.ID));
		}
	}
}
