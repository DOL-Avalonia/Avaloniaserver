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
using System;
using DOL.Database;

namespace DOL.GS
{
	/// <summary>
	/// represents a point in a way path
	/// </summary>
	public class TPPoint : Point3D
	{
		protected TPPoint m_next = null;
		protected TPPoint m_prev = null;
		protected eTPPointType m_type;
		protected bool m_flag;

		public TPPoint(TPPoint pp) : this(pp,pp.Type) {}

		public TPPoint(Point3D p, eTPPointType type) : this(p.X,  p.Y,  p.Z, type) {}

		public TPPoint(int x, int y, int z, eTPPointType type) : base(x, y, z)
		{
			m_type = type;
			m_flag = false;
		}

		/// <summary>
		/// next waypoint in path
		/// </summary>
		public TPPoint Next
		{
			get { return m_next; }
			set { m_next = value; }
		}

		/// <summary>
		/// previous waypoint in path
		/// </summary>
		public TPPoint Prev
		{
			get { return m_prev; }
			set { m_prev = value; }
		}

		/// <summary>
		/// flag toggle when go through pathpoint
		/// </summary>
		public bool FiredFlag
		{
			get { return m_flag; }
			set { m_flag = value; }
		}

		/// <summary>
		/// path type
		/// </summary>
		public eTPPointType Type
		{
			get { return m_type; }
			set { m_type = value; }
		}

		public TPPoint GetNearestNextPoint(IPoint3D pos)
		{
			var nearest = this;
			var dist = nearest.GetDistanceTo(pos);

			var pp = this;
			while (pp.Next != null)
			{
				pp = pp.Next;
				var d = pp.GetDistanceTo(pos);
				if (d < dist)
				{
					nearest = pp;
					dist = d;
				}
			}

			return nearest;
		}
	}
}
