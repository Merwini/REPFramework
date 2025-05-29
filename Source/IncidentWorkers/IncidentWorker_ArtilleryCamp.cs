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
            HE_IncidentParms heParms = parms as HE_IncidentParms;
            Map map;

            if ((heParms.site == null) || !(heParms.site is Site site))
                return false;

            Settlement nearestSettlement = GetNearestSettlement(heParms.site.Tile); 

            if (nearestSettlement != null)
            {
                if (HE_Settings.debugLogging)
                {
                    Log.Message($"IncidentWorker_ArtilleryCamp Site tile: {heParms.site.Tile}, Closest player colony: {nearestSettlement.Label}");
                }
                map = nearestSettlement.Map;
            }
            else
            {
                return false;
            }

            //get the MapComponent_ArtillerySiege for the map
            MapComponent_ArtillerySiege comp = map.GetComponent<MapComponent_ArtillerySiege>();

            //set up a StartSiege method on the comp. Call it here
                //feed it faction, how many shells, how often, how long it last, raid info?

            return true;
        }

        public Settlement GetNearestSettlement(int tile)
        {
            return Find.WorldObjects.Settlements
                .Where(s => s.Faction == Faction.OfPlayer || s.HasMap)
                .OrderBy(s => Find.WorldGrid.ApproxDistanceInTiles(tile, s.Tile))
                .FirstOrDefault();
        }
    }
}
