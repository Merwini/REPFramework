using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace rep.heframework
{
    public class MapToolDef : ThingDef
    {
        public int count = -1;

        public float chance = 1;

        public bool isSpawnPoint = false;
    }
}
