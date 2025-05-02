using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using UnityEngine;

namespace rep.heframework
{
    public static class HEF_Utils
    {
        public static List<Faction> ReturnHEFFactions()
        {
            List<Faction> factions = Find.FactionManager.AllFactions.Where(f => (f.def.HasModExtension<PawnGroupMakerExtensionHEF>())).ToList();

            #region logging
            if (HEF_Settings.debugLogging)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"ReturnHEFFactions is returning:");
                foreach (Faction f in factions)
                {
                    sb.AppendLine($"name: {f.Name}     def: {f.def.defName}");
                    Log.Message(sb.ToString());
                }
            }
            #endregion

            return factions;
        }

        public static List<Faction> ReturnHostileHEFFactions()
        {
            List<Faction> hostileFactions = ReturnHEFFactions().Where(f => f.HostileTo(Find.FactionManager.OfPlayer)).ToList();

            #region logging
            if (HEF_Settings.debugLogging)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"ReturnHostileHEFFactions is returning:");
                foreach (Faction f in hostileFactions)
                {
                    sb.AppendLine($"name: {f.Name}    def: {f.def.defName}");
                    Log.Message(sb.ToString());
                }
            }
            #endregion

            return hostileFactions;
        }

        public static List<Site> FindHEFSitesFor(Faction fact)
        {
            List<Site> list = Find.WorldObjects.Sites
                .Where(site => site != null
                               && site.Faction == fact
                               && site.MainSitePartDef != null
                               && site.MainSitePartDef.HasModExtension<WorldObjectExtensionHEF>())
                .ToList();

            #region logging
            if (HEF_Settings.debugLogging)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"FindHEFSitesFor faction: {fact.Name} is returning:");
                foreach (Site s in list)
                {
                    sb.AppendLine($"label: {s.Label}    tile: {s.Tile}    def: {s.def.defName}");
                }
                Log.Message(sb.ToString());
            }
            #endregion

            return list;
        }

        public static Dictionary<SitePartDef, int> FindExistingHEFSiteDefsFor(Faction fact)
        {
            Dictionary<SitePartDef, int> defCounts = FindDefCountsForSites(FindHEFSitesFor(fact));

            #region logging
            if (HEF_Settings.debugLogging)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"FindExistingHEFSiteDefsFor faction: {fact.Name} is returning:");
                foreach (KeyValuePair<SitePartDef, int> kvp in defCounts)
                {
                    sb.AppendLine($"def: {kvp.Key.defName}, count: {kvp.Value}");
                }
                Log.Message(sb.ToString());
            }
            #endregion

            return defCounts;
        }

        public static Dictionary<SitePartDef, int> FindDefCountsForSites(List<Site> sites)
        {
            Dictionary<SitePartDef, int> defCounts = new Dictionary<SitePartDef, int>();

            if (sites != null)
            {
                foreach (Site site in sites)
                {
                    if (site?.MainSitePartDef != null)
                    {
                        SitePartDef def = site.MainSitePartDef;
                        defCounts.TryGetValue(def, out int count);
                        defCounts[def] = count + 1;
                    }
                }
            }

            #region logging
            if (HEF_Settings.debugLogging)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("FindDefCountsForSites is returning:");
                foreach (var kvp in defCounts)
                {
                    sb.AppendLine($"def: {kvp.Key.defName}, count: {kvp.Value}");
                }
                Log.Message(sb.ToString());
            }
            #endregion

            return defCounts;
        }

        public static List<SitePartDef> FindEligibleHEFSiteDefsFor(Faction fact)
        {
            Dictionary<SitePartDef, int> usedCounts = FindExistingHEFSiteDefsFor(fact);
            List<SitePartDef> eligibleDefs = new List<SitePartDef>();

            foreach (SitePartDef def in DefDatabase<SitePartDef>.AllDefs)
            {
                WorldObjectExtensionHEF extension = def.GetModExtension<WorldObjectExtensionHEF>();
                if (extension != null 
                    && extension.factionsToPawnGroups.Any(x => x.Faction == fact.def))
                    //TODO && cull choices based on min and max threat point values, so ou can separate early-game and late-game sites
                    //TODO implement a List<string> prerequisiteTags on WorldObjectExtensionHEF and check if those tags are present, to enable sites that require another site first
                {
                    {
                        int count = usedCounts.TryGetValue(def, out int c) ? c : 0;
                        if (count < extension.maximumSiteCount)
                        {
                            eligibleDefs.Add(def);
                        }
                    }
                }
            }

            #region logging
            if (HEF_Settings.debugLogging)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"FindEligibleHEFSiteDefsFor faction: {fact.Name} is returning:");
                foreach (SitePartDef spd in eligibleDefs)
                {
                    sb.AppendLine($"def: {spd.defName}");
                }
                Log.Message(sb.ToString());
            }
            #endregion

            return eligibleDefs;
        }

        public static List<string> FindStringsForDefs(List<SitePartDef> defs)
        {
            List<string> tags = defs?.Where(def => def != null)
                                     .SelectMany(def => def.tags)
                                     .ToList() ?? new List<string>();

            #region logging
            if (HEF_Settings.debugLogging)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"FindStringsForDefs is returning:");
                foreach (string tag in tags)
                {
                    sb.AppendLine($"tag: {tag}");
                }
                Log.Message(sb.ToString());
            }
            #endregion

            return tags;
        }

        public static bool CheckIfAllTagsPresent(List<string> pgmTags, List<string> siteTags)
        {
            foreach (string str in pgmTags)
            {
                if (!siteTags.Contains(str))
                {
                    return false;
                }
            }
            return true;
        }

        // debugTest represents whether the raid is being generated naturally or through dev tool, which would pre-fill some fields in parms
        // about half of this is copied from vanilla TryGenerateRaidInfo
        // this occurs after TryGenerateRaidInfo has already assigned parms.faction and diverted if that faction has PawnGroupMakerExtensionHEF
        public static bool TryGenerateExtendedRaidInfo(IncidentParms parms, out List<Pawn> pawns, bool debugTest = false)
        {
            Dictionary<SitePartDef, int> hefSiteDefs = FindDefCountsForSites(FindHEFSitesFor(parms.faction));

            // Due to the Expansion Site system, it will be easiest to select the PawnGroupMaker first, as there is otherwise a high probability that selected Strategy/ArrivalMode will have no legal PawnGroup
            if (!TryResolveTaggedPawnGroup(parms, hefSiteDefs.Keys.ToList(), out TaggedPawnGroupMaker groupMaker))
            {
                pawns = new List<Pawn>();
                return false;
            }

            ResolveRaidStrategy(parms, groupMaker);
            ResolveRaidArriveMode(parms, groupMaker);
            //ResolveRaidAgeRestriction(parms); //TODO - need to copy vanilla

            // vanilla, only used for generating mech cluster sketch I think
            if (!debugTest)
            {
                parms.raidStrategy.Worker.TryGenerateThreats(parms);
            }

            // vanilla
            if (!debugTest && !parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms))
            {
                pawns = null;
                return false;
            }

            float points = parms.points;
            parms.points = AdjustedRaidPoints(parms.points, hefSiteDefs, parms.raidArrivalMode, parms.raidStrategy, parms.faction, PawnGroupKindDefOf.Combat, parms.raidAgeRestriction);
            PawnGroupMakerParms defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms);
            pawns = GeneratePawns(defaultPawnGroupMakerParms, groupMaker).ToList();
            if (pawns.Count == 0)
            {
                if (debugTest)
                {
                    Log.Error("Got no pawns spawning raid from parms " + parms);
                }
                return false;
            }
            if (!debugTest)
            {
                parms.raidArrivalMode.Worker.Arrive(pawns, parms);
            }
            parms.pawnCount = pawns.Count;
            //PostProcessSpawnedPawns(parms, pawns); //TODO re-enable this for 1.5
            if (debugTest)
            {
                parms.target.StoryState.lastRaidFaction = parms.faction;
            }
            else
            {
                //GenerateRaidLoot(parms, points, pawns); //TODO copy vanilla
            }
            return true;
        }

        public static bool TryResolveTaggedPawnGroup(IncidentParms parms, List<SitePartDef> siteDefs, out TaggedPawnGroupMaker groupMaker)
        {
            StringBuilder sb = new StringBuilder();

            List<TaggedPawnGroupMaker> possibleGroupMakers = new List<TaggedPawnGroupMaker>();
            int highestTier = 0;

            List<string> hefTags = FindStringsForDefs(siteDefs);
            PawnGroupMakerExtensionHEF extension = parms.faction.def.GetModExtension<PawnGroupMakerExtensionHEF>();
            foreach (TaggedPawnGroupMaker tpgm in extension.taggedPawnGroupMakers)
            {
                if (CheckIfAllTagsPresent(tpgm.requiredSiteTags, hefTags))
                {
                    possibleGroupMakers.Add(tpgm);
                    if (tpgm.groupTier > highestTier)
                    {
                        highestTier = tpgm.groupTier;
                    }
                }
            }

            #region logging
            if (HEF_Settings.debugLogging)
            {
                sb.AppendLine($"TryResolveTaggedPawnGroup found the following potential TaggedPawnGroupMakers:");
                foreach (TaggedPawnGroupMaker tpgm in possibleGroupMakers)
                {
                    sb.AppendLine($"name: {tpgm.groupName ?? "unnamed"}");
                }
            }
            #endregion

            if (possibleGroupMakers.Count == 0)
            {
                groupMaker = null;
                #region logging
                if (HEF_Settings.debugLogging)
                {
                    sb.AppendLine("No valid TaggedPawnGroupMakers found.");
                    Log.Message(sb.ToString());
                }
                #endregion
                return false;
            }

            if (extension.alwaysUseHighestTier)
            {
                possibleGroupMakers = possibleGroupMakers.Where(x => x.groupTier == highestTier).ToList();
                #region moreLogging
                if (HEF_Settings.debugLogging)
                {
                    sb.AppendLine($"TryResolveTaggedPawnGroup using only highest tier. Tier: {highestTier} Possible groups in that tier:");
                    foreach (TaggedPawnGroupMaker tpgm in possibleGroupMakers)
                    {
                        sb.AppendLine($"name: {tpgm.groupName ?? "unnamed"}");
                    }
                }
                #endregion
            }

            possibleGroupMakers.TryRandomElementByWeight((TaggedPawnGroupMaker gm) => gm.commonality, out groupMaker);

            #region evenMoreLogging
            if (HEF_Settings.debugLogging)
            {
                sb.AppendLine($"TryResolveTaggedPawnGroup final groupMaker selection: {groupMaker.groupName ?? "unnamed"}");
                Log.Message(sb.ToString());
            }
            #endregion

            return groupMaker != null;
        }

        public static void ResolveRaidStrategy(IncidentParms parms, TaggedPawnGroupMaker groupMaker)
        {
            if (parms.raidStrategy == null)
            {
                parms.raidStrategy = groupMaker.allowedRaidStrategies.RandomElement(); //TODO consider using point curve weight
            }

            #region logging
            if (HEF_Settings.debugLogging)
            {
                Log.Message($"ResolveRaidStrategy selected: {parms.raidStrategy.defName}");
            }
            #endregion
        }

        public static void ResolveRaidArriveMode(IncidentParms parms, TaggedPawnGroupMaker groupMaker)
        {
            if (parms.raidArrivalMode == null)
            {
                parms.raidArrivalMode = groupMaker.allowedArrivalModes.RandomElement();
            }

            #region logging
            if (HEF_Settings.debugLogging)
            {
                Log.Message($"ResolveRaidArriveMode selected: {parms.raidArrivalMode.defName}");
            }
            #endregion
        }

        // Mostly copied from vanilla, with a multiplier based on world sites
        public static float AdjustedRaidPoints(float points, Dictionary<SitePartDef, int> sites, PawnsArrivalModeDef raidArrivalMode, RaidStrategyDef raidStrategy, Faction faction, PawnGroupKindDef groupKind, RaidAgeRestrictionDef ageRestriction = null)
        {
            float siteModifiedPoints = points * GetThreatPointModifierWithSites(sites);

            float curvedPoints = siteModifiedPoints;


            if (raidArrivalMode.pointsFactorCurve != null)
            {
                curvedPoints *= raidArrivalMode.pointsFactorCurve.Evaluate(points);
            }
            if (raidStrategy.pointsFactorCurve != null)
            {
                curvedPoints *= raidStrategy.pointsFactorCurve.Evaluate(points);
            }
            if (ageRestriction != null)
            {
                curvedPoints *= ageRestriction.threatPointsFactor;
            }
            siteModifiedPoints = Mathf.Max(points, raidStrategy.Worker.MinimumPoints(faction, groupKind) * 1.05f);

            #region logging
            if (HEF_Settings.debugLogging)
            {
                Log.Message($"AdjustedRaidPoints original: {points}, modified by world sites: {siteModifiedPoints}, post-curves: {curvedPoints}");
            }
            #endregion

            return curvedPoints;
        }

        public static IEnumerable<Pawn> GeneratePawns(PawnGroupMakerParms parms, PawnGroupMaker groupMaker, bool warnOnZeroResults = true)
        {
            if (parms.groupKind == null)
            {
                Log.Error("Tried to generate pawns with null pawn group kind def. parms=" + parms);
                yield break;
            }
            if (parms.faction == null)
            {
                Log.Error("Tried to generate pawn kinds with null faction. parms=" + parms);
                yield break;
            }
            foreach (Pawn item in groupMaker.GeneratePawns(parms, warnOnZeroResults))
            {
                yield return item;
            }
        }

        public static WorldObjectExtensionHEF GetWorldObjectExtension(FactionDef factionDef, GenStepParams parms)
        {
            if (parms.sitePart != null)
            {
                return parms.sitePart?.def.GetModExtension<WorldObjectExtensionHEF>();
            }

            // Getting the extension for a faction settlement instead of site
            return factionDef.GetModExtension<WorldObjectExtensionHEF>();
        }

        public static float GetThreatPointModifierWithSites(Dictionary<SitePartDef, int> siteDefsWithCounts)
        {
            float modifier = 1f;
            int totalInstances = 0;

            foreach (KeyValuePair<SitePartDef, int> entry in siteDefsWithCounts)
            {
                SitePartDef spd = entry.Key;
                int count = entry.Value;
                totalInstances += count;

                WorldObjectExtensionHEF extension = spd.GetModExtension<WorldObjectExtensionHEF>();
                if (extension != null)
                {
                    modifier += extension.threatPointModifier * count;
                }
            }

            if (HEF_Settings.debugLogging)
            {
                Log.Message($"GetThreatPointModifierWithSites is returning a modifier of {modifier} based on {totalInstances} site instances");
            }

            return modifier;
        }
    }
}