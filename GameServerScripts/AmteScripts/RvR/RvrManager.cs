using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Amte;
using AmteScripts.Utils;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using DOL.Territory;
using log4net;

namespace AmteScripts.Managers
{
	public class RvrManager
	{
        const string ALBION = "Albion";
        const string HIBERNIA = "Hibernia";
        const string MIDGARD = "Midgard";

        //const string RvRNoviceALB = "RvR-Novice-ALB";
        //const string RvRNoviceHIB = "RvR-Novice-HIB";
        //const string RvRNoviceMID = "RvR-Novice-MID";

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

        //const string RvRDivineALB = "RvR-Divine-ALB";
        //const string RvRDivineHIB = "RvR-Divine-HIB";
        //const string RvRDivineMID = "RvR-Divine-MID";

        private static int RVR_RADIUS = Properties.RvR_AREA_RADIUS;
        private static DateTime _startTime = DateTime.Today.AddHours(20D); //20H00
        private static DateTime _endTime = _startTime.Add(TimeSpan.FromHours(6)); //2H00 + 1
		private const int _checkInterval = 30 * 1000; // 30 seconds
		private static readonly GameLocation _stuckSpawn = new GameLocation("", 51, 434303, 493165, 3088, 1069);
        private Dictionary<ushort, IList<string>> RvrStats = new Dictionary<ushort, IList<string>>();

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
			_timer.Start(10000);
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
        private readonly Dictionary<string, RvRMap> _maps =
            new Dictionary<string, RvRMap>();

		private RvrManager()
		{
            Albion = GuildMgr.GetGuildByName(ALBION);
            if (Albion == null)
                Albion = GuildMgr.CreateGuild(eRealm.Albion, ALBION);

            Hibernia = GuildMgr.GetGuildByName(HIBERNIA);
            if (Hibernia == null)
                Hibernia = GuildMgr.CreateGuild(eRealm.Hibernia, HIBERNIA);

            Midgard = GuildMgr.GetGuildByName(MIDGARD);
            if (Midgard == null)
                Midgard = GuildMgr.CreateGuild(eRealm.Midgard, MIDGARD);        

            Albion.SaveIntoDatabase();
            Hibernia.SaveIntoDatabase();
            Midgard.SaveIntoDatabase();         
			InitMapsAndTerritories();
		}

        public void OnControlChange(string lordId, Guild guild)
        {           
            var territory = _maps.Values.FirstOrDefault(m => m.RvRTerritory != null && m.RvRTerritory.BossId.Equals(lordId));

            if (territory != null && territory.RvRTerritory != null)
            {
                TerritoryManager.ApplyEmblemToTerritory(territory.RvRTerritory, guild, territory.RvRTerritory.Boss);
                territory.RvRTerritory.Mobs.ForEach(m => 
                {
                    m.GuildName = guild.Name;
                    m.Realm = guild.Realm;
                });
                territory.RvRTerritory.GuildOwner = guild.Name;
                territory.RvRTerritory.Boss.Realm = guild.Realm;
            }
        }

        public RvRTerritory GetRvRTerritory(ushort regionId)
        {
            var map = this._maps.Values.FirstOrDefault(v => v.RvRTerritory != null && v.Location.RegionID.Equals(regionId));

            if (map == null)
            {
                return null;
            }

            return map.RvRTerritory;
        }

        public IEnumerable<ushort> InitMapsAndTerritories()
		{
            var npcs = WorldMgr.GetNPCsByGuild("RVR", eRealm.None).Where(n => n.Name.StartsWith("RvR-"));
			_maps.Clear();
           
            //var RvRNovices = npcs.Where(n => n.Name.StartsWith("RvR-Novice"));
            var RvRDebutants = npcs.Where(n => n.Name.StartsWith("RvR-Novice"));
            var RvRStandards = npcs.Where(n => n.Name.StartsWith("RvR-Debutant"));
            var RvRExperts = npcs.Where(n => n.Name.StartsWith("RvR-Expert"));
            var RvRMasters = npcs.Where(n => n.Name.StartsWith("RvR-Master"));
            //var RvRDivines = npcs.Where(n => n.Name.StartsWith("RvR-Divine"));

            if (RvRDebutants == null || RvRStandards == null || RvRExperts == null || RvRMasters == null)
            {
                throw new KeyNotFoundException("RvR Maps");
            }

            //RvRNovices.ForEach(novice =>
            //{
            //    string name = null;
            //    var map = this.BuildRvRMap(novice);

            //    if (map == null) {  /*Skip Null Map*/ return; }

            //    if (novice.Name.EndsWith("HIB"))
            //    {
            //        name = RvRNoviceHIB;
            //    }
            //    else if (novice.Name.EndsWith("ALB"))
            //    {
            //        name = RvRNoviceALB;
            //    }
            //    else if (novice.Name.EndsWith("MID"))
            //    {
            //        name = RvRNoviceMID;
            //    }
            //    _maps.Add(name, map);
            //});

            RvRDebutants.ForEach(debutant =>
            {
                string name = null;
                var map = this.BuildRvRMap(debutant);

                if (map == null) {  /*Skip Null Map*/ return; }

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
                _maps.Add(name, map);
            });


            RvRStandards.ForEach(standard =>
            {
                string name = null;
                var map = this.BuildRvRMap(standard);

                if (map == null) {  /*Skip Null Map*/ return; }

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
                _maps.Add(name, map);
            });


            RvRExperts.ForEach(expert =>
            {
                string name = null;
                var map = this.BuildRvRMap(expert);

                if (map == null) {  /*Skip Null Map*/ return; }

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
                _maps.Add(name, map);
            });


            RvRMasters.ForEach(master =>
            {
                string name = null;
                var map = this.BuildRvRMap(master);

                if (map == null) {  /*Skip Null Map*/ return; }

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
                _maps.Add(name, map);
            });

            //RvRDivines.ForEach(divine =>
            //{
            //    string name = null;
            //    var map = this.BuildRvRMap(divine);

            //    if (map == null) {  /*Skip Null Map*/ return; }

            //    if (divine.Name.EndsWith("HIB"))
            //    {
            //        name = RvRDivineHIB;
            //    }
            //    else if (divine.Name.EndsWith("ALB"))
            //    {
            //        name = RvRDivineALB;
            //    }
            //    else if (divine.Name.EndsWith("MID"))
            //    {
            //        name = RvRDivineMID;
            //    }
            //    _maps.Add(name, map);
            //});

            _regions = _maps.Values.GroupBy(v => v.Location.RegionID).Select(v => v.Key).OrderBy(v => v);
            _regions.ForEach(r => this.RvrStats.Add(r, new string[] { }));
       
            return from m in _maps select m.Value.Location.RegionID;
        }

        private RvRMap BuildRvRMap(GameNPC initNpc)
        {
            RvRTerritory rvrTerritory = null;
            if (!_maps.Values.Any(v => v.Location.RegionID.Equals(initNpc.CurrentRegionID)))
            {
                var lord = initNpc.CurrentRegion.Objects.FirstOrDefault(o => o != null && o is LordRvR) as LordRvR;

                if (lord == null)
                {
                    log.Error("Cannot Init RvR because no LordRvR was present in Region " + initNpc.CurrentRegionID + " for InitNpc: " + initNpc.Name + ". Add a LordRvR in this RvR");
                    return null;
                }
                var areaName = string.IsNullOrEmpty(lord.GuildName) ? initNpc.Name : lord.GuildName;
                var area = new Area.Circle(areaName, lord.X, lord.Y, lord.Z, RVR_RADIUS);
                rvrTerritory = new RvRTerritory(area, area.Description, lord.CurrentRegionID, lord.CurrentZone.ID, lord);
            }        

            return new RvRMap()
            {
                Location = new GameLocation(initNpc.Name, initNpc.CurrentRegionID, initNpc.X, initNpc.Y, initNpc.Z),
                RvRTerritory = rvrTerritory
            };
        }

        private int _CheckRvr(RegionTimer callingtimer)
		{
			Console.WriteLine("Check RVR");
			if (!_isOpen)
			{
            	_regions.ForEach(id => WorldMgr.GetClientsOfRegion(id).Foreach(RemovePlayer));
				if (DateTime.Now >= _startTime && DateTime.Now < _endTime)
            		Open(false);
			}
			else
			{
                foreach (var id in _regions)
                {
                    foreach (var cl in WorldMgr.GetClientsOfRegion(id))
                    {
                        if (cl.Player.Guild == null)
                        {
                            RemovePlayer(cl.Player);
                        }
                        else
                        {
                            cl.Player.IsInRvR = true;
                        }
                    }
                }

                if (!_isForcedOpen)
				{
					if ((DateTime.Now < _startTime || DateTime.Now > _endTime) && !Close())
                        _regions.ForEach(id => WorldMgr.GetClientsOfRegion(id).Foreach(RemovePlayer));                  
				}
			}

            if (DateTime.Now > _endTime)
            {
                _startTime = _startTime.AddHours(24D);
                _endTime = _endTime.AddHours(24D);
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
            this._maps.Where(m => m.Value.RvRTerritory != null).Foreach(m => {
                ((LordRvR)m.Value.RvRTerritory.Boss).StartRvR();
                TerritoryManager.ClearEmblem(m.Value.RvRTerritory, m.Value.RvRTerritory.Boss);
            });
        
			return true;
		}

		public bool Close()
		{
			if (!_isOpen)
				return false;
			_isOpen = false;
			_isForcedOpen = false;

            this._maps.Where(m => m.Value.RvRTerritory != null).Foreach(m => {
                ((LordRvR)m.Value.RvRTerritory.Boss).StopRvR();
                m.Value.RvRTerritory.Reset();
                TerritoryManager.ClearEmblem(m.Value.RvRTerritory, m.Value.RvRTerritory.Boss);
            });

            this._maps.Values.GroupBy(v => v.Location.RegionID).ForEach(region => {
                WorldMgr.GetClientsOfRegion(region.Key).Where(player => player.Player != null).Foreach(RemovePlayer);
                GameServer.Database.SelectObjects<DOLCharacters>("Region = " + region.Key).Foreach(RemovePlayer);
            }); 

			return true;
		}

		public bool AddPlayer(GamePlayer player)
		{
			if (!_isOpen || player.Level < 20)
				return false;
			if (player.Client.Account.PrivLevel == (uint)ePrivLevel.GM)
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

                if (isAdded)
                {
                    player.IsInRvR = true;
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
                //if (player.Level >= 20 && player.Level < 26)
                //{
                //    key = "RvR-Novice-" + realm;
                //    if (!_maps.ContainsKey(key))
                //    {
                //        throw new KeyNotFoundException(key);
                //    }                 
                //}
                if (player.Level >= 20 && player.Level < 29)
                {
                    key = "RvR-Debutant-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }                   
                }
                else if (player.Level >= 29 && player.Level < 38)
                {
                    key = "RvR-Standard-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }                 
                }
                else if (player.Level >= 38 && player.Level < 46)
                {
                    key = "RvR-Expert-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }                 
                }
                else if (player.Level >= 46)
                {
                    key = "RvR-Master-" + realm;
                    if (!_maps.ContainsKey(key))
                    {
                        throw new KeyNotFoundException(key);
                    }                   
                }
                //else if (player.Level >= 50 && player.IsRenaissance)
                //{
                //    //RvR DIVINITÉS acessible UNIQUEMENT aux joueurs « IsRenaissance » Level50
                //    key = "RvR-Divine-" + realm;
                //    if (!_maps.ContainsKey(key))
                //    {
                //        throw new KeyNotFoundException(key);
                //    }                 
                //}

                player.MoveTo(_maps[key].Location);
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
            player.IsInRvR = false;
			if (player.Client.Account.PrivLevel == (uint)ePrivLevel.GM)
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

        public IList<string> GetStatistics(GamePlayer player)
		{
            if (!IsInRvr(player))
			{
                return new string[] { "Vous n'etes pas dans un RvR actuellement." };
            }

            if (DateTime.Now.Subtract(_statLastCacheUpdate) >= new TimeSpan(0, 0, 30))
            {             
				_statLastCacheUpdate = DateTime.Now;
                var clients = WorldMgr.GetClientsOfRegion(player.CurrentRegionID);
				var albCount = clients.Where(c => c.Player.Realm == eRealm.Albion).Count();
				var midCount = clients.Where(c => c.Player.Realm == eRealm.Midgard).Count();
				var hibCount = clients.Where(c => c.Player.Realm == eRealm.Hibernia).Count();

                long prAlb = clients.Where(c => c.Player.Realm == eRealm.Albion).Sum(c => c.Player.Guild.RealmPoints);
                long prHib = clients.Where(c => c.Player.Realm == eRealm.Hibernia).Sum(c => c.Player.Guild.RealmPoints);
                long prMid = clients.Where(c => c.Player.Realm == eRealm.Midgard).Sum(c => c.Player.Guild.RealmPoints);

                var maps = this._maps.Values.Where(m => m.RvRTerritory != null && m.Location.RegionID.Equals(player.CurrentRegionID));

                if (maps != null)
                {
                    var lords = maps.Select(r => (LordRvR)r.RvRTerritory.Boss).ToList();

                    if (lords != null)
                    {
                        this.RvrStats[player.CurrentRegionID] = new List<string>
                        {
                            "Statistiques du RvR:",
                            " - Albion: ",
                            (_isOpen ? albCount : 0) + " joueurs",
                            (_isOpen ? prAlb : 0) + " PR",
                            " - Midgard: ",
                             (_isOpen ? midCount : 0) + " joueurs",
                              (_isOpen ? prMid : 0) + " PR",
                            " - Hibernia: ",
                             (_isOpen ? hibCount : 0) + " joueurs",
                              (_isOpen ? prHib : 0) + " PR",
                            "",
                            " - Total: ",
                            (IsOpen ? clients.Count : 0) + " joueurs",
                            (IsOpen ? prAlb + prMid + prHib : 0) + " PR",
                            "",
                            string.Join("\n", lords.Select(l => l.GetScores())),
                            "",
                            "Le rvr est " + (_isOpen ? "ouvert" : "fermé") + ".",
                            "(Mise à jour toutes les 30 secondes)"

                        };
                    }
                }
			}
            return this.RvrStats[player.CurrentRegionID];
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
            return _maps.Values.Any(v => v.Location.RegionID.Equals(id));
		}

		private static void _MessageToLiving(GameLiving living, string message)
		{
			if (living is GamePlayer)
				((GamePlayer)living).Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
		}
	}

    public class RvRMap
    {
        public RvRTerritory RvRTerritory { get; set; }
        public GameLocation Location { get; set; }
    }
}
