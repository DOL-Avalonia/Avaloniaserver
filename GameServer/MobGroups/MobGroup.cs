using DOL.GS;
using DOL.GS.Quests;
using DOLDatabase.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.GS.GameNPC;

namespace DOL.MobGroups
{
    public class MobGroup
    {
        private MobGroupInfo originalGroupInfo;

        public MobGroup(string id)
        {
            this.GroupId = id;
            this.NPCs = new List<GameNPC>();
            this.GroupInfos = new MobGroupInfo();
        }
       
        public MobGroup(GroupMobDb db, GroupMobStatusDb groupInteract, GroupMobStatusDb originalStatus)
        {
            this.InternalId = db.ObjectId;
            this.GroupId = db.GroupId;
            this.SlaveGroupId = db.SlaveGroupId;
            this.IsQuestConditionFriendly = db.IsQuestConditionFriendly;
            this.CompletedQuestID = db.CompletedQuestID;
            this.ComletedQuestCount = db.ComletedQuestCount;
            this.NPCs = new List<GameNPC>();
            this.GroupInfos = new MobGroupInfo()
            {
                Effect = db.Effect != null ? int.TryParse(db.Effect, out int effect) ? effect : (int?)null : (int?)null,
                Flag = db.Flag != null ? Enum.TryParse(db.Flag, out eFlags fl) ? fl : (eFlags?)null : (eFlags?)null,
                IsInvincible = db.IsInvincible != null ? bool.TryParse(db.IsInvincible, out bool dbInv) ? dbInv : (bool?)null : (bool?)null,
                Model = db.Model != null ? int.TryParse(db.Model, out int model) ? model : (int?)null : (int?)null,
                Race = db.Race != null ? Enum.TryParse(db.Race, out eRace race) ? race : (eRace?)null : (eRace?)null,
                VisibleSlot = db.VisibleSlot != null ? byte.TryParse(db.VisibleSlot, out byte slot) ? slot : (byte?)null : (byte?)null
            };

            this.mobGroupOriginFk = originalStatus?.GroupStatusId;
            this.originalGroupInfo = GetMobInfoFromSource(originalStatus);
            this.SetGroupInteractions(groupInteract);
        }

        private static MobGroupInfo GetMobInfoFromSource(GroupMobStatusDb source)
        {
            return source == null ? null : new MobGroupInfo()
            {
                Effect = source.Effect != null ? int.TryParse(source.Effect, out int grEffect) ? grEffect : (int?)null : (int?)null,
                Flag = source.Flag != null ? Enum.TryParse(source.Flag, out eFlags groupFlag) ? groupFlag : (eFlags?)null : (eFlags?)null,
                IsInvincible = source.SetInvincible != null ? bool.TryParse(source.SetInvincible, out bool inv) ? inv : (bool?)null : (bool?)null,
                Model = source.Model != null ? int.TryParse(source.Model, out int grModel) ? grModel : (int?)null : (int?)null,
                Race = source.Race != null ? Enum.TryParse(source.Race, out eRace grRace) ? grRace : (eRace?)null : (eRace?)null,
                VisibleSlot = source.VisibleSlot != null ? byte.TryParse(source.VisibleSlot, out byte grSlot) ? grSlot : (byte?)null : (byte?)null
            };
        }

        /// <summary>
        /// Is this npc-player relation allows Friendly interact 
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsQuestFriendly(GameNPC npc, GamePlayer player)
        {
            if (npc.CurrentGroupMob != null && npc.CurrentGroupMob.CompletedQuestID > 0 && npc.CurrentGroupMob.ComletedQuestCount > 0)
            {
                DataQuest finishedQuest = null;

                foreach (DataQuest q in player.QuestListFinished.Where(q => q is DataQuest))
                {
                    if (q != null && q.Id.Equals(npc.CurrentGroupMob.CompletedQuestID))
                    {
                        finishedQuest = q;
                        break;
                    }
                }

                if (finishedQuest != null && finishedQuest.Count >= npc.CurrentGroupMob.ComletedQuestCount)
                {
                    return npc.CurrentGroupMob.IsQuestConditionFriendly;
                }
            }

            return false;
        } 


        /// <summary>
        /// Is NPC aggressive on Quest Assciated Condition
        /// </summary>
        /// <param name="npc"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsQuestAggresive(GameNPC npc, GamePlayer player)
        {
            if (npc.CurrentGroupMob != null && npc.CurrentGroupMob.CompletedQuestID > 0 && npc.CurrentGroupMob.ComletedQuestCount > 0)
            {
                DataQuest finishedQuest = null;

                foreach (DataQuest q in player.QuestListFinished.Where(q => q is DataQuest))
                {
                    if (q != null && q.Id.Equals(npc.CurrentGroupMob.CompletedQuestID))
                    {
                        finishedQuest = q;
                        break;
                    }
                }

                if (finishedQuest != null && finishedQuest.Count >= npc.CurrentGroupMob.ComletedQuestCount)
                {
                    return !npc.CurrentGroupMob.IsQuestConditionFriendly;
                }
            }

            return false;
        }


        private static MobGroupInfo CopyGroupInfo(MobGroupInfo copy)
        {
            return copy == null ? null : new MobGroupInfo()
            {
                Effect = copy.Effect,
                Flag = copy.Flag,
                IsInvincible = copy.IsInvincible,
                Model = copy.Model,
                Race = copy.Race,
                VisibleSlot = copy.VisibleSlot
            };
        }

        public string mobGroupInterfactFk
        {
            get;
            private set;
        }

        public string mobGroupOriginFk
        {
            get;
            private set;
        }

        public string InternalId
        {
            get;
            set;
        }

        public string GroupId
        {
            get;
            set;
        }

        public string SlaveGroupId
        {
            get;
            set;
        }

        public bool IsQuestConditionFriendly
        {
            get;
            set;
        }
      
        public int CompletedQuestID
        {
            get;
            set;
        }
      
        public int ComletedQuestCount
        {
            get;
            set;
        }

        public MobGroupInfo GroupInfos
        {
            get;
            set;
        }

        public List<GameNPC> NPCs
        {
            get;
            set;
        }

        public MobGroupInfo GroupInteractions
        {
            get;
            set;
        }

        public void SetGroupInteractions(GroupMobStatusDb groupInteract)
        {
            this.mobGroupInterfactFk = groupInteract?.GroupStatusId;
            this.GroupInteractions = GetMobInfoFromSource(groupInteract);
        }

        public void SetGroupInfo(GroupMobStatusDb status)
        {
            this.GroupInfos = GetMobInfoFromSource(status);
            this.mobGroupOriginFk = status?.GroupStatusId;
            this.originalGroupInfo = GetMobInfoFromSource(status);
            this.ApplyGroupInfos();
        }

        public void ApplyGroupInfos()
        {          
            this.NPCs.ForEach(n => n.Flags = this.GroupInfos.Flag.HasValue ? this.GroupInfos.Flag.Value : (eFlags)n.FlagsDb);            

            if (this.GroupInfos.Model.HasValue)
            {
                this.NPCs.ForEach(n => n.Model = (ushort)this.GroupInfos.Model.Value);
            }
            else
            {
                this.NPCs.ForEach(n => n.Model = n.ModelDb);
            }

            if (this.GroupInfos.Race.HasValue)
            {
                this.NPCs.ForEach(n => n.Race = (short)this.GroupInfos.Race.Value);
            }
            else
            {
                this.NPCs.ForEach(n => n.Race = (short)n.RaceDb);
            }
           
            this.NPCs.ForEach(npc => {
                npc.VisibleActiveWeaponSlots = this.GroupInfos.VisibleSlot.HasValue ? this.GroupInfos.VisibleSlot.Value : npc.VisibleWeaponsDb;
                foreach (GamePlayer player in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    player.Out.SendLivingEquipmentUpdate(npc);
                }
            });          
        }

        public void ResetGroupInfo()
        {
            if (this.originalGroupInfo != null)
            {
                this.GroupInfos = CopyGroupInfo(this.originalGroupInfo);
                this.ApplyGroupInfos();
                this.SaveToDabatase();
            }
        }      

        public void SaveToDabatase()
        {
            GroupMobDb db = null;
            bool isNew = this.InternalId == null;

            if (this.InternalId == null)
            {
                db = new GroupMobDb();
            }
            else
            {
                db = GameServer.Database.FindObjectByKey<GroupMobDb>(this.InternalId);

                if (db == null)
                {
                    db = new GroupMobDb();
                    isNew = true;
                }
            }

            db.Flag = this.GroupInfos.Flag?.ToString();
            db.Race = this.GroupInfos.Race?.ToString();
            db.VisibleSlot = this.GroupInfos.VisibleSlot?.ToString();
            db.Effect = this.GroupInfos.Effect?.ToString();
            db.Model = this.GroupInfos.Model?.ToString();
            db.GroupId = this.GroupId;
            db.SlaveGroupId = this.SlaveGroupId;
            db.IsInvincible = this.GroupInfos.IsInvincible?.ToString();
            db.ObjectId = this.InternalId;
            db.GroupMobInteract_FK_Id = this.mobGroupInterfactFk;
            db.GroupMobOrigin_FK_Id = this.mobGroupOriginFk;
            db.ComletedQuestCount = this.ComletedQuestCount;
            db.CompletedQuestID = this.CompletedQuestID;
            db.IsQuestConditionFriendly = this.IsQuestConditionFriendly;
            
            if (isNew)
            {
                if (GameServer.Database.AddObject(db))
                {
                    this.InternalId = db.ObjectId;
                }
            }
            else
            {
                GameServer.Database.SaveObject(db);
            }
        }
    }
}
