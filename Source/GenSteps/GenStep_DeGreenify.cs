﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;

namespace rep.heframework
{
    class GenStep_DeGreenify : GenStep
	{
		private static bool debug_WarnedMissingTerrain = false;

		public override int SeedPart => 87654321;

		ThingDef rockThing;
		ThingDef brickThing;
		TerrainDef rockyTerrain;
		TerrainDef flagstoneTerrain;
		TerrainDef tileTerrain;
		Map map;

        public override void Generate(Map map, GenStepParams parms)
        {
			this.map = map;
            rockThing = PickRockType(map);
            brickThing = StoneFromRock(rockThing);

			MapGenFloatGrid elevation = MapGenerator.Elevation;
			MapGenFloatGrid fertility = MapGenerator.Fertility;
			MapGenFloatGrid caves = MapGenerator.Caves;
			TerrainGrid terrainGrid = map.terrainGrid;
			foreach (IntVec3 allCell in map.AllCells)
            {
                DegreenifyTerrainAt(allCell, map, elevation, fertility, terrainGrid);
            }

			List<Thing> thingsToDegreenify = map.listerThings.AllThings.ToList();

			foreach (Thing thing in thingsToDegreenify)
            {
                DegreenifyThing(thing);
            }
        }

        private ThingDef PickRockType(Map map)
        {
            //TODO better compatibility with modded rock types?
            return Find.World.NaturalRockTypesIn(map.Tile).RandomElement() ?? HEDefOf.Granite;
        }

        private ThingDef StoneFromRock(ThingDef rockThing)
        {
            ThingDef brickThing;
            if (rockThing == HEDefOf.Limestone)
            {
                brickThing = HEDefOf.BlocksLimestone;
				rockyTerrain = HEDefOf.Limestone_Rough;
				flagstoneTerrain = HEDefOf.FlagstoneLimestone;
				tileTerrain = HEDefOf.TileLimestone;
            }
            else if (rockThing == HEDefOf.Slate)
            {
                brickThing = HEDefOf.BlocksSlate;
				rockyTerrain = HEDefOf.Slate_Rough;
				flagstoneTerrain = HEDefOf.FlagstoneSlate;
				tileTerrain = HEDefOf.TileSlate;
			}
            else if (rockThing == HEDefOf.Sandstone)
            {
                brickThing = HEDefOf.BlocksSandstone;
				rockyTerrain = HEDefOf.Sandstone_Rough;
				flagstoneTerrain = HEDefOf.FlagstoneSandstone;
				tileTerrain = HEDefOf.TileSandstone;
			}
            else if (rockThing == HEDefOf.Marble)
            {
                brickThing = HEDefOf.BlocksMarble;
				rockyTerrain = HEDefOf.Marble_Rough;
				flagstoneTerrain = HEDefOf.FlagstoneMarble;
				tileTerrain = HEDefOf.TileMarble;
			}
            else
            {
                brickThing = HEDefOf.BlocksGranite;
				rockyTerrain = HEDefOf.Granite_Rough;
				flagstoneTerrain = HEDefOf.FlagstoneGranite;
				tileTerrain = HEDefOf.TileGranite;
			}

            return brickThing;
        }

		private void DegreenifyTerrainAt(IntVec3 cell, Map map, MapGenFloatGrid elevation, MapGenFloatGrid fertility, TerrainGrid terrainGrid)
        {
			TerrainDef terrain = cell.GetTerrain(map);
			if (terrain == HEDefOf.HE_GreenSoil)
			{
				TerrainDef terrainDef;
				terrainDef = TerrainFrom(cell, map, elevation[cell], fertility[cell], preferRock: false);
				terrainGrid.SetTerrain(cell, terrainDef);
			}
			else if (terrain == HEDefOf.HE_GreenRockFloor)
            {
				terrainGrid.SetTerrain(cell, rockyTerrain);
			}

			else if (terrain == HEDefOf.HE_GreenFlagstone)
			{
				terrainGrid.SetTerrain(cell, flagstoneTerrain);
			}

			else if (terrain == HEDefOf.HE_GreenBrick)
			{
				terrainGrid.SetTerrain(cell, tileTerrain);
			}
		}

		private void DegreenifyThing(Thing thing)
		{
			//I think checking if it's a building first saves more CPU cycles than going straight into the == checks
			//Since e.g. plants would then only be checked once instead of twice
			if (thing is Building building)
			{
				if (building.def == HEDefOf.HE_GreenRock)
				{
					IntVec3 c = building.Position;
					building.DeSpawn();
					GenSpawn.Spawn(rockThing, c, map);
				}
				else if (building.def == HEDefOf.HE_GreenWall)
				{
					IntVec3 c = building.Position;
					building.DeSpawn();
					Thing newThing = ThingMaker.MakeThing(ThingDefOf.Wall, brickThing);
					GenSpawn.Spawn(newThing, c, map);
				}
			}
		}

        //Exact copy of vanilla
        public static TerrainDef TerrainFrom(IntVec3 c, Map map, float elevation, float fertility, bool preferRock)
        {
            TerrainDef terrainDef = null;
            BiomeDef biomeDef = map.BiomeAt(c);
            bool flag = map.TileInfo.Mutators.Any((TileMutatorDef m) => m.preventsPondGeneration);
            if (!map.TileInfo.Mutators.Any((TileMutatorDef m) => m.preventPatches))
            {
                foreach (TerrainPatchMaker terrainPatchMaker in biomeDef.terrainPatchMakers)
                {
                    if (!flag || !terrainPatchMaker.isPond)
                    {
                        terrainDef = terrainPatchMaker.TerrainAt(c, map, fertility);
                        if (terrainDef != null)
                        {
                            break;
                        }
                    }
                }
            }
            if (terrainDef == null)
            {
                if (elevation > 0.55f && elevation < 0.61f && !biomeDef.noGravel)
                {
                    terrainDef = biomeDef.gravelTerrain ?? TerrainDefOf.Gravel;
                }
                else if (elevation >= 0.61f)
                {
                    terrainDef = GenStep_RocksFromGrid.RockDefAt(c).building.naturalTerrain;
                }
            }
            if (terrainDef == null)
            {
                terrainDef = TerrainThreshold.TerrainAtValue(biomeDef.terrainsByFertility, fertility);
            }
            if (terrainDef == null)
            {
                if (!debug_WarnedMissingTerrain)
                {
                    Log.Error("No terrain found in biome " + biomeDef.defName + " for elevation=" + elevation + ", fertility=" + fertility);
                    debug_WarnedMissingTerrain = true;
                }
                terrainDef = TerrainDefOf.Sand;
            }
            if (preferRock && terrainDef.supportsRock)
            {
                terrainDef = GenStep_RocksFromGrid.RockDefAt(c).building.naturalTerrain;
            }
            return terrainDef;
        }
    }
}
