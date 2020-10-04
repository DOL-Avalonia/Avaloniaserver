using System;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.Scripts;
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
                //Player isWanted when Killed by Guard
                if (killed != null)
                {
                    bool isWanted = killer is GuardNPC || killed.Reputation < 0;
                    if (killer is GamePlayer || isWanted)
                    {
                        GameServer.Database.AddObject(new DBDeathLog((GameObject)sender, killer, isWanted));
                    }
                }
            }
        }            
    }
}
