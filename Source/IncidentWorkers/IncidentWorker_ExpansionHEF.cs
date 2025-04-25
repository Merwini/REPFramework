
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
        internal List<SitePartDef> eligibleSitePartDefs;

        internal Dictionary<Faction, List<SitePartDef>> factionEligibleSitesDict;
        
        internal SitePartDef sitePartDef;
        internal int tile;
        internal Site site;

        public override bool CanFireNowSub(IncidentParms parms)
        {
            return (float)GenDate.DaysPassedSinceSettle >= HEF_Settings.earliestExpansionDays;
        }

        public override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!PopulateFactionSiteDictionary(parms))
                return false;

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


        internal bool PopulateFactionSiteDictionary(IncidentParms parms)
        {
            StringBuilder sb = new StringBuilder();

            factionEligibleSitesDict = new Dictionary<Faction, List<SitePartDef>>();

            List<Faction> possibleFactions = new List<Faction>();

            // If debug tool forcing a chosen faction
            if (parms.faction != null && parms.faction.def.HasModExtension<PawnGroupMakerExtensionHEF>())
            {
                factionEligibleSitesDict[parms.faction] = HEF_Utils.FindEligibleHEFSiteDefsFor(parms.faction);
                if (HEF_Settings.debugLogging)
                {
                    sb.AppendLine($"PopulateFactionSiteDictionary had faction forced: {parms.faction.Name} with {factionEligibleSitesDict.TryGetValue(parms.faction).Count} sites available.");
                }
            }
            else
            {
                if (HEF_Settings.debugLogging)
                {
                    sb.AppendLine("PopulateFactionSiteDictionary searching all factions for eligible sites.");
                }

                foreach (Faction fact in Find.FactionManager.AllFactions.Where(f => f.def.HasModExtension<PawnGroupMakerExtensionHEF>()))
                {
                    List<SitePartDef> list = HEF_Utils.FindEligibleHEFSiteDefsFor(fact);
                    if (list.Count != 0)
                    {
                        factionEligibleSitesDict[fact] = list;
                        if (HEF_Settings.debugLogging)
                        {
                            sb.AppendLine($"Added {fact.Name} with {list.Count} sites available.");
                        }
                    }
                }
            }
            if (HEF_Settings.debugLogging)
            {
                Log.Message(sb.ToString());
            }

            return factionEligibleSitesDict.Any(x => x.Value.Count != 0);
        }

        internal bool TryResolveExpansionFaction(IncidentParms parms)
        {
            List<Faction> eligibleFactions = factionEligibleSitesDict.Keys
                .Where(f => HEF_Settings.FriendlyHEFsCanExpand || f.HostileTo(Find.FactionManager.OfPlayer))
                .ToList();

            if (eligibleFactions.NullOrEmpty())
            {
                if (HEF_Settings.debugLogging)
                {
                    Log.Message("TryResolveExpansionFaction unable to choose a faction, no factions with available sites.");
                }
                return false;
            }

            eligibleFactions.TryRandomElement(out parms.faction);

            if (HEF_Settings.debugLogging)
            {
                Log.Message($"TryResolveExpansionFaction selected faction: {parms.faction.Name}");
            }

            return true;
        }

        internal bool TryResolveExpansionDef(IncidentParms parms)
        {
            if (!factionEligibleSitesDict.TryGetValue(parms.faction, out eligibleSitePartDefs) || eligibleSitePartDefs.NullOrEmpty())
            {
                if (HEF_Settings.debugLogging)
                {
                    Log.Message($"TryResolveExpansionDef unable to choose a SitePartDef, didn't get any eligible sites for faction: {parms.faction}");
                }
                return false;
            }

            //TODO custom expansion patterns for different storytellers? e.x. has to spawn X minor expansions before doing a major one, random, spawn in a set pattern
            sitePartDef = eligibleSitePartDefs.RandomElementByWeight(s => s.selectionWeight);

            if (HEF_Settings.debugLogging)
            {
                Log.Message($"TryResolveExpansionDef selected SitePartDef {sitePartDef.defName} for faction: {parms.faction.Name}");
            }

            return sitePartDef != null;
        }

        internal bool TryAdjustPoints(IncidentParms parms)
        {
            WorldObjectExtensionHEF extension = (WorldObjectExtensionHEF)sitePartDef.GetModExtension<WorldObjectExtensionHEF>();
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

