using DOL.AI.Brain;
using DOL.Database;
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
        private string inactiveGroupStatusAddsKey = "Spawner_inactive_adds";
        private string activeGroupStatusAddsKey = "Spawner_active_adds";
        private string dbId;
        private bool hasLoadedAdd;
        private string addsGroupmobId;
        private int percentLifeAddsActivity;
        private bool isAggroType;
        private int npcTemplate1;
        private int npcTemplate2;
        private int npcTemplate3;
        private int npcTemplate4;

        public Spawner()
            : base()
        {
        }

        public Spawner(INpcTemplate template)
            : base(template)
        {
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
                    db.PercentLifeAddsActivity = this.percentLifeAddsActivity;
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
            this.addsGroupmobId = Guid.NewGuid().ToString();
            foreach (var npc in npcs)
            {
                MobGroupManager.Instance.AddMobToGroup(npc, this.addsGroupmobId, true);
            }

            var inactiveStatus = this.GetInativeStatus();        

            MobGroupManager.Instance.Groups[this.addsGroupmobId].SetGroupInfo(inactiveStatus);
        }

        private void SetPositionAndLoad(GameNPC npc, bool isXOffset, bool isPositiveOffset)
        {
            npc.LoadedFromScript = true;
            npc.X = this.X + (isXOffset ? (isPositiveOffset ? WorldMgr.GIVE_ITEM_DISTANCE : WorldMgr.GIVE_ITEM_DISTANCE * -1) : 0);
            npc.Y = this.Y + (!isXOffset ? (isPositiveOffset ? WorldMgr.GIVE_ITEM_DISTANCE : WorldMgr.GIVE_ITEM_DISTANCE * -1) : 0);
            npc.Z = this.Z;
            npc.Heading = this.Heading;
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
                MobGroupManager.Instance.Groups[this.addsGroupmobId].SetGroupInfo(this.GetActiveStatus());
            }
        }

        public override void Die(GameObject killer)
        {
            base.Die(killer);
            MobGroupManager.Instance.Groups[this.addsGroupmobId].NPCs.ForEach(n => n.Die(killer));
        }


        private GroupMobStatusDb GetInativeStatus()
        {
            var result = GameServer.Database.SelectObjects<GroupMobStatusDb>("GroupStatusId = @GroupStatusId", new QueryParameter("GroupStatusId", this.inactiveGroupStatusAddsKey));

            if (result != null && result.Any())
            {
                var inactiveStatus = result.FirstOrDefault();  
                return inactiveStatus;
            }
            else
            {
                eFlags f = eFlags.PEACE | eFlags.CANTTARGET;         

                var inactiveStatus = new GroupMobStatusDb()
                {
                    Flag = (int)f,
                    SetInvincible = true.ToString(),
                    GroupStatusId = this.inactiveGroupStatusAddsKey
                };
                GameServer.Database.AddObject(inactiveStatus);
                return inactiveStatus;
            }
        }


        public override bool AddToWorld()
        {
            base.AddToWorld();
            //Handle repop         
            this.ClearOldMobs();
            return true;
        }


        private GroupMobStatusDb GetActiveStatus()
        {
            var result = GameServer.Database.SelectObjects<GroupMobStatusDb>("GroupStatusId = @GroupStatusId", new QueryParameter("GroupStatusId", this.activeGroupStatusAddsKey));

            if (result != null && result.Any())
            {
                return result.FirstOrDefault();
            }
            else
            {
                var activeStatus = new GroupMobStatusDb()
                {
                    SetInvincible = false.ToString(),
                    GroupStatusId = this.activeGroupStatusAddsKey
                };
                GameServer.Database.AddObject(activeStatus);
                return activeStatus;
            }
        }
    }
}
