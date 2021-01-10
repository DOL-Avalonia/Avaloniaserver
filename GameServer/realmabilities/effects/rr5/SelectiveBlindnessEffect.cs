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
using System.Collections.Generic;
using DOL.Events;

namespace DOL.GS.Effects
{

    public class SelectiveBlindnessEffect : TimedEffect
    {
        private const int Duration = 20 * 1000;

        private GameLiving _effectOwner;

        public SelectiveBlindnessEffect(GameLiving source)
            : base(Duration)
        {
                EffectSource = source as GamePlayer;
        }

        public GameLiving EffectSource { get; }

        public override void Start(GameLiving target)
        {
            base.Start(target);
            _effectOwner = target;
            foreach (GamePlayer p in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE(target.CurrentRegion)))
            {
                p.Out.SendSpellEffectAnimation(_effectOwner, _effectOwner, 7059, 0, false, 1);
            }

            GameEventMgr.AddHandler(EffectSource, GameLivingEvent.AttackFinished, new DOLEventHandler(EventHandler));
            GameEventMgr.AddHandler(EffectSource, GameLivingEvent.CastFinished, new DOLEventHandler(EventHandler));
         }

        public override void Stop()
        {
            if (_effectOwner != null)
            {
                GameEventMgr.RemoveHandler(EffectSource, GameLivingEvent.AttackFinished, new DOLEventHandler(EventHandler));
                GameEventMgr.RemoveHandler(EffectSource, GameLivingEvent.CastFinished, new DOLEventHandler(EventHandler));
           }

            base.Stop();
        }

        /// <summary>
        /// Event that will make effect stops
        /// </summary>
        /// <param name="e">The event which was raised</param>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">EventArgs associated with the event</param>
        protected void EventHandler(DOLEvent e, object sender, EventArgs args)
        {
            Cancel(false);
        }

        public override string Name => "Selective Blindness";

        public override ushort Icon => 3058;

        // Delve Info
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>
                {
                    "You can't attack an ennemy."
                };

                return list;
            }
        }
    }
}

