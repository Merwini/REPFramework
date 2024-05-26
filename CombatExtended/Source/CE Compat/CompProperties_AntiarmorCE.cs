using CombatExtended;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using rep.heframework;

namespace rep.heframework.cecompat
{
    public class CompProperties_AntiarmorCE : CompProperties_Antiarmor
    {
        public CompProperties_AntiarmorCE()
        {
            this.compClass = typeof(CompAntiarmorCE);
        }
    }
}