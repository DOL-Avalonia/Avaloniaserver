﻿/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
 
// Class dedicated to RewardQuests based off Tolakram's DataQuest system
using System;
using System.Text;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Specialized;

using DOL.Database;
using DOL.Events;
using DOL.Language;
using DOL.GS.Behaviour;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Quests
{	
	public class DQRewardQ : DataQuest
	{
		/// <summary>
		/// Declare a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        // Some variables to declare.
		protected int m_step = 1;
		protected DBDQRewardQ m_dqRewardQ = null;
		protected CharacterXDQRewardQ m_charQuest = null;
		protected GameObject m_startObject = null;
		protected GameNPC m_startNPC = null;
		protected IDQRewardQStep m_customQuestStep = null;
		protected DQRQuestGoal newgoals = null;		
		protected string collectItem = "";
		protected string m_lastErrorText = "";
		protected int firstGoals = 0;
        private eStartType startType;
		/// <summary>
		/// In order to avoid conflicts with scripted quests data quest ID's are added to this number when sending a quest ID to the client
		/// </summary>
		public const ushort DQREWARDQ_CLIENTOFFSET = 34767; // ADDED AN EXTRA 2000 TO THIS, I DONT THINK WE NEED MORE QUESTS THAN THAT     


        /// <summary>
        /// A static list of every search area for all data quests
        /// </summary>
        protected static List<KeyValuePair<int, QuestSearchArea>> m_allQuestSearchAreas = new List<KeyValuePair<int, QuestSearchArea>>();

        /// <summary>
        /// How many search areas are part of this quest
        /// </summary>
        protected int m_numSearchAreas = 0;

        /// <summary>
        /// An item given to a player when starting with a search.
        /// </summary>        
        protected List<string> m_questGoals = new List<string>(); // initial list of quest goals upon starting the quest
		protected List<DQRQuestGoal.GoalType> m_goalType = new List<DQRQuestGoal.GoalType>(); // the quest goal types kill, interact etc
		protected List<int> m_goalRepeatNo = new List<int>(); // how many times does goal need to be met ie (0/3)
		protected List<string> m_goalTargetName = new List<string>(); // the target object/NPC name for the goal	
		protected List<string> m_goalTargetText = new List<string>(); // target text, used for mob to say somethen when slain, or interact
		protected List<int> m_goalStepPosition = new List<int>(); // at what step is this goal created
		protected List<string> m_advanceTexts = new List<string>(); // whisper text needed to advance an interact goal				
		protected List<string> m_collectItems = new List<string>();	// the dummy itemtemplate a player must collect/deliver. Used for its icon image in journal
		protected List<DQRQuestGoal> m_goals = new List<DQRQuestGoal>(); // list of all the goals for this quest
		byte m_numOptionalRewardsChoice = 0; // how many optional rewards a player can choose at end of quest
		protected List<ItemTemplate> m_optionalRewards = new List<ItemTemplate>(); // itemtemplates of optional rewards
		protected List<ItemTemplate> m_optionalRewardChoice = new List<ItemTemplate>(); // itemtemplates of the chosen optional rewards upon completion
		protected int[] m_rewardItemsChosen = null; // position of reward item chosen, sent in packet
		protected List<ItemTemplate> m_finalRewards = new List<ItemTemplate>(); // standard rewards for this quest		
		protected List<string> m_questDependencies = new List<string>(); // quests that needed completion before this quest is offered
		protected List<byte> m_allowedClasses = new List<byte>(); // allowed classes for this quest		
        protected List<short> m_allowedRaces= new List<short>(); // allowed Races for this quest		
        string m_classType = ""; // the optional classtype/script that can be called to implement custom actions during the quest.
		// location info of goal to put red dot on map
		protected List<int> m_xOffset = new List<int>();
		protected List<int> m_yOffset = new List<int>();
		protected List<int> m_zoneID = new List<int>();
				
		/// <summary>
		/// Add a goal for this quest. No unique indentifier
		/// </summary>		
		protected DQRQuestGoal AddGoal(string description, DQRQuestGoal.GoalType type, int targetNumber, string questItem, string targetObject)
		{
			var goal = new DQRQuestGoal("none", this, description, type, m_goals.Count + 1, targetNumber, questItem, targetObject);
			m_goals.Add(goal);
			return goal;
		}
		
		/// <summary>
		/// Add a goal for this quest and give it a unique identifier
		/// </summary>		
		protected DQRQuestGoal AddGoal(string id, string description, DQRQuestGoal.GoalType type, int targetNumber, string questItem, string targetObject)
		{
			var goal = new DQRQuestGoal(id, this, description, type, m_goals.Count + 1, targetNumber, questItem, targetObject);
			m_goals.Add(goal);
			return goal;
		}
		
		/// <summary>
		/// Create an empty Quest
		/// </summary>
		public DQRewardQ()
			: base()
		{
		}

        /// <summary>
        /// DataQuest object used for delving RewardItems or other information
        /// </summary>        
        public DQRewardQ(DBDQRewardQ dqrewardq)
          : base()
        {
            _questPlayer = null;
            m_step = 1;
            id = dqrewardq.ID;
            m_dqRewardQ = dqrewardq;
            ParseQuestData();            
        }

		/// <summary>
		/// DataQuest object assigned to an object or NPC that is used to start or offer the quest
		/// </summary>		
		public DQRewardQ(DBDQRewardQ dqrewardq, GameObject startingObject)
		{
            _questPlayer = null;
			m_step = 1;
			m_dqRewardQ = dqrewardq;
            id = dqrewardq.ID;
            m_startObject = startingObject;
            m_lastErrorText = "";            
			ParseQuestData();
        }

		/// <summary>
		/// Dataquest that belongs to a player
		/// </summary>		
		public DQRewardQ(GamePlayer questingPlayer, DBDQRewardQ dqrewardq, CharacterXDQRewardQ charQuest)		
		{
            _questPlayer = questingPlayer;
			m_step = 1;
			m_dqRewardQ = dqrewardq;
			m_charQuest = charQuest;
            id = dqrewardq.ID;
			ParseQuestData();
			ParseDQCustomProperties();
			
			firstGoals = m_goalStepPosition.Count(x => x == 1);
			
			for (int i = 0; i < (m_DQcustomProperties.Count / 2); i++)
			{				
				if (m_collectItems.Count > 0 && m_collectItems[i] != null)
				{
					collectItem = m_collectItems[i];
				}
				else if (_stepItemTemplates.Count > 0 && _stepItemTemplates[i] != null)
				{
					collectItem = _stepItemTemplates[i];
				}
				newgoals = AddGoal(m_questGoals[i], m_goalType[i], m_goalRepeatNo[i], collectItem, m_goalTargetName[i]);
                CurrentGoal = newgoals;
			}
		}		
		
		/// <summary>
		/// This is a dataquest that belongs to a player
		/// </summary>		
		public DQRewardQ(GamePlayer questingPlayer, GameObject sourceObject, DBDQRewardQ dqrewardq, CharacterXDQRewardQ charQuest)
		{
            _questPlayer = questingPlayer;
			m_step = 1;
			m_dqRewardQ = dqrewardq;
			m_charQuest = charQuest;
            id = dqrewardq.ID;
            if (sourceObject != null)
			{
				if (sourceObject is GameNPC)
				{
					m_startNPC = sourceObject as GameNPC;
				}

				m_startObject = sourceObject;
			}

			ParseQuestData();
			ParseDQCustomProperties();
			
			firstGoals = m_goalStepPosition.Count(x => x == 1);
			
			for (int i = 0; i < firstGoals; i++)
			{				
				if (m_collectItems.Count > 0 && m_collectItems[i] != null)
				{
					collectItem = m_collectItems[i];
				}
				else if (_stepItemTemplates.Count > 0 && _stepItemTemplates[i] != null)
				{
					collectItem = _stepItemTemplates[i];
				}
				newgoals = AddGoal(m_questGoals[i], m_goalType[i], m_goalRepeatNo[i], collectItem, m_goalTargetName[i]);
                CurrentGoal = newgoals;
			}		
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(DQRewardQuestNotify));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(DQRewardQuestNotify));
		}


        public override eStartType StartType => this.startType;

		public override string TargetName => this.CurrentGoal?.TargetObject;

		public override ushort TargetRegion => this.TargetRegions.Count >= this.CurrentGoal.GoalIndex ? this.TargetRegions[this.CurrentGoal.GoalIndex - 1] : (ushort)0;


        /// <summary>
        /// Split the quest strings into individual step data
        /// It's important to remember that there must be an entry, even if empty, for each column for each step.
        /// For example; something|||something for a 4 part quest
        /// </summary>
        protected void ParseQuestData()
		{
			if (m_dqRewardQ == null)
				return;

			string lastParse = "";

			try
			{                
				string[] parse1;
				// quest goals
				lastParse = m_dqRewardQ.QuestGoals;				
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{
						string[] parse2 = str.Split(';');
						m_questGoals.Add(parse2[0]);
						m_goalStepPosition.Add(Convert.ToUInt16(parse2[1]));						
					}
				}				
				// what type of goal this is? kill, interact
				lastParse = m_dqRewardQ.GoalType;
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{
						m_goalType.Add((DQRQuestGoal.GoalType)Convert.ToByte(str));
					}
				}
				// target for the goal ie: (0/5)
				lastParse = m_dqRewardQ.GoalRepeatNo;
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{
						m_goalRepeatNo.Add(Convert.ToUInt16(str));
					}
				}

				// the name of the target in the goal
                lastParse = m_dqRewardQ.GoalTargetName;
                if (!string.IsNullOrEmpty(lastParse))
                {
                    parse1 = lastParse.Split('|');
                    foreach (string str in parse1)
                    {
                        if (str == string.Empty)
                        {
                            // if there's not npc for this step then empty is ok
                            m_goalTargetName.Add(string.Empty);
                            TargetRegions.Add(0);
                        }
                        else
                        {
                            string[] parse2 = str.Split(';');
                            m_goalTargetName.Add(parse2[0]);
                            TargetRegions.Add(Convert.ToUInt16(parse2[1]));
                        }
                    }
                }

                // the text that npc / mob says after interact or die
                lastParse = m_dqRewardQ.GoalTargetText;
                if (!string.IsNullOrEmpty(lastParse))
                {
                    parse1 = lastParse.Split('|');
                    foreach (string str in parse1)
                    {
                        m_goalTargetText.Add(str);
                    }
                }

				// the text that must be whispered to the target to advance the quest
				lastParse = m_dqRewardQ.AdvanceText;
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{
						m_advanceTexts.Add(str);
					}
				}
				// the dummy itemtemplates used to collect/deliver items in journal
				lastParse = m_dqRewardQ.CollectItemTemplate;
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{                     
                        m_collectItems.Add(str);                         
					}
				}			
				// list of optional rewards for this quest
				lastParse = m_dqRewardQ.OptionalRewardItemTemplates;
				if (!string.IsNullOrEmpty(lastParse))
				{
					m_numOptionalRewardsChoice = Convert.ToByte(lastParse.Substring(0, 1));
					parse1 = lastParse.Substring(1).Split('|');
					foreach (string str in parse1)
					{
						if (!string.IsNullOrEmpty(str))
						{
							ItemTemplate item = GameServer.Database.FindObjectByKey<ItemTemplate>(str);
							if (item != null)
							{
								m_optionalRewards.Add(item);
							}
							else
							{
                                string errorText = string.Format("DataQuest: Optional reward ItemTemplate not found: {0}", str);
								log.Error(errorText);
                                m_lastErrorText += " " + errorText;
							}
						}
					}
				}
				// list of standard rewards for this quest
				lastParse = m_dqRewardQ.FinalRewardItemTemplates;
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{
						ItemTemplate item = GameServer.Database.FindObjectByKey<ItemTemplate>(str);
						if (item != null)
						{
							m_finalRewards.Add(item);
						}
						else
						{
                            string errorText = string.Format("DataQuest: Final reward ItemTemplate not found: {0}", str);
                            log.Error(errorText);
                            m_lastErrorText += " " + errorText;
                        }
					}
				}
				// quest dependency required to start this quest
				lastParse = m_dqRewardQ.QuestDependency;
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{
						if (str != "")
						{
							m_questDependencies.Add(str);
						}
					}
				}
				// allowed classes who can be offered this quest
				lastParse = m_dqRewardQ.AllowedClasses;
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{
						m_allowedClasses.Add(Convert.ToByte(str));
					}
				}

                // allowed Races who can be offered this quest
                lastParse = m_dqRewardQ.AllowedRaces;
                if (!string.IsNullOrEmpty(lastParse))
                {
                    parse1 = lastParse.Split('|');
                    foreach (string str in parse1)
                    {
                        m_allowedRaces.Add(Convert.ToInt16(str));
                    }
                }

                // quest classtype, used if tying to implement custom code to execute with this quest
                lastParse = m_dqRewardQ.ClassType;
				if (!string.IsNullOrEmpty(lastParse))
				{
					m_classType = lastParse;					
				}
				// xloc for questgoal dot on map
				lastParse = m_dqRewardQ.XOffset;
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{
                        if (!string.IsNullOrEmpty(str))
                        {
                            m_xOffset.Add(Convert.ToInt32(str));
                        }
					}
				}
				// yloc for questgoal dot on map
				lastParse = m_dqRewardQ.YOffset;
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{
                        if (!string.IsNullOrEmpty(str))
                        {
                            m_yOffset.Add(Convert.ToInt32(str));
                        }            
					}
				}
				// zoneid for questgoal dot on map
				lastParse = m_dqRewardQ.ZoneID;
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{
                        if (!string.IsNullOrEmpty(str))
                        {
                            m_zoneID.Add(Convert.ToInt32(str));
                        }              
					}
				}

				lastParse = m_dqRewardQ.StepText;
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{
						StepTexts.Add(str);
					}
				}

				lastParse = m_dqRewardQ.StepItemTemplates;
				if (!string.IsNullOrEmpty(lastParse))
				{
					parse1 = lastParse.Split('|');
					foreach (string str in parse1)
					{
						_stepItemTemplates.Add(str);
					}
				}

				if (!string.IsNullOrEmpty(m_dqRewardQ.Reputation))
                {
					if (int.TryParse(m_dqRewardQ.Reputation, out int parsed))
                    {
						if (parsed < 0)
                        {
							this.Reputation = parsed;
                        }
                        else
                        {
							this.Reputation = 0;
                        }
                    }
                }
			}			
			
			catch (Exception ex)
			{
                string errorText = "Error parsing quest data for " + m_dqRewardQ.QuestName + " (" + m_dqRewardQ.ID + "), last string to parse = '" + lastParse + "'.";
				log.Error(errorText, ex);
                m_lastErrorText += " " + errorText + " " + ex.Message;
			}
		}		
		
		/// <summary>
		/// The current goal that is being checked for data
		/// </summary>
		public DQRQuestGoal CurrentGoal { get; private set; }
		
		/// <summary>
		/// Checks if all the goals for this quest are completed
		/// </summary>
		/// <returns>true if all goals are completed</returns>
		public bool CheckGoalsCompleted()
		{
			foreach (DQRQuestGoal goal in Goals)
			{
				if (goal.Type == DQRQuestGoal.GoalType.InteractFinish || goal.Type == DQRQuestGoal.GoalType.DeliverFinish)
				{
					return true;
				}
				if (!goal.IsAchieved)
				{
					return false;
				}
			}
			
			return true;
		}
		
		/// <summary>
		/// Checks if all the goals for this quest are completed
		/// </summary>
		/// <returns>true if all goals are completed</returns>
		public bool GoalsCompleted()
		{
			foreach (DQRQuestGoal goal in Goals)
			{
				if (!goal.IsAchieved)
				{
					return false;
				}
			}
			++Step;
			// all current goals are completed, increase step so we can see if there are more Steps
			if (Step <= StepCount)
			{
				AddNewGoals();
				return false;
			}
			
			return true;
		}
		
		/// <summary>
		/// Checks for new goals and adds them to players journal for that quest
		/// </summary>
		public void AddNewGoals()
		{
			try
			{
				foreach (int nextGoals in m_goalStepPosition)			
				{				
					if (nextGoals == Step)
					{
						int index = m_goalStepPosition.IndexOf(nextGoals);
						collectItem = "";
						if (m_collectItems.Count > index && m_collectItems[index] != null)
						{
							collectItem = m_collectItems[index];
						}
						else if (_stepItemTemplates.Count > index && _stepItemTemplates[index] != null)
						{
							collectItem = _stepItemTemplates[index];
						}

						newgoals = AddGoal(m_questGoals[index], m_goalType[index], m_goalRepeatNo[index], collectItem, m_goalTargetName[index]);
						CurrentGoal = newgoals;// we do this , to check for an interactfinish goaltype
					}
				}
                _questPlayer.Out.SendQuestListUpdate();
                _questPlayer.Out.SendMessage("To see the next step, open your Journal", eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
                _questPlayer.Out.SendMessage("To see the next step, open your Journal", eChatType.CT_Important, eChatLoc.CL_SystemWindow);	
				
				if (m_startNPC != null)
				{
					UpdateQuestIndicator(m_startNPC, _questPlayer);
				}
	
				foreach (GameNPC npc in _questPlayer.GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE(_questPlayer.CurrentRegion)))
				{
					UpdateQuestIndicator(npc, _questPlayer);
				}
			}
			catch (Exception ex)
			{
				log.Error("error adding extra goals", ex);
			}
		}
		
		/// <summary>
		/// Name of this quest to show in quest log
		/// </summary>
		public override string Name
		{
			get	{ return m_dqRewardQ.QuestName; }
		}		
						
		/// <summary>
		/// Get the quest 'story' formatted with personalized messaging in the packet
		/// </summary>
		public string Story
		{
			get { return m_dqRewardQ.StoryText; }
		}
		
		/// <summary>
        /// Additional text sent to the player upon accepting the quest
        /// </summary>
        public string AcceptText
        {
            get { return m_dqRewardQ.AcceptText; }
        }
		
		/// <summary>
		/// List of all goals for this quest
		/// </summary>
		public List<DQRQuestGoal> Goals
		{
			get { return m_goals; }
		}
		
		/// <summary>
		/// List of final rewards for this quest
		/// </summary>
		public virtual List<ItemTemplate> FinalRewards
		{
			get { return m_finalRewards; }
		}

		/// <summary>
		/// How many optional items can the player choose
		/// </summary>
		public virtual byte NumOptionalRewardsChoice
		{
			get { return m_numOptionalRewardsChoice; }
			set { m_numOptionalRewardsChoice = value; }
		}

		/// <summary>
		/// List of optional rewards for this quest
		/// </summary>
		public virtual List<ItemTemplate> OptionalRewards
		{
			get { return m_optionalRewards; }
			set { m_optionalRewards = value; }
		}        		
		
		/// <summary>
		/// Final text to display to player when quest is finished
		/// </summary>
		public virtual string FinishText
		{
			get { return BehaviourUtils.GetPersonalizedMessage(m_dqRewardQ.FinishText, _questPlayer); }
		}
		
		/// <summary>
		/// The CharacterXDataQuest entry for the player doing this quest
		/// </summary>
		public virtual CharacterXDQRewardQ CharDataQuest
		{
			get { return m_charQuest; }
		}

		/// <summary>
		/// The unique ID for this quest
		/// </summary>
		public virtual int ID
		{
			get { return m_dqRewardQ.ID; }
		}

		/// <summary>
		/// Unique quest ID to send to the client
		/// </summary>
		public virtual ushort ClientQuestID
		{
			get { return (ushort)(m_dqRewardQ.ID + DQREWARDQ_CLIENTOFFSET); } 
		}		
		
		/// <summary>
		/// Minimum level this quest can be done
		/// </summary>
		public override int Level
		{
			get	{ return m_dqRewardQ.MinLevel; }
		}
				
		/// <summary>
		/// The amount of times a player has completed this quest
		/// </summary>
		public override short Count
		{
			get { return m_charQuest != null ? m_charQuest.Count : (short)0; }
			set
			{
				short oldCount = m_charQuest.Count;
				m_charQuest.Count = value;
				if (m_charQuest.Count != oldCount)
				{
					GameServer.Database.SaveObject(m_charQuest);
				}
			}
		}
		
		/// <summary>
		/// Maximum number of times this quest can be done
		/// </summary>
		public override int MaxQuestCount
		{
			get { return m_dqRewardQ.MaxCount == 0 ? int.MaxValue :  m_dqRewardQ.MaxCount; }
		}

		/// <summary>
		/// Description of this quest to show in quest log and also in 'summary' when offered quest window
		/// </summary>
		public override string Description
		{
			get	{ return m_dqRewardQ.Summary; }			
		}		
		
		/// <summary>
		/// ZoneID used for displaying quest dot on map
		/// </summary>
		public List<int> ZoneID
		{
			get	{ return m_zoneID; }
		}
		/// <summary>
		/// xoffset used for displaying quest dot on map
		/// </summary>
		public List<int> XOffset
		{
			get	{ return m_xOffset; }
		}
		/// <summary>
		/// yoffset used for displaying quest dot on map
		/// </summary>
		public List<int> YOffset
		{
			get	{ return m_yOffset; }
		}

		public int? Reputation
        {
			get;
			private set;
        }

		public int RewardReputation
		{
			get { return m_dqRewardQ.RewardReputation; }
		}

		/// <summary>
		/// Current step of this quest. Only used to determine if quest is completed or active. 0 = complete, 1 = active
		/// </summary>
		public override int Step
		{
			get { return m_charQuest == null ? 0 : m_charQuest.Step; }
			set
			{
				if (m_charQuest != null)
				{
					int oldStep = m_charQuest.Step;
					m_charQuest.Step = (short)value;
					if (m_charQuest.Step != oldStep)
					{
						GameServer.Database.SaveObject(m_charQuest);
					}
				}
			}
		}

        /// <summary>
        /// Current step of this quest. Only used to determine if quest is completed or active. 0 = complete, 1 = active
        /// </summary>
        public int StepCount
		{
			get { return m_dqRewardQ.StepCount; }			
		}
		
		/// <summary>
		/// Get or create the CharacterXDataQuest for this player
		/// </summary>		
		public static CharacterXDQRewardQ GetCharacterQuest(GamePlayer player, int ID, bool create)
		{
			CharacterXDQRewardQ charQuest = GameServer.Database.SelectObjects<CharacterXDQRewardQ>("`Character_ID` = @Character_ID AND `DataQuestID` = @DataQuestID", new[] { new QueryParameter("@Character_ID", player.QuestPlayerID), new QueryParameter("@DataQuestID", ID) }).FirstOrDefault();

			if (charQuest == null && create)
			{
				charQuest = new CharacterXDQRewardQ(player.QuestPlayerID, ID); // add value here to ID to avoid clash with other char dq's TODO review this number
				charQuest.Count = 0;
				charQuest.Step = 0;
				GameServer.Database.AddObject(charQuest);
			}

			return charQuest;
		}

		/// <summary>
		/// Can this player do this quest
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public override bool CheckQuestQualification(GamePlayer player)
		{
			if (player.Level < m_dqRewardQ.MinLevel || player.Level > m_dqRewardQ.MaxLevel)
			{
				return false;
			}

			if (m_allowedClasses.Count > 0)
			{
				if (!m_allowedClasses.Contains((byte)player.CharacterClass.ID))
				{
					return false;
				}
			}

            if (m_allowedRaces.Count > 0)
            {
                if (!m_allowedRaces.Contains(player.Race))
                {
                    return false;
                }
            }

            lock (player.QuestList)
			{
				foreach (AbstractQuest q in player.QuestList)
				{
					if (q is DQRewardQ && (q as DQRewardQ).ID == ID)
					{
						return false;  // player is currently doing this quest
					}
				}
			}

			lock (player.QuestListFinished)
			{
				foreach (AbstractQuest q in player.QuestListFinished)
				{
					if (q is DQRewardQ && (q as DQRewardQ).ID == ID)
					{
						if (q.IsDoingQuest(q) || (q as DQRewardQ).Count >= MaxQuestCount)
						{
							return false; // player has done this quest the max number of times
						}
					}
				}

				// check to see if this quest requires another to be done first TODO change questdependancy to check for quest id maybe?
				if (m_questDependencies.Count > 0)
				{
					int numFound = 0;

					foreach (string str in m_questDependencies)
					{
						foreach (AbstractQuest q in player.QuestListFinished)
						{
							if (q is DQRewardQ && (q as DQRewardQ).Name.ToLower() == str.ToLower())
							{
								numFound++;
								break;
							}
						}
					}

					if (numFound < m_questDependencies.Count)
					{
						return false;
					}
				}

				if (this.Reputation.HasValue)
                {
					if (this.Reputation.Value < 0 && player.Reputation >= 0)
                    {
						return false;
                    }
					else if (this.Reputation.Value == 0 && player.Reputation < 0)
                    {
						return false;
                    }
                }
			}

			return true;
		}

		/// <summary>
		/// Is the player currently doing this quest
		/// </summary>		
		public override bool IsDoingQuest(AbstractQuest checkQuest)
		{
			if (checkQuest is DQRewardQ && (checkQuest as DQRewardQ).ID == ID)
			{
				return Step > 0;
			}

			return false;
		}

		/// <summary>
		/// Update the quest indicator
		/// </summary>		
		public virtual void UpdateQuestIndicator(GameNPC npc, GamePlayer player)
		{
            player.Out.SendNPCsQuestEffect(npc, npc.GetQuestIndicator(player));
		}	
		
		/// <summary>
		/// Name of target that you interact with to finish the quest
		/// </summary>
		public string FinishName
		{
			get
			{
				try
				{
					return m_dqRewardQ.FinishNPC;
				}
				catch (Exception ex)
				{
					log.Error("DataQuest [" + ID + "] no finish target set", ex);
					return "";
				}				 
			}
		}
		/*
		
		/// <summary>
		/// Target name for the current step TODO change this to reflect quest goal if goal is the target
		/// </summary>
		public List<string> TargetName
		{
			get
			{
				try
				{
					return m_goalTargetName;
				}
				catch (Exception ex)
				{
					log.Error("DataQuest [" + ID + "] TargetName error for Step " + Step, ex);
					return "";
				}				 
			}
		}
		*//*
		/// <summary>
		/// Target region for the current step
		/// </summary>
		public ushort GoalTargetRegion
		{
			get
			{
				try
				{
					if (m_goalTargetRegion.Count > 0)
					{
						return m_goalTargetRegion[CurrentGoal.GoalIndex];
					}
				}
				catch (Exception ex)
				{
					log.Error("DataQuest [" + ID + "] TargetRegion error for Step " + Step, ex);
				}

				return 0;
			}
		}*/

		/// <summary>
		/// Target text for the current step
		/// </summary>
		protected string GoalTargetText
		{
			get
			{
				try
				{	
					return m_goalTargetText.Count > 0 ? m_goalTargetText[CurrentGoal.GoalIndex - 1] : string.Empty;					
				}
				catch (Exception ex)
				{
					log.Error("DataQuest [" + ID + "] TargetText error for Step " + Step, ex);
				}

				return "Error retrieving target text for step " + Step;
			}
		}		
		
		/// <summary>
		/// Text needed to advance the step or end the quest for the current step
		/// </summary>
		public string AdvanceText
		{
			get
			{
				try
				{
					if (m_advanceTexts.Count > 0 && CurrentGoal != null)
					{
                        return !string.IsNullOrEmpty(m_advanceTexts[CurrentGoal.GoalIndex - 1]) ? m_advanceTexts[CurrentGoal.GoalIndex - 1] : "";
                    }
				}
				catch (Exception ex)
				{
					log.Error("DataQuest [" + ID + "] AdvanceText error for Step " + Step, ex);
                    if (QuestPlayer != null) ChatUtil.SendDebugMessage(QuestPlayer, "DataQuest [" + ID + "] AdvanceText error for Step " + Step);
                }

				return "";
			}
		}

        /// <summary>
        /// Any money reward for the current step
        /// </summary>
        public long RewardMoney
		{
			get	{ return m_dqRewardQ.RewardMoney; }
		}
                
		/// <summary>
		/// The experience reward for a player, displayed as a percentage of thier current level in quest window
		/// </summary>		
        public int ExperiencePercent(GamePlayer player)
        {
            int currentLevel = player.Level;
            if (currentLevel > player.MaxLevel)
            {
            	return 0;
            }
            long experienceToLevel = player.GetExperienceNeededForLevel(currentLevel + 1) -
                player.GetExperienceNeededForLevel(currentLevel);

            return (int)((RewardXP * 100) / experienceToLevel);
        }        
        
		/// <summary>
		/// Xp reward for completing quest
		/// </summary>
		protected long RewardXP
		{
			get { return m_dqRewardQ.RewardXP; }
		}
		/// <summary>
		/// Championlevel xp for completing quest
		/// </summary>
		protected long RewardCLXP
		{
			get { return m_dqRewardQ.RewardCLXP; }
		}
		/// <summary>
		/// RPs for completing quest
		/// </summary>
		protected long RewardRP
		{
			get { return m_dqRewardQ.RewardRP; }
		}
		/// <summary>
		/// BPs for completing quest
		/// </summary>
		protected long RewardBP
		{
			get { return m_dqRewardQ.RewardBP; }
		}	

		/// <summary>
		/// Executes a custom class attached to this quest. Not supported yet
		/// </summary>
		protected virtual bool ExecuteCustomQuestStep(GamePlayer player, int step, eGoalTypeCheck goalTypeCheck)
		{
			bool canContinue = true;

			if (!string.IsNullOrEmpty(m_classType))
			{
				if (m_customQuestStep == null)
				{
					foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
					{
						if (assembly.GetType(m_classType) != null)
						{
							try
							{
								m_customQuestStep = assembly.CreateInstance(m_classType, false, BindingFlags.CreateInstance, null, new object[] { }, null, null) as IDQRewardQStep;
							}
							catch (Exception e)
							{
								log.Error(" Error creating an instance of custom step class", e);
							}

							break;
						}
					}

					if (m_customQuestStep == null)
					{
						foreach (Assembly assembly in ScriptMgr.Scripts)
						{
							if (assembly.GetType(m_classType) != null)
							{
								try
								{
									m_customQuestStep = assembly.CreateInstance(m_classType, false, BindingFlags.CreateInstance, null, new object[] { }, null, null) as IDQRewardQStep;
								}
								catch (Exception e)
								{
									log.Error(" Error creating an instance of custom step class in GSS folder", e);
								}

								break;
							}
						}
					}
				}

				if (m_customQuestStep == null)
				{
					log.ErrorFormat("Failed to construct custom DataQuest step of ClassType {0}!  Quest will continue anyway.", m_classType);
                    if (QuestPlayer != null) ChatUtil.SendDebugMessage(QuestPlayer, string.Format("Failed to construct custom DataQuest step of ClassType {0}!  Quest will continue anyway.", m_classType));
                }
			}

			if (m_customQuestStep != null)
			{
				canContinue = m_customQuestStep.Execute(this, player, step, goalTypeCheck);
			}

			return canContinue;
		}
		
		/// <summary>
		/// Try to advance the quest step, doing any actions required to start the next step
		/// </summary>		
		protected virtual bool AdvanceQuestStep(GameObject obj = null, int? countItem = null)
		{
			try
			{				
				bool advance = false;

				if (ExecuteCustomQuestStep(QuestPlayer, Step, eGoalTypeCheck.Step))
				{
					advance = true;
				}

				if (advance)
				{
                    newgoals.Advance(countItem.HasValue ? countItem.Value : 1);
                    //_questPlayer.Out.SendQuestListUpdate(); //TODO check which is better, this call, or the one in the questgoal.advance


                    if (GoalsCompleted())
					{	
                        if (CurrentGoal.Type == DQRQuestGoal.GoalType.InteractFinish)
                        {
                            if (obj as GameNPC != null && FinishName == obj.Name)
                            {
                                _questPlayer.Out.SendQuestRewardWindow(obj as GameNPC, _questPlayer, this);
                            }
                        }
                        else if (CurrentGoal.Type == DQRQuestGoal.GoalType.Collect ||
							CurrentGoal.Type == DQRQuestGoal.GoalType.DeliverFinish ||
							CurrentGoal.Type == DQRQuestGoal.GoalType.InteractWhisper)
                        {
                            if (obj is GameNPC npc && npc.Name == this.CurrentGoal.TargetObject)
                            {
                                _questPlayer.Out.SendQuestRewardWindow(obj as GameNPC, _questPlayer, this);
                            }
                        }
					}
					else
					{
						if ((CurrentGoal.Type == DQRQuestGoal.GoalType.DeliverFinish ||
							CurrentGoal.Type == DQRQuestGoal.GoalType.InteractDeliver ||
							CurrentGoal.Type == DQRQuestGoal.GoalType.InteractWhisper) && CurrentGoal.QuestItem != null)
						{
							var slot = QuestPlayer.Inventory.FindFirstEmptySlot(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

							if (slot != eInventorySlot.Invalid)
							{
								QuestPlayer.Inventory.AddItem(slot, new GameInventoryItem(CurrentGoal.QuestItem));
								if (obj as GameNPC != null)
                                {
									if (StepTexts.Count >= Step && StepTexts[Step - 1] != null)
									{
										QuestPlayer.Out.SendCustomTextWindow(obj.Name + " dit", new string[] { StepTexts[Step - 1] });
									}

									this.UpdateNextTargetNPCIcon(obj.CurrentRegionID);
								}								
							}
							else
							{
								QuestPlayer.Out.SendMessage("Vos Sacs sont pleins pour recevoir l'objet de quete", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
							}
                        }
                        else
						{
							this.UpdateNextTargetNPCIcon(obj.CurrentRegionID);
						}
					}                

                    // Then say any source text for the new step
                    /* TODO maybe put something here to support text after receiving a quest item or something
					if (!string.IsNullOrEmpty(SourceText))
					{
						TryTurnTo(obj, _questPlayer);

						if (obj != null)
                        {
                            if (obj.Realm == eRealm.None)
                            {
                                SendMessage(_questPlayer, SourceText, 0, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                            }
                            else
                            {
                                SendMessage(_questPlayer, SourceText, 0, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                            }
                        }
					}*/

                    return true;
				}
			}
			catch (Exception ex)
			{
				log.Error("DataQuest [" + ID + "] AdvanceQuestStep error when advancing from Step " + Step, ex);
                if (QuestPlayer != null) ChatUtil.SendDebugMessage(QuestPlayer, "[DEBUG] AdvanceQuestStep error when advancing from Step " + Step + ": " + ex.Message);
			}

			return false;
		}
		
		/// <summary>
		/// A player doing this quest whispers something to a living //TODO interect step of a quest
		/// </summary>		
		public virtual void OnPlayerWhisper(GamePlayer p, GameObject obj, string text)
		{			
			if (CurrentGoal != null && CurrentGoal.Type == DQRQuestGoal.GoalType.InteractWhisper && CurrentGoal.TargetObject == obj.Name && text == CurrentGoal.AdvanceText)
			{
				var deliverItem = GetPlayerDeliverItem(p);

				if (deliverItem != null && obj is GameNPC npc)
				{				
					p.Inventory.RemoveItem(deliverItem);
					UpdateQuestIndicator(npc, _questPlayer);				
				}

				AdvanceQuestStep(obj);			
			}			
		}
		
		/// <summary>
		/// Notify is sent to all quests in the players active quest list
		/// </summary>		
		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{			
			try
			{				
				// Interact to check quest offer
				if (e == GameObjectEvent.Interact)
				{
					var a = args as InteractEventArgs;
					var o = sender as GameObject;
					var p = a.Source as GamePlayer;

					if (p != null && o != null)
					{						
						CheckOfferQuest(p, o);
					}
					
					return;
				}

				// Interact when already doing quest
				if (e == GamePlayerEvent.InteractWith)
				{					
					var p = sender as GamePlayer;					
					var a = args as InteractWithEventArgs;					
					OnPlayerInteract(p, a.Target);
					
					return;
				}								
				
				if (e == GamePlayerEvent.Whisper)
				{
					WhisperEventArgs a = args as WhisperEventArgs;
					GamePlayer p = sender as GamePlayer;

					if (p != null)
					{
						OnPlayerWhisper(p, a.Target, a.Text);
					}
				}

				// Enemy of player with quest was killed, check quests and steps
				if (e == GamePlayerEvent.EnemyKilled)
				{
					var a = args as EnemyKilledEventArgs;
					var player = sender as GamePlayer;
					GameLiving killed = a.Target;

					OnEnemyKilled(player, killed);

					return;
				}

				if (e == GamePlayerEvent.Dying)
				{
					var a = args as DyingEventArgs;
					var npc = sender as GameNPC;

					if (CurrentGoal.Type == DQRQuestGoal.GoalType.KillGroup)
					{
						if (npc != null && a.PlayerKillers.Contains(this.QuestPlayer) && npc.CurrentGroupMob?.GroupId.Equals(this.CurrentGoal.TargetObject) == true)
						{
							if (MobGroups.MobGroupManager.Instance.IsAllOthersGroupMobDead(npc))
							{
								AdvanceQuestStep(npc);
							}
						}
					}
					return;
				}

                if (e == GamePlayerEvent.GiveItem)
                {
                    var giveArgs = args as GiveItemEventArgs;
                    var player = sender as GamePlayer;
                    if (giveArgs != null)
                    {
                        OnPlayerGiveItem(player, giveArgs.Target, giveArgs.Item);
                    }
                    
                    return;
                }

				if (e == GamePlayerEvent.AcceptQuest 
					&& (CurrentGoal.Type == DQRQuestGoal.GoalType.InteractDeliver ||
					CurrentGoal.Type == DQRQuestGoal.GoalType.InteractFinish ||
					CurrentGoal.Type == DQRQuestGoal.GoalType.DeliverFinish ||
					CurrentGoal.Type == DQRQuestGoal.GoalType.InteractWhisper) && CurrentGoal.QuestItem != null)
				{
					var player = sender as GamePlayer;

					if (player != null)
					{
						var slot = player.Inventory.FindFirstEmptySlot(eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);

						if (slot == eInventorySlot.Invalid)
						{
							player.Out.SendMessage("Vous n'avez plus de places dans vos sacs pour faire cette quete", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
						}
						else
						{
							player.Inventory.AddItem(slot, new GameInventoryItem(CurrentGoal.QuestItem));
							UpdateNextTargetNPCIcon(player.CurrentRegionID);
						}
					}
				}	

                // Player completes a /search command in quest area
                //if (e == GamePlayerEvent.SearchArea)
                //{
                //    var a = args as AreaEventArgs;
                //    var player = sender as GamePlayer;
                //    OnAreaSearched(player, a.Area as Area.Search);

                //    return;
                //}

                // Player is trying to finish a Reward Quest
                if (e == GamePlayerEvent.QuestRewardChosen)
				{
					QuestRewardChosenEventArgs rewardArgs = args as QuestRewardChosenEventArgs;
					if (rewardArgs == null)
						return;

					// Check if this particular quest has been finished.

					if (ClientQuestID != rewardArgs.QuestID)
						return;

					m_optionalRewardChoice.Clear();
					m_rewardItemsChosen = rewardArgs.ItemsChosen;

					if (ExecuteCustomQuestStep(QuestPlayer, 0, eGoalTypeCheck.RewardsChosen))
					{
						if (OptionalRewards.Count > 0)
						{
							for (int reward = 0; reward < rewardArgs.CountChosen; ++reward)
							{
								m_optionalRewardChoice.Add(OptionalRewards[rewardArgs.ItemsChosen[reward]]);
							}

							if (NumOptionalRewardsChoice > 0 && rewardArgs.CountChosen <= 0)
							{
                                QuestPlayer.Out.SendMessage(LanguageMgr.GetTranslation(QuestPlayer.Client, "RewardQuest.Notify"), eChatType.CT_System, eChatLoc.CL_ChatWindow);
								return;
							}
						}

						FinishQuest(null);
					}
				}
			}
			catch (Exception ex)
			{
				log.Error("DataQuest [" + ID + "] Notify Error for " + e.Name, ex);
                if (QuestPlayer != null) ChatUtil.SendDebugMessage(QuestPlayer, "DataQuest [" + ID + "] Notify Error for " + e.Name);
            }
		}

        protected override void OnPlayerGiveItem(GamePlayer player, GameObject obj, InventoryItem item)
        {
            if (item?.OwnerID == null || CurrentGoal.QuestItem?.Id_nb?.ToLowerInvariant().Equals(item?.Id_nb?.ToLowerInvariant()) == false || Step == 0 || m_goalTargetName.Count < newgoals.GoalIndex)
            {
                return;
            }

            if (m_goalTargetName[newgoals.GoalIndex - 1] == obj.Name && (TargetRegion == obj.CurrentRegionID || TargetRegion == 0)
               && player.Level >= Level)
            {
                if (ExecuteCustomQuestStep(player, Step, eStepCheckType.GiveItem))
                {
                    if (CurrentGoal.Type == DQRQuestGoal.GoalType.Collect)
                    {                    
                        TryTurnTo(obj, player);

                        if (m_goalTargetText?.Count >= Step && !string.IsNullOrEmpty(m_goalTargetText[Step - 1]))
                        {
                            if (obj.Realm == eRealm.None)
                            {
                                // mobs and other non realm objects send chat text and not popup text.
                                SendMessage(QuestPlayer, m_goalTargetText[Step - 1], 0, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                            }
                            else
                            {
                                SendMessage(QuestPlayer, m_goalTargetText[Step - 1], 0, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                            }
                        }

                        GameInventoryItem deltaItem = null;
                        var position = (eInventorySlot)item.SlotPosition;
                        int overloadCount = item.Count - CurrentGoal.Target + CurrentGoal.Current;

                        if (overloadCount > 0)
                        {
                            int previousCount = item.Count;                     
                            item.Count = overloadCount;
                            deltaItem = GameInventoryItem.Create(item);
                            item.Count = previousCount;
                        }

                        if (AdvanceQuestStep(obj, item.Count))
                        {                          
                            RemoveItem(obj, player, item, true);

                            if (overloadCount > 0 && deltaItem != null)
                            {
                                player.Inventory.AddItem(position, deltaItem);
                            }
                        }
                    }
                }
                else
                {
                    ChatUtil.SendDebugMessage(player, "Received item not in Collect or Step item list.");
                }
            }
        }


        public static void DQRewardQuestNotify(DOLEvent e, object sender, EventArgs args)
		{
			try
			{	// Reward Quest accept
				if (e == GamePlayerEvent.AcceptQuest)
				{
					var qargs = args as QuestEventArgs;
					if (qargs == null)
						return;
	
					GamePlayer player = qargs.Player;
					GameLiving giver = qargs.Source;
	
					foreach (DBDQRewardQ quest in GameObject.DQRewardCache)
					{
						if ((quest.ID + DQREWARDQ_CLIENTOFFSET) == qargs.QuestID)
						{
							CharacterXDQRewardQ charQuest = GetCharacterQuest(player, quest.ID, true);
							var dq = new DQRewardQ(player, giver, quest, charQuest);
							dq.Step = 1;
							player.AddQuest(dq);
							if (giver is GameNPC)
							{
	                            var npc = giver as GameNPC;
	                            player.Out.SendNPCsQuestEffect(npc, npc.GetQuestIndicator(player));
							}
							player.Out.SendSoundEffect(7, 0, 0, 0, 0, 0);
							player.Out.SendMessage("Vous avez reçu la quete: " + dq.Name, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
							if (!string.IsNullOrWhiteSpace(dq.AcceptText))
                            {
                                var formatMsg = dq.AcceptText.Replace(@"\n", "\n");

								if (dq.StepTexts.Count > 0 && dq.StepTexts[0] != null)
								{
									formatMsg += ";\n" + dq.StepTexts[0];
								}

								var finalMsg = formatMsg.SplitCSV(true);

								player.Out.SendCustomTextWindow(giver.Name + " dit", finalMsg);
                            }
							break;
						}
					}

				return;				
				}	
			}
			catch (Exception ex)
			{
				log.Error("error trying to accept quest", ex);
			}
		}		

		/// <summary>
		/// A player has interacted with an object that has a DataQuest.
		/// Check to see if we can offer this quest to the player and display the text
		/// </summary>
		/// <param name="player"></param>
		/// <param name="obj"></param>
		protected virtual void CheckOfferQuest(GamePlayer player, GameObject obj)
		{
			try
			{
				// Can we offer this quest to the player?
				if (CheckQuestQualification(player))
				{				
	                // Send offer quest dialog
	                var offerNPC = obj as GameNPC;
					if (offerNPC != null)
					{
						TryTurnTo(obj, player);
						player.Out.SendQuestOfferWindow(offerNPC, player, this);						
					}				
				}
			}
			catch (Exception ex)
			{
				log.Error("error trying to offer quest", ex);
			}
		}

		protected virtual void TryTurnTo(GameObject obj, GamePlayer player)
		{
			var npc = obj as GameNPC;

			if (npc != null)
			{
				npc.TurnTo(player, 10000);
			}
		}			
		
		/// <summary>
		/// A player with this quest has interacted with an object.
		/// See if this object is part of the quest and respond accordingly
		/// </summary>		
		protected override void OnPlayerInteract(GamePlayer player, GameObject obj)
		{
			try
			{
				if (CheckInteractPending(obj))
				{
					if (CurrentGoal != null)
					{
						var deliverItem = GetPlayerDeliverItem(player);

						TryTurnTo(obj, player);		
						if (!string.IsNullOrEmpty(GoalTargetText)) // TODO this might need to be changed to send a custommessage to allow for \n \r formatting
						{
							SendMessage(_questPlayer, GoalTargetText, 0, eChatType.CT_System, eChatLoc.CL_PopupWindow);
						}
						if (CurrentGoal.Type == DQRQuestGoal.GoalType.Interact || CurrentGoal.Type == DQRQuestGoal.GoalType.InteractDeliver || CurrentGoal.Type == DQRQuestGoal.GoalType.DeliverFinish)
						{
							if (deliverItem == null)
							{
								if (this.CurrentGoal.QuestItem != null)
								{
									player.Out.SendMessage("Malheuresement vous ne possedez pas " + this.CurrentGoal.QuestItem.Name + ".", eChatType.CT_Chat, eChatLoc.CL_SystemWindow);
								}
							}
							else
							{
								AdvanceQuestStep(obj);
								if (obj as GameNPC != null)
								{
									player.Inventory.RemoveItem(deliverItem);
									UpdateQuestIndicator(obj as GameNPC, _questPlayer);
									UpdateNextTargetNPCIcon(obj.CurrentRegionID);
								}
								else
								{
									foreach (GamePlayer others in _questPlayer.GetPlayersInRadius(1000))
									{
										others.Out.SendEmoteAnimation(_questPlayer, eEmote.PlayerPickup);
									}
								}
							}							
  						}
						if (CurrentGoal.Type == DQRQuestGoal.GoalType.InteractFinish
							|| CurrentGoal.Type == DQRQuestGoal.GoalType.Interact)
						{
							AdvanceQuestStep(obj);                            
                        }
						return;
					}
				}
				if (GoalsCompleted() && obj as GameNPC != null && FinishName == obj.Name)
				{
					player.Out.SendQuestRewardWindow(obj as GameNPC, player, this);
					return;
				}
			}
			catch (Exception ex)
			{
				log.Error("error trying to interact", ex);
			}
		}

		private void UpdateNextTargetNPCIcon(ushort regionId)
		{
			var target = WorldMgr.GetObjectsByNameFromRegion<GameNPC>(CurrentGoal.TargetObject, regionId, eRealm.None);
			if (target != null && target.Count() == 1)
			{
				UpdateQuestIndicator(target[0], QuestPlayer);
			}
		}


		InventoryItem GetPlayerDeliverItem(GamePlayer player)
		{			
			if (CurrentGoal?.QuestItem?.Id_nb == null)
            {
				return null;
            }
			return player.Inventory.GetFirstItemByID(CurrentGoal.QuestItem.Id_nb, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack);
		}
		
		/// <summary>
		/// Check if a target object is the current goal target for interact/interactDeliver.
		/// </summary>		
		public bool CheckInteractPending(GameObject target)
		{
			try
			{
				if (target == null)			
				{
					return false;
				}			
				foreach (DQRQuestGoal goal in Goals)
				{
					if (!goal.IsAchieved && 
						(goal.Type == DQRQuestGoal.GoalType.Interact ||
						goal.Type == DQRQuestGoal.GoalType.InteractDeliver ||
						goal.Type == DQRQuestGoal.GoalType.InteractWhisper||
						goal.Type == DQRQuestGoal.GoalType.InteractFinish ||
						goal.Type == DQRQuestGoal.GoalType.DeliverFinish) && goal.TargetObject == target.Name)
					{
						CurrentGoal = goal;
						
						return true;
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				log.Error("error trying to check pending goal", ex);
			}
			return false;
		}
		
		/// <summary>
		/// Check if a target object shows indicator icon to player.
		/// </summary>		
		public bool CheckInteractPendingIcon(GameObject target)
		{
			try
			{
				if (target == null)
				{
					return false;
				}
				foreach (DQRQuestGoal goal in Goals)
				{
					if (!goal.IsAchieved && (goal.Type == DQRQuestGoal.GoalType.Interact || goal.Type == DQRQuestGoal.GoalType.InteractDeliver || goal.Type == DQRQuestGoal.GoalType.InteractWhisper) && goal.TargetObject == target.Name)
					{
						CurrentGoal = goal;
						
						return true;
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				log.Error("error trying to check pending goal", ex);
			}
			return false;
		}
		
		/// <summary>
		/// Check if a target object is in players quest goals.
		/// </summary>		
		public bool CheckTargetToGoalList(GameObject target)
		{
			try
			{
				if (target == null)
				{
					return false;
				}
				
				foreach (DQRQuestGoal goal in Goals)
				{
					if (!goal.IsAchieved && goal.TargetObject.Equals(target.Name, StringComparison.OrdinalIgnoreCase) && goal.ZoneID1 == target.CurrentZone.ID)
					{
						CurrentGoal = goal;
						return true;
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				log.Error("error checking target to goal list", ex);
			}
			return false;
		}
		
		/// <summary>
		/// Enemy of a player with a dqrewardq is killed, check for quest advancement
		/// </summary>		
		protected virtual void OnEnemyKilled(GamePlayer player, GameLiving living)
		{
            if (player != null)
            {
                if (CheckTargetToGoalList(living as GameObject))
                {
                    if (CurrentGoal.Type == DQRQuestGoal.GoalType.Kill)
                    {
                        if (!string.IsNullOrEmpty(GoalTargetText))
                        {
                            if (living.Realm == eRealm.None)
                            {
                                // mobs and other non realm objects send chat text and not popup text.
                                SendMessage(_questPlayer, GoalTargetText, 0, eChatType.CT_Say, eChatLoc.CL_ChatWindow);
                            }
                            else
                            {
                                SendMessage(_questPlayer, GoalTargetText, 0, eChatType.CT_System, eChatLoc.CL_PopupWindow);
                            }
                        }
                        AdvanceQuestStep(living);
                    }			
                }
			}
		}

        /// <summary>
        /// Triggered from quest commands like /search
        /// </summary>        
        public override bool Command(GamePlayer player, AbstractQuest.eQuestCommand command, AbstractArea area)
        {
            if (player == null || area == null)
            {
                return false;
            }

            foreach (DQRQuestGoal goal in Goals)
            {
                if (!goal.IsAchieved && goal.Type == DQRQuestGoal.GoalType.Search)
                {
                    if (goal.TargetObject.ToLower() == area.Description.ToLower()) // we have the correct area
                    {                        
                        StartQuestActionTimer(player, command, 3, "You begin searching the area ...");
                        foreach (GamePlayer others in player.GetPlayersInRadius(1000))
                        {
                            others.Out.SendEmoteAnimation(player, eEmote.PlayerPickup);
                        }                        
                        CurrentGoal = goal;
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// A quest command like /search is completed, so do something // patchsearch
        /// </summary>        
        protected override void QuestCommandCompleted(AbstractQuest.eQuestCommand command, GamePlayer player)
        {
            if (QuestPlayer == player)
            {
                if (!AdvanceQuestStep(null))
                {
                    SendMessage(QuestPlayer, "You fail to find anything!", 0, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }
        }

        /// <summary>
        /// Finish the quest and update the player quest list
        /// </summary>
        public virtual bool FinishQuest(GameObject obj)
		{
			if (_questPlayer == null || m_charQuest == null || !m_charQuest.IsPersisted)
				return false;

			int lastStep = Step;

			TryTurnTo(obj, _questPlayer);			

			// try rewards first

			lock (_questPlayer.Inventory)
			{
				if (_questPlayer.Inventory.IsSlotsFree(m_finalRewards.Count + m_optionalRewardChoice.Count, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
				{
					const string xpError = "Your XP is turned off, you must turn it on to complete this quest!";
					const string rpError = "Your RP is turned off, you must turn it on to complete this quest!";					
					
					if (RewardXP > 0)
					{
						if (!_questPlayer.GainXP) // deny finishing quest if xp or rp is turned off for player
						{
							QuestPlayer.Out.SendMessage(xpError, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
							return false;
						}
						else if (RewardRP > 0 && !_questPlayer.GainRP)
						{
                            QuestPlayer.Out.SendMessage(rpError, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
							return false;
						}

                        _questPlayer.GainExperience(GameLiving.eXPSource.Quest, RewardXP);
					}

					if (RewardRP > 0)
					{
						if (!_questPlayer.GainRP)
						{
							QuestPlayer.Out.SendMessage(rpError, eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
							return false;
						}

                        _questPlayer.GainRealmPoints(RewardRP);
					}

					if (RewardReputation > 0)
                    {
						_questPlayer.RecoverReputation(RewardReputation);
                    }

					foreach (ItemTemplate item in m_finalRewards)
					{
						if (item != null)
						{
							GiveItem(_questPlayer, item);
						}
					}

					foreach (ItemTemplate item in m_optionalRewardChoice)
					{
						if (item != null)
						{
							GiveItem(_questPlayer, item);
						}
					}

					if (RewardCLXP > 0)
					{
                        _questPlayer.GainChampionExperience(RewardCLXP, GameLiving.eXPSource.Quest);
						
					}
					
					if (RewardBP > 0)
					{
                        _questPlayer.GainBountyPoints(RewardBP);					
					}
						
					if (RewardMoney > 0)
					{
                        _questPlayer.AddMoney(RewardMoney, "You are awarded {0}!");
	                    InventoryLogging.LogInventoryAction("(QUEST;" + Name + ")", QuestPlayer, eInventoryActionType.Quest, RewardMoney);						
					}					
				}
				else
				{
					SendMessage(_questPlayer, "Your inventory does not have enough space to finish this quest!", 0, eChatType.CT_System, eChatLoc.CL_PopupWindow);
					return false;
				}
			}

			m_charQuest.Step = 0;
			m_charQuest.Count++;
            ClearDQCustomProperties();
            GameServer.Database.SaveObject(m_charQuest);

            _questPlayer.Out.SendMessage("You have completed the quest: " + Name, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
            _questPlayer.Out.SendMessage(String.Format(LanguageMgr.GetTranslation(_questPlayer.Client, "AbstractQuest.FinishQuest.Completed", Name)), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

            // Remove this quest from the players active quest list and either
            // Add or update the quest in the players finished list
			_questPlayer.Out.SendQuestListUpdate();
            _questPlayer.QuestList.Remove(this);

			bool addq = true;
			lock (_questPlayer.QuestListFinished)
			{
				foreach (AbstractQuest q in _questPlayer.QuestListFinished)
				{
					if (q is DQRewardQ && (q as DQRewardQ).ID == ID)
					{
						(q as DQRewardQ).CharDataQuest.Step = 0;
						(q as DQRewardQ).CharDataQuest.Count++;
						addq= false;
						break;
					}
				}
			}

			if (addq)
			{
                _questPlayer.QuestListFinished.Add(this);
			}

            

            // TODO swap sound depending on realm
            _questPlayer.Out.SendSoundEffect(11, 0, 0, 0, 0, 0);			

			if (obj is GameNPC)
			{
				UpdateQuestIndicator(obj as GameNPC, _questPlayer);
			}

			if (m_startNPC != null)
			{
				UpdateQuestIndicator(m_startNPC, _questPlayer);
			}

			foreach (GameNPC npc in _questPlayer.GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE(_questPlayer.CurrentRegion)))
			{
				UpdateQuestIndicator(npc, _questPlayer);
			}

			return true;
		}

		/// <summary>
		/// Replace special characters in an item string
		/// Supported parsing:
		/// %c = character class
		/// </summary>		
		protected virtual string ParseItemString(string idnb, GamePlayer player)
		{
			string parsed = idnb;

			parsed = parsed.Replace("%c", ((eCharacterClass)player.CharacterClass.ID).ToString());

			return parsed;
		}
		
        /// <summary>
        /// Called to abort the quest and remove it from the database!
        /// </summary>
        public override void AbortQuest()
		{
			if (_questPlayer == null || m_charQuest == null || !m_charQuest.IsPersisted) return;
			
			Step = 0;
			_questPlayer.Out.SendQuestListUpdate();
            _questPlayer.Out.SendMessage(LanguageMgr.GetTranslation(_questPlayer.Client, "AbstractQuest.AbortQuest", Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);

			if (_questPlayer.QuestList.Contains(this))
			{
                _questPlayer.QuestList.Remove(this);
			}

			if (m_charQuest.Count == 0)
			{
				if (_questPlayer.QuestListFinished.Contains(this))
				{
                    _questPlayer.QuestListFinished.Remove(this);
				}

				DeleteFromDatabase();
			}
            
			if (m_startNPC != null)
			{
				UpdateQuestIndicator(m_startNPC, _questPlayer);
			}
			else foreach (GameNPC npc in _questPlayer.GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE(_questPlayer.CurrentRegion)))
			{
				UpdateQuestIndicator(npc, _questPlayer);
			}
		}

		/// <summary>
		/// Saves this quest into the database
		/// </summary>
		public override void SaveIntoDatabase()
		{
			if(m_charQuest.IsPersisted)
				GameServer.Database.SaveObject(m_charQuest);
			else
				GameServer.Database.AddObject(m_charQuest);
		}

		/// <summary>
		/// Quest aborted, deleting from player
		/// </summary>
		public override void DeleteFromDatabase()
		{
			if (m_charQuest == null || !m_charQuest.IsPersisted) return;

			CharacterXDQRewardQ charQuest = GameServer.Database.FindObjectByKey<CharacterXDQRewardQ>(m_charQuest.ID);
			if (charQuest != null)
			{
				GameServer.Database.DeleteObject(charQuest);
			}
		}		
		
		/// <summary>
		/// This HybridDictionary holds all the custom properties of this quest
		/// </summary>
		protected readonly HybridDictionary m_DQcustomProperties = new HybridDictionary();

		/// <summary>
		/// This method parses the custom properties string of the m_dbQuest
		/// into the HybridDictionary for easier use and access
		/// </summary>
		public void ParseDQCustomProperties()
		{
			if(m_charQuest.CustomPropertiesString == null)
				return;

			lock(m_DQcustomProperties)
			{
				m_DQcustomProperties.Clear();
				foreach(string property in m_charQuest.CustomPropertiesString.SplitCSV())
				{
					if(property.Length > 0)
					{
						string[] values = property.Split('=');
						m_DQcustomProperties[values[0]] = values[1];
					}					 
				}
			}
		}

		/// <summary>
		/// This method sets a custom Property to a specific value
		/// </summary>		
		public void SetDQCustomProperty(string key, string value)
		{
			if(key==null)
				throw new ArgumentNullException("key");
			if(value==null)
				throw new ArgumentNullException("value");

			//Make the string safe
			key = key.Replace(';',',');
			key = key.Replace('=','-');
			value = value.Replace(';',',');
			value = value.Replace('=','-');
			lock(m_DQcustomProperties)
			{
				m_DQcustomProperties[key]=value;
			}
			SaveDQCustomProperties();
		}

		/// <summary>
		/// Saves the custom properties into the database
		/// </summary>
		protected void SaveDQCustomProperties()
		{
			var builder = new StringBuilder();
			lock(m_DQcustomProperties)
			{
				foreach(string hKey in m_DQcustomProperties.Keys)
				{
					builder.Append(hKey);
					builder.Append("=");
					builder.Append(m_DQcustomProperties[hKey]);
					builder.Append(";");
				}
			}
			m_charQuest.CustomPropertiesString = builder.ToString();
			SaveIntoDatabase();
		}		

		/// <summary>
		/// This method retrieves a custom property from the database
		/// </summary>
		public string GetDQCustomProperty(string key)
		{
			if(key==null)
				throw new ArgumentNullException("key");

			return (string)m_DQcustomProperties[key];
		}
        /// <summary>
        /// This method clears the custom property string to remove problems with repeatable quests
        /// </summary>
        protected void ClearDQCustomProperties()
        {
            if (m_charQuest.CustomPropertiesString == null)
            {
                return;
            }
            m_charQuest.CustomPropertiesString = "";
            // TODO check this. Might need to be a value such as "Completed"
        }        
    }

	/// <summary>
	/// A single quest goal.
	/// </summary>
	public class DQRQuestGoal
	{
		private DQRewardQ m_quest;
		private string m_description;
        private int m_current, m_target;
        ItemTemplate goalItem = null;

		public enum GoalType : byte
		{            
            Search = 2,				// Search in a specified location to advance the goal.
            Kill = 3,				// Kill the target to advance the goal.
			Interact = 4,			// Interact with the target to advance the goal.
			InteractFinish = 5,		// Interact with the target to finish the quest.
			InteractWhisper = 6,	// Whisper to the target to advance the goal. 
			InteractDeliver = 7,    // Deliver a dummy item to the target to advance the goal.
			DeliverFinish = 8,      // Deliver item to the target to finish the quest.
			KillGroup = 9,
			Collect = 10,			// Player must give the target an item to advance the step	
			Unknown = 255
		}
		/// <summary>
		/// Constructs a new QuestGoal.
		/// </summary>		
		public DQRQuestGoal(string id, DQRewardQ quest, string description, GoalType type, int index, int target, string questItem, string targetobject)
		{			
			m_quest = quest;
			m_description = description;
			Type = type;
			GoalIndex = index;
			m_current = 0;
			m_target = 0;
			Target = target;			
			TargetObject = targetobject;
			goalItem = GameServer.Database.FindObjectByKey<ItemTemplate>(questItem);
		}

		/// <summary>
		/// Ready-to-use description of the goal and its current status.
		/// </summary>
		public string Description
		{
			get { return m_quest.QuestPlayer != null ? String.Format("Quest Goal: {0} ({1}/{2})", m_description, Current, Target) : m_description; }
		}

		/// <summary>
		/// The type of the goal, i.e. whether to scout or to kill things.
		/// </summary>
		public GoalType Type { get; private	set; }

        public int GoalIndex { get; }
        /// <summary>
        /// Target object for this goal.
        /// </summary>
        public string TargetObject { get; } = "";

        /// <summary>
        /// Target object for this goal.
        /// </summary>
        public string AdvanceText
        {
            get { return m_quest.AdvanceText; }
        }

        /// <summary>
        /// The quest item required for this goal.
        /// </summary>
        public ItemTemplate QuestItem
		{
			get { return ((Current > 0) || 
					Type == GoalType.InteractDeliver || 
					Type == GoalType.Collect ||
					Type == GoalType.InteractFinish ||
					Type == GoalType.DeliverFinish ||
					Type == GoalType.InteractWhisper) ? goalItem : null; }
			set { goalItem = value; }
		}

		/// <summary>
		/// Current status of this goal.
		/// </summary>
		public int Current
		{
			get 
			{
				if (m_quest.QuestPlayer == null)
					return m_current;
				String propertyValue = m_quest.GetDQCustomProperty(String.Format("goal{0}Current", GoalIndex));
				if (propertyValue == null)
				{
					Current = 0;
					return Current;
				}
				return Int16.Parse(propertyValue); 
			}
			set 
			{
				if (m_quest.QuestPlayer == null)
					m_current = value;
				else
				{
					m_quest.SetDQCustomProperty(String.Format("goal{0}Current", GoalIndex), value.ToString());
					m_quest.SaveIntoDatabase();
				}
			}
		}

		/// <summary>
		/// Target status of this goal.
		/// </summary>
		public int Target
		{
			get 
			{
				if (m_quest.QuestPlayer == null)
					return m_current;
				String propertyValue = m_quest.GetDQCustomProperty(String.Format("goal{0}Target", GoalIndex));
				if (propertyValue == null)
				{
					Target = 0;
					return Target;
				}
				return Int16.Parse(propertyValue); 
			}
			protected set 
			{
				if (m_quest.QuestPlayer == null)
					m_target = value;
				else
				{
					m_quest.SetDQCustomProperty(String.Format("goal{0}Target", GoalIndex), value.ToString());
					m_quest.SaveIntoDatabase();
				}
			}
		}

		/// <summary>
		/// Whether or not the goal has been achieved yet.
		/// </summary>
		public bool IsAchieved
		{
			get { return (Current == Target); }
		}

		public void Advance(int countItem)
		{
			if (Current <= Target)
			{
                //Handle stacked item on collect Step
                Current = Current + countItem;

                //Handle overload
                if (Current > Target)
                {
                    Current = Target;
                }

				m_quest.QuestPlayer.Out.SendMessage(Description, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);
				m_quest.QuestPlayer.Out.SendQuestUpdate(m_quest);
				
				// Check for updates
				if (IsAchieved)
				{
					// check if all quest is achieved
					bool done = true;
					foreach (DQRQuestGoal goal in m_quest.Goals)
					{
						done &= goal.IsAchieved;
					}
					
					//if (done && m_quest.QuestGiver.IsWithinRadius(m_quest.QuestPlayer, WorldMgr.VISIBILITY_DISTANCE)) // do this elsewhere
					//	m_quest.QuestPlayer.Out.SendNPCsQuestEffect(m_quest.QuestGiver, m_quest.QuestGiver.GetQuestIndicator(m_quest.QuestPlayer));
				}					
			}
		}
        // goal location info to put red dot on map
        public int ZoneID1
        {
            get { return m_quest.ZoneID.ElementAtOrDefault(GoalIndex - 1); }
        }

        public int XOffset1
        {
            get { return m_quest.XOffset.ElementAtOrDefault(GoalIndex - 1); }
        }

        public int YOffset1
        {
            get { return m_quest.YOffset.ElementAtOrDefault(GoalIndex - 1); }
        }
    }		
}
