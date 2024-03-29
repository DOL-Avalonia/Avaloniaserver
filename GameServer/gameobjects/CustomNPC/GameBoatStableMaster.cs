
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
using DOL.Database;
using DOL.Events;
using DOL.Language;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    /// <summary>
    /// Stable master that sells and takes horse route tickes
    /// </summary>
    public class GameBoatStableMaster : GameMerchant
    {
        /// <summary>
        /// Called when the living is about to get an item from someone
        /// else
        /// </summary>
        /// <param name="source">Source from where to get the item</param>
        /// <param name="item">Item to get</param>
        /// <returns>true if the item was successfully received</returns>
        public override bool ReceiveItem(GameLiving source, InventoryItem item)
        {
            if (source == null || item == null)
            {
                return false;
            }

            if (source is GamePlayer)
            {
                GamePlayer player = (GamePlayer)source;

                if (player.Reputation < 0)
                {
                    TurnTo(player, 5000);
                    player.Out.SendMessage("Je ne re�ois rien de la part des hors-la-loi", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if (item.Name.ToLower().StartsWith(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameStableMaster.ReceiveItem.TicketTo")) && item.Item_Type == 40)
                {
                    foreach (GameNPC npc in GetNPCsInRadius(1500))
                    {
                        if (npc is GameTaxiBoat)
                        {
                            player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameBoatStableMaster.ReceiveItem.Departed", Name), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                            return false;
                        }
                    }

                    string destination = item.Name.Substring(LanguageMgr.GetTranslation(ServerProperties.Properties.DB_LANGUAGE, "GameStableMaster.ReceiveItem.TicketTo").Length);
                    PathPoint path = MovementMgr.LoadPath(item.Id_nb);

                    // PathPoint path = MovementMgr.Instance.LoadPath(this.Name + "=>" + destination);
                    if ((path != null) && (Math.Abs(path.X - X) < 500) && (Math.Abs(path.Y - Y) < 500))
                    {
                        player.Inventory.RemoveCountFromStack(item, 1);
                        InventoryLogging.LogInventoryAction(player, this, eInventoryActionType.Merchant, item.Template);

                        GameTaxiBoat boat = new GameTaxiBoat();
                        boat.Name = "Boat to " + destination;
                        boat.Realm = source.Realm;
                        boat.X = path.X;
                        boat.Y = path.Y;
                        boat.Z = path.Z;
                        boat.CurrentRegion = CurrentRegion;
                        boat.Heading = path.GetHeading(path.Next);
                        boat.AddToWorld();
                        boat.CurrentWayPoint = path;
                        GameEventMgr.AddHandler(boat, GameNPCEvent.PathMoveEnds, new DOLEventHandler(OnHorseAtPathEnd));

                        // new MountHorseAction(player, boat).Start(400);
                        new HorseRideAction(boat).Start(30 * 1000);

                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameBoatStableMaster.ReceiveItem.SummonedBoat", Name, destination), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return true;
                    }
                    else
                    {
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameBoatStableMaster.ReceiveItem.UnknownWay", Name, destination), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    }
                }
            }

            return base.ReceiveItem(source, item);
        }

        /// <summary>
        /// Handles 'horse route end' events
        /// </summary>
        /// <param name="e"></param>
        /// <param name="o"></param>
        /// <param name="args"></param>
        public void OnHorseAtPathEnd(DOLEvent e, object o, EventArgs args)
        {
            if (!(o is GameNPC))
            {
                return;
            }

            GameNPC npc = (GameNPC)o;
            GameEventMgr.RemoveHandler(npc, GameNPCEvent.PathMoveEnds, new DOLEventHandler(OnHorseAtPathEnd));
            npc.StopMoving();
            npc.RemoveFromWorld();
        }

        /// <summary>
        /// Handles delayed player mount on horse
        /// </summary>
        protected class MountHorseAction : RegionAction
        {
            /// <summary>
            /// The target horse
            /// </summary>
            protected readonly GameNPC m_horse;

            /// <summary>
            /// Constructs a new MountHorseAction
            /// </summary>
            /// <param name="actionSource">The action source</param>
            /// <param name="horse">The target horse</param>
            public MountHorseAction(GamePlayer actionSource, GameNPC horse)
                : base(actionSource)
            {
                if (horse == null)
                {
                    throw new ArgumentNullException("horse");
                }

                m_horse = horse;
            }

            /// <summary>
            /// Called on every timer tick
            /// </summary>
            protected override void OnTick()
            {
                GamePlayer player = (GamePlayer)m_actionSource;
                player.MountSteed(m_horse, true);
            }
        }

        /// <summary>
        /// Handles delayed horse ride actions
        /// </summary>
        protected class HorseRideAction : RegionAction
        {
            /// <summary>
            /// Constructs a new HorseStartAction
            /// </summary>
            /// <param name="actionSource"></param>
            public HorseRideAction(GameNPC actionSource)
                : base(actionSource)
            {
            }

            /// <summary>
            /// Called on every timer tick
            /// </summary>
            protected override void OnTick()
            {
                GameNPC horse = (GameNPC)m_actionSource;
                horse.MoveOnPath(horse.MaxSpeed);
            }
        }
    }
}
