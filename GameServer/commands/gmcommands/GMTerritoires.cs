using DOL.Database;
using DOL.GS;
using DOL.GS.Commands;
using DOL.GS.PacketHandler;
using DOL.MobGroups;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.commands.gmcommands
{
    [CmdAttribute(
        "&GMTerritoires",
        ePrivLevel.GM,
        "GM Territoires Cmd",
        "'/GMTerritoires add <areaId> <zoneId> <name> <groupId>' ajoute un territoire en spécifant l'<areaId>, la <zoneId>, le <name> et le <groupId> à utiliser et pour ce territoire")]
    public class GMTerritoires
        : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 6)
            {
                DisplaySyntax(client);
                return;
            }

            string areaId = null;

            switch (args[1].ToLowerInvariant())
            {

                case "add":
                    areaId = args[2];
                    ushort zoneId = 0;
                    string name = args[4];
                    string groupId = args[5];

                    if (string.IsNullOrEmpty(areaId) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(groupId))
                    {
                        DisplaySyntax(client);
                        break;
                    }

                    if (ushort.TryParse(args[3], out zoneId))
                    {
                        var areaDb = GameServer.Database.SelectObjects<DBArea>("area_Id = @id", new QueryParameter("id", areaId))?.FirstOrDefault();

                        if (areaDb == null)
                        {
                            client.Out.SendMessage("Impossible de trouver l'area avec l'id : " + areaId, eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        if (!WorldMgr.Regions.ContainsKey(areaDb.Region))
                        {
                            client.Out.SendMessage("Impossible de trouver la region : " + areaDb.Region , eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        var zone = WorldMgr.Regions[areaDb.Region].Zones.FirstOrDefault(z => z.ID.Equals(zoneId));

                        if (zone == null)
                        {
                            client.Out.SendMessage("Impossible de trouver la zone : " + zoneId + " dans la region " + areaDb.Region, eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        if (MobGroupManager.Instance.Groups.ContainsKey(groupId))
                        {
                            client.Out.SendMessage("Impossible de trouver le groupeId : " + groupId, eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        var territory = new TerritoryDb()
                        {
                            AreaId = areaId,
                            AreaX = areaDb.X,
                            AreaY = areaDb.Y,
                            Name = name,
                            RegionId = areaDb.Region,
                            ZoneId = zoneId,
                            GroupId = groupId
                        };

                        if (!GameServer.Database.AddObject(territory))
                        {
                            client.Out.SendMessage("Le Territoire " + name + " n'a pas pu etre sauvegardé dans la base de données.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                            break;
                        }

                        client.Out.SendMessage("Le Territoire " + name + " a été créé correctement.", eChatType.CT_System, eChatLoc.CL_ChatWindow);
                    }

                    break;

                default:
                    DisplaySyntax(client);
                    break;
            }
        }
    }
}
