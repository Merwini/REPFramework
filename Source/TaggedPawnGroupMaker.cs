﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace rep.heframework
{
    public class TaggedPawnGroupMaker : PawnGroupMaker
    {
        public string groupName;

        List<string> upgradesFrom;

        public int groupTier;

        //Intended use is that if the PawnGroupMaker has multiple tags, ALL tags must be present in Expansion sites
        public List<string> requiredSiteTags = new List<string>();

        //Since HE raids will be more intentional that normal raids, use inclusive lists instead of exclusive ones
        public List<RaidStrategyDef> allowedRaidStrategies;

        //Want to be able to use vanilla RaidStrategies without being tied to their arrival modes
        public List<PawnsArrivalModeDef> allowedArrivalModes;
    }
}