
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace rep.heframework
{
    public class IncidentWorker_ExpansionHEF : IncidentWorker
    {
        internal List<Faction> hefFactions;
        internal List<SitePartDef> eligibleHEFSites;
        
        internal SitePartDef sitePartDef;
        internal int tile;
        internal Site site;

        public override bool CanFireNowSub(IncidentParms parms)
        {
            return (float)GenDate.DaysPassedSinceSettle >= HEF_Settings.earliestExpansionDays;
        }

        public override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryResolveExpansionFaction(parms))
                return false;

            if (!TryResolveExpansionDef(parms))
                return false;

            if (!TryAdjustPoints(parms))
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
            if (parms.faction != null && parms.faction.def.HasModExtension<PawnGroupMakerExtension>())
            {
                return true;
            }

            //Get a list of HEF factions. Incident fails if no legal targets are available.
            hefFactions = Find.FactionManager.AllFactions.Where(f => (f.def.HasModExtension<PawnGroupMakerExtension>())).ToList();
            if (!HEF_Settings.FriendlyHEFsCanExpand)
            {
                hefFactions = hefFactions.Where(f => f.HostileTo(Find.FactionManager.OfPlayer)).ToList();
            }

            if (hefFactions.NullOrEmpty())
            {
                Log.Warning("Tried to fire incident to create a Hostility Extended expansion, but no eligible factions were found.");
                return false;
            }

            return hefFactions.TryRandomElement(out parms.faction);
        }

        internal virtual bool TryResolveExpansionDef(IncidentParms parms)
        {
            eligibleHEFSites = HEF_Utils.FindEligibleHEFSiteDefsFor(parms.faction);
            if (eligibleHEFSites.NullOrEmpty())
            {
                //TODO try to find a different faction that can still do an expansion before returning false - cache faction list in TryResolveExpansionFaction and remove them if they can't place an expansion
                //TODO but also need to implement the repeatable expansions for e.g. adding threat points
                //TODO cull choices based on min and max threat point values, so ou can separate early-game and late-game sites
                Log.Warning($"Tried to fire incident to create a Hostility Extended expansion for {parms.faction}, but no eligible expansions were found.");
                return false;
            }

            //TODO custom expansion patterns for different storytellers? e.x. has to spawn X minor expansions before doing a major one, random, spawn in a set pattern
            //TODO able to configure minimum threat points (pre-curve? since parms.points are curved by SiteTuning.ThreatPointsToSiteThreatPointsCurve) for these sites
            sitePartDef = eligibleHEFSites.RandomElementByWeight(s => s.selectionWeight);

            return sitePartDef != null;
        }

        internal bool TryAdjustPoints(IncidentParms parms)
        {
            WorldObjectExtension extension = (WorldObjectExtension)sitePartDef.GetModExtension<WorldObjectExtension>();
            if (extension == null)
            {
                //TODO fallback, try a different one
                Log.Warning($"Tried to fire incident to create a Hostility Extended expansion, but selected expansion {sitePartDef.defName} is missing its WorldObjectExtension");
                return false;
            }

            if (extension.threatPointCurve != null)
            {
                float defaultPoints = StorytellerUtility.DefaultThreatPointsNow(Find.World);
                parms.points = extension.threatPointCurve.Evaluate(defaultPoints) * SiteTuning.SitePointRandomFactorRange.RandomInRange;
                if (Prefs.DevMode)
                {
                    Log.Message($"Making Hostility Expanded expansion. Threat points pre-curve: {defaultPoints}, post-curve: {parms.points}");
                }
            }

            return true;
        }

        internal bool TrySelectTile()
        {
            TileFinder.TryFindNewSiteTile(out tile, HEF_Settings.minimumExpansionDistance, HEF_Settings.maximumExpansionDistance, false, TileFinderMode.Near);

            return tile >= 0;
        }
        
        internal bool TryMakeSite(IncidentParms parms)
        {
            site = SiteMaker.MakeSite(sitePartDef, tile, parms.faction, threatPoints: parms.points);
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

