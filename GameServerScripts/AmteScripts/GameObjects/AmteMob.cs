using System;
using System.Collections.Generic;
using System.Linq;
using DOL.AI;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS;
using DOL.GS.Scripts;

public class AmteMob : GameNPC, IAmteNPC
{
	private readonly Dictionary<string, DBBrainsParam> _nameXcp = new Dictionary<string, DBBrainsParam>();

	private readonly AmteCustomParam _linkParam;

	public AmteMob()
	{
		SetOwnBrain(new AmteMobBrain(Brain));
        _linkParam = new AmteCustomParam(
            "link",
            () => {       
                
                if (!(Brain is AmteMobBrain))
                {
                    SetOwnBrain(new AmteMobBrain(Brain));      
                }

                return ((AmteMobBrain)Brain).AggroLink.ToString();
            },
            v => {

                if (!(Brain is AmteMobBrain))
                {
                    SetOwnBrain(new AmteMobBrain(Brain));
                }

                ((AmteMobBrain)Brain).AggroLink = int.Parse(v);                
            },
			"0");
	}

	public AmteMob(INpcTemplate npc)
		: base(npc)
	{
		SetOwnBrain(new AmteMobBrain(Brain));
		_linkParam = new AmteCustomParam(
			"link",
           () => {

               if (Brain is StandardMobBrain)
               {
                   SetOwnBrain(new AmteMobBrain(Brain));
               }

               return ((AmteMobBrain)Brain).AggroLink.ToString();
           },
            v => {

                if (Brain is StandardMobBrain)
                {
                    SetOwnBrain(new AmteMobBrain(Brain));
                }

                ((AmteMobBrain)Brain).AggroLink = int.Parse(v);
            },
            "0");
	}

	public override bool IsFriend(GameNPC npc)
	{
		if (npc.Brain is IControlledBrain)
			return GameServer.ServerRules.IsSameRealm(this, npc, true);
		if (Faction == null && npc.Faction == null)
			return npc.Name == Name || (!string.IsNullOrEmpty(npc.GuildName)  && npc.GuildName == GuildName);
		return base.IsFriend(npc);
	}

	public override void LoadFromDatabase(DataObject obj)
    {
        base.LoadFromDatabase(obj);

        if (Brain != null)
        {
            SetOwnBrain(Brain);
        }

        LoadDbBrainParam(obj.ObjectId);

        // load some stats from the npctemplate
        if (NPCTemplate != null && !NPCTemplate.ReplaceMobValues)
        {
            if (NPCTemplate.Spells != null) this.Spells = NPCTemplate.Spells;
            if (NPCTemplate.Styles != null) this.Styles = NPCTemplate.Styles;
            if (NPCTemplate.Abilities != null)
            {
                lock (m_lockAbilities)
                {
                    foreach (Ability ab in NPCTemplate.Abilities)
                        m_abilities[ab.KeyName] = ab;
                }
            }
        }
    }

    private void LoadDbBrainParam(string dataid)
    {
        var data = GameServer.Database.SelectObjects<DBBrainsParam>("MobID = @MobId", new QueryParameter("@MobId", dataid));
        for (var cp = GetCustomParam(); cp != null; cp = cp.next)
        {
            var cp1 = cp;
            var param = data.Where(o => o.Param == cp1.name).FirstOrDefault();
            if (param == null)
            {
                continue;
            }
            cp.Value = param.Value;
            if (_nameXcp.ContainsKey(cp.name))
            {
                _nameXcp[cp.name] = param;
            }
            else
            {
                _nameXcp.Add(cp.name, param);
            }
        }
    }

    public override void SaveIntoDatabase()
	{
		base.SaveIntoDatabase();

		DBBrainsParam param;
		for (var cp = GetCustomParam(); cp != null; cp = cp.next)
        {
            if (_nameXcp.TryGetValue(cp.name, out param) && param.MobID == InternalID)
            {
                param.Value = cp.Value;
                GameServer.Database.SaveObject(param);

            }
            else if (cp.defaultValue != cp.Value)
            {
                param = new DBBrainsParam
                {
                    MobID = InternalID,
                    Param = cp.name,
                    Value = cp.Value
                };
                if(_nameXcp.ContainsKey(cp.name))
                {
                    _nameXcp[cp.name] = param;
                }
                else
                {
                    _nameXcp.Add(cp.name, param);
                }
                GameServer.Database.AddObject(param);
            }
        }
			
	}

    public override void DeleteFromDatabase()
	{
		base.DeleteFromDatabase();
		_nameXcp.Values.Foreach(o => GameServer.Database.DeleteObject(o));
	}

    public override ABrain SetOwnBrain(ABrain brain)
    {
        if (this is IGuardNPC)
        {
            return base.SetOwnBrain(brain);
        }
      
        if (!(brain is AmteMobBrain))
        {
            return base.SetOwnBrain(new AmteMobBrain(brain));
        }

        return base.SetOwnBrain(brain);
    }

    public virtual AmteCustomParam GetCustomParam()
	{
		return _linkParam;
	}

	public virtual IList<string> DelveInfo()
	{
		var list = new List<string>();
		for (var cp = GetCustomParam(); cp != null; cp = cp.next)
			list.Add(" - " + cp.name + ": " + cp.Value);
		return list;
	}

    public override void CustomCopy(GameObject source)
    {
        base.CustomCopy(source);
        LoadDbBrainParam(source.InternalID);
    }
}
