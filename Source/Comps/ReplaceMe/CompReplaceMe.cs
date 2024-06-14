using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace rep.heframework
{
    public class CompReplaceMe : ThingComp
    {
        CompProperties_ReplaceMe Props => props as CompProperties_ReplaceMe;

        int ticksLeft = 0;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            ticksLeft = Props.ticksToReplace;
        }

        public override void CompTick()
        {
            if (ticksLeft > 0)
            {
                ticksLeft--;
                return;
            }

            if (parent.Map != null && parent.Position != null)
            {
                Thing replacementThing = ThingMaker.MakeThing(Props.thingToSpawn);
                IntVec3 position = parent.Position;
                Map map = parent.Map;
                replacementThing.stackCount = parent.stackCount;
                this.parent.Destroy(DestroyMode.Vanish);
                GenSpawn.Spawn(replacementThing, position, map);
            }
        }
    }
}