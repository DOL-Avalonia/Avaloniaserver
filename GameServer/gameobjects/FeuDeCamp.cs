﻿using System;
using System.Reflection;
using System.Linq;

using DOL.Database;
using DOL.Events;
using log4net;
using System.Timers;
using GameServerScripts.Utils;

namespace DOL.GS
{
    public class FeuDeCamp : GameNPC
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        public static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public FeuDeCamp()
            : base()
        {
            LoadedFromScript = true;
        }


        private ushort m_Range;
        // 4 secondes entre chaque tests
        private const double PROXIMITY_CHECK_INTERVAL = 4 * 1000;
        private const double REGENERATION_BOOST_FACTOR = 1.2;

        private Timer m_ProximityCheckTimer;
        private Timer m_LifeTimer;

        GameStaticItem m_RealFeu;    

        public string Template_ID
        {
            get;
            set;
        }

        public ushort Radius
        {
            get;
            set;
        }
        
        public double Lifetime
        {
            get;
            set;
        }

        public int EndurancePercentRate
        {
            get;
            set;
        }

        public int HealthPercentRate
        {
            get;
            set;
        }


        public bool IsHealthType
        {
            get;
            set;
        }

        public bool IsManaType
        {
            get;
            set;
        }

        public bool IsHealthTrapType
        {
            get;
            set;
        }

        public bool IsManaTrapType
        {
            get;
            set;
        }

        public bool IsEnduranceType
        {
            get;
            set;
        }

        public int HealthTrapDamagePercent
        {
            get;
            set;
        }

        public int ManaTrapDamagePercent
        {
            get;
            set;
        }


        public override bool AddToWorld()
        {
            Level = 0;
            Flags = (eFlags)GameNPC.eFlags.PEACE |
                (eFlags)GameNPC.eFlags.CANTTARGET;

            m_ProximityCheckTimer = new Timer(PROXIMITY_CHECK_INTERVAL);
            m_ProximityCheckTimer.Elapsed +=
                new ElapsedEventHandler(ProximityCheck);

            m_LifeTimer = new Timer(Lifetime * 60 * 1000);
            m_LifeTimer.Elapsed +=
                new ElapsedEventHandler(DeleteObject);

            m_ProximityCheckTimer.Start();
            m_LifeTimer.Start();

            m_RealFeu = new GameStaticItem();

            m_RealFeu.Name = Name = "Feu de Camp";
            m_RealFeu.X = X;
            m_RealFeu.Y = Y;
            m_RealFeu.Z = Z;
            m_RealFeu.Model = Model;
            m_RealFeu.CurrentRegion = CurrentRegion;
            m_RealFeu.Heading = Heading;

            m_RealFeu.AddToWorld();

            log.Debug("FeuDeCamp added");

            return base.AddToWorld();
        }

        void ProximityCheck(object sender, ElapsedEventArgs e)
        {
           

            foreach (GamePlayer Player in WorldMgr.GetPlayersCloseToSpot(this.CurrentRegionID, this.X, this.Y, this.Z, Radius))
            {
                if (Player.IsSitting)
                {  
                    if (IsHealthType)
                    {
                        Player.Health += HealthPercentRate * (Player.MaxHealth / 100);

                        if (Player.Health > Player.MaxHealth)
                        {
                            Player.Health = Player.MaxHealth;
                        }
                    }

                    if (IsEnduranceType)
                    {
                        Player.Endurance += Endurance * (Player.MaxEndurance / 100);

                        if (Player.Endurance > Player.Endurance)
                        {
                            Player.Endurance = Player.MaxEndurance;
                        }
                    }
                    
                    if (IsManaType)
                    {
                        Player.Mana += ManaPercent * (Player.MaxMana / 100);

                        if (Player.Mana > Player.MaxMana)
                        {
                            Player.Mana = Player.MaxMana;
                        }
                    }

                    if (IsHealthTrapType && HealthTrapDamagePercent > 0)
                    {
                        Player.Health -= HealthTrapDamagePercent * (Player.MaxHealth / 100);

                        if (Player.Health <= 0)
                        {
                            Player.Health = 0;
                            Player.Die(null);
                        }
                    }

                    if (IsManaTrapType && ManaTrapDamagePercent > 0)
                    {
                        Player.Mana -= ManaTrapDamagePercent * (Player.MaxMana / 100);

                        if (Player.Mana < 0)
                        {
                            Player.Mana = 0;
                        }
                    }
                }            
            }
        }

        void DeleteObject(object sender, ElapsedEventArgs e)
        {
            m_LifeTimer.Stop();
            m_ProximityCheckTimer.Stop();

            m_RealFeu.Delete();

            Delete();
        }
    }

    public class FeuDeCampEvent
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        public static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void OnScriptLoaded(DOLEvent e, object sender, EventArgs args)
        {
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
            var Feu = FeuxCampMgr.Instance.m_firecamps.Values.FirstOrDefault(f => f.Template_ID == Args.SourceItem.Id_nb);

            if (Player != null && Feu != null)
            {
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
}
