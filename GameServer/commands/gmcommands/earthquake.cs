﻿using System;
using DOL.GS.PacketHandler;
using DOL.GS.Commands;


namespace DOL.GS.Scripts
{
    [CmdAttribute(
   "&earthquake",
   ePrivLevel.GM,
   "earthquake [radius] [intensity] [duration] [delay]",
   "/earthquake")]
    public class EarthQuakeCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (client == null || client.Player == null || client.ClientState != DOL.GS.GameClient.eClientState.Playing) return;
          
            uint unk1 = 0;
            float radius, intensity, duration, delay = 0;
            radius = 1200.0f;
            intensity = 50.0f;
            duration = 1000.0f;
            int x, y, z = 0;
            if (client.Player.GroundTarget == null)
            {
                x = client.Player.X;
                y = client.Player.Y;
                //            z = client.Player.Z;
            }
            else
            {
                x = client.Player.GroundTarget.X;
                y = client.Player.GroundTarget.Y;
                z = client.Player.GroundTarget.Z;
            }
            if (args.Length > 1)
            {
                try
                {
                    unk1 = (uint)Convert.ToSingle(args[1]);
                }
                catch { }
            }
            if (args.Length > 2)
            {
                try
                {
                    radius = (float)Convert.ToSingle(args[2]);
                }
                catch { }
            }
            if (args.Length > 3)
            {
                try
                {
                    intensity = (float)Convert.ToSingle(args[3]);
                }
                catch { }
            }
            if (args.Length > 4)
            {
                try
                {
                    duration = (float)Convert.ToSingle(args[4]);
                }
                catch { }
            }
            if (args.Length > 5)
            {
                try
                {
                    delay = (float)Convert.ToSingle(args[5]);
                }
                catch { }
            }
            GSTCPPacketOut pak = new GSTCPPacketOut(0x47);
            pak.WriteIntLowEndian(unk1);
            pak.WriteIntLowEndian((uint)x);
            pak.WriteIntLowEndian((uint)y);
            pak.WriteIntLowEndian((uint)z);
            pak.Write(BitConverter.GetBytes(radius), 0, sizeof(System.Single));
            pak.Write(BitConverter.GetBytes(intensity), 0, sizeof(System.Single));
            pak.Write(BitConverter.GetBytes(duration), 0, sizeof(System.Single));
            pak.Write(BitConverter.GetBytes(delay), 0, sizeof(System.Single));
            client.Out.SendTCP(pak);

            foreach (GamePlayer player in client.Player.GetPlayersInRadius((ushort)radius))
            {
                if (player == client.Player)
                    continue;
                GSTCPPacketOut pakBis = new GSTCPPacketOut(0x47);
                pakBis.WriteIntLowEndian(unk1);
                pakBis.WriteIntLowEndian((uint)x);
                pakBis.WriteIntLowEndian((uint)y);
                pakBis.WriteIntLowEndian((uint)z);
                pakBis.Write(BitConverter.GetBytes(radius), 0, sizeof(System.Single));
                int distance = player.GetDistance(client.Player);
                float newIntensity = intensity * (1 - distance / radius);
                pakBis.Write(BitConverter.GetBytes(newIntensity), 0, sizeof(System.Single));
                pakBis.Write(BitConverter.GetBytes(duration), 0, sizeof(System.Single));
                pakBis.Write(BitConverter.GetBytes(delay), 0, sizeof(System.Single));
                player.Out.SendTCP(pakBis);
            }
            
            return;
        }
    }
}
