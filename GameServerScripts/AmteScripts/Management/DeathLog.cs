using System;
using System.Reflection;
using AmteScripts.Managers;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using DOL.GS.Scripts;
using GameServerScripts.Amtescripts.Managers;
using log4net;


namespace DOL.GS.GameEvents
{
    public static class DeathLog
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        [ScriptLoadedEvent]
        public static void OnScriptCompiled(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.AddHandler(GameLivingEvent.Dying, new DOLEventHandler(LivingKillEnnemy));

            log.Info("DeathLog initialized");
        }

        [ScriptUnloadedEvent]
        public static void OnScriptUnloaded(DOLEvent e, object sender, EventArgs args)
        {
            GameEventMgr.RemoveHandler(GameLivingEvent.Dying, new DOLEventHandler(LivingKillEnnemy));
        }


        public static void LivingKillEnnemy(DOLEvent e, object sender, EventArgs args)
        {
            var dyingArgs = args as DyingEventArgs;
            if (dyingArgs != null)
            {
                var killer = dyingArgs.Killer;
                var killed = sender as GamePlayer;
                var killerPlayer = killer as GamePlayer;
                //Player isWanted when Killed by Guard
                if (killed != null)
                {
                    //If killer is GM, let go
                    if (killerPlayer != null && killerPlayer.Client.Account.PrivLevel > 1)
                    {
                        return;
                    }

                    if (IsKillAllowed(killed))
                    {
                        return;
                    }                 

                    bool isWanted = killed.Reputation < 0;
                    bool isLegitimeKiller = killer is GuardNPC || killerPlayer != null;

                    //Log interplayer kills & Killed by Guard
                    //Dot not log killed by npcs
                    if (isLegitimeKiller)
                    {
                        //Log Death
                        GameServer.Database.AddObject(new DBDeathLog((GameObject)sender, killer, isWanted));
                        if (killerPlayer != null)
                        {
                            if (DeathCheck.Instance.IsChainKiller(killerPlayer, killed))
                            {
                                killerPlayer.Reputation -= 2;
                                killerPlayer.SaveIntoDatabase();
                                killerPlayer.Out.SendMessage("Vous avez perdu 2 points de réputations pour cause d'assassinats multiples.", PacketHandler.eChatType.CT_System, PacketHandler.eChatLoc.CL_SystemWindow);
                            }
                        }
                    }
                }
                else
                {
                    //Check if Guard was killed
                    if (sender is GuardNPC && killerPlayer != null && !IsKillAllowed(killerPlayer))
                    {
                        if (killerPlayer.Group != null)
                        {
                            foreach (GamePlayer player in killerPlayer.Group.GetMembersInTheGroup())
                            {
                                GuardKillLostReputation(player);
                            }
                        }
                        else
                        {
                            GuardKillLostReputation(killerPlayer);
                        }
                    }
                }
            }
        }    
        
        private static void GuardKillLostReputation(GamePlayer player)
        {
            if (player.Client.Account.PrivLevel > 1)
            {
                player.Reputation--;
                player.SaveIntoDatabase();
                player.Out.SendMessage("Vous avez perdu 1 points de réputation pour cause d'assassinat de garde", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        /// <summary>
        /// Is kill allowed ? Allow kills in PvP zones etc..
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        private static bool IsKillAllowed(GamePlayer player)
        {
            if (player.isInBG ||
                player.CurrentRegion.IsRvR ||
                PvpManager.Instance.IsPvPRegion(player.CurrentRegion.ID) ||
                Territory.TerritoryManager.Instance.IsTerritoryArea(player.CurrentAreas))
            {
                return true;
            }

            return false;
        }
    }
}
