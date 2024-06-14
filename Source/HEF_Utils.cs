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
            List<Faction> factions = Find.FactionManager.AllFactions.Where(f => (f.def.HasModExtension<PawnGroupMakerExtension>())).ToList();

            return factions;
        }

        public static List<Faction> ReturnHostileHEFFactions()
        {
            List<Faction> hostileFactions = ReturnHEFFactions().Where(f => f.HostileTo(Find.FactionManager.OfPlayer)).ToList();

            return hostileFactions;
        }

        public static List<Site> FindHEFSitesFor(Faction fact)
        {
            List<Site> hefSites = new List<Site>();

            List<Site> worldSites = Find.WorldObjects.Sites;
            foreach (Site site in worldSites)
            {
                if (site == null)
                    continue;
                if (site.Faction != fact)
                    continue;
                if (site.MainSitePartDef == null)
                    continue;
                if (site.MainSitePartDef.HasModExtension<SiteDefendersExtension>())
                {
                    hefSites.Add(site);
                    continue;
                }
            }

            return hefSites;
        }

        public static List<SitePartDef> FindExistingHEFSiteDefsFor(Faction fact)
        {
            List<Site> hefSites = FindHEFSitesFor(fact);
            List<SitePartDef> hefSitePartDefs = FindDefsForSites(hefSites);
            
            return hefSitePartDefs;
        }

        public static List<SitePartDef> FindEligibleHEFSiteDefsFor(Faction fact)
        {
            HashSet<SitePartDef> eligibleDefs = new HashSet<SitePartDef>();

            foreach (SitePartDef def in DefDatabase<SitePartDef>.AllDefs)
            {
                SiteDefendersExtension extension = def.GetModExtension<SiteDefendersExtension>();
                if (extension != null && extension.factionsToPawnGroups.Any(x => x.faction == fact.def.defName))
                {
                    eligibleDefs.Add(def);
                }
            }

            List<SitePartDef> usedDefs = FindExistingHEFSiteDefsFor(fact);

            if (!usedDefs.NullOrEmpty())
            {
                foreach (SitePartDef def in usedDefs)
                {
                    eligibleDefs.Remove(def);
                }
            }

            return eligibleDefs.ToList();
        }

        public static List<SitePartDef> FindDefsForSites(List<Site> sites)
        {
            List<SitePartDef> defs = new List<SitePartDef>();

            if (!sites.NullOrEmpty())
            {
                foreach (Site site in sites)
                {
                    defs.Add(site.MainSitePartDef);
                }
            }

            return defs;
        }

        public static List<string> FindStringsForDefs(List<SitePartDef> defs)
        {
            List<string> tags = new List<string>();

            if (!defs.NullOrEmpty())
            {
                foreach (SitePartDef def in defs)
                {
                    //the list is initialized empty in the class, so no null check needed
                    foreach (string tag in def.tags)
                    {
                        tags.Add(tag);
                    }
                }
            }

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

        public static bool TryGenerateExtendedRaidInfo(IncidentParms parms, out List<Pawn> pawns, bool debugTest = false)
        {
            List<Site> hefSites;
            List<SitePartDef> hefSiteDefs;

            hefSites = HEF_Utils.FindHEFSitesFor(parms.faction);
            hefSiteDefs = HEF_Utils.FindDefsForSites(hefSites);

            PawnGroupKindDef combat = PawnGroupKindDefOf.Combat;

            //Due to the Expansion Site system, it will be easiest to select the PawnGroupMaker first, as there is otherwise a high probability that selected Strategy/ArrivalMode will have no legal PawnGroup
            if (!TryResolveTaggedPawnGroup(parms, hefSiteDefs, out TaggedPawnGroupMaker groupMaker))
            {
                pawns = new List<Pawn>();
                return false;
            }

            ResolveRaidStrategy(parms, groupMaker);
            ResolveRaidArriveMode(parms, groupMaker);
            //ResolveRaidAgeRestriction(parms); //TODO - need to copy vanilla

            if (!debugTest && !parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms))
            {
                pawns = null;
                return false;
            }
            float points = parms.points;
            parms.points = AdjustedRaidPoints(parms.points, parms.raidArrivalMode, parms.raidStrategy, parms.faction, combat, parms.raidAgeRestriction);
            PawnGroupMakerParms defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(combat, parms);
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
            List<TaggedPawnGroupMaker> possibleGroupMakers = new List<TaggedPawnGroupMaker>();

            List<string> hefTags = HEF_Utils.FindStringsForDefs(siteDefs);
            FactionDef factionDef = parms.faction.def;
            PawnGroupMakerExtension extension = factionDef.GetModExtension<PawnGroupMakerExtension>();
            foreach (TaggedPawnGroupMaker tpgm in extension.taggedPawnGroupMakers)
            {
                if (HEF_Utils.CheckIfAllTagsPresent(tpgm.requiredSiteTags, hefTags))
                {
                    if (Prefs.DevMode)
                    {
                        if (tpgm.groupName != null)
                        {
                            Log.Message($"Adding {tpgm.groupName} as pgm option");
                        }
                        else
                        {
                            Log.Message($"Adding unnamed pgm as pgm option");
                        }
                    }
                    possibleGroupMakers.Add(tpgm);
                }
            }

            possibleGroupMakers.TryRandomElementByWeight((TaggedPawnGroupMaker gm) => gm.commonality, out groupMaker);
            if (Prefs.DevMode)
            {
                if (groupMaker.groupName != null)
                {
                    Log.Message($"Selected {groupMaker.groupName} as pgm");
                }
                else
                {
                    Log.Message($"Selected unnamed pgm as pgm");
                }
            }

            return groupMaker != null;
        }

        public static void ResolveRaidStrategy(IncidentParms parms, TaggedPawnGroupMaker groupMaker)
        {
            if (parms.raidStrategy != null)
                return;

            parms.raidStrategy = groupMaker.allowedRaidStrategies.RandomElement(); //TODO consider using point curve weight
        }

        public static void ResolveRaidArriveMode(IncidentParms parms, TaggedPawnGroupMaker groupMaker)
        {
            if (parms.raidArrivalMode != null)
                return;

            parms.raidArrivalMode = groupMaker.allowedArrivalModes.RandomElement();
        }

        public static float AdjustedRaidPoints(float points, PawnsArrivalModeDef raidArrivalMode, RaidStrategyDef raidStrategy, Faction faction, PawnGroupKindDef groupKind, RaidAgeRestrictionDef ageRestriction = null)
        {
            //TODO adjust raid points based on certain sites

            if (raidArrivalMode.pointsFactorCurve != null)
            {
                points *= raidArrivalMode.pointsFactorCurve.Evaluate(points);
            }
            if (raidStrategy.pointsFactorCurve != null)
            {
                points *= raidStrategy.pointsFactorCurve.Evaluate(points);
            }
            if (ageRestriction != null)
            {
                points *= ageRestriction.threatPointsFactor;
            }
            points = Mathf.Max(points, raidStrategy.Worker.MinimumPoints(faction, groupKind) * 1.05f);
            return points;
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
    }
}