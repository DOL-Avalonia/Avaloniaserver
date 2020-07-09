using DOL.GS;
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
        private string mobGroupInterfactFk;

        public MobGroup(string id)
        {
            this.GroupId = id;
            this.NPCs = new List<GameNPC>();
            this.GroupInfos = new MobGroupInfo();
        }
       
        public MobGroup(GroupMobDb db, GroupMobInteract groupInteract)
        {
            this.InternalId = db.ObjectId;
            this.GroupId = db.GroupId;
            this.SlaveGroupId = db.SlaveGroupId;
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

            this.originalGroupInfo = CopyGroupInfo(this.GroupInfos);
            this.SetGroupInteractions(groupInteract);
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

        public void SetGroupInteractions(GroupMobInteract groupInteract)
        {
            this.mobGroupInterfactFk = groupInteract?.InteractId;
            this.GroupInteractions = groupInteract == null ? null : new MobGroupInfo()
            {
                Effect = groupInteract.Effect != null ? int.TryParse(groupInteract.Effect, out int grEffect) ? grEffect : (int?)null : (int?)null,
                Flag = groupInteract.Flag != null ? Enum.TryParse(groupInteract.Flag, out eFlags groupFlag) ? groupFlag : (eFlags?)null : (eFlags?)null,
                IsInvincible = groupInteract.SetInvincible != null ? bool.TryParse(groupInteract.SetInvincible, out bool inv) ? inv : (bool?)null : (bool?)null,
                Model = groupInteract.Model != null ? int.TryParse(groupInteract.Model, out int grModel) ? grModel : (int?)null : (int?)null,
                Race = groupInteract.Race != null ? Enum.TryParse(groupInteract.Race, out eRace grRace) ? grRace : (eRace?)null : (eRace?)null,
                VisibleSlot = groupInteract.VisibleSlot != null ? byte.TryParse(groupInteract.VisibleSlot, out byte grSlot) ? grSlot : (byte?)null : (byte?)null
            };
        }

        public void ResetGroupInfo()
        {
           this.GroupInfos = CopyGroupInfo(this.originalGroupInfo);
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
            db.GroupId = this.GroupId;
            db.SlaveGroupId = this.SlaveGroupId;
            db.IsInvincible = this.GroupInfos.IsInvincible?.ToString();
            db.ObjectId = this.InternalId;
            db.GroupMobInteract_FK_Id = this.mobGroupInterfactFk;
            
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
