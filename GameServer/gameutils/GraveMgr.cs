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

using System.Reflection;
using DOL.Database;
using log4net;

namespace DOL.GS
{
	/// <summary>
	/// GraveMgr initiates the loading of gravestones on startup.
	/// </summary>
	public sealed class GraveMgr
	{
        /// <summary>
        /// Defines a logger for this class
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Initiate loading gravestones into the world on server startup
		/// </summary>
		public static bool Init()
		{
			var gravestones = GameServer.Database.SelectAllObjects<DBGravestones>();
			foreach (DBGravestones grave in gravestones)
			{
				if (!LoadGrave(grave))
				{
					log.Error("Unable to load " + grave.Name + ", check your database!");
				}
			}
			return true;
		}

        /// <summary>
        /// Method for loading graves from the database
        /// </summary>        
		public static bool LoadGrave(DBGravestones grave)
		{
			GameGravestone gstone = new GameGravestone();
			gstone.LoadFromDatabase(grave);
			gstone.AddToWorld();
			return true;
		}
	}
}
