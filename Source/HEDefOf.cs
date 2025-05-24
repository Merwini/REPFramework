using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace rep.HEramework
{
    [DefOf]
    public class HEDefOf
    {
        public static ThingDef HE_SpawnPoint;
        public static ThingDef HE_SpawnPointRandom;
        public static ThingDef HE_SpawnPointSingle;
        public static ThingDef HE_SpawnPointSingleRandom;

        public static ThingDef HE_GreenWall;
        public static ThingDef HE_GreenRock;
        public static TerrainDef HE_GreenSoil;
        public static TerrainDef HE_GreenRockFloor;
        public static TerrainDef HE_GreenFlagstone;
        public static TerrainDef HE_GreenBrick;

        public static ThingDef Wall;
        public static TerrainDef Soil;


        public static ThingDef Granite;
        public static ThingDef Limestone;
        public static ThingDef Slate;
        public static ThingDef Sandstone;
        public static ThingDef Marble;

        public static ThingDef ChunkGranite;

        public static ThingDef BlocksGranite;
        public static ThingDef BlocksLimestone;
        public static ThingDef BlocksSlate;
        public static ThingDef BlocksSandstone;
        public static ThingDef BlocksMarble;

        //_Rough TerrainDefs are generated as implied defs but should still be available by the time DefOf runs
        public static TerrainDef Granite_Rough;
        public static TerrainDef Limestone_Rough;
        public static TerrainDef Slate_Rough;
        public static TerrainDef Sandstone_Rough;
        public static TerrainDef Marble_Rough;

        public static TerrainDef FlagstoneGranite;
        public static TerrainDef FlagstoneLimestone;
        public static TerrainDef FlagstoneSlate;
        public static TerrainDef FlagstoneSandstone;
        public static TerrainDef FlagstoneMarble;

        public static TerrainDef TileGranite;
        public static TerrainDef TileLimestone;
        public static TerrainDef TileSlate;
        public static TerrainDef TileSandstone;
        public static TerrainDef TileMarble;
    }
}