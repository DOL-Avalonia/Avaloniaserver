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
using DOL.GS.PacketHandler;
using DOL.Events;
using DOL.GS.RealmAbilities;

namespace DOL.GS.Effects
{
    /// <summary>
    /// BloodDrinking Effect SB RR5 RA
    /// </summary>
    /// /// <author>Stexx</author>
    public class BloodDrinkingEffect : TimedEffect
    {
        private const int Duration = 30 * 1000; // 30 secs
        private const double Healpercent = 20; // 20% heal
        private const int Effect = 1567;

        private GamePlayer EffectOwner;

        public BloodDrinkingEffect()
            : base(Duration)
        { }

        public override void Start(GameLiving target)
        {
            base.Start(target);
            if (target is GamePlayer)
            {
                EffectOwner = target as GamePlayer;
                foreach (GamePlayer p in EffectOwner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE(EffectOwner.CurrentRegion)))
                {
                    p.Out.SendSpellEffectAnimation(EffectOwner, EffectOwner, Effect, 0, false, 1);
                }

                GameEventMgr.AddHandler(EffectOwner, GameLivingEvent.AttackFinished, new DOLEventHandler(OnAttack));
                GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Quit, new DOLEventHandler(PlayerLeftWorld));
                GameEventMgr.AddHandler(EffectOwner, GameLivingEvent.Dying, new DOLEventHandler(PlayerLeftWorld));
                GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.Linkdeath, new DOLEventHandler(PlayerLeftWorld));
                GameEventMgr.AddHandler(EffectOwner, GamePlayerEvent.RegionChanged, new DOLEventHandler(PlayerLeftWorld));
            }
        }

        public override void Stop()
        {
            if (EffectOwner != null)
            {
                GameEventMgr.RemoveHandler(EffectOwner, GameLivingEvent.AttackFinished, new DOLEventHandler(OnAttack));
                GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Quit, new DOLEventHandler(PlayerLeftWorld));
                GameEventMgr.RemoveHandler(EffectOwner, GameLivingEvent.Dying, new DOLEventHandler(PlayerLeftWorld));
                GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.Linkdeath, new DOLEventHandler(PlayerLeftWorld));
                GameEventMgr.RemoveHandler(EffectOwner, GamePlayerEvent.RegionChanged, new DOLEventHandler(PlayerLeftWorld));
            }

            base.Stop();
        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        /// <param name="e">The event which was raised</param>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">EventArgs associated with the event</param>
        protected void PlayerLeftWorld(DOLEvent e, object sender, EventArgs args)
        {
            if (sender is GamePlayer player)
            {
                BloodDrinkingEffect bloodDrinking = player.EffectList.GetOfType<BloodDrinkingEffect>();
                bloodDrinking?.Cancel(false);
            }
        }

        /// <summary>
        /// Called when a player leaves the game
        /// </summary>
        /// <param name="e">The event which was raised</param>
        /// <param name="sender">Sender of the event</param>
        /// <param name="args">EventArgs associated with the event</param>
        protected void OnAttack(DOLEvent e, object sender, EventArgs arguments)
        {
            if (!(arguments is AttackFinishedEventArgs args) || args.AttackData == null)
            {
                return;
            }

            if (args.AttackData.SpellHandler != null)
            {
                return;
            }

            if (args.AttackData.AttackResult != GameLiving.eAttackResult.HitUnstyled
                && args.AttackData.AttackResult != GameLiving.eAttackResult.HitStyle)
            {
                return;
            }

            AttackData ad = args.AttackData;

            if (!(sender is GameLiving living))
            {
                return;
            }

            if (!MatchingDamageType(ref ad))
            {
                return;
            }

            double healPercent = Healpercent;
            int healAbsorbed = (int)(0.01 * healPercent * (ad.Damage + ad.CriticalDamage));
            if (healAbsorbed > 0)
            {
                if (living.Health < living.MaxHealth)
                {
                    // TODO correct messages
                    MessageToLiving(living, $"Blooddrinking ability is healing you for {healAbsorbed} health points!", eChatType.CT_Spell);
                    foreach (GamePlayer p in EffectOwner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE(EffectOwner.CurrentRegion)))
                    {
                        // heal effect
                        p.Out.SendSpellEffectAnimation(EffectOwner, EffectOwner, 3011, 0, false, 1);
                    }

                    living.Health = living.Health + healAbsorbed;
                }
                else
                {
                    MessageToLiving(living, "You are already fully healed!", eChatType.CT_Spell);
                }
            }
        }

        // Check if Melee
        protected virtual bool MatchingDamageType(ref AttackData ad)
        {

            if (ad == null || (ad.AttackResult != GameLiving.eAttackResult.HitStyle && ad.AttackResult != GameLiving.eAttackResult.HitUnstyled))
            {
                return false;
            }

            if (!ad.IsMeleeAttack && ad.AttackType != AttackData.eAttackType.Ranged)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// sends a message to a living
        /// </summary>
        /// <param name="living"></param>
        /// <param name="message"></param>
        /// <param name="type"></param>
        public void MessageToLiving(GameLiving living, string message, eChatType type)
        {
            if (living is GamePlayer && !string.IsNullOrEmpty(message))
            {
                living.MessageToSelf(message, type);
            }
        }

        public override string Name => "Blooddrinking";

        public override ushort Icon => 1843;

        // Delve Info
        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>
                {
                    "Cause the Shadowblade to be healed for 20% of all damage he does for 30 seconds"
                };

                return list;
            }
        }
    }
}