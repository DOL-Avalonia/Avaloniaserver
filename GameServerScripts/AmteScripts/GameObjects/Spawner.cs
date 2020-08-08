using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.Styles;
using DOL.MobGroups;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    /// <summary>
    /// This Class spanws Mob and add them to a GroupMob
    /// </summary>
    public class Spawner
        : AmteMob
    {
        private readonly string inactiveDefaultGroupStatusAddsKey = "Spawner_inactive_adds";
        private readonly string activeDefaultGroupStatusAddsKey = "Spawner_active_adds";
        public string inactiveGroupStatusAddsKey;
        private string activeGroupStatusAddsKey;
        private string dbId;
        private bool hasLoadedAdd;
        private string addsGroupmobId;
        private int percentLifeAddsActivity;
        private bool isAggroType;
        private int npcTemplate1;
        private int npcTemplate2;
        private int npcTemplate3;
        private int npcTemplate4;
        private int addsRespawnCountTotal;
        private int addsRespawnCurrentCount;


        public Spawner()
            : base()
        {
        }

        public Spawner(INpcTemplate template)
            : base(template)
        {
        }

        public string InactiveGroupStatusAddsKey
        {
            get
            {
                if (inactiveGroupStatusAddsKey != null)
                {
                    return inactiveGroupStatusAddsKey;
                }

                return inactiveDefaultGroupStatusAddsKey;
            }
        }

        public string ActiveGroupStatusAddsKey
        {
            get
            {
                if (activeGroupStatusAddsKey != null)
                {
                    return activeGroupStatusAddsKey;
                }

                return activeDefaultGroupStatusAddsKey;
            }
        }


        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            var result = GameServer.Database.SelectObjects<SpawnerTemplate>("MobID = @MobID", new QueryParameter("MobID", obj.ObjectId));
            if (result != null)
            {
                var db = result.FirstOrDefault();

                if (db != null)
                {
                    this.dbId = db.ObjectId;
                    this.percentLifeAddsActivity = db.PercentLifeAddsActivity;
                    this.isAggroType = db.IsAggroType;
                    this.npcTemplate1 = db.NpcTemplate1;
                    this.npcTemplate2 = db.NpcTemplate2;
                    this.npcTemplate3 = db.NpcTemplate3;
                    this.npcTemplate4 = db.NpcTemplate4;
                    this.addsRespawnCountTotal = db.AddsRespawnCount;
                    this.addsRespawnCurrentCount = 0;

                    if (db.ActiveStatusId != null)
                        this.activeGroupStatusAddsKey = db.ActiveStatusId;

                    if (db.InactiveStatusId != null)
                        this.inactiveGroupStatusAddsKey = db.InactiveStatusId;
                }
            }
        }

        public override void SaveIntoDatabase()
        {
            base.SaveIntoDatabase();

            var result = GameServer.Database.SelectObjects<SpawnerTemplate>("MobID = @MobID", new QueryParameter("MobID", this.dbId));

            if (result != null)
            {
                var db = result.FirstOrDefault();

                if (db != null)
                {
                    db.IsAggroType = this.isAggroType;
                    db.NpcTemplate1 = this.npcTemplate1;
                    db.NpcTemplate2 = this.npcTemplate2;
                    db.NpcTemplate3 = this.npcTemplate3;
                    db.NpcTemplate4 = this.npcTemplate4;                 
                    db.AddsRespawnCount = this.addsRespawnCountTotal;
                    db.PercentLifeAddsActivity = this.percentLifeAddsActivity;

                    if (!this.ActiveGroupStatusAddsKey.Equals(this.activeDefaultGroupStatusAddsKey))
                    {
                        db.ActiveStatusId = this.ActiveGroupStatusAddsKey;
                    }

                    if (!this.InactiveGroupStatusAddsKey.Equals(this.inactiveDefaultGroupStatusAddsKey))
                    {
                        db.InactiveStatusId = this.InactiveGroupStatusAddsKey;
                    }

                    GameServer.Database.SaveObject(db);
                }
            }
        }


        private void ClearOldMobs()
        {
            if (this.hasLoadedAdd && MobGroupManager.Instance.Groups.ContainsKey(this.addsGroupmobId))
            {
                //handle repop
                foreach (var mob in MobGroupManager.Instance.Groups[this.addsGroupmobId].NPCs)
                {
                    mob.RemoveFromWorld();
                    mob.Delete();
                }

                MobGroupManager.Instance.RemoveGroupsAndMobs(this.addsGroupmobId, true);
                this.hasLoadedAdd = false;
            }
        }


        private void InstanciateMobs()
        {     

            GameNPC npc1 = null;
            GameNPC npc2 = null;
            GameNPC npc3 = null;
            GameNPC npc4 = null;

            var template1 = NpcTemplateMgr.GetTemplate(npcTemplate1);
            var template2 = NpcTemplateMgr.GetTemplate(npcTemplate2);
            var template3 = NpcTemplateMgr.GetTemplate(npcTemplate3);
            var template4 = NpcTemplateMgr.GetTemplate(npcTemplate4);

            foreach (var asm in ScriptMgr.GameServerScripts)
            {
                if (npc1 == null && template1 != null)
                {
                    try
                    {
                        npc1 = asm.CreateInstance(template1.ClassType, false) as GameNPC;
                    }
                    catch {  }
                }

                if (npc2 == null && template2 != null)
                {
                    try
                    {
                        npc2 = asm.CreateInstance(template2.ClassType, false) as GameNPC;
                    }
                    catch { }
                }

                if (npc3 == null && template3 != null)
                {
                    try
                    {
                        npc3 = asm.CreateInstance(template3.ClassType, false) as GameNPC;
                    }
                    catch { }
                }

                if (npc4 == null && template4 != null)
                {
                    try
                    {
                        npc4 = asm.CreateInstance(template4.ClassType, false) as GameNPC;
                    }
                    catch { }
                }
            }
            bool isXOffset = false;
            bool isPositiveOffset = false;
            List<GameNPC> npcs = new List<GameNPC>();
            if (npc1 != null)
            {
                npc1.LoadTemplate(template1);
                SetPositionAndLoad(npc1, isXOffset, isPositiveOffset);
                isXOffset = !isXOffset;
                isPositiveOffset = !isPositiveOffset;
                npcs.Add(npc1);
            }

            if (npc2 != null)
            {
                npc2.LoadTemplate(template2);
                SetPositionAndLoad(npc2, isXOffset, isPositiveOffset);
                isXOffset = !isXOffset;
                isPositiveOffset = !isPositiveOffset;
                npcs.Add(npc2);
            }       

            if (npc3 != null)
            {
                npc3.LoadTemplate(template3);
                SetPositionAndLoad(npc3, isXOffset, isPositiveOffset);
                isXOffset = !isXOffset;
                isPositiveOffset = !isPositiveOffset;
                npcs.Add(npc3);
            }           

            if (npc4 != null)
            {
                npc4.LoadTemplate(template4);
                SetPositionAndLoad(npc4, isXOffset, isPositiveOffset);
                npcs.Add(npc4);
            }          
            
            this.hasLoadedAdd = true;
            this.AddToMobGroup(npcs);
        }

        private void AddToMobGroup(IEnumerable<GameNPC> npcs)
        {
            this.addsGroupmobId = "spwn_" + (this.dbId != null ? this.dbId.Substring(0, 8) : Guid.NewGuid().ToString().Substring(0, 8));
            foreach (var npc in npcs)
            {
                MobGroupManager.Instance.AddMobToGroup(npc, this.addsGroupmobId, true);
            }

            GroupMobStatusDb status;

            if (this.percentLifeAddsActivity == 0)
            {
                status = this.GetActiveStatus();
            }
            else
            {
                status = this.GetInativeStatus();
            }      

            MobGroupManager.Instance.Groups[this.addsGroupmobId].SetGroupInfo(status, true);
        }

        private void SetPositionAndLoad(GameNPC npc, bool isXOffset, bool isPositiveOffset)
        {
            npc.LoadedFromScript = true;
            npc.X = this.X + (isXOffset ? (isPositiveOffset ? WorldMgr.GIVE_ITEM_DISTANCE : WorldMgr.GIVE_ITEM_DISTANCE * -1) : 0);
            npc.Y = this.Y + (!isXOffset ? (isPositiveOffset ? WorldMgr.GIVE_ITEM_DISTANCE : WorldMgr.GIVE_ITEM_DISTANCE * -1) : 0);
            npc.Z = this.Z;
            npc.Heading = this.Heading;
            npc.RespawnInterval = -1;
            npc.CurrentRegion = WorldMgr.GetRegion(this.CurrentRegionID);
            npc.CurrentRegionID = this.CurrentRegionID;
            npc.AddToWorld();
        }

        public override void StartAttack(GameObject target)
        {
            base.StartAttack(target);

            if (this.isAggroType && !hasLoadedAdd)
            {   
                this.InstanciateMobs();
            }
        }

        public override void TakeDamage(AttackData ad)
        {
            base.TakeDamage(ad);

            if (!this.isAggroType && !this.hasLoadedAdd)
            {
                this.InstanciateMobs();
            }

            if (this.percentLifeAddsActivity == 0 || this.HealthPercent <= this.percentLifeAddsActivity)
            {
                MobGroupManager.Instance.Groups[this.addsGroupmobId].SetGroupInfo(this.GetActiveStatus(), true);
            }
        }


        public void OnGroupMobDead(DOLEvent e, object sender, EventArgs arguments)
        {
            //check group
            MobGroup senderGroup = sender as MobGroup;

            if (senderGroup != null && senderGroup.GroupId.Equals(this.addsGroupmobId))
            {
                //own group is dead
                //Check if group can respawn
                if (this.addsRespawnCountTotal > 0 && this.addsRespawnCurrentCount < this.addsRespawnCountTotal)
                {
                    this.addsRespawnCurrentCount++;
                    foreach (var npc in MobGroupManager.Instance.Groups[this.addsGroupmobId].NPCs)
                    {
                        npc.RespawnInterval = 2000;
                        npc.StartRespawn();
                        npc.RespawnInterval = -1;
                    }
                }
            }
        }


        public override void Die(GameObject killer)
        {
            base.Die(killer);
            MobGroupManager.Instance.Groups[this.addsGroupmobId].NPCs.ForEach(n => n.Die(killer));
        }


        private GroupMobStatusDb GetInativeStatus()
        {
            var result = GameServer.Database.SelectObjects<GroupMobStatusDb>("GroupStatusId = @GroupStatusId", new QueryParameter("GroupStatusId", this.InactiveGroupStatusAddsKey));

            if (result != null && result.Any())
            {
                var inactiveStatus = result.FirstOrDefault();  
                return inactiveStatus;
            }
            else
            {
                //Default
                bool insertDefault = false;
                var inactiveDefaultList = GameServer.Database.SelectObjects<GroupMobStatusDb>("GroupStatusId = @GroupStatusId", new QueryParameter("GroupStatusId", this.inactiveDefaultGroupStatusAddsKey));
                GroupMobStatusDb inactiveStatus = null;

                if (inactiveDefaultList != null)
                {
                    var inactiveDefault = inactiveDefaultList.FirstOrDefault();

                    if (inactiveDefault != null)
                    {
                        inactiveStatus = inactiveDefault;
                    }
                    else
                    {
                        insertDefault = true;
                    }
                }
                else
                {
                    insertDefault = true;
                }

                if (insertDefault)
                {
                    eFlags f = eFlags.PEACE | eFlags.CANTTARGET;

                    inactiveStatus = new GroupMobStatusDb()
                    {
                        Flag = (int)f,
                        SetInvincible = true.ToString(),
                        GroupStatusId = this.inactiveGroupStatusAddsKey
                    };
                    GameServer.Database.AddObject(inactiveStatus);
                }               
             
                return inactiveStatus;
            }
        }


        public override bool AddToWorld()
        {
            base.AddToWorld();          

            //Handle repop         
            this.ClearOldMobs();

            //reset adds currentCount respawn
            this.addsRespawnCurrentCount = 0;

            //register handler
            GameEventMgr.AddHandler(GameEvents.GroupMobEvent.MobGroupDead, this.OnGroupMobDead);

            return true;
        }


        public override bool RemoveFromWorld()
        {
            GameEventMgr.RemoveHandler(GameEvents.GroupMobEvent.MobGroupDead, this.OnGroupMobDead);
            return base.RemoveFromWorld();
        }


        private GroupMobStatusDb GetActiveStatus()
        {
            var result = GameServer.Database.SelectObjects<GroupMobStatusDb>("GroupStatusId = @GroupStatusId", new QueryParameter("GroupStatusId", this.ActiveGroupStatusAddsKey));

            if (result != null && result.Any())
            {
                return result.FirstOrDefault();
            }
            else
            {

                GroupMobStatusDb activeStatus = null;

                var activeDefaultList = GameServer.Database.SelectObjects<GroupMobStatusDb>("GroupStatusId = @GroupStatusId", new QueryParameter("GroupStatusId", this.activeDefaultGroupStatusAddsKey));

                bool insertActive = false;

                if (activeDefaultList != null)
                {
                    var activeDefault = activeDefaultList.FirstOrDefault();

                    if (activeDefault != null)
                    {
                        activeStatus = activeDefault;
                    }
                    else
                    {
                        insertActive = true;
                    }
                }
                else
                {
                    insertActive = true;
                }

                if (insertActive)
                {

                    activeStatus = new GroupMobStatusDb()
                    {
                        SetInvincible = false.ToString(),
                        Flag = 0,
                        GroupStatusId = this.activeGroupStatusAddsKey
                    };
                    GameServer.Database.AddObject(activeStatus);
                }

                return activeStatus;
            }
        }
    }
}
