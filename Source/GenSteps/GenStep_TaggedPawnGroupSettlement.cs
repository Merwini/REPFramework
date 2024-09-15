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
            return map.ParentFaction;
        }

        public override WorldObjectExtension GetWorldObjectExtension(FactionDef factionDef, GenStepParams parms)
        {
            return (WorldObjectExtension)factionDef.GetModExtension<WorldObjectExtension>();
        }

        public override PawnGroupMaker GetPawnGroupMaker(FactionDef factionDef, PawnGroupMakerExtension pext, WorldObjectExtension wext)
        {
            return factionDef.pawnGroupMakers.Where(pgm => pgm.kindDef == PawnGroupKindDefOf.Settlement).RandomElement();
        }

        public override float GetClampedThreatPoints(GenStepParams parms, WorldObjectExtension extension, float targetPoints)
        {
            //TODO use targetPoints instead of extension.threatPointsRange once it is implemented
            return Mathf.Clamp(extension.threatPointsRange.RandomInRange, 0, extension.maximumThreatPoints);
        }
    }
}

