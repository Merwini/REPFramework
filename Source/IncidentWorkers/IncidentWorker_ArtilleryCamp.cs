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
            // IncidentWorker_ExpansionHE passes a HE_IncidentParms with copied data from the original IncidentParms
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

            WorldObjectExtensionHE objectExtension = heParms.site.MainSitePartDef.GetModExtension<WorldObjectExtensionHE>();
            if (objectExtension == null)
            {
                if (HE_Settings.debugLogging)
                {
                    Log.Message($"IncidentWorker_ArtilleryCamp returning false due to missing WorldObjectExtensionHE on site {site.Label}");
                }
                return false;
            }

            PawnGroupMakerExtensionHE factionExtension = heParms.faction.def.GetModExtension<PawnGroupMakerExtensionHE>();
            if (factionExtension == null)
            {
                if (HE_Settings.debugLogging)
                {
                    Log.Message($"IncidentWorker_ArtilleryCamp returning false due to missing PawnGroupMakerExtensionHE on faction {parms.faction}");
                }
                return false;
            }

            MapComponent_ArtillerySiege comp = map.GetComponent<MapComponent_ArtillerySiege>();

            // TODO I think maps automatically get every component but I'm not sure. Maybe remove this later when I get the answer.
            if (comp == null)
            {
                map.components.Add(new MapComponent_ArtillerySiege(map));
            }

            TaggedPawnGroupMaker waveRaidGroup = null;
            float waveRaidPoints = 0;
            if (objectExtension.doWaveRaids)
            {
                waveRaidGroup = factionExtension.taggedPawnGroupMakers.FirstOrDefault(x => x.groupName != null && x.groupName == objectExtension.waveRaidGroupName);

                waveRaidPoints = heParms.points * objectExtension.waveRaidPointMultiplier;
            }

            TaggedPawnGroupMaker finalRaidGroup = null;
            float finalRaidPoints = 0;
            if (objectExtension.doFinalRaid)
            {
                finalRaidGroup = factionExtension.taggedPawnGroupMakers.FirstOrDefault(x => x.groupName != null && x.groupName == objectExtension.finalRaidGroupName);

                finalRaidPoints = heParms.points * objectExtension.finalRaidPointMultiplier;
            }

            if (!comp.StartArtillerySiege(
                siegingFaction: heParms.faction,
                sourceSite: heParms.site,
                parms: heParms,
                extension: objectExtension))
            {
                Log.Warning($"IncidentWorker_ArtilleryCamp failed to start artillery siege for site {site.Label} of faction {heParms.faction}.");
                return false;
            }

            return true;
        }

        public Settlement GetNearestSettlement(int tile)
        {
            return Find.WorldObjects.Settlements
                .Where(s => s.Faction == Faction.OfPlayer && s.HasMap)
                .OrderBy(s => Find.WorldGrid.ApproxDistanceInTiles(tile, s.Tile))
                .FirstOrDefault();
        }
    }
}
