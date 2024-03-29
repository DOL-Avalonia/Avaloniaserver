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

using DOL.Events;
using DOL.GS.Housing;
using DOL.GS.Keeps;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandler(PacketHandlerType.TCP, eClientPackets.DialogResponse, "Response Packet from a Question Dialog", eClientStatus.PlayerInGame)]
    public class DialogResponseHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            ushort data1 = packet.ReadShort();
            ushort data2 = packet.ReadShort();
            ushort data3 = packet.ReadShort();
            var messageType = (byte)packet.ReadByte();
            var response = (byte)packet.ReadByte();

            new DialogBoxResponseAction(client.Player, data1, data2, data3, messageType, response).Start(1);
        }

        /// <summary>
        /// Handles dialog responses from players
        /// </summary>
        protected class DialogBoxResponseAction : RegionAction
        {
            /// <summary>
            /// The general data field
            /// </summary>
            private readonly int _data1;

            /// <summary>
            /// The general data field
            /// </summary>
            private readonly int _data2;

            /// <summary>
            /// The general data field
            /// </summary>
            private readonly int _data3;

            /// <summary>
            /// The dialog type
            /// </summary>
            private readonly int _messageType;

            /// <summary>
            /// The players response
            /// </summary>
            private readonly byte _response;

            /// <summary>
            /// Constructs a new DialogBoxResponseAction
            /// </summary>
            /// <param name="actionSource">The responding player</param>
            /// <param name="data1">The general data field</param>
            /// <param name="data2">The general data field</param>
            /// <param name="data3">The general data field</param>
            /// <param name="messageType">The dialog type</param>
            /// <param name="response">The players response</param>
            public DialogBoxResponseAction(GamePlayer actionSource, int data1, int data2, int data3, int messageType, byte response)
                : base(actionSource)
            {
                _data1 = data1;
                _data2 = data2;
                _data3 = data3;
                _messageType = messageType;
                _response = response;
            }

            /// <summary>
            /// Called on every timer tick
            /// </summary>
            protected override void OnTick()
            {
                var player = (GamePlayer)m_actionSource;

                if (player == null)
                {
                    return;
                }
                
                switch ((eDialogCode)_messageType)
                {
                    case eDialogCode.CustomDialog:
                        {
                            if (_data2 == 0x01)
                            {
                                CustomDialogResponse callback;
                                lock (player)
                                {
                                    callback = player.CustomDialogCallback;
                                    player.CustomDialogCallback = null;
                                }

                                if (callback == null)
                                {
                                    return;
                                }

                                callback(player, _response);
                            }

                            break;
                        }

                    case eDialogCode.GuildInvite:
                        {
                            var guildLeader = WorldMgr.GetObjectByIDFromRegion(player.CurrentRegionID, (ushort)_data1) as GamePlayer;
                            if (_response == 0x01) // accept
                            {
                                if (guildLeader == null)
                                {
                                    player.Out.SendMessage("You need to be in the same region as the guild leader to accept an invitation.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (player.Guild != null)
                                {
                                    player.Out.SendMessage("You are still in a guild, you'll have to leave it first.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (guildLeader.Guild != null)
                                {
                                    guildLeader.Guild.AddPlayer(player);
                                    // Need refresh the social window
                                    guildLeader.Guild.UpdateMember(player);
                                    return;
                                }

                                player.Out.SendMessage("Player doing the invite is not in a guild!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                return;
                            }

                            guildLeader?.Out.SendMessage(player.Name + " declined your invite.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                            return;
                        }

                    case eDialogCode.GuildLeave:
                        {
                            if (_response == 0x01) // accepte
                            {
                                if (player.Guild == null)
                                {
                                    player.Out.SendMessage("You are not in a guild.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                // Need clear social interface when the player leave the guild
                                string mes = "I,";
                                mes += ','; // Guild Level
                                mes += ','; // Guild Bank money
                                mes += ','; // Guild Dues enable/disable
                                mes += ','; // Guild Bounty
                                mes += ','; // Guild Experience
                                mes += ','; // Guild Merit Points
                                mes += ','; // Guild houseLot ?
                                mes += ','; // online Guild member ?
                                mes += ','; //"Banner available for purchase", "Missing banner buying permissions"
                                mes += ","; // Guild Motd
                                mes += ","; // Guild oMotd
                                player.Out.SendMessage(mes, eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);

                                player.Guild.RemovePlayer(player.Name, player);

                                // clear member list
                                string[] buffer = new string[10];

                                player.Out.SendMessage("TE," + 0 + "," + 0 + "," + 0, eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);

                                foreach (string member in buffer)
                                    player.Out.SendMessage(member, eChatType.CT_SocialInterface, eChatLoc.CL_SystemWindow);
                            }
                            else
                            {
                                player.Out.SendMessage("You decline to quit your guild.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            }

                            break;
                        }

                    case eDialogCode.QuestSubscribe:
                        {
                            var questNpc = (GameLiving)WorldMgr.GetObjectByIDFromRegion(player.CurrentRegionID, (ushort)_data2);
                            if (questNpc == null)
                            {
                                return;
                            }

                            var args = new QuestEventArgs(questNpc, player, (ushort)_data1);
                            GamePlayerEvent action;
                            if (_response == 0x01) // accept
                            {
                                // TODO add quest to player
                                // Note: This is done withing quest code since we have to check requirements, etc for each quest individually
                                // i'm reusing the questsubscribe command for quest abort since its 99% the same, only different event dets fired
                                action = _data3 == 0x01 ? GamePlayerEvent.AbortQuest : GamePlayerEvent.AcceptQuest;
                                player.Notify(action, player, args);

                                return;
                            }

                            action = _data3 == 0x01 ? GamePlayerEvent.ContinueQuest : GamePlayerEvent.DeclineQuest;
                            player.Notify(action, player, args);

                            return;
                        }

                    case eDialogCode.GroupInvite:
                        {
                            if (_response == 0x01)
                            {
                                GameClient cln = WorldMgr.GetClientFromID(_data1);

                                GamePlayer groupLeader = cln?.Player;
                                if (groupLeader == null)
                                {
                                    return;
                                }

                                if (player.Group != null)
                                {
                                    player.Out.SendMessage("You are still in a group.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (!GameServer.ServerRules.IsAllowedToGroup(groupLeader, player, false))
                                {
                                    return;
                                }

                                if (player.InCombatPvE)
                                {
                                    player.Out.SendMessage("You can't join a group while in combat!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (groupLeader.Group != null)
                                {
                                    if (groupLeader.Group.Leader != groupLeader)
                                    {
                                        return;
                                    }

                                    if (groupLeader.Group.MemberCount >= ServerProperties.Properties.GROUP_MAX_MEMBER)
                                    {
                                        player.Out.SendMessage("The group is full.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                        return;
                                    }

                                    groupLeader.Group.AddMember(player);
                                    groupLeader.Group.UpdateGroupWindow();
                                    groupLeader.Group.SendGroupUpdates(player);
                                    GameEventMgr.Notify(GamePlayerEvent.AcceptGroup, player);
                                    return;
                                }

                                var group = new Group(groupLeader);
                                GroupMgr.AddGroup(group);

                                group.AddMember(groupLeader);
                                group.AddMember(player);
                                groupLeader.Group.UpdateGroupWindow();
                                group.SendGroupUpdates(groupLeader);
                                group.SendGroupUpdates(player);

                                GameEventMgr.Notify(GamePlayerEvent.AcceptGroup, player);
                            }

                            break;
                        }

                    case eDialogCode.KeepClaim:
                        {
                            if (_response == 0x01)
                            {
                                if (player.Guild == null)
                                {
                                    player.Out.SendMessage("You have to be a member of a guild, before you can use any of the commands!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                AbstractGameKeep keep = GameServer.KeepManager.GetKeepCloseToSpot(player.CurrentRegionID, player, WorldMgr.VISIBILITY_DISTANCE(player.CurrentRegion));
                                if (keep == null)
                                {
                                    player.Out.SendMessage("You have to be near the keep to claim it.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                    return;
                                }

                                if (keep.CheckForClaim(player))
                                {
                                    keep.Claim(player);
                                }
                            }

                            break;
                        }

                    case eDialogCode.HousePayRent:
                        {
                            if (_response == 0x00)
                            {
                                if (player.TempProperties.getProperty<long>(HousingConstants.MoneyForHouseRent, -1) != -1)
                                {
                                    player.TempProperties.removeProperty(HousingConstants.MoneyForHouseRent);
                                }

                                if (player.TempProperties.getProperty<long>(HousingConstants.BPsForHouseRent, -1) != -1)
                                {
                                    player.TempProperties.removeProperty(HousingConstants.BPsForHouseRent);
                                }

                                player.TempProperties.removeProperty(HousingConstants.HouseForHouseRent);

                                return;
                            }

                            var house = player.TempProperties.getProperty<House>(HousingConstants.HouseForHouseRent, null);
                            var moneyToAdd = player.TempProperties.getProperty<long>(HousingConstants.MoneyForHouseRent, -1);
                            var bpsToMoney = player.TempProperties.getProperty<long>(HousingConstants.BPsForHouseRent, -1);

                            if (moneyToAdd != -1)
                            {
                                // if we're giving money and already have some in the lockbox, make sure we don't
                                // take more than what would cover 4 weeks of rent.
                                if (moneyToAdd + house.KeptMoney > HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS)
                                {
                                    moneyToAdd = (HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS) - house.KeptMoney;
                                }

                                // take the money from the player
                                if (!player.RemoveMoney(moneyToAdd))
                                {
                                    return;
                                }

                                InventoryLogging.LogInventoryAction(player, $"(HOUSE;{house.HouseNumber})", eInventoryActionType.Other, moneyToAdd);

                                // add the money to the lockbox
                                house.KeptMoney += moneyToAdd;

                                // save the house and the player
                                house.SaveIntoDatabase();
                                player.SaveIntoDatabase();

                                // notify the player of what we took and how long they are prepaid for
                                player.Out.SendMessage($"You deposit {Money.GetString(moneyToAdd)} in the lockbox.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                player.Out.SendMessage($"The lockbox now has {Money.GetString(house.KeptMoney)} in it.  The weekly payment is {Money.GetString(HouseMgr.GetRentByModel(house.Model))}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                player.Out.SendMessage($"The house is now prepaid for the next {(house.KeptMoney / HouseMgr.GetRentByModel(house.Model))} payments.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                                // clean up
                                player.TempProperties.removeProperty(HousingConstants.MoneyForHouseRent);
                            }
                            else
                            {
                                if (bpsToMoney + house.KeptMoney > HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS)
                                {
                                    bpsToMoney = (HouseMgr.GetRentByModel(house.Model) * ServerProperties.Properties.RENT_LOCKBOX_PAYMENTS) - house.KeptMoney;
                                }

                                if (!player.RemoveBountyPoints(Money.GetGold(bpsToMoney)))
                                {
                                    return;
                                }

                                // add the bps to the lockbox
                                house.KeptMoney += bpsToMoney;

                                // save the house and the player
                                house.SaveIntoDatabase();
                                player.SaveIntoDatabase();

                                // notify the player of what we took and how long they are prepaid for
                                player.Out.SendMessage($"You deposit {Money.GetString(bpsToMoney)} in the lockbox.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                player.Out.SendMessage($"The lockbox now has {Money.GetString(house.KeptMoney)} in it.  The weekly payment is {Money.GetString(HouseMgr.GetRentByModel(house.Model))}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                                player.Out.SendMessage($"The house is now prepaid for the next {(house.KeptMoney / HouseMgr.GetRentByModel(house.Model))} payments.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                                // clean up
                                player.TempProperties.removeProperty(HousingConstants.BPsForHouseRent);
                            }

                            // clean up
                            player.TempProperties.removeProperty(HousingConstants.MoneyForHouseRent);
                            break;
                        }

                    case eDialogCode.MasterLevelWindow:
                        {
                            player.Out.SendMasterLevelWindow(_response);
                            break;
                        }
                }
            }
        }
    }
}