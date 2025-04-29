using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI.Group;


namespace rep.heframework
{
    public class GenStep_TaggedPawnGroupSettlement : GenStep_TaggedPawnGroup
    {
        public override Faction GetMapFaction(Map map, GenStepParams parms)
        {
            if (HEF_Settings.debugLogging)
            {
                Log.Message($"GetMapFaction (settlement) returning {map.ParentFaction}");
            }

            return map.ParentFaction;
        }

        public override float GetUnmodifiedThreatPoints(WorldObjectExtensionHEF extension, GenStepParams parms, Faction faction)
        {
            return extension.defenderThreatPointsRange.RandomInRange;
        }

        public override PawnGroupMaker GetPawnGroupMaker(FactionDef factionDef, PawnGroupMakerExtensionHEF pext, WorldObjectExtensionHEF wext)
        {
            return factionDef.pawnGroupMakers.Where(pgm => pgm.kindDef == PawnGroupKindDefOf.Settlement).RandomElement();
        }
    }
}

