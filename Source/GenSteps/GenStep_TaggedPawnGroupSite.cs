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
    public class GenStep_TaggedPawnGroupSite : GenStep_TaggedPawnGroup
    {
        public override Faction GetMapFaction(Map map, GenStepParams parms)
        {
            Faction faction = parms.sitePart?.site?.Faction;
            if (faction == null)
            {
                faction = map.ParentFaction;
            }
            return faction;
        }

        public override WorldObjectExtension GetWorldObjectExtension(FactionDef factionDef, GenStepParams parms)
        {
            return (WorldObjectExtension)parms.sitePart?.def.GetModExtension<WorldObjectExtension>();
        }

        public override PawnGroupMaker GetPawnGroupMaker(FactionDef factionDef, PawnGroupMakerExtension pext, WorldObjectExtension wext)
        {
            string groupName = wext.factionsToPawnGroups.FirstOrDefault(link => link.faction == factionDef).pawnGroups.RandomElement();
            PawnGroupMaker pawnGroupMaker = pext.taggedPawnGroupMakers.FirstOrDefault(pgm => pgm.groupName == groupName);

            return pawnGroupMaker;
        }

        public override float GetClampedThreatPoints(GenStepParams parms, WorldObjectExtension extension, float targetPoints)
        {
            //TODO use targetPoints instead of parms.threatPoints once it is implemented
            Log.Warning($"parms points {parms.sitePart.parms.threatPoints}");
            return Mathf.Clamp(parms.sitePart.parms.threatPoints, extension.threatPointsRange.min, extension.threatPointsRange.max);
        }
    }
}

