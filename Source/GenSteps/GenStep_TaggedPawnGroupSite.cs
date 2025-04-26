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

        public override PawnGroupMaker GetPawnGroupMaker(FactionDef factionDef, PawnGroupMakerExtensionHEF pext, WorldObjectExtensionHEF wext)
        {
            string groupName = wext.factionsToPawnGroups.FirstOrDefault(link => link.Faction == factionDef).PawnGroups.RandomElement();
            PawnGroupMaker pawnGroupMaker = pext.taggedPawnGroupMakers.FirstOrDefault(pgm => pgm.groupName == groupName);

            return pawnGroupMaker;
        }

        
    }
}

