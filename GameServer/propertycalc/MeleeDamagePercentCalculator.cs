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
using DOL.GS.Keeps;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The melee damage bonus percent calculator
    ///
    /// BuffBonusCategory1 is used for buffs
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 is used for debuff
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.MeleeDamage)]
    public class MeleeDamagePercentCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            int step = 0;
            try
            {
                if (living is GameNPC)
                {
                    // NPC buffs effects are halved compared to debuffs, so it takes 2% debuff to mitigate 1% buff
                    // See PropertyChangingSpell.ApplyNpcEffect() for details.
                    int buffs = living.BaseBuffBonusCategory[property] << 1;
                    step = 1;
                    int debuff = Math.Abs(living.DebuffCategory[property]);
                    step = 2;
                    int specDebuff = Math.Abs(living.SpecDebuffCategory[property]);
                    step = 3;

                    buffs -= specDebuff;
                    step = 4;
                    if (buffs > 0)
                    {
                        buffs = buffs >> 1;
                        step = 5;
                    }
                        
                    buffs -= debuff;
                    step = 6;

                    return living.AbilityBonus[property] + buffs;
                }

                // hardcap at 10%
                int itemPercent = Math.Min(10, living.ItemBonus[(int)property]);
                step = 7;
                int debuffPercent = Math.Min(10, Math.Abs(living.DebuffCategory[(int)property]));
                step = 8;
                int percent = living.BaseBuffBonusCategory[(int)property] + living.SpecBuffBonusCategory[(int)property] + itemPercent - debuffPercent;
                step = 9;

                // Apply RA bonus
                percent += living.AbilityBonus[(int)property];

                return percent;
            }
            catch (Exception e)
            {
                Log.Error(String.Format("MeleeDamagePercentCalculator CalcValue at step {0}, name {1}", step, living.Name), e);
                // Default value
                return 1;
            }
            
        }
    }
}
