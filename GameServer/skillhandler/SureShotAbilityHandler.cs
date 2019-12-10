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
using DOL.GS.PacketHandler;
using DOL.GS.Effects;
using DOL.Language;

namespace DOL.GS.SkillHandler
{
    /// <summary>
    /// Handler for Sure Shot ability
    /// </summary>
    [SkillHandler(Abilities.SureShot)]
    public class SureShotAbilityHandler : IAbilityActionHandler
    {
        public void Execute(Ability ab, GamePlayer player)
        {
            SureShotEffect sureShot = player.EffectList.GetOfType<SureShotEffect>();
            if (sureShot != null)
            {
                sureShot.Cancel(false);
                return;
            }

            if (!player.IsAlive)
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Skill.Ability.SureShot.CannotUseDead"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            RapidFireEffect rapidFire = player.EffectList.GetOfType<RapidFireEffect>();
            rapidFire?.Cancel(false);

            TrueshotEffect trueshot = player.EffectList.GetOfType<TrueshotEffect>();
            trueshot?.Cancel(false);

            new SureShotEffect().Start(player);
        }
    }
}
