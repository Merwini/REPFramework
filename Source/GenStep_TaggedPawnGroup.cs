using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using Verse.AI.Group;

namespace rep.heframework
{
    public class GenStep_TaggedPawnGroup : GenStep
    {
        public override int SeedPart => 987654321; //I have no idea if this matters or can be any number

        public FloatRange threatPointsRange = new FloatRange(240f, 10000f);

        public float threatPointAdjustmentFlat = 0;

        public string pawnGroupMakerName;

        public override void Generate(Map map, GenStepParams parms)
        {
            Faction faction = parms.sitePart.site.Faction;
            FactionDef factionDef = faction.def;
            PawnGroupMakerExtension extension = factionDef.GetModExtension<PawnGroupMakerExtension>();

            if (extension == null)
            {
                Log.Error("Tried to generate pawns for site " + parms.sitePart.site.Label + ", but faction lacks a PawnGroupMakerExtension.");
                return;
            }

            Lord lord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, map.Center), map);
            IEnumerable<Pawn> pawns = GeneratePawnsFromTaggedGroup(map, extension, pawnGroupMakerName, parms);

            if (pawns == null)
            {
                return; //will have already errored in the GeneratePawnsFromTaggedGroup method. Not sure if LordJob with no pawns will cause errors.
            }

            foreach (Pawn pawn in pawns)
            {
                IntVec3 c;
                if (!CellFinder.TryFindRandomSpawnCellForPawnNear(map.Center, map, out c))
                {
                    pawn.Discard();
                    Log.Error("Unable to find cell to spawn pawn for site " + parms.sitePart.site.Label);
                    break;
                }
                lord.AddPawn(pawn);
                GenSpawn.Spawn(pawn, c, map);
            }
        }

        internal IEnumerable<Pawn> GeneratePawnsFromTaggedGroup(Map map, PawnGroupMakerExtension extension, string pawnGroupMakerName, GenStepParams parms)
        {

            float threatPoints = ClampToRange(parms.sitePart.parms.threatPoints, threatPointsRange) + threatPointAdjustmentFlat;
            if (threatPoints < 0)
                threatPoints = 0;

            if (Prefs.DevMode)
            {
                Log.Message("threat points used for pawn gen: " + threatPoints.ToString());
            }

            if (pawnGroupMakerName == null)
            {
                Log.Warning("Tried generating pawns for site " + parms.sitePart.site.Label + ", but no pawnGroupMakerName was set. No pawns will be generated.");
                return null;
            }

            PawnGroupMaker groupMaker = extension.taggedPawnGroupMakers.FirstOrDefault(pgm => pgm.groupName == pawnGroupMakerName);

            if (groupMaker == null)
            {
                Log.Warning("Tried generating pawns for site " + parms.sitePart.site.Label + ", but no pawnGroupMaker could be selected with the name \"" + pawnGroupMakerName + "\". No pawns will be generated.");
                return null;
            }

            return groupMaker.GeneratePawns(new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Combat,
                tile = map.Tile,
                faction = parms.sitePart.site.Faction,
                points = threatPoints
            });
        }

        public float ClampToRange(float value, FloatRange range)
        {
            if (value < range.min)
            {
                return range.min;
            }
            else if (value > range.max)
            {
                return range.max;
            }
            else
            {
                return value;
            }
        }
    }
}