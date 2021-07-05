using DOL.GS.PacketHandler;
using System;

namespace DOL.GS.Spells
{
    [SpellHandler("Earthquake")]
    public class EarthquakeSpellHandler : SpellHandler
    {
        uint unk1 = 0;
        float radius, intensity, duration, delay = 0;
        int x, y, z = 0;

        public EarthquakeSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
            radius = 1200.0f;
            intensity = 50.0f;
            duration = 1000.0f;
        }

        public override bool CastSpell()
        {
            if (Caster.GroundTarget == null)
            {
                x = Caster.X;
                y = Caster.Y;
            }
            else
            {
                x = Caster.X;
                y = Caster.Y;
                z = Caster.Z;
            }
            /*if (args.Length > 1)
            {
                try
                {
                    unk1 = (uint)Convert.ToSingle(args[1]);
                }
                catch { }
            }*/
            if (Spell.Radius > 0)
            {
                try
                {
                    radius = Spell.Radius;
                }
                catch { }
            }
            if (Spell.Damage > 0)
            {
                try
                {
                    intensity = (float)Spell.Damage;
                }
                catch { }
            }
            if (Spell.Duration > 0)
            {
                try
                {
                    duration = Spell.Duration;
                }
                catch { }
            }
            if (Spell.CastTime > 0)
            {
                try
                {
                    delay = Spell.CastTime;
                }
                catch { }
            }
            
            if(Caster is GamePlayer player)
            {
                int distance = player.GetDistance(new Point2D(x, y));
                float newIntensity = intensity * (1 - distance / radius);
                GSTCPPacketOut pak = new GSTCPPacketOut(0x47);
                pak.WriteIntLowEndian(unk1);
                pak.WriteIntLowEndian((uint)x);
                pak.WriteIntLowEndian((uint)y);
                pak.WriteIntLowEndian((uint)z);
                pak.Write(BitConverter.GetBytes(radius), 0, sizeof(float));
                pak.Write(BitConverter.GetBytes(newIntensity), 0, sizeof(float));
                pak.Write(BitConverter.GetBytes(duration), 0, sizeof(float));
                pak.Write(BitConverter.GetBytes(delay), 0, sizeof(float));
                player.Out.SendTCP(pak);
            }
                
            return base.CastSpell();
        }

        public override void ApplyEffectOnTarget(GameLiving target, double effectiveness)
        {
            int distance = target.GetDistance(new Point2D(x, y));
            float newIntensity = intensity * (1 - distance / radius);
            int damage = (int)newIntensity;
            if (target is GamePlayer player)
            {
                if (player == Caster as GamePlayer)
                    return;
                GSTCPPacketOut pakBis = new GSTCPPacketOut(0x47);
                pakBis.WriteIntLowEndian(unk1);
                pakBis.WriteIntLowEndian((uint)x);
                pakBis.WriteIntLowEndian((uint)y);
                pakBis.WriteIntLowEndian((uint)z);
                pakBis.Write(BitConverter.GetBytes(radius), 0, sizeof(float));
                pakBis.Write(BitConverter.GetBytes(newIntensity), 0, sizeof(float));
                pakBis.Write(BitConverter.GetBytes(duration), 0, sizeof(float));
                pakBis.Write(BitConverter.GetBytes(delay), 0, sizeof(float));
                player.Out.SendTCP(pakBis);
            }

            AttackData ad = new AttackData();
            ad.Attacker = Caster;
            ad.Target = target;
            ad.AttackType = AttackData.eAttackType.Spell;
            ad.SpellHandler = this;
            ad.AttackResult = GameLiving.eAttackResult.HitUnstyled;
            ad.IsSpellResisted = false;
            ad.Damage = damage;

            m_lastAttackData = ad;
            SendDamageMessages(ad);
            DamageTarget(ad, true);
            target.StartInterruptTimer(target.SpellInterruptDuration, ad.AttackType, Caster);
        }
    }
}
