using System;
using System.Reflection;
using AmteScripts.Managers;
using DOL.Database;
using DOL.Events;
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

                    //Allow kills and do not log in PvP zones etc..
                    if (killed.isInBG || 
                        killed.CurrentRegion.IsRvR ||
                        PvpManager.Instance.IsPvPRegion(killed.CurrentRegion.ID) ||
                        Territory.TerritoryManager.Instance.IsTerritoryArea(killed.CurrentAreas))
                    {
                        return;
                    }

                    bool isWanted = killer is GuardNPC || killed.Reputation < 0;
                    if (killer is GamePlayer || isWanted)
                    {
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
            }
        }            
    }
}
