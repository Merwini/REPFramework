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
    public class CompProperties_ReplaceMe : CompProperties
    {
        public CompProperties_ReplaceMe()
        {
            compClass = typeof(CompReplaceMe);
        }

        public ThingDef thingToSpawn;

        public int ticksToReplace = 0;
    }
}