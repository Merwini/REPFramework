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
    public class HEF_Settings : ModSettings
    {
        //TODO friendly expansion will only be a relevant setting once war mechanics are implemented
        public static bool friendlyHEFsCanExpand = false;

        public static int earliestExpansionDays = 30; //TODO balance

        public static bool debugLogging = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref friendlyHEFsCanExpand, "FriendlyHEFsCanExpand", false);
            Scribe_Values.Look(ref debugLogging, "debugLogging", false);
            Scribe_Values.Look(ref earliestExpansionDays, "earliestExpansionDays");

            base.ExposeData();
        }
    }
}