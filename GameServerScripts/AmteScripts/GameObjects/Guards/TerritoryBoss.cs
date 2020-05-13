using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;
using DOL.Territory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Scripts
{
    public class TerritoryBoss
        : AmteMob, IGuardNPC
    {
        private string originalGuildName;

        public TerritoryBoss()
        {
            var brain = new TerritoryBrain();
            brain.AggroLink = 3;
            SetOwnBrain(brain);
        }
  

        public override bool Interact(GamePlayer player)
        {
            if (player.Client.Account.PrivLevel == 1 && !IsWithinRadius(player, InteractDistance))
            {
                player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "GameObject.Interact.TooFarAway", GetName(0, true)), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                Notify(GameObjectEvent.InteractFailed, this, new InteractEventArgs(player));
                return false;
            }
            Notify(GameObjectEvent.Interact, this, new InteractEventArgs(player));
            player.Notify(GameObjectEvent.InteractWith, player, new InteractWithEventArgs(this));

            if (string.IsNullOrWhiteSpace(GuildName) || player.Guild == null)
                return false;
            if (player.Client.Account.PrivLevel == 1 && player.GuildName != GuildName)
                return false;
            if (!player.GuildRank.Claim)
            {
                player.Out.SendMessage(string.Format("Bonjour {0}, je ne discute pas avec les bleus, circulez.", player.Name), eChatType.CT_System, eChatLoc.CL_PopupWindow);
                return true;
            }

            return true;
        }


        public override void Die(GameObject killer)
        {
            base.Die(killer);

            if (killer.GuildName != null)
            {
                this.GuildName = killer.GuildName;
                TerritoryManager.Instance.ChangeGuildOwner(this.InternalID, killer.GuildName, isBoss: true);
                //handle bonus
            }
        }


        public override void LoadFromDatabase(DataObject obj)
        {
            base.LoadFromDatabase(obj);

            Mob mob = obj as Mob;
            if (mob != null)
            {
                this.originalGuildName = mob.Guild;
            }
        }
    }
}
