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
using System.Linq;
using System.Collections.Generic;

using DOL.GS.Realm;
using DOL.GS.Effects;
using DOL.Events;

namespace DOL.GS.PlayerClass
{
    [CharacterClass((int)eCharacterClass.Bainshee, "Bainshee", "Magician")]
    public class ClassBainshee : ClassMagician
    {
        public ClassBainshee()
        {
            m_profession = "PlayerClass.Profession.PathofAffinity";
            m_specializationMultiplier = 10;
            m_primaryStat = eStat.INT;
            m_secondaryStat = eStat.DEX;
            m_tertiaryStat = eStat.CON;
            m_manaStat = eStat.INT;
        }

        public override bool HasAdvancedFromBaseClass()
        {
            return true;
        }

        private const int WraithFormResetDelay = 30000;

        /// <summary>
        /// Timer Action for Reseting Wraith Form
        /// </summary>
        private RegionTimerAction<GamePlayer> _wraithTimerAction;

        /// <summary>
        /// Event Trigger When Player Zoning Out to Force Reset Form
        /// </summary>
        private DOLEventHandler _wraithTriggerEvent;

        /// <summary>
        /// Bainshee Transform While Casting.
        /// </summary>
        /// <param name="player"></param>
        public override void Init(GamePlayer player)
        {
            base.Init(player);

            // Add Cast Listener.
            _wraithTimerAction = new RegionTimerAction<GamePlayer>(Player, pl =>
            {
                if (pl.CharacterClass is ClassBainshee bainshee)
                {
                    bainshee.TurnOutOfWraith();
                }
            });

            _wraithTriggerEvent = new DOLEventHandler(TriggerUnWraithForm);
            GameEventMgr.AddHandler(Player, GameLivingEvent.CastFinished, new DOLEventHandler(TriggerWraithForm));
        }

        /// <summary>
        /// Check if this Spell Cast Trigger Wraith Form
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        protected virtual void TriggerWraithForm(DOLEvent e, object sender, EventArgs arguments)
        {
            var player = sender as GamePlayer;

            if (player != Player)
            {
                return;
            }

            if (!(arguments is CastingEventArgs args) || args.SpellHandler == null)
            {
                return;
            }

            if (!args.SpellHandler.HasPositiveEffect)
            {
                TurnInWraith();
            }
        }

        /// <summary>
        /// Check if we should remove Wraith Form
        /// </summary>
        /// <param name="e"></param>
        /// <param name="sender"></param>
        /// <param name="arguments"></param>
        protected virtual void TriggerUnWraithForm(DOLEvent e, object sender, EventArgs arguments)
        {
            GamePlayer player = sender as GamePlayer;

            if (player != Player)
            {
                return;
            }

            TurnOutOfWraith(true);
        }

        /// <summary>
        /// Turn in Wraith Change Model and Start Timer for Reverting.
        /// If Already in Wraith Form Restart Timer Only.
        /// </summary>
        public virtual void TurnInWraith()
        {
            if (Player == null)
            {
                return;
            }

            if (_wraithTimerAction.IsAlive)
            {
                _wraithTimerAction.Stop();
            }
            else
            {
                switch (Player.Race)
                {
                    case 11: Player.Model = 1885; break; // Elf
                    case 12: Player.Model = 1884; break; // Lurikeen
                    default: Player.Model = 1883; break; // Celt
                }

                GameEventMgr.AddHandler(Player, GameObjectEvent.RemoveFromWorld, _wraithTriggerEvent);
            }

            _wraithTimerAction.Start(WraithFormResetDelay);
        }

        /// <summary>
        /// Turn out of Wraith.
        /// Stop Timer and Remove Event Handlers.
        /// </summary>
        public void TurnOutOfWraith()
        {
            TurnOutOfWraith(false);
        }

        /// <summary>
        /// Turn out of Wraith.
        /// Stop Timer and Remove Event Handlers.
        /// </summary>
        public virtual void TurnOutOfWraith(bool forced)
        {
            if (Player == null)
            {
                return;
            }

            // Keep Wraith Form if Pulsing Offensive Spell Running
            if (!forced && Player.ConcentrationEffects.OfType<PulsingSpellEffect>().Any(pfx => pfx.SpellHandler != null && !pfx.SpellHandler.HasPositiveEffect))
            {
                TurnInWraith();
                return;
            }

            if (_wraithTimerAction.IsAlive)
            {
                _wraithTimerAction.Stop();
            }

            GameEventMgr.RemoveHandler(Player, GameObjectEvent.RemoveFromWorld, _wraithTriggerEvent);

            Player.Model = (ushort)Player.Client.Account.Characters[Player.Client.ActiveCharIndex].CreationModel;
        }

        public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
        {
             PlayerRace.Celt, PlayerRace.Elf, PlayerRace.Lurikeen,
        };
    }
}
