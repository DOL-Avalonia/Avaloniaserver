using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using log4net;

namespace AmteScripts.Managers
{
    public class RvrManager
    {

        const string RvRNoviceALB = "RvR-Novice-ALB";
        const string RvRNoviceHIB = "RvR-Novice-HIB";
        const string RvRNoviceMID = "RvR-Novice-MID";

        const string RvRDebutantALB = "RvR-Debutant-ALB";
        const string RvRDebutantHIB = "RvR-Debutant-HIB";
        const string RvRDebutantMID = "RvR-Debutant-MID";

        const string RvRStandardALB = "RvR-Standard-ALB";
        const string RvRStandardHIB = "RvR-Standard-HIB";
        const string RvRStandardMID = "RvR-Standard-MID";

        const string RvRExpertALB = "RvR-Expert-ALB";
        const string RvRExpertHIB = "RvR-Expert-HIB";
        const string RvRExpertMID = "RvR-Expert-MID";

        const string RvRMasterALB = "RvR-Master-ALB";
        const string RvRMasterHIB = "RvR-Master-HIB";
        const string RvRMasterMID = "RvR-Master-MID";

        const string RvRDivineALB = "RvR-Divine-ALB";
        const string RvRDivineHIB = "RvR-Divine-HIB";
        const string RvRDivineMID = "RvR-Divine-MID";

        private static readonly TimeSpan _startTime = new TimeSpan(20, 0, 0); //20H
        private static readonly TimeSpan _endTime = new TimeSpan(2, 0, 0).Add(TimeSpan.FromDays(1)); //2h du mat
        private const int _checkInterval = 30 * 1000; // 30 seconds
        private static readonly GameLocation _stuckSpawn = new GameLocation("", 51, 434303, 493165, 3088, 1069);

        #region Static part
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static RvrManager _instance;
        private static RegionTimer _timer;

        public static RvrManager Instance { get { return _instance; } }

        [ScriptLoadedEvent]
        public static void OnServerStarted(DOLEvent e, object sender, EventArgs args)
        {
            log.Info("RvRManger: Started");
            _instance = new RvrManager();
            _timer = new RegionTimer(WorldMgr.GetRegion(1).TimeManager)
            {
                Callback = _instance._CheckRvr
            };
            _timer.Start(1);
        }

        [ScriptUnloadedEvent]
        public static void OnServerStopped(DOLEvent e, object sender, EventArgs args)
        {
            log.Info("RvRManger: Stopped");
            _timer.Stop();
        }
        #endregion

        private bool _isOpen;
        private bool _isForcedOpen;
        private IEnumerable<ushort> _regions;

        private readonly Guild Albion;
        private readonly Guild Hibernia;
        private readonly Guild Midgard;


        public bool IsOpen { get { return _isOpen; } }
        public IEnumerable<ushort> Regions { get { return _regions; } }

        /// <summary>
        /// &lt;regionID, Tuple&lt;TPs, spawnAlb, spawnMid, spawnHib&gt;&gt;
        /// </summary>
        private readonly Dictionary<string, Tuple<GameNPC, GameLocation>> _maps =
            new Dictionary<string, Tuple<GameNPC, GameLocation>>();

        private RvrManager()
        {
            Albion = GuildMgr.GetGuildByName(nameof(Albion));
            if (Albion == null)
                Albion = GuildMgr.CreateGuild(eRealm.Albion, nameof(Albion));

            Hibernia = GuildMgr.GetGuildByName(nameof(Hibernia));
            if (Hibernia == null)
                Hibernia = GuildMgr.CreateGuild(eRealm.Hibernia, nameof(Hibernia));

            Midgard = GuildMgr.GetGuildByName(nameof(Midgard));
            if (Midgard == null)
                Midgard = GuildMgr.CreateGuild(eRealm.Midgard, nameof(Midgard));


            Albion.SaveIntoDatabase();
            Hibernia.SaveIntoDatabase();
            Midgard.SaveIntoDatabase();         
            FindRvRMaps();
        }

        public IEnumerable<ushort> FindRvRMaps()
        {
            var npcs = WorldMgr.GetNPCsByGuild("RVR", eRealm.None).Where(n => n.Name.StartsWith("RvR-"));
            _maps.Clear();
           
            var RvRNovices = npcs.Where(n => n.Name.StartsWith("RvR-Novice"));
            var RvRDebutants = npcs.Where(n => n.Name.StartsWith("RvR-Debutant"));
            var RvRStandards = npcs.Where(n => n.Name.StartsWith("RvR-Standard"));
            var RvRExperts = npcs.Where(n => n.Name.StartsWith("RvR-Expert"));
            var RvRMasters = npcs.Where(n => n.Name.StartsWith("RvR-Master"));
            var RvRDivines = npcs.Where(n => n.Name.StartsWith("RvR-Divine"));

            if (RvRNovices == null || RvRDebutants == null || RvRStandards == null || RvRExperts == null || RvRMasters == null)
            {
                throw new KeyNotFoundException("RvR Maps");
            }

            RvRNovices.ForEach(novice =>
            {
                string name = null;

                if (novice.Name.EndsWith("HIB"))
                {
                    name = RvRNoviceHIB;
                }
                else if (novice.Name.EndsWith("ALB"))
                {
                    name = RvRNoviceALB;
                }
                else if (novice.Name.EndsWith("MID"))
                {
                    name = RvRNoviceMID;
                }
                _maps.Add(name, new Tuple<GameNPC, GameLocation>(novice,
                     new GameLocation(novice.Name, novice.CurrentRegionID, novice.X, novice.Y, novice.Z, novice.Heading)));
            });

            RvRDebutants.ForEach(debutant =>
            {
                string name = null;

                if (debutant.Name.EndsWith("HIB"))
                {
                    name = RvRDebutantHIB;
                }
                else if (debutant.Name.EndsWith("ALB"))
                {
                    name = RvRDebutantALB;
                }
                else if (debutant.Name.EndsWith("MID"))
                {
                    name = RvRDebutantMID;
                }
                _maps.Add(name, new Tuple<GameNPC, GameLocation>(debutant,
                     new GameLocation(debutant.Name, debutant.CurrentRegionID, debutant.X, debutant.Y, debutant.Z, debutant.Heading)));
            });


            RvRStandards.ForEach(standard =>
            {
                string name = null;

                if (standard.Name.EndsWith("HIB"))
                {
                    name = RvRStandardHIB;
                }
                else if (standard.Name.EndsWith("ALB"))
                {
                    name = RvRStandardALB;
                }
                else if (standard.Name.EndsWith("MID"))
                {
                    name = RvRStandardMID;
                }
                _maps.Add(name, new Tuple<GameNPC, GameLocation>(standard,
                     new GameLocation(standard.Name, standard.CurrentRegionID, standard.X, standard.Y, standard.Z, standard.Heading)));
            });


            RvRExperts.ForEach(expert =>
            {
                string name = null;

                if (expert.Name.EndsWith("HIB"))
                {
                    name = RvRExpertHIB;
                }
                else if (expert.Name.EndsWith("ALB"))
                {
                    name = RvRExpertALB;
                }
                else if (expert.Name.EndsWith("MID"))
                {
                    name = RvRExpertMID;
                }
                _maps.Add(name, new Tuple<GameNPC, GameLocation>(expert,
                     new GameLocation(expert.Name, expert.CurrentRegionID, expert.X, expert.Y, expert.Z, expert.Heading)));
            });


            RvRMasters.ForEach(master =>
            {
                string name = null;

                if (master.Name.EndsWith("HIB"))
                {
                    name = RvRMasterHIB;
                }
                else if (master.Name.EndsWith("ALB"))
                {
                    name = RvRMasterALB;
                }
                else if (master.Name.EndsWith("MID"))
                {
                    name = RvRMasterMID;
                }
                _maps.Add(name, new Tuple<GameNPC, GameLocation>(master,
                     new GameLocation(master.Name, master.CurrentRegionID, master.X, master.Y, master.Z, master.Heading)));
            });

            RvRDivines.ForEach(divine =>
            {
                string name = null;

                if (divine.Name.EndsWith("HIB"))
                {
                    name = RvRDivineHIB;
                }
                else if (divine.Name.EndsWith("ALB"))
                {
                    name = RvRDivineALB;
                }
                else if (divine.Name.EndsWith("MID"))
                {
                    name = RvRDivineMID;
                }
                _maps.Add(name, new Tuple<GameNPC, GameLocation>(divine,
                     new GameLocation(divine.Name, divine.CurrentRegionID, divine.X, divine.Y, divine.Z, divine.Heading)));
            });

            _regions = _maps.Values.GroupBy(v => v.Item2.RegionID).Select(v => v.Key);
       
            return (from m in _maps select m.Value.Item2.RegionID);
        }

        private int _CheckRvr(RegionTimer callingtimer)
        {
            Console.WriteLine("Check RVR");
            if (!_isOpen)
            {
            	_regions.ForEach(id => WorldMgr.GetClientsOfRegion(id).Foreach(RemovePlayer));
            	if (DateTime.Now.TimeOfDay >= _startTime && DateTime.Now.TimeOfDay < _endTime)
            		Open(false);
            }
            else
            {
                _regions.ForEach(id => WorldMgr.GetClientsOfRegion(id).Where(cl => cl.Player.Guild == null).Foreach(cl => RemovePlayer(cl.Player)));
                if (!_isForcedOpen)
                {
                    if ((DateTime.Now.TimeOfDay < _startTime || DateTime.Now.TimeOfDay > _endTime) && !Close())
                        _regions.ForEach(id => WorldMgr.GetClientsOfRegion(id).Foreach(RemovePlayer));
                }
            }
            return _checkInterval;
        }

        public bool Open(bool force)
        {
            _isForcedOpen = force;
            if (_isOpen)
                return true;
            _isOpen = true;           

            Hibernia.RealmPoints = 0;
            Albion.RealmPoints = 0;
            Midgard.RealmPoints = 0;          
            return true;
        }

        public bool Close()
        {
            if (!_isOpen)
                return false;
            _isOpen = false;
            _isForcedOpen = false;

            this._maps.Values.GroupBy(v => v.Item2.RegionID).ForEach(region => {
                WorldMgr.GetClientsOfRegion(region.Key).Where(player => player.Player != null).Foreach(RemovePlayer);
                GameServer.Database.SelectObjects<DOLCharacters>("Region = " + region.Key).Foreach(RemovePlayer);
            }); 
          
            return true;
        }

        public bool AddPlayer(GamePlayer player)
        {
            if (!_isOpen || player.Level < 20)
                return false;
			if (player.Client.Account.PrivLevel >= (uint)ePrivLevel.GM)
			{
				player.Out.SendMessage("Casse-toi connard de GM !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
				return false;
			}
        	RvrPlayer rvr = new RvrPlayer(player);
            GameServer.Database.AddObject(rvr);

        	if (player.Guild != null)
        		player.Guild.RemovePlayer("RVR", player);

            bool isAdded = false;

            if (player.Level < 20)
            {
                player.Out.SendMessage("Reviens quand tu auras plus d'expérience !", eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return isAdded;
            }
            else
            {     
                switch (player.Realm)
                {
                    case eRealm.Albion:
                        Albion.AddPlayer(player);
                        isAdded = this.AddPlayerToCorrectZone(player, "ALB");
                        break;

                    case eRealm.Midgard:
                        Midgard.AddPlayer(player);
                        isAdded = this.AddPlayerToCorrectZone(player, "MID");
                        break;

                    case eRealm.Hibernia:
                        Hibernia.AddPlayer(player);
                        isAdded = this.AddPlayerToCorrectZone(player, "HIB");
                        break;
                }

                if (player.Guild != null)
                    foreach (var i in player.Inventory.AllItems.Where(i => i.Emblem != 0))
                        i.Emblem = player.Guild.Emblem;

            }
            return isAdded;
        }

        private bool AddPlayerToCorrectZone(GamePlayer player, string realm)
        {
            string key = null;
            try
            {
                if (player.Level >= 20 && player.Level < 26)
                {
                    key = "RvR-Novice-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }                 
                }
                else if (player.Level >= 26 && player.Level < 32)
                {
                    key = "RvR-Debutant-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }                   
                }
                else if (player.Level >= 32 && player.Level < 38)
                {
                    key = "RvR-Standard-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }                 
                }
                else if (player.Level >= 38 && player.Level < 44)
                {
                    key = "RvR-Expert-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }                 
                }
                else if (player.Level >= 44 && !player.IsRenaissance)
                {
                    key = "RvR-Master-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }                   
                }
                else if (player.Level >= 50 && player.IsRenaissance)
                {
                    //RvR DIVINITÉS acessible UNIQUEMENT aux joueurs « IsRenaissance » Level50
                    key = "RvR-Divine-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }                 
                }

                player.MoveTo(_maps[key].Item2);
                player.Bind(true);
                return true;
            }
            catch(KeyNotFoundException e)
            {
                log.Error(e.Message, e);
                return false;
            }
        }


        public void RemovePlayer(GameClient client)
		{
			if (client.Player != null)
				RemovePlayer(client.Player);
		}

        public void RemovePlayer(GamePlayer player)
        {
			if (player.Client.Account.PrivLevel >= (uint)ePrivLevel.GM)
				return;
            var rvr = GameServer.Database.SelectObject<RvrPlayer>("PlayerID = '" + GameServer.Database.Escape(player.InternalID) + "'");
            if (rvr == null)
            {
				player.MoveTo(_stuckSpawn);
				if (player.Guild != null && (player.Guild.Name.Equals("RVR")))
					player.Guild.RemovePlayer("RVR", player);
				player.SaveIntoDatabase();
            }
            else
            {
                rvr.ResetCharacter(player);
                player.MoveTo((ushort)rvr.OldRegion, rvr.OldX, rvr.OldY, rvr.OldZ, (ushort)rvr.OldHeading);
                if (player.Guild != null)
                    player.Guild.RemovePlayer("RVR", player);
                if (!string.IsNullOrWhiteSpace(rvr.GuildID))
                {
                    var guild = GuildMgr.GetGuildByGuildID(rvr.GuildID);
					if (guild != null)
					{
						guild.AddPlayer(player, guild.GetRankByID(rvr.GuildRank));

						foreach (var i in player.Inventory.AllItems.Where(i => i.Emblem != 0))
							i.Emblem = guild.Emblem;
					}
                }
                player.SaveIntoDatabase();
                GameServer.Database.DeleteObject(rvr);
            }
        }

        public void RemovePlayer(DOLCharacters ch)
        {
            var rvr = GameServer.Database.SelectObject<RvrPlayer>("PlayerID = '" + GameServer.Database.Escape(ch.ObjectId) + "'");
            if (rvr == null)
            {
                // AHHHHHHHHHHH
            }
            else
            {
                rvr.ResetCharacter(ch);
                GameServer.Database.SaveObject(ch);
                GameServer.Database.DeleteObject(rvr);
            }
        }


        private IList<string> _statCache = new List<string>();
        private DateTime _statLastCacheUpdate = DateTime.Now;

        public IList<string> GetStatistics(ushort region)
        {
            if (DateTime.Now.Subtract(_statLastCacheUpdate) >= new TimeSpan(0, 0, 30))
            {
                _statLastCacheUpdate = DateTime.Now;
                var clients = WorldMgr.GetClientsOfRegion(region);
                var albCount = clients.Where(c => c.Player.Realm == eRealm.Albion).Count();
                var midCount = clients.Where(c => c.Player.Realm == eRealm.Midgard).Count();
                var hibCount = clients.Where(c => c.Player.Realm == eRealm.Hibernia).Count();

                long prAlb = clients.Where(c => c.Player.Realm == eRealm.Albion).Sum(c => c.Player.Guild.RealmPoints);
                long prHib = clients.Where(c => c.Player.Realm == eRealm.Hibernia).Sum(c => c.Player.Guild.RealmPoints);
                long prMid = clients.Where(c => c.Player.Realm == eRealm.Midgard).Sum(c => c.Player.Guild.RealmPoints);

               _statCache = new List<string>
                    {
                        "Statistiques du RvR:",
                        " - Albion: ",
                        (_isOpen ? albCount : 0) + " joueurs",
                        (_isOpen ? prAlb : 0) + " PR",
                        " - Midgard: ",
                         (_isOpen ? midCount : 0) + " joueurs",
                          (_isOpen ? prMid : 0) + " PR",
                        " - Hibernia: ",
                         (IsOpen ? hibCount : 0) + " joueurs",
                          (_isOpen ? prHib : 0) + " PR",
                        "",
                        " - Total: ",
                        (IsOpen ? clients.Count : 0) + " joueurs",
                        (IsOpen ? prAlb + prMid + prHib : 0) + " PR",
                "",
                        "Le rvr est " + (_isOpen ? "ouvert" : "fermé") + ".",
                        "(Mise à jour toutes les 30 secondes)"
                    };
            }
            return _statCache;
        }

        public bool IsInRvr(GameLiving obj)
        {
            return obj != null && _regions.Any(id => id == obj.CurrentRegionID);
        }

        public bool IsAllowedToAttack(GameLiving attacker, GameLiving defender, bool quiet)
        {
            if (attacker.Realm == defender.Realm)
            {
                if (!quiet)
                    _MessageToLiving(attacker, "Vous ne pouvez pas attaquer un membre de votre royaume !");
                return false;
            }
            return true;
        }

        public bool IsRvRRegion(ushort id)
        {
            return _maps.Values.Select(v => v.Item2).FirstOrDefault(l => l.RegionID == id) != null;
        }

        private static void _MessageToLiving(GameLiving living, string message)
        {
            if (living is GamePlayer)
                ((GamePlayer)living).Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
