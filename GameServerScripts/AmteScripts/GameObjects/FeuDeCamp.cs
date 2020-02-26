using System;
using System.Reflection;

using DOL.Database;

using DOL.Events;
using DOL.GS.Spells;
using DOL.GS.Scripts;
using DOL.GS.PacketHandler;
using log4net;
using System.Collections;
using System.Collections.Generic;
using System.Timers;

namespace DOL.GS.Scripts
{
    public class FeuDeCampEvent
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        public static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
            ItemTemplate Temp;
            Temp = Feu;
            GameEventMgr.AddHandler(PlayerInventoryEvent.ItemDropped,
                new DOLEventHandler(EventPlayerDropItem));
            log.Info("FeuDeCamp chargé.");
        }

        [ScriptUnloadedEvent]
        public static void OnScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.RemoveHandler(PlayerInventoryEvent.ItemDropped,
                new DOLEventHandler(EventPlayerDropItem));
        }

        protected static ItemTemplate m_Feu;
        public static ItemTemplate Feu
        {
            get
            {
                m_Feu = (ItemTemplate)GameServer.Database.FindObjectByKey<ItemTemplate>("tif_s_feu");
                if (m_Feu == null)
                {
                    m_Feu = new ItemTemplate();
                    m_Feu.CanDropAsLoot = true;
                    m_Feu.Charges = 1;
                    m_Feu.Id_nb = "tif_s_feu";
                    m_Feu.IsDropable = true;
                    m_Feu.IsPickable = false;
                    m_Feu.IsTradable = true;
                    m_Feu.Item_Type = 41;
                    m_Feu.Level = 0;
                    m_Feu.Model = 3470;
                    m_Feu.Name = "Necessaire à Feu de Camp";
                    m_Feu.Object_Type = (int)eObjectType.GenericItem;
                    m_Feu.Realm = 0;
                    m_Feu.Quality = 100;
                    m_Feu.Price = 10000;

                    GameServer.Database.AddObject(m_Feu);
                }
                return m_Feu;
            }
        }

        public static void EventPlayerDropItem(DOLEvent e, object sender,
            EventArgs args)
        {
            ItemDroppedEventArgs Args = args as ItemDroppedEventArgs;
            GamePlayer Player = sender as GamePlayer;
            if (Player != null && Args.SourceItem.Id_nb == "tif_s_feu")
            {
                FeuDeCamp Feu = new FeuDeCamp();
                Feu.X = Player.X;
                Feu.Y = Player.Y;
                Feu.Z = Player.Z;
                Feu.CurrentRegion = Player.CurrentRegion;
                Feu.Heading = Player.Heading;
                Feu.AddToWorld();

                Args.GroundItem.Delete();
            }
        }
    }

    public class FeuDeCamp : GameNPC
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        public static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private SortedDictionary<GamePlayer, object> m_LastPlayers;

        public FeuDeCamp()
            : base()
        {
            LoadedFromScript = true;
            m_LastPlayers = new SortedDictionary<GamePlayer,object>();
        }

        private ushort m_Range;
        private const int REGEN_DISTANCE = WorldMgr.GIVE_ITEM_DISTANCE;
        // 4 secondes entre chaque tests
        private const double PROXIMITY_CHECK_INTERVAL = 4 * 1000;
        private const double LIFETIME = 20 * 60 * 1000;
        private const double REGENERATION_BOOST_FACTOR = 1.2;

        private Timer m_ProximityCheckTimer;
        private Timer m_LifeTimer;

        GameStaticItem m_RealFeu;

        public override bool AddToWorld()
        {
            Model = 1;
            Level = 0;
            Flags = (eFlags)GameNPC.eFlags.PEACE |
                (eFlags)GameNPC.eFlags.CANTTARGET;

            m_ProximityCheckTimer = new Timer(PROXIMITY_CHECK_INTERVAL);
            m_ProximityCheckTimer.Elapsed +=
                new ElapsedEventHandler(ProximityCheck);

            m_LifeTimer = new Timer(LIFETIME);
            m_LifeTimer.Elapsed +=
                new ElapsedEventHandler(DeleteObject);

            m_ProximityCheckTimer.Start();
            m_LifeTimer.Start();

            m_RealFeu = new GameStaticItem();

            m_RealFeu.Name = Name = "Feu de Camp";
            m_RealFeu.Model = 2656;

            m_RealFeu.X = X;
            m_RealFeu.Y = Y;
            m_RealFeu.Z = Z;
            m_RealFeu.CurrentRegion = CurrentRegion;
            m_RealFeu.Heading = Heading;

            m_RealFeu.AddToWorld();

            log.Debug("FeuDeCamp added");

            return base.AddToWorld();
        }

        void ProximityCheck(object sender, ElapsedEventArgs e)
        {
            foreach (GamePlayer Player in WorldMgr.GetPlayersCloseToSpot(this.CurrentRegionID, this.X, this.Y, this.Z, REGEN_DISTANCE))
            {
                if (Player.IsSitting)
                {
                    if (!m_LastPlayers.ContainsKey(Player))
                    {
                        Player.StartHealthRegeneration();
                        Player.StartEnduranceRegeneration();
                        Player.StartPowerRegeneration();

                        //log.DebugFormat("Proximity check : Player {0} added (In range & sitting)",
                        //    Player.Name);

                        m_LastPlayers.Add(Player, null);
                    }
                }
                else if (m_LastPlayers.ContainsKey(Player))
                {
                    m_LastPlayers.Remove(Player);
                    //log.DebugFormat("Proximity check : Player {0} removed (Not sitting anymore)",
                    //    Player.Name);
                }
            }

            List<GamePlayer> ToDelete = new List<GamePlayer>();
            foreach (KeyValuePair<GamePlayer, object> Pair in m_LastPlayers)
            {
                GamePlayer Player = Pair.Key;
                bool ContainPlayer = false;
                foreach (GamePlayer Player2 in WorldMgr.GetPlayersCloseToSpot(this.CurrentRegionID, this.X, this.Y, this.Z, REGEN_DISTANCE))
                {
                    if (Player == Player2)
                    {
                        ContainPlayer = true;
                        break;
                    }
                }

                if (!ContainPlayer)
                {
                    //Player.RegenerationFactor = 1.0;
                    //ToDelete.Add(Player);
                    //log.DebugFormat("Proximity check : Player {0} removed (Out of range)",
                    //    Player.Name);
                }
            }

            foreach (GamePlayer Player in ToDelete)
            {
                m_LastPlayers.Remove(Player);
            }

            //log.DebugFormat("Proximity check : {0} player(s) in range",
            //    m_LastPlayers.Count);
        }

        void DeleteObject(object sender, ElapsedEventArgs e)
        {
            m_LifeTimer.Stop();
            m_ProximityCheckTimer.Stop();

            m_RealFeu.Delete();

            Delete();
        }
    }
}
