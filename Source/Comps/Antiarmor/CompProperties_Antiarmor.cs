using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using UnityEngine;

namespace rep.heframework
{
    public class CompProperties_Antiarmor : CompProperties
    {
        public AbilityDef ability;

        public ThingDef pseudoweapon;

        public int magazineSize = -1;

        //TODO someday?
        //public List<Vector3> fireOffsets;

        public int ticksBetweenShots = 1;

        public int armorSharpThreshold = 0;

        public CompProperties_Antiarmor()
        {
            this.compClass = typeof(CompAntiarmor);
        }
    }
}