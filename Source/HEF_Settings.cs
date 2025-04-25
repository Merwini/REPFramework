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
        public static bool FriendlyHEFsCanExpand = false;

        public static int earliestExpansionDays = 15; //TODO balance

        public static bool debugLogging = false;

        //TODO organize, customize, save
    }
}