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
    class IncidentWorker_ArtilleryCamp : IncidentWorker
    {
        public override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            
            // Only 1 siege at a time. We can't be too cruel.
            if (map.GetComponent<MapComponent_ArtillerySiege>() == null)
            {
                map.components.Add(new MapComponent_ArtillerySiege(map));

                // This is so inefficient
                Find.WorldObjects.Sites.FirstOrDefault(site => site.Tile == parms.podOpenDelay);
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
