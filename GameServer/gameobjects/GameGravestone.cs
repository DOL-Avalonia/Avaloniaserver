/*
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

using DOL.Database;
using DOL.Language;

namespace DOL.GS
{
	/// <summary>
	/// This class holds all information that
	/// that creates a gravestone.
	/// </summary>
	public class GameGravestone : GameStaticItem
	{		
		/// <summary>
		/// Constructor called for existing gravestones.
		/// </summary>
		public GameGravestone() : base()
		{	
			LoadedFromScript = false;
			ObjectState=eObjectState.Inactive;
		}

        /// <summary>
        /// Returns the xpvalue of this gravestone.
        /// </summary>
        public long XPValue { get; set; }

        /// <summary>
        /// Constructs a new empty Gravestone.
        /// </summary>
        public GameGravestone(GamePlayer player, long xpValue):base()
		{
			m_saveInDB = false;
			m_name = LanguageMgr.GetTranslation(player.Client.Account.Language, "GameGravestone.GameGravestone.Grave", player.Name);
			m_Heading = player.Heading;
			Position = player.Position;
			CurrentRegionID = player.CurrentRegionID;
			m_level = 0;

            if (player.Realm == eRealm.Albion)
            {
                m_model = 145; //Albion Gravestone
            }
            else if (player.Realm == eRealm.Midgard)
            {
                m_model = 636; //Midgard Gravestone
            }
            else if (player.Realm == eRealm.Hibernia)
            {
                m_model = 637; //Hibernia Gravestone
            }

            XPValue = xpValue;

			m_InternalID = player.InternalID;	// gravestones use the player unique id for themself
			ObjectState=eObjectState.Inactive;						
		}
			
		
		/// <summary>
		/// Loads this gravestone from the Gravestone DB.
		/// </summary>
		public override void LoadFromDatabase(DataObject obj)
		{
			DBGravestones item = obj as DBGravestones;
			
			InternalID = item.ObjectId;
			CurrentRegionID = item.Region;
            TranslationId = item.TranslationId;
			Name = item.Name;
            ExamineArticle = item.ExamineArticle;
			Model = item.Model;
			Emblem = item.Emblem;
			Realm = (eRealm)item.Realm;
			Heading = item.Heading;
			X = item.X;
			Y = item.Y;
			Z = item.Z;
			RespawnInterval = item.RespawnInterval;
            XPValue = item.XPValue;
		}
		
		/// <summary>
		/// Saves this gravestone in the Gravestone DB.
		/// </summary>
		public override void SaveIntoDatabase()
		{
			DBGravestones obj = null;
			if (InternalID != null)
			{
				obj = (DBGravestones)GameServer.Database.FindObjectByKey<DBGravestones>(InternalID);
			}
			if (obj == null)
			{
				if (LoadedFromScript == false)
				{
					obj = new DBGravestones();
				}
				else
				{
					return;
				}
			}
            obj.TranslationId = TranslationId;
			obj.Name = Name;
            obj.ExamineArticle = ExamineArticle;
			obj.Model = Model;
			obj.XPValue = XPValue;
			obj.Heading = Heading;
			obj.Region = CurrentRegionID;
			obj.X = X;
			obj.Y = Y;
			obj.Z = Z;
			
			if (InternalID == null)
			{
				GameServer.Database.AddObject(obj);
				InternalID = obj.ObjectId;
			}
			else
			{
				GameServer.Database.SaveObject(obj);
			}
		}
		
		/// <summary>
		/// Deletes this gravestone from the Gravestone DB.
		/// </summary>
		public override void DeleteFromDatabase()
		{
			if(InternalID != null)
			{
				DBGravestones obj = (DBGravestones) GameServer.Database.FindObjectByKey<DBGravestones>(InternalID);
                if (obj != null)
                {
                    GameServer.Database.DeleteObject(obj);
                }
			}
			InternalID = null;
		}
	}
}
