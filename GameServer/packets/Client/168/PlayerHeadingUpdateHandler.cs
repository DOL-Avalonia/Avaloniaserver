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
using System.Reflection;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerHeadingUpdate, "Handles Player Heading Update (Short State)", eClientStatus.PlayerInGame)]
    public class PlayerHeadingUpdateHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            if (client == null || client.Player == null)
                return;

            if (client.Player.ObjectState != GameObject.eObjectState.Active) return;

            ushort sessionId = packet.ReadShort();
            if (client.SessionID != sessionId)
            {
                // GameServer.BanAccount(client, 120, "Hack sessionId", string.Format("Wrong sessionId:0x{0} in 0xBA packet (SessionID:{1})", sessionId, client.SessionID));
                return; // client hack
            }
            
            client.Player.Heading = packet.ReadShort();
            packet.Skip(1); // unknown
            int flags = packet.ReadByte();
            //			client.Player.PetInView = ((flags & 0x04) != 0); // TODO
            client.Player.GroundTargetInView = ((flags & 0x08) != 0);
            client.Player.TargetInView = ((flags & 0x10) != 0);
            packet.Skip(1);
            byte ridingFlag = (byte)packet.ReadByte();
			flags = 0; // reset flags, only some sent back out
            if (client.Player.IsWireframe)
            {
                flags |= 0x01;
            }
            
            if (client.Player.IsStealthed)
            {
                flags |= 0x02;
            }            
			if (client.Player.IsDiving)
            {
                flags |= 0x04;
            }
            if (client.Player.IsTorchLighted)
            {
                flags |= 0x80;
            }
			
            byte steedSlot = (byte)client.Player.SteedSeatPosition;
			
            GSUDPPacketOut outpak = new GSUDPPacketOut(client.Out.GetPacketCode(eServerPackets.PlayerHeading));
            outpak.WriteShort((ushort)client.SessionID);
            outpak.WriteShort(client.Player.Heading);
            outpak.WriteByte(steedSlot);            
            outpak.WriteByte((byte)flags);
            outpak.WriteByte(0);
            outpak.WriteByte(ridingFlag);
            outpak.WriteByte(client.Player.HealthPercent);
            outpak.WriteByte(client.Player.ManaPercent);
            outpak.WriteByte(client.Player.EndurancePercent);
            outpak.WriteByte(0); // null term?
            outpak.WritePacketLength();

            foreach (GamePlayer player in client.Player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE(client.Player.CurrentRegion)))
            {
                if (player != null && player != client.Player)
                {                   
                    player.Out.SendUDPRaw(outpak);
                }
            }
        }
    }
}
