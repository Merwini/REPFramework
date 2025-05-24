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

            if (HE_Settings.debugLogging)
            {
                Log.Message($"GetMapFaction (site) returning {faction}");
            }

            return faction;
        }

        public override float GetUnmodifiedThreatPoints(WorldObjectExtensionHE extension, GenStepParams parms, Faction faction)
        {
            return parms.sitePart.parms.threatPoints;
        }

        public override PawnGroupMaker GetPawnGroupMaker(FactionDef factionDef, PawnGroupMakerExtensionHEF pext, WorldObjectExtensionHE wext)
        {
            string groupName = wext.factionsToPawnGroups.FirstOrDefault(link => link.Faction == factionDef).PawnGroups.RandomElement();
            PawnGroupMaker pawnGroupMaker = pext.taggedPawnGroupMakers.FirstOrDefault(pgm => pgm.groupName == groupName);

            return pawnGroupMaker;
        }

        
    }
}

