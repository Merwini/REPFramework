﻿using RimWorld;
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
            if (HE_Settings.debugLogging)
            {
                Log.Message($"GetMapFaction (settlement) returning {map.ParentFaction}");
            }

            return map.ParentFaction;
        }

        public override float GetUnmodifiedThreatPoints(WorldObjectExtensionHE extension, GenStepParams parms, Faction faction)
        {
            return extension.settlementThreatPoints;
        }

        public override PawnGroupMaker GetPawnGroupMaker(FactionDef factionDef, PawnGroupMakerExtensionHE pext, WorldObjectExtensionHE wext)
        {
            return factionDef.pawnGroupMakers.Where(pgm => pgm.kindDef == PawnGroupKindDefOf.Settlement).RandomElement();
        }
    }
}

