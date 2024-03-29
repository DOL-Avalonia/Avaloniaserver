using System;
using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.Events;

namespace DOL.GS.Effects
{
    /// <summary>
    /// Adrenaline Rush
    /// </summary>
    public class NaturesWombEffect : TimedEffect
    {

        public NaturesWombEffect()
            : base(5000)
        {
        }

        private GameLiving _owner;

        public override void Start(GameLiving target)
        {
            base.Start(target);
            _owner = target;
            if (target is GamePlayer player)
            {
                foreach (GamePlayer p in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE(player.CurrentRegion)))
                {
                    p.Out.SendSpellEffectAnimation(player, player, Icon, 0, false, 1);
                }
            }

            GameEventMgr.AddHandler(target, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttack));

            // [StephenxPimentel]
            // 1.108 updates this so it no longer stuns, but silences.
            // Rest of the code is now located in SpellHandler. (Line 617)
            _owner.StopCurrentSpellcast();
        }

        private void OnAttack(DOLEvent e, object sender, EventArgs arguments)
        {
            if (!(sender is GameLiving living))
            {
                return;
            }

            AttackedByEnemyEventArgs attackedByEnemy = arguments as AttackedByEnemyEventArgs;
            AttackData ad = null;
            if (attackedByEnemy != null)
            {
                ad = attackedByEnemy.AttackData;
            }

            if (ad == null || ad.Damage + ad.CriticalDamage < 1)
            {
                return;
            }

            int heal = ad.Damage + ad.CriticalDamage;
            ad.Damage = 0;
            ad.CriticalDamage = 0;

            GamePlayer player = living as GamePlayer;
            if (ad.Attacker is GamePlayer attackplayer)
            {
                attackplayer.Out.SendMessage($"{living.Name}\'s druidic powers absorb your attack!", eChatType.CT_SpellResisted, eChatLoc.CL_SystemWindow);
            }

            int modheal = living.MaxHealth - living.Health;
            if (modheal > heal)
            {
                modheal = heal;
            }

            living.Health += modheal;
            player?.Out.SendMessage($"Your druidic powers convert your enemies attack and heal you for {modheal}!", eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
        }

        public override string Name => "Nature's Womb";

        public override ushort Icon => 3052;

        public override void Stop()
        {
            GameEventMgr.RemoveHandler(_owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttack));
            _owner.IsStunned = false;
            _owner.DisableTurning(false);
            if (_owner is GamePlayer player)
            {
                player.Out.SendUpdateMaxSpeed();
            }
            else
            {
                _owner.CurrentSpeed = _owner.MaxSpeed;
            }

            base.Stop();
        }

        public int SpellEffectiveness { get; } = 100;

        public override IList<string> DelveInfo
        {
            get
            {
                var list = new List<string>
                {
                    "Stuns you for 5 seconds but absorbs all damage taken"
                };

                return list;
            }
        }
    }
}