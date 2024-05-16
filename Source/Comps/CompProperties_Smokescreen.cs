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
    public partial class CompProperties_Smokescreen : CompProperties
    {
        public ThingDef smokeProjectile;

        public int projectileCount = 1;

        public int projectileSpreadDegrees = 0;

        public float range = 1;

        public int charges = 1;

        public int cooldown = 0;

        public float damageThreshold = 0;

        public bool alwaysLaunchForward = false;

        public CompProperties_Smokescreen()
        {
            this.compClass = typeof(CompSmokescreen);
        }
    }
}