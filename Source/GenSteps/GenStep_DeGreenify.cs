using System;
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
            return Find.World.NaturalRockTypesIn(map.Tile).RandomElement() ?? HefDefOf.Granite;
        }

        private ThingDef StoneFromRock(ThingDef rockThing)
        {
            ThingDef brickThing;
            if (rockThing == HefDefOf.Limestone)
            {
                brickThing = HefDefOf.BlocksLimestone;
				rockyTerrain = HefDefOf.Limestone_Rough;
				flagstoneTerrain = HefDefOf.FlagstoneLimestone;
				tileTerrain = HefDefOf.TileLimestone;
            }
            else if (rockThing == HefDefOf.Slate)
            {
                brickThing = HefDefOf.BlocksSlate;
				rockyTerrain = HefDefOf.Slate_Rough;
				flagstoneTerrain = HefDefOf.FlagstoneSlate;
				tileTerrain = HefDefOf.TileSlate;
			}
            else if (rockThing == HefDefOf.Sandstone)
            {
                brickThing = HefDefOf.BlocksSandstone;
				rockyTerrain = HefDefOf.Sandstone_Rough;
				flagstoneTerrain = HefDefOf.FlagstoneSandstone;
				tileTerrain = HefDefOf.TileSandstone;
			}
            else if (rockThing == HefDefOf.Marble)
            {
                brickThing = HefDefOf.BlocksMarble;
				rockyTerrain = HefDefOf.Marble_Rough;
				flagstoneTerrain = HefDefOf.FlagstoneMarble;
				tileTerrain = HefDefOf.TileMarble;
			}
            else
            {
                brickThing = HefDefOf.BlocksGranite;
				rockyTerrain = HefDefOf.Granite_Rough;
				flagstoneTerrain = HefDefOf.FlagstoneGranite;
				tileTerrain = HefDefOf.TileGranite;
			}

            return brickThing;
        }

		private void DegreenifyTerrainAt(IntVec3 cell, Map map, MapGenFloatGrid elevation, MapGenFloatGrid fertility, TerrainGrid terrainGrid)
        {
			TerrainDef terrain = cell.GetTerrain(map);
			if (terrain == HefDefOf.HEF_GreenSoil)
			{
				TerrainDef terrainDef = null;
				terrainDef = TerrainFrom(cell, map, elevation[cell], fertility[cell], null, preferSolid: true);
				terrainGrid.SetTerrain(cell, terrainDef);
			}
			else if (terrain == HefDefOf.HEF_GreenRockFloor)
            {
				terrainGrid.SetTerrain(cell, rockyTerrain);
			}

			else if (terrain == HefDefOf.HEF_GreenFlagstone)
			{
				terrainGrid.SetTerrain(cell, flagstoneTerrain);
			}

			else if (terrain == HefDefOf.HEF_GreenBrick)
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
				if (building.def == HefDefOf.HEF_GreenRock)
				{
					IntVec3 c = building.Position;
					building.DeSpawn();
					GenSpawn.Spawn(rockThing, c, map);
				}
				else if (building.def == HefDefOf.HEF_GreenWall)
				{
					IntVec3 c = building.Position;
					building.DeSpawn();
					Thing newThing = ThingMaker.MakeThing(ThingDefOf.Wall, brickThing);
					GenSpawn.Spawn(newThing, c, map);
				}
			}
		}

		//Exact copy of vanilla
		private TerrainDef TerrainFrom(IntVec3 c, Map map, float elevation, float fertility, RiverMaker river, bool preferSolid)
		{
			TerrainDef terrainDef = null;
			if (river != null)
			{
				terrainDef = river.TerrainAt(c, recordForValidation: true);
			}
			if (terrainDef == null && preferSolid)
			{
				return GenStep_RocksFromGrid.RockDefAt(c).building.naturalTerrain;
			}
			TerrainDef terrainDef2 = BeachMaker.BeachTerrainAt(c, map.Biome);
			if (terrainDef2 == TerrainDefOf.WaterOceanDeep)
			{
				return terrainDef2;
			}
			if (terrainDef != null && terrainDef.IsRiver)
			{
				return terrainDef;
			}
			if (terrainDef2 != null)
			{
				return terrainDef2;
			}
			if (terrainDef != null)
			{
				return terrainDef;
			}
			for (int i = 0; i < map.Biome.terrainPatchMakers.Count; i++)
			{
				terrainDef2 = map.Biome.terrainPatchMakers[i].TerrainAt(c, map, fertility);
				if (terrainDef2 != null)
				{
					return terrainDef2;
				}
			}
			if (elevation > 0.55f && elevation < 0.61f)
			{
				return TerrainDefOf.Gravel;
			}
			if (elevation >= 0.61f)
			{
				return GenStep_RocksFromGrid.RockDefAt(c).building.naturalTerrain;
			}
			terrainDef2 = TerrainThreshold.TerrainAtValue(map.Biome.terrainsByFertility, fertility);
			if (terrainDef2 != null)
			{
				return terrainDef2;
			}
			if (!debug_WarnedMissingTerrain)
			{
				Log.Error("No terrain found in biome " + map.Biome.defName + " for elevation=" + elevation + ", fertility=" + fertility);
				debug_WarnedMissingTerrain = true;
			}
			return TerrainDefOf.Sand;
		}
	}
}
