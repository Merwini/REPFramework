using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace rep.heframework
{
    public class MapComponent_ArtillerySiege : MapComponent
    {
        private int lastProcessedDay = -1;

        public MapComponent_ArtillerySiege(Map map) : base(map)
        {
            
        }

        public override void MapComponentTick()
        {
            if (Find.TickManager.TicksGame % 200 != 0)
                return;

            int currentDay = GenDate.DaysPassed;

            if (currentDay != lastProcessedDay)
            {
                lastProcessedDay = currentDay;
            }
        }

        //Virtual so maybe I override it for a CE-version?
        public virtual void FireArtilleryBarrage()
        {
            
        }

        public virtual void WarnArtilleryBarrage()
        {
            //TODO
        }
    }
}
