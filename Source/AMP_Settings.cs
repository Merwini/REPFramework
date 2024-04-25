using CombatExtended;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace rep.framework
{
    public class AMP_Settings : ModSettings
    {
        public static bool FriendlyAMPsCanExpand = false;
        public static bool SitesMustBeUnique = true;

        public static int minimumExpansionDistance = 2;
        public static int maximumExpansionDistance = 8;

        public static int earliestExpansionDays = 15; //TODO balance
        public static int earliestRaidDays = 30; //TODO balance

        //TODO organize, customize, save
    }
}