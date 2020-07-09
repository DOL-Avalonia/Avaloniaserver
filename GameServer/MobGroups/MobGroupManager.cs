using DOL.Database;
using DOL.GS;
using DOLDatabase.Tables;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DOL.MobGroups
{
    public class MobGroupManager
    {
        private readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static MobGroupManager instance;

        public static MobGroupManager Instance => instance ?? (instance = new MobGroupManager());

        private MobGroupManager()
        {
            this.Groups = new Dictionary<string, MobGroup>();
        }

        public Dictionary<string, MobGroup> Groups
        {
            get;
        }

        public bool IsAllOthersGroupMobDead(GameNPC npc)
        {
            if (npc == null || npc.CurrentGroupMob == null)
            {
                return false;
            }

            if (!this.Groups.ContainsKey(npc.CurrentGroupMob.GroupId))
            {
                log.Warn($"Mob has a GroupMobId with a value: {npc.CurrentGroupMob} but the group is not declared");
                return false;
            }
        
            bool allDead = this.Groups[npc.CurrentGroupMob.GroupId].NPCs.All(m => !m.IsAlive);

            if (allDead)
            {
                //Handle interaction if any slave group
                this.HandleInteraction(this.Groups[npc.CurrentGroupMob.GroupId]);

                //Reset GroupInfo
                this.Groups[npc.CurrentGroupMob.GroupId].ResetGroupInfo();
            }

            return allDead;
        }

        private void HandleInteraction(MobGroup master)
        {
            if (master.SlaveGroupId != null && this.Groups.ContainsKey(master.SlaveGroupId) && master.GroupInteractions != null)
            {
                var slave = this.Groups[master.SlaveGroupId];

                if (slave.NPCs?.Any() == true)
                {
                    if (master.GroupInteractions.IsInvincible.HasValue)
                    {
                        slave.GroupInfos.IsInvincible = master.GroupInteractions.IsInvincible.Value;
                    }

                    if (master.GroupInteractions.Effect.HasValue)
                    {
                        slave.GroupInfos.Effect = master.GroupInteractions.Effect.Value;

                        slave.NPCs.ForEach(npc =>
                        {
                            foreach (GamePlayer player in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                            {
                                player.Out.SendSpellEffectAnimation(npc, npc, (ushort)master.GroupInteractions.Effect.Value, 0, false, 1);
                            }
                        });
                    }

                    if (master.GroupInteractions.Flag.HasValue)
                    {
                        slave.GroupInfos.Flag = master.GroupInteractions.Flag.Value;
                        slave.NPCs.ForEach(n => n.Flags = master.GroupInteractions.Flag.Value);
                    }

                    if (master.GroupInteractions.Model.HasValue)
                    {
                        slave.GroupInfos.Model = master.GroupInteractions.Model.Value;
                        slave.NPCs.ForEach(n => n.Model = (ushort)master.GroupInteractions.Model.Value);
                    }

                    if (master.GroupInteractions.Race.HasValue)
                    {
                        slave.GroupInfos.Race = master.GroupInteractions.Race.Value;
                        slave.NPCs.ForEach(n => n.Race = (short)master.GroupInteractions.Race.Value);
                    }

                    if (master.GroupInteractions.VisibleSlot.HasValue)
                    {
                        slave.GroupInfos.VisibleSlot = master.GroupInteractions.VisibleSlot.Value;
                        slave.NPCs.ForEach(npc => {
                            npc.VisibleActiveWeaponSlots = master.GroupInteractions.VisibleSlot.Value;
                            foreach (GamePlayer player in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                            {
                                player.Out.SendLivingEquipmentUpdate(npc);
                            } 
                        });
                    }

                    slave.SaveToDabatase();
                }
            }
        }

        public List<string> GetInfos(MobGroup mobGroup)
        {
            if (mobGroup == null)
            {
                return null;
            }

            var infos = new List<string>();
            infos.Add(" - GroupId : " + mobGroup.GroupId);
            infos.Add(" - Db Id : " + (mobGroup.InternalId ?? string.Empty));
            infos.Add(" - GroupInfos : ");
            infos.Add(" - Effect : " + mobGroup.GroupInfos.Effect);
            infos.Add(" - Flag : " + mobGroup.GroupInfos.Flag?.ToString() ?? "-") ;
            infos.Add(" - IsInvincible : " + (mobGroup.GroupInfos.IsInvincible?.ToString() ??  "-"));
            infos.Add(" - Model : " + (mobGroup.GroupInfos.Model?.ToString() ?? "-"));
            infos.Add(" - Race : " + (mobGroup.GroupInfos.Race?.ToString() ?? "-"));
            infos.Add(" - VisibleSlot : " + (mobGroup.GroupInfos.VisibleSlot?.ToString() ?? "-"));
            infos.Add("");
            infos.Add(" - InteractGroupId : " + (mobGroup.SlaveGroupId ?? "-"));
            infos.Add("");
            if (mobGroup.GroupInteractions != null)
            {
                infos.Add(" Actions on Group Killed : ");
                infos.Add(" - Set Effect : " + mobGroup.GroupInteractions.Effect);
                infos.Add(" - Set Flag : " + mobGroup.GroupInteractions.Flag?.ToString() ?? "-");
                infos.Add(" - Set IsInvincible : " + (mobGroup.GroupInteractions.IsInvincible?.ToString() ?? "-"));
                infos.Add(" - Set Model : " + (mobGroup.GroupInteractions.Model?.ToString() ?? "-"));
                infos.Add(" - Set Race : " + (mobGroup.GroupInteractions.Race?.ToString() ?? "-"));
                infos.Add(" - Set VisibleSlot : " + (mobGroup.GroupInteractions.VisibleSlot?.ToString() ?? "-"));
            }
            infos.Add("******************");
            infos.Add(" - NPC Count: " + mobGroup.NPCs.Count);
            mobGroup.NPCs.ForEach(n => infos.Add(string.Format("Name: {0} | Id: {1} | Region: {2} | Alive: {3} ", n.Name, n.ObjectID, n.CurrentRegionID, n.IsAlive)));
            return infos;
        }

        public string GetGroupIdFromMobId(string mobId)
        {
            if (mobId == null)
            {
                return null;
            }

            foreach (var group in this.Groups)
            {
                if (group.Value.NPCs.Any(npc => npc.InternalID.Equals(mobId)))
                {
                    return group.Key;
                }
            }

            return null;
        }    

        public bool RemoveGroupsAndMobs(string groupId)
        {
            if (!this.Groups.ContainsKey(groupId))
            {
                return false;
            }

            foreach (var npc in this.Groups[groupId].NPCs.ToList())
            {
               this.RemoveMobFromGroup(npc, groupId);
            }
         
            this.Groups[groupId].NPCs.RemoveAll(n => n != null);
            this.Groups.Remove(groupId);

            var all = GameServer.Database.SelectAllObjects<GroupMobDb>();

            if (all != null)
            {
                foreach (var item in all)
                {
                    GameServer.Database.DeleteObject(item);
                }
            }

            return true;
        }


        public bool LoadFromDatabase()
        {

            var groups = GameServer.Database.SelectAllObjects<GroupMobXMobs>();

            if (groups != null)
            {
                foreach(var group in groups)
                {
                    if (!this.Groups.ContainsKey(group.GroupId))
                    {
                        var groupDb = GameServer.Database.SelectObjects<GroupMobDb>("GroupId = @GroupId", new QueryParameter("GroupId", group.GroupId))?.FirstOrDefault();
                        if (groupDb != null)
                        {
                            var groupInteraction = groupDb.SlaveGroupId != null ? GameServer.Database.FindObjectByKey<GroupMobInteract>(groupDb.GroupMobInteract_FK_Id) : null;
                            this.Groups.Add(group.GroupId, new MobGroup(groupDb, groupInteraction));
                        }                           
                    }                    

                    if (WorldMgr.Regions.ContainsKey(group.RegionID))
                    {
                        var mobInWorld = WorldMgr.Regions[group.RegionID].Objects?.FirstOrDefault(o => o?.InternalID?.Equals(group.MobID) == true && o is GameNPC) as GameNPC;

                        if (mobInWorld != null && this.Groups.ContainsKey(group.GroupId))
                        {
                            if (this.Groups[group.GroupId].NPCs.FirstOrDefault(m => m.InternalID.Equals(mobInWorld.InternalID)) == null)
                            {
                                this.Groups[group.GroupId].NPCs.Add(mobInWorld);
                                mobInWorld.CurrentGroupMob = this.Groups[group.GroupId];
                            }
                        }
                    }
                }
            }

            return true;
        }

        public bool AddMobToGroup(GameNPC npc, string groupId)
        {
            if (npc == null || groupId == null)
            {
                return false;
            }

            bool isnew = false;
            if (!this.Groups.ContainsKey(groupId))
            {
                this.Groups.Add(groupId, new MobGroup(groupId));
                isnew = true;
            }


            this.Groups[groupId].NPCs.Add(npc);
            npc.CurrentGroupMob = this.Groups[groupId];            

            if (isnew)
            {
                GameServer.Database.AddObject(new GroupMobDb() 
                { 
                     GroupId = groupId
                });

                GameServer.Database.AddObject(new GroupMobXMobs()
                {
                    GroupId = groupId,
                    MobID = npc.InternalID,
                    RegionID = npc.CurrentRegionID                        
                });
            }
            else
            {
                var exists = GameServer.Database.SelectObjects<GroupMobXMobs>("MobID = @mobid", new QueryParameter("mobid", npc.InternalID))?.FirstOrDefault();

                if (exists != null)
                {
                    exists.RegionID = npc.CurrentRegionID;
                    exists.GroupId = groupId;                    
                    GameServer.Database.SaveObject(exists);
                }
                else
                {
                    GroupMobXMobs newgroup = new GroupMobXMobs()
                    {
                        MobID = npc.InternalID,
                        GroupId = groupId,
                        RegionID = npc.CurrentRegionID
                    };

                    GameServer.Database.AddObject(newgroup);                    
                }
            }

            return true;
        }


        public bool RemoveMobFromGroup(GameNPC npc, string groupId)
        {
            if (npc == null || groupId == null)
            {
                return false;
            }

            if (!this.Groups.ContainsKey(groupId))
            {
                log.Error($"Impossible to remove Group beacause inmemory Groups does not contain groupId: { groupId }");
                return false;
            }


            if (!this.Groups[groupId].NPCs.Remove(npc))
            {
                log.Error($"Impossible to remove NPC { npc.InternalID } from groupId: { groupId }");
                return false;
            }

            var grp = GameServer.Database.SelectObjects<GroupMobXMobs>("MobID = @mobid AND GroupId = @groupid", new QueryParameter[]
            {
                new QueryParameter("mobid", npc.InternalID),
                new QueryParameter("groupid", groupId)  
                
            } )?.FirstOrDefault();

            if (grp == null)
            {
                log.Error($"Impossible to remove GroupMobXMobs entry with MobId: {npc.InternalID} and groupId: { groupId }");
                this.Groups[groupId].NPCs.Add(npc);
                return false;
            }
            else
            {
                npc.CurrentGroupMob = null;
                return GameServer.Database.DeleteObject(grp);
            }
        }
    }
}
