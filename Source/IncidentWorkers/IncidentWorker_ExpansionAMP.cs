
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace rep.framework
{
    public class IncidentWorker_ExpansionAMP : IncidentWorker
    {
        internal List<Faction> ampFactions;
        //internal List<SitePartDef> existingAMPSites;
        internal List<SitePartDef> eligibleAMPSites;
        
        internal SitePartDef ampDef;
        internal int tile;
        internal Site site;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return (float)GenDate.DaysPassedSinceSettle >= AMP_Settings.earliestExpansionDays;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryResolveExpansionFaction(parms))
                return false;

            if (!TryResolveExpansionDef(parms))
                return false;

            if (!TrySelectTile())
                return false;

            if (!TryMakeSite(parms))
                return false;

            if (!TryPlaceSite())
                return false;

            SendStandardLetter(parms, site, parms.faction.Name, site.Label, ""); //TODO set up letter and translate //TODO find a way to sneak explanations into site info

            return true;
        }

        internal bool TryResolveExpansionFaction(IncidentParms parms)
        {
            //Early return for debug tool forcing a chosen faction
            if (parms.faction != null && parms.faction.def is AMP_FactionDef)
            {
                return true;
            }

            //Get a list of AMP factions. Incident fails if no legal targets are available.
            ampFactions = Find.FactionManager.AllFactions.Where(f => (f.def is AMP_FactionDef)).ToList();
            if (!AMP_Settings.FriendlyAMPsCanExpand)
            {
                ampFactions = ampFactions.Where(f => f.HostileTo(Find.FactionManager.OfPlayer)).ToList();
            }

            if (ampFactions.NullOrEmpty())
                return false;

            return ampFactions.TryRandomElement(out parms.faction);
        }

        internal virtual bool TryResolveExpansionDef(IncidentParms parms)
        {
            eligibleAMPSites = AMP_Utils.FindEligibleAMPSiteDefsFor(parms.faction);
            if (eligibleAMPSites.NullOrEmpty())
                return false;

            //TODO custom expansion patterns for different storytellers? e.x. has to spawn X minor expansions before doing a major one, random, spawn in a set pattern
            ampDef = eligibleAMPSites.RandomElementByWeight(s => s.selectionWeight);

            return ampDef != null;
        }

        internal bool TrySelectTile()
        {
            TileFinder.TryFindNewSiteTile(out tile, AMP_Settings.minimumExpansionDistance, AMP_Settings.maximumExpansionDistance, false, TileFinderMode.Near);

            return tile >= 0;
        }
        
        internal bool TryMakeSite(IncidentParms parms)
        {
            site = SiteMaker.MakeSite(ampDef, tile, parms.faction);
            site.sitePartsKnown = true; //TODO site that hides information for sites

            return site != null;
        }

        internal bool TryPlaceSite()
        {
            Find.WorldObjects.Add(site);
            return true;
        }
    }

}

