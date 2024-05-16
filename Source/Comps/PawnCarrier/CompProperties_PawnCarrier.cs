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
    public class CompProperties_PawnCarrier : CompProperties
    {
        public bool autonomous = true;

        public bool releaseAllOnKilled = false;

        public bool releaseAllOnDowned = false;

        public int releaseNumberOnKilled = 0;

        public int releaseNumberOnDowned = 0;

        public bool infinitePawnSupply = false;

        public int ticksBetweenPawnRelease = 60;

        public int maxCapacity = 1;

        public int releaseAtOnce = 1;

        public bool spawnBasedOnFacing = true;

        public int spawnXOffsetFacingNorth= 0;

        public int spawnZOffsetFacingNorth = 0;

        public bool randomPawnKinds = true;

        public Type lordJobType;

        public bool joinParentLord = false;

        public bool sappers = false;

        public bool breachers = false;

        public bool useAvoidGridSmart = false;

        public bool canTimeOutOrFlee = false;

        public bool canKidnap = false;

        public bool canSteal = false;

        public bool canPickUpOpportunisticWeapons = false;

        public List<PawnKindDef> pawnKindsCarried = new List<PawnKindDef>();

        public CompProperties_PawnCarrier()
        {
            this.compClass = typeof(CompPawnCarrier);
        }


    }

    //TODO use this instead of List<PawnKindDef>
    //public class PawnKindCountPair
    //{
    //    PawnKindDef kind;
    //    int count = 1;
    //}
}