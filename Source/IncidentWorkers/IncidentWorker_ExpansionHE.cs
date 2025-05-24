
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
    public class IncidentWorker_ExpansionHE : IncidentWorker
    {
        internal Dictionary<Faction, List<SitePartDef>> factionEligibleSitesDict;
        
        internal SitePartDef sitePartDef;
        WorldObjectExtensionHE extension;
        internal int tile;
        internal Site site;

        public override bool CanFireNowSub(IncidentParms parms)
        {
            return (float)GenDate.DaysPassedSinceSettle >= HE_Settings.earliestExpansionDays;
        }

        public override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!PopulateFactionSiteDictionary(parms))
                return false;

            if (!TryResolveExpansionFaction(parms))
                return false;

            if (!TryResolveExpansionDef(parms))
                return false;

            extension = sitePartDef.GetModExtension<WorldObjectExtensionHE>();

            if (!TryAdjustPoints(parms))
                return false;

            if (!TrySelectTile())
                return false;

            if (!TryMakeSite(parms))
                return false;

            if (!TryPlaceSite())
                return false;

            if (!TryFireIncident(parms))
            {
                site.Destroy(); // since it would have already been placed
                return false;
            }

            SendStandardLetter(parms, site, parms.faction.Name, site.Label, ""); //TODO set up letter and translate //TODO find a way to sneak explanations into site info

            return true;
        }


        internal bool PopulateFactionSiteDictionary(IncidentParms parms)
        {
            StringBuilder sb = new StringBuilder();

            factionEligibleSitesDict = new Dictionary<Faction, List<SitePartDef>>();

            List<Faction> possibleFactions = new List<Faction>();

            // If debug tool forcing a chosen faction
            if (parms.faction != null && parms.faction.def.HasModExtension<PawnGroupMakerExtensionHEF>())
            {
                factionEligibleSitesDict[parms.faction] = HE_Utils.FindEligibleHESiteDefsFor(parms.faction);
                if (HE_Settings.debugLogging)
                {
                    sb.AppendLine($"PopulateFactionSiteDictionary had faction forced: {parms.faction.Name} with {factionEligibleSitesDict.TryGetValue(parms.faction).Count} sites available.");
                }
            }
            else
            {
                if (HE_Settings.debugLogging)
                {
                    sb.AppendLine("PopulateFactionSiteDictionary searching all factions for eligible sites.");
                }

                foreach (Faction fact in Find.FactionManager.AllFactions.Where(f => f.def.HasModExtension<PawnGroupMakerExtensionHEF>()))
                {
                    List<SitePartDef> list = HE_Utils.FindEligibleHESiteDefsFor(fact);
                    if (list.Count != 0)
                    {
                        factionEligibleSitesDict[fact] = list;
                        if (HE_Settings.debugLogging)
                        {
                            sb.AppendLine($"Added {fact.Name} with {list.Count} sites available.");
                        }
                    }
                }
            }
            if (HE_Settings.debugLogging)
            {
                Log.Message(sb.ToString());
            }

            return factionEligibleSitesDict.Any(x => x.Value.Count != 0);
        }

        internal bool TryResolveExpansionFaction(IncidentParms parms)
        {
            List<Faction> eligibleFactions = factionEligibleSitesDict.Keys
                .Where(f => HE_Settings.friendlyHEFsCanExpand || f.HostileTo(Find.FactionManager.OfPlayer))
                .ToList();

            if (eligibleFactions.NullOrEmpty())
            {
                if (HE_Settings.debugLogging)
                {
                    Log.Message("TryResolveExpansionFaction unable to choose a faction, no factions with available sites.");
                }
                return false;
            }

            eligibleFactions.TryRandomElement(out parms.faction);

            if (HE_Settings.debugLogging)
            {
                Log.Message($"TryResolveExpansionFaction selected faction: {parms.faction.Name}");
            }

            return true;
        }

        internal bool TryResolveExpansionDef(IncidentParms parms)
        {
            if (!factionEligibleSitesDict.TryGetValue(parms.faction, out List<SitePartDef> eligibleSitePartDefs) || eligibleSitePartDefs.NullOrEmpty())
            {
                if (HE_Settings.debugLogging)
                {
                    Log.Message($"TryResolveExpansionDef unable to choose a SitePartDef, didn't get any eligible sites for faction: {parms.faction}");
                }
                return false;
            }

            //TODO custom expansion patterns for different storytellers? e.x. has to spawn X minor expansions before doing a major one, random, spawn in a set pattern
            sitePartDef = eligibleSitePartDefs.RandomElementByWeight(s => s.selectionWeight);

            if (HE_Settings.debugLogging)
            {
                Log.Message($"TryResolveExpansionDef selected SitePartDef {sitePartDef.defName} for faction: {parms.faction.Name}");
            }

            return sitePartDef != null;
        }

        internal bool TryAdjustPoints(IncidentParms parms)
        {
            if (extension.threatPointCurve != null)
            {
                float defaultPoints = StorytellerUtility.DefaultThreatPointsNow(Find.World);
                parms.points = extension.threatPointCurve.Evaluate(defaultPoints) * SiteTuning.SitePointRandomFactorRange.RandomInRange;
                if (HE_Settings.debugLogging)
                {
                    Log.Message($"TryAdjustPoints threat points pre-curve: {defaultPoints}, post-curve: {parms.points}");
                }
            }

            return true;
        }

        internal bool TrySelectTile()
        {
            TileFinder.TryFindNewSiteTile(out tile, extension.minimumTileDistance, extension.maximumTileDistance, false, TileFinderMode.Near);

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

        internal bool TryFireIncident(IncidentParms parms)
        {
            if (extension.fireIncidentOnSpawn == null)
                return true;

            //todo do I need to check that the IncidentDef exists? I think it would cause an error at game start if the xml is wrong

            //todo do I need to adjust the parms?

            HE_IncidentParms heParms 

            parms.podOpenDelay = tile; // can't pass the site directly to the IncidentWorker, but it will make it easier to find the associated site

            return extension.fireIncidentOnSpawn.Worker.TryExecute(parms);
        }
    }
}

