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
    [DefOf]
    public class HefDefOf
    {
        public static MapToolDef HEF_SpawnPoint;
        public static MapToolDef HEF_SpawnPointRandom;
        public static MapToolDef HEF_SpawnPointSingle;
        public static MapToolDef HEF_SpawnPointSingleRandom;

        public static ThingDef HEF_GreenWall;
        public static ThingDef HEF_GreenRock;
        public static TerrainDef HEF_GreenSoil;
        public static TerrainDef HEF_GreenRockFloor;
        public static TerrainDef HEF_GreenFlagstone;
        public static TerrainDef HEF_GreenBrick;

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