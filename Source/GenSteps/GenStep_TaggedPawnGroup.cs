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

        bool usingFallbackSpawnPoint = false;
        //private int nextSpawnPointIndex = 0;

        Map map;

        //public int NextSpawnPointIndex
        //{
        //    get
        //    {
        //        int num = nextSpawnPointIndex;
        //        if (nextSpawnPointIndex >= spawnPoints.Count - 1)
        //        {
        //            nextSpawnPointIndex = 0;
        //        }
        //        else
        //        {
        //            nextSpawnPointIndex++;
        //        }

        //        return num;
        //    }
        //}
        public override void Generate(Map map, GenStepParams parms)
        {
            Faction faction = parms.sitePart.site.Faction;
            FactionDef factionDef = faction.def;
            this.map = map;
            PawnGroupMakerExtension extension = factionDef.GetModExtension<PawnGroupMakerExtension>();
            List<SpawnCounter> spawnPoints = FindSpawnPoints(map);

            if (extension == null)
            {
                Log.Error("Tried to generate pawns for site " + parms.sitePart.site.Label + ", but faction lacks a PawnGroupMakerExtension.");
                return;
            }

            //TODO see if need a different lord for each spawn point
            Lord lord = LordMaker.MakeNewLord(faction, new LordJob_DefendBase(faction, map.Center), map);
            List<Pawn> pawns = GeneratePawnsFromTaggedGroup(map, extension, pawnGroupMakerName, parms);

            if (pawns == null)
            {
                return; //will have already errored in the GeneratePawnsFromTaggedGroup method. Not sure if LordJob with no pawns will cause errors.
            }

            SpawnPawnsAtPoints(pawns, spawnPoints, lord);
        }

        //TODO maybe the spawn points should be picked randomly instead of round-robin?
        //TODO maybe the pawns list should be divided into a List<List<Pawn>> and then all spawned at the end?
        void SpawnPawnsAtPoints(List<Pawn> pawns, List<SpawnCounter> spawnLocations, Lord lord)
        {
            Log.Warning($"pawns {pawns.Count}");
            Log.Warning($"spawns {spawnLocations.Count}");
            int locationIndex = 0;
            bool stopSpawning = false;

            foreach (Pawn pawn in pawns)
            {
                if (stopSpawning)
                {
                    break;
                }

                bool tryingToSpawn = true;
                while (tryingToSpawn)
                {
                    //loop through spawn locations until a valid one is found, add the default if all locations are exhausted
                    bool lookingForPoint = true;
                    while (lookingForPoint)
                    {
                        //make sure we haven't run out of valid spawn points
                        if (spawnLocations.Count == 0)
                        {
                            AddDefaultSpawnPoint(spawnLocations);
                        }

                        //reset the index if it is larger than the collection
                        if (locationIndex >= spawnLocations.Count)
                        {
                            locationIndex = 0;
                        }

                        //check if the current index has spawns left, if not, remove it and continue
                        if (spawnLocations[locationIndex].possibleSpawns == 0)
                        {
                            spawnLocations.RemoveAt(locationIndex);
                            continue;
                        }

                        //if code has passed the prior checks, can now use the location and break the loop
                        lookingForPoint = false;

                        continue;
                    }

                    if (!TrySpawnPawnAt(pawn, spawnLocations[locationIndex].point, lord))
                    {
                        //if no usable cell is available at that location, remove that spawn point and go back to looking for one. If the fallback was already being used, throw an error and discontinue spawning
                        if (usingFallbackSpawnPoint)
                        {
                            Log.Error("Unable to find cell to spawn pawn for new site, and was already using fallback. Giving up on spawning more pawns.");
                            stopSpawning = true;
                            break;
                        }
                        else
                        {
                            spawnLocations.RemoveAt(locationIndex);
                            continue;
                        }
                    }
                    //if location was used, increment the index now and break the outer loop
                    else
                    {
                        locationIndex++;
                        tryingToSpawn = false;
                    }
                }


            }

            //return true;
        }

        bool TrySpawnPawnAt(Pawn pawn, IntVec3 location, Lord lord)
        {
            IntVec3 cell;
            if (!RCellFinder.TryFindRandomCellNearWith(location, c => c.Walkable(map), map, out cell, 1, 4))
            {
                pawn.Discard();
                Log.Warning($"Unable to find cell to spawn pawn for new site at location {location.ToString()}, removing this as a spawn point");
                return false;
            }
            GenSpawn.Spawn(pawn, cell, map);
            lord.AddPawn(pawn);

            return true;
        }

        void AddDefaultSpawnPoint(List<SpawnCounter> spawnLocations)
        {
            Log.Warning("Map has run out of valid spawn locations. Adding a fallback spawn point at the center.");
            spawnLocations.Add(new SpawnCounter { point = map.Center });
            usingFallbackSpawnPoint = true;
        }

        internal List<Pawn> GeneratePawnsFromTaggedGroup(Map map, PawnGroupMakerExtension extension, string pawnGroupMakerName, GenStepParams parms)
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
            }).ToList();
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

        public List<SpawnCounter> FindSpawnPoints(Map map)
        {
            List<SpawnCounter> spawnPoints = new List<SpawnCounter>();
            List<Thing> spawnerPointThings = new List<Thing>();

            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing is MapToolBuilding spawner && spawner.isSpawnPoint)
                {
                    spawnerPointThings.Add(thing);
                    if (Rand.Value <= spawner.chance)
                    {
                        SpawnCounter spawnCounter = new SpawnCounter() { point = spawner.Position, possibleSpawns = spawner.count };
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

            spawnPoints.Shuffle();
            return spawnPoints;
        }
    }

    public class SpawnCounter
    {
        public IntVec3 point = new IntVec3(0,0,0);

        public int possibleSpawns = -1;
    }
}