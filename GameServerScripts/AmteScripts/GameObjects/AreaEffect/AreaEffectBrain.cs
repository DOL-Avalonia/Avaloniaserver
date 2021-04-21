using DOL.GS;
using DOL.GS.Scripts;

namespace DOL.AI.Brain
{
	public class AreaEffectBrain : APlayerVicinityBrain
	{
		public override int ThinkInterval
		{
			get { return 1000; }
		}

        public override void Think()
		{
			if(Body is AreaEffect areaEffect)
            {
                areaEffect.CheckGroupMob();
                if (areaEffect.SpellID != 0)
                    areaEffect.ApplySpell();
                else
                    areaEffect.ApplyEffect();
                if (areaEffect.CheckFamily() is AreaEffect nextArea)
                    new NextAreaTimer(nextArea).Start(areaEffect.IntervalMin*1000);
            }
		}

        private class NextAreaTimer : GameTimer
        {
            private AreaEffect nextArea;

            public NextAreaTimer(AreaEffect actionSource) : base(actionSource.CurrentRegion.TimeManager)
            {
                nextArea = actionSource;
            }

            protected override void OnTick()
            {
                nextArea.CallAreaEffect();
                Stop();
            }
        }
    }

    
}
