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

        bool usingFallbackSpawnPoint = false;

        Map map;

        public override void Generate(Map map, GenStepParams parms)
        {
            this.map = map;
            List<SpawnCounter> spawnPoints = FindSpawnPoints(map);

            Faction faction = GetMapFaction(map, parms);
            if (faction == null)
            {
                Log.Error("Tried to generate pawns for new map, but could not find the faction that owns the map");
                return;
            }

            FactionDef factionDef = faction.def;

            PawnGroupMakerExtensionHEF factionExtension = factionDef.GetModExtension<PawnGroupMakerExtensionHEF>();
            if (factionExtension == null)
            {
                Log.Error("Tried to generate pawns for new map, but faction lacks a PawnGroupMakerExtension.");
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

            //TODO get a threat point target which will be modified by active world objects and quests
            //for sites it will be a modification of parms.threatPoints, for Settlements it will pull from the WorldObjectExtension
            float threatPointTarget = 0;


            float threatPoints = GetClampedThreatPoints(parms, objectExtension, threatPointTarget);
            if (threatPoints < 0f)
            {
                Log.Error("Tried to generate pawns for new map, but could not calculate threat points for pawns");
            }

            List<Pawn> pawns = GeneratePawnsFromGroupMaker(map, pawnGroupMaker, faction, threatPoints);
            if (pawns.NullOrEmpty())
            {
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
                //loop through spawn locations until a valid one is found, add the default if all locations are exhausted
                bool lookingForSpawn = true;
                while (lookingForSpawn)
                {
                    //make sure we haven't run out of valid spawn points
                    if (spawns.Count == 0)
                    {
                        AddDefaultSpawnPoint(spawns);
                    }

                    //reset the index if it is larger than the collection
                    if (locationIndex >= spawns.Count)
                    {
                        locationIndex = 0;
                    }

                    //check if the current index has spawns left, if not, remove it and continue
                    if (spawns[locationIndex].possibleSpawns == 0)
                    {
                        spawns.RemoveAt(locationIndex);
                        continue;
                    }

                    //if code has passed the prior checks, can now use the location and break the loop
                    if (spawnDict.TryGetValue(spawns[locationIndex], out List<Pawn> list))
                    {
                        list.Add(pawn);
                    }

                    lookingForSpawn = false;
                    locationIndex++;
                    continue;
                }
            }

            return spawnDict;
        }

        //TODO maybe the spawn points should be picked randomly instead of round-robin?
        void SpawnPawnsAtPoints(Dictionary<SpawnCounter, List<Pawn>> dict, Faction faction)
        {
            foreach (var pair in dict)
            {
                Lord lord = LordMaker.MakeNewLord(faction, new LordJob_DefendPoint(pair.Key.point), map);
                foreach (Pawn pawn in pair.Value)
                {
                    TrySpawnPawnAt(pawn, pair.Key.point, lord);
                }
            }
        }

        bool TrySpawnPawnAt(Pawn pawn, IntVec3 location, Lord lord)
        {
            IntVec3 cell;
            if (!RCellFinder.TryFindRandomCellNearWith(location, c => c.Walkable(map), map, out cell, 1, 4))
            {
                pawn.Discard();
                Log.Warning($"Unable to find cell to spawn pawn for new site at location {location.ToString()}, discarding this pawn");
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

        internal List<Pawn> GeneratePawnsFromGroupMaker(Map map, PawnGroupMaker pawnGroupMaker, Faction faction, float threatPoints)
        {
            if (Prefs.DevMode)
            {
                Log.Message("threat points used for pawn gen: " + threatPoints.ToString());
            }

            return pawnGroupMaker.GeneratePawns(new PawnGroupMakerParms
            {
                groupKind = PawnGroupKindDefOf.Combat,
                tile = map.Tile,
                faction = faction,
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
                MapToolExtensionHEF extension = (MapToolExtensionHEF)thing.def.GetModExtension<MapToolExtensionHEF>();
                if (extension != null)
                {
                    if (Prefs.DevMode)
                    {
                        Log.Message($"found spawner at {thing.Position.ToString()} with count {extension.count} and probability {extension.chance}");
                    }
                    spawnerPointThings.Add(thing);
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

            return spawnPoints;
        }

        public abstract Faction GetMapFaction(Map map, GenStepParams parms);

        public abstract PawnGroupMaker GetPawnGroupMaker(FactionDef factionDef, PawnGroupMakerExtensionHEF pext, WorldObjectExtensionHEF wext);

        public abstract float GetClampedThreatPoints(GenStepParams parms, WorldObjectExtensionHEF extension, float targetPoints);
    }

    public class SpawnCounter
    {
        public IntVec3 point = new IntVec3(0,0,0);

        public int possibleSpawns = -1;
    }
}