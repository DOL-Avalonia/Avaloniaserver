using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.RealmAbilities
{
    public class ShadowStrikeAbility : TimedRealmAbility
    {
        private DBSpell _dbspell;
        private Spell _spell;
        private SpellLine _spellline;
        private ShadowStrikeSpellHandler dd;
        private GamePlayer _player;

        public ShadowStrikeAbility(DBAbility dba, int level) : base(dba, level)
        {
            CreateSpell();
        }

        private void CreateSpell()
        {
            _dbspell = new DBSpell
            {
                Name = "Shadow Strike",
                Icon = 7073,
                ClientEffect = 5464,
                Damage = 0,
                DamageType = 0,
                Target = "Enemy",
                Radius = 0,
                Type = "ShadowStrike",
                Value = 0,
                Duration = 0,
                ResurrectHealth = 1,
                Pulse = 0,
                PulsePower = 0,
                Power = 0,
                CastTime = 10,
                EffectGroup = 0,
                Range = 1000
            };

            _spell = new Spell(_dbspell, 0); // make spell level 0 so it bypasses the spec level adjustment code
            _spellline = new SpellLine("RAs", "RealmAbilities", "RealmAbilities", true);
        }

        protected bool CastSpell(GameLiving target)
        {
            if (target.IsAlive && _spell != null)
            {
                dd = ScriptMgr.CreateSpellHandler(_player, _spell, _spellline) as ShadowStrikeSpellHandler;
                dd.IgnoreDamageCap = true;
                return dd.StartSpell(target);
            }
            return false;
        }

        public override void Execute(GameLiving living)
        {
            if (CheckPreconditions(living, DEAD | SITTING | MEZZED | STUNNED))
            {
                return;
            }
            _player = living as GamePlayer;
            if (!_player.IsStealthed)
            {
                _player.Out.SendMessage("You must be stealthed to use this ability.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            CreateSpell();
            if(_player.TargetObject is GameLiving && CastSpell(_player.TargetObject as GameLiving))
            {
                dd.FinishSpellCast(_player);
            }
            else
            {
                _player.Out.SendMessage("You need to target a living object to use this ability.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
        }
    }
}