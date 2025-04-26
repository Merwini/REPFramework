using RimWorld;
using RimWorld.Planet;
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
    public abstract class GenStep_TaggedPawnGroup : GenStep
    {
        public override int SeedPart => 987654321; //I have no idea if this matters or can be any number

        public float threatPointAdjustmentFlat = 0;

        public string pawnGroupMakerName;

        Map map;

        public override void Generate(Map map, GenStepParams parms)
        {
            this.map = map;
            List<SpawnCounter> spawnPoints = FindSpawnPoints(map);

            Faction faction = GetMapFaction(map, parms);
            if (faction == null)
            {
                Log.Error($"Tried to generate pawns for new map, but could not find the faction that owns the map");
                return;
            }

            FactionDef factionDef = faction.def;

            PawnGroupMakerExtensionHEF factionExtension = factionDef.GetModExtension<PawnGroupMakerExtensionHEF>();
            if (factionExtension == null)
            {
                Log.Error($"Tried to generate pawns for new map, but def {factionDef.defName} for faction {faction.Name} lacks a PawnGroupMakerExtension.");
                return;
            }

            WorldObjectExtensionHEF objectExtension = HEF_Utils.GetWorldObjectExtension(factionDef, parms);
            if (objectExtension == null)
            {
                Log.Error("Tried to generate pawns for new map, but world object (site or settlement) lacks a WorldObjectExtension");
                return;
            }

            PawnGroupMaker pawnGroupMaker = GetPawnGroupMaker(factionDef, factionExtension, objectExtension);
            if (pawnGroupMaker == null)
            {
                Log.Error("Tried to generate pawns for new map, but could not pick a PawnGroupMaker");
                return;
            }

            // Want to preserve this number for logging
            float modifiedThreatPoints = parms.sitePart.parms.threatPoints * HEF_Utils.GetThreatPointModifierWithSites(HEF_Utils.FindExistingHEFSiteDefsFor(faction));
            
            float clampedThreatPoints = objectExtension.defenderThreatPointsRange.ClampToRange(modifiedThreatPoints);
            if (clampedThreatPoints < 0f)
            {
                Log.Warning("While trying to generate pawns for new map, threat points ended up 0 or negative.");
            }

            if (HEF_Settings.debugLogging)
            {
                Log.Message($"Generating map defenders using {clampedThreatPoints} points after clamping. Was {modifiedThreatPoints} after modified by world sites. {parms.sitePart.parms.threatPoints} originally.");
            }

            List<Pawn> pawns = GeneratePawnsFromGroupMaker(map, pawnGroupMaker, faction, clampedThreatPoints);
            if (pawns.NullOrEmpty())
            {
                Log.Warning($"While trying to generate pawns for new map, failed to generate any pawns using {clampedThreatPoints} threat points");
                return;
            }

            Dictionary<SpawnCounter, List<Pawn>> spawnDict = SplitPawnsUp(spawnPoints, pawns);

            SpawnPawnsAtPoints(spawnDict, faction);
        }

        Dictionary<SpawnCounter, List<Pawn>> SplitPawnsUp(List<SpawnCounter> spawns, List<Pawn> pawns)
        {
            Dictionary<SpawnCounter, List<Pawn>> spawnDict = new Dictionary<SpawnCounter, List<Pawn>>();
            spawns.Shuffle();

            //initial dictionary add with some cleaning
            foreach (SpawnCounter spawn in spawns)
            {
                if (spawn.possibleSpawns != 0)
                {
                    spawnDict.Add(spawn, new List<Pawn>());
                }
            }

            int locationIndex = 0;
            foreach (Pawn pawn in pawns)
            {
                // Loop through spawn locations until a valid one is found, add the default if all locations are exhausted
                while (true)
                {
                    // If all spawn points are exhausted, add the Default spawn point at map center.
                    if (spawns.Count == 0)
                    {
                        AddDefaultSpawnPoint(spawns, spawnDict);
                    }

                    // Reset the index if it is larger than the collection
                    if (locationIndex >= spawns.Count)
                    {
                        locationIndex = 0;
                    }

                    // If spawn point at current index is exhausted, remove it from the list and continue
                    if (spawns[locationIndex].possibleSpawns == 0)
                    {
                        spawns.RemoveAt(locationIndex);
                        continue;
                    }

                    // If spawn point has passed the prior checks, add a pawn to that spawn point, and break to the next pawn
                    if (spawnDict.TryGetValue(spawns[locationIndex], out List<Pawn> list))
                    {
                        list.Add(pawn);
                    }

                    locationIndex++;
                    break;
                }
            }

            return spawnDict;
        }

        void SpawnPawnsAtPoints(Dictionary<SpawnCounter, List<Pawn>> dict, Faction faction)
        {
            foreach (var entry in dict)
            {
                Lord lord = LordMaker.MakeNewLord(faction, new LordJob_DefendPoint(entry.Key.point), map);
                foreach (Pawn pawn in entry.Value)
                {
                    TrySpawnPawnAt(pawn, entry.Key.point, lord);
                }
            }
        }

        bool TrySpawnPawnAt(Pawn pawn, IntVec3 location, Lord lord)
        {
            IntVec3 cell;
            if (!RCellFinder.TryFindRandomCellNearWith(location, c => c.Walkable(map), map, out cell, 1, 4))
            {
                pawn.Discard();
                Log.Warning($"Unable to find cell to spawn pawn for at location {location}, discarding this pawn");
                return false;
            }
            GenSpawn.Spawn(pawn, cell, map);
            lord.AddPawn(pawn);

            return true;
        }

        void AddDefaultSpawnPoint(List<SpawnCounter> spawnLocations, Dictionary<SpawnCounter, List<Pawn>> dict)
        {
            Log.Warning("Map has run out of valid spawn locations. Adding a fallback spawn point at the center.");
            spawnLocations.Add(new SpawnCounter { point = map.Center });
            dict[spawnLocations[0]] = new List<Pawn>();
        }

        internal List<Pawn> GeneratePawnsFromGroupMaker(Map map, PawnGroupMaker pawnGroupMaker, Faction faction, float threatPoints)
        {
            return pawnGroupMaker.GeneratePawns(new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Combat,
                tile = map.Tile,
                faction = faction,
                points = threatPoints
            }).ToList();
        }

        public List<SpawnCounter> FindSpawnPoints(Map map)
        {
            List<SpawnCounter> spawnPoints = new List<SpawnCounter>();
            List<Thing> spawnerPointThings = new List<Thing>();

            StringBuilder sb = new StringBuilder();

            if (HEF_Settings.debugLogging)
            {
                sb.AppendLine($"GenStep_TaggedPawnGroup.FindSpawnPoints found the following spawn points:");
            }

            foreach (Thing thing in map.listerThings.AllThings)
            {
                MapToolExtensionHEF extension = thing.def.GetModExtension<MapToolExtensionHEF>();
                if (extension != null)
                {
                    spawnerPointThings.Add(thing);

                    if (HEF_Settings.debugLogging)
                    {
                        sb.AppendLine($"position: {thing.Position}    count: {extension.count}    chance: {extension.chance}");
                    }

                    if (Rand.Value <= extension.chance)
                    {
                        SpawnCounter spawnCounter = new SpawnCounter() { point = thing.Position, possibleSpawns = extension.count };
                        spawnPoints.Add(spawnCounter);
                    }
                }
            }

            if (!spawnerPointThings.NullOrEmpty())
            {
                foreach (Thing thing in spawnerPointThings)
                {
                    thing.Destroy();
                }
            }

            if (HEF_Settings.debugLogging)
            {
                Log.Message(sb.ToString());
            }

            return spawnPoints;
        }

        public abstract Faction GetMapFaction(Map map, GenStepParams parms);

        public abstract PawnGroupMaker GetPawnGroupMaker(FactionDef factionDef, PawnGroupMakerExtensionHEF pext, WorldObjectExtensionHEF wext);
    }

    public class SpawnCounter
    {
        public IntVec3 point = new IntVec3(0,0,0);

        public int possibleSpawns = -1;
    }
}