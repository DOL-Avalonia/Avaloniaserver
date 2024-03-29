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
using DOL.Events;
using DOL.Language;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    /// <summary>
    /// AbstractArea extend this if you wish to implement e new custom area.
    /// For examples see Area.Cricle, Area.Square
    /// </summary>
    public abstract class AbstractArea : IArea
    {
        protected DBArea dbArea = null;

        /// <summary>
        /// Variable holding whether or not players can broadcast in this area
        /// </summary>
        public bool CanBroadcast { get; set; }

        /// <summary>
        /// Variable holding whether or not to check for LOS for spells in this area
        /// </summary>
        public bool CheckLOS { get; set; }

        /// <summary>
        /// Display entered message
        /// </summary>
        public virtual bool DisplayMessage { get; set; } = true;

        /// <summary>
        /// Can players be attacked by other players in this area
        /// </summary>
        public virtual bool IsSafeArea { get; set; } = false;

        public bool IsPvP { get; set; } = false;

        /// <summary>
        /// Constant holding max number of areas per zone, increase if more ares are needed,
        /// this will slightly increase memory usage on server
        /// </summary>
        public const ushort MAX_AREAS_PER_ZONE = 50;

        /// <summary>
        /// Holds the translation id
        /// </summary>
        protected string m_translationId;

        /// <summary>
        /// Constructs a new AbstractArea
        /// </summary>
        /// <param name="desc"></param>
        public AbstractArea(string desc)
        {
            Description = desc;
        }

        public AbstractArea()
            : base()
        {    
        }

        /// <summary>
        /// Returns the ID of this Area
        /// </summary>
        public ushort ID { get; set; }

        public int RealmPoints { get; set; }

        public virtual LanguageDataObject.eTranslationIdentifier TranslationIdentifier => LanguageDataObject.eTranslationIdentifier.eArea;

        /// <summary>
        /// Gets or sets the translation id
        /// </summary>
        public string TranslationId
        {
            get { return m_translationId; }
            set { m_translationId = value ?? string.Empty; }
        }

        /// <summary>
        /// Return the description of this Area
        /// </summary>
        public string Description { get; protected set; }

        /// <summary>
        /// Gets or sets the area sound
        /// </summary>
        public byte Sound { get; set; }

        public void UnRegisterPlayerEnter(DOLEventHandler callback)
        {
            GameEventMgr.RemoveHandler(this, AreaEvent.PlayerEnter, callback);
        }

        public void UnRegisterPlayerLeave(DOLEventHandler callback)
        {
            GameEventMgr.RemoveHandler(this, AreaEvent.PlayerLeave, callback);
        }

        public void RegisterPlayerEnter(DOLEventHandler callback)
        {
            GameEventMgr.AddHandler(this, AreaEvent.PlayerEnter, callback);
        }

        public void RegisterPlayerLeave(DOLEventHandler callback)
        {
            GameEventMgr.AddHandler(this, AreaEvent.PlayerLeave, callback);
        }

        /// <summary>
        /// Checks wether area intersects with given zone
        /// </summary>
        /// <param name="zone"></param>
        /// <returns></returns>
        public abstract bool IsIntersectingZone(Zone zone);

        /// <summary>
        /// Checks wether given spot is within areas boundaries or not
        /// </summary>
        /// <param name="spot"></param>
        /// <returns></returns>
        public abstract bool IsContaining(IPoint3D spot);

        public abstract bool IsContaining(IPoint3D spot, bool checkZ);

        public abstract bool IsContaining(int x, int y, int z);

        public abstract bool IsContaining(int x, int y, int z, bool checkZ);

        public bool CanVol { get; protected set; }
        public DBArea DbArea { get => dbArea; set => dbArea = value; }

        /// <summary>
        /// Called whenever a player leaves the given area
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnPlayerLeave(GamePlayer player)
        {
            if (DisplayMessage && !string.IsNullOrWhiteSpace(Description))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractArea.Left", Description), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            player.IsAllowToVolInThisArea = true;

            player.Notify(AreaEvent.PlayerLeave, this, new AreaEventArgs(this, player));
        }

        /// <summary>
        /// Called whenever a player enters the given area
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnPlayerEnter(GamePlayer player)
        {
            if (DisplayMessage && !string.IsNullOrWhiteSpace(Description))
            {
                string description = Description;
                string screenDescription = description;

                if (player.GetTranslation(this) is DBLanguageArea translation)
                {
                    if (!Util.IsEmpty(translation.Description))
                    {
                        description = translation.Description;
                    }

                    if (!Util.IsEmpty(translation.ScreenDescription))
                    {
                        screenDescription = translation.ScreenDescription;
                    }
                }

                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "AbstractArea.Entered", description), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                // Changed by Apo 9. August 2010: Areas never send an screen description, but we will support it with an server property
                if (ServerProperties.Properties.DISPLAY_AREA_ENTER_SCREEN_DESC)
                {
                    player.Out.SendMessage(screenDescription, eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
                }
            }

            if (Sound != 0)
            {
                player.Out.SendRegionEnterSound(Sound);
            }

            player.IsAllowToVolInThisArea = this.CanVol;

            player.Notify(AreaEvent.PlayerEnter, this, new AreaEventArgs(this, player));
        }

        public abstract void LoadFromDatabase(DBArea area);
    }
}
