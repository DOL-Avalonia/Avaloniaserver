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
            this.Groups = new Dictionary<string, List<GameNPC>>();
        }

        public Dictionary<string, List<GameNPC>> Groups
        {
            get;
        }

        public bool IsAllOthersGroupMobDead(GameNPC npc)
        {
            if (npc == null || npc.GroupMobId == null)
            {
                return false;
            }

            if (!this.Groups.ContainsKey(npc.GroupMobId))
            {
                log.Warn($"Mob has a GroupMobId with a value: {npc.GroupMobId} but the group is not declared");
                return false;
            }

            return this.Groups[npc.GroupMobId].All(m => !m.IsAlive);
        }


        public string GetGroupIdFromMobId(string mobId)
        {
            if (mobId == null)
            {
                return null;
            }

            foreach (var group in this.Groups)
            {
                if (group.Value.Any(npc => npc.InternalID.Equals(mobId)))
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

            foreach (var npc in this.Groups[groupId].ToList())
            {
               this.RemoveMobFromGroup(npc, groupId);
            }
         
            this.Groups[groupId].RemoveAll(n => n != null);
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
                        this.Groups.Add(group.GroupId, new List<GameNPC>());
                    }                    

                    if (WorldMgr.Regions.ContainsKey(group.RegionID))
                    {
                        var mobInWorld = WorldMgr.Regions[group.RegionID].Objects?.FirstOrDefault(o => o?.InternalID?.Equals(group.MobID) == true && o is GameNPC) as GameNPC;

                        if (mobInWorld != null)
                        {
                            if (this.Groups[group.GroupId].FirstOrDefault(m => m.InternalID.Equals(mobInWorld.InternalID)) == null)
                            {
                                this.Groups[group.GroupId].Add(mobInWorld);
                                mobInWorld.GroupMobId = group.GroupId;
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
            if (! this.Groups.ContainsKey(groupId))
            {
                this.Groups.Add(groupId, new List<GameNPC>());
                isnew = true;
            }


            this.Groups[groupId].Add(npc);
            npc.GroupMobId = groupId;            

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


            if (!this.Groups[groupId].Remove(npc))
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
                this.Groups[groupId].Add(npc);
                return false;
            }
            else
            {
                npc.GroupMobId = null;
                return GameServer.Database.DeleteObject(grp);
            }
        }
    }
}
