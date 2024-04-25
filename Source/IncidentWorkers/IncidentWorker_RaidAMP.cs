using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace rep.heframework
{
	public class IncidentWorker_RaidAMP : IncidentWorker_RaidEnemy
    {
        
		protected override bool CanFireNowSub(IncidentParms parms)
        {
            return (float)GenDate.DaysPassedSinceSettle >= HEF_Settings.earliestRaidDays;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (!TryGenerateRaidInfo(parms, out var pawns))
            {
                return false;
            }
            TaggedString letterLabel = GetLetterLabel(parms);
            TaggedString letterText = GetLetterText(parms, pawns);
            PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(pawns, ref letterLabel, ref letterText, GetRelatedPawnsInfoLetterText(parms), informEvenIfSeenBefore: true);
            List<TargetInfo> list = new List<TargetInfo>();
            if (parms.pawnGroups != null)
            {
                List<List<Pawn>> list2 = IncidentParmsUtility.SplitIntoGroups(pawns, parms.pawnGroups);
                List<Pawn> list3 = list2.MaxBy((List<Pawn> x) => x.Count);
                if (list3.Any())
                {
                    list.Add(list3[0]);
                }
                for (int i = 0; i < list2.Count; i++)
                {
                    if (list2[i] != list3 && list2[i].Any())
                    {
                        list.Add(list2[i][0]);
                    }
                }
            }
            else if (pawns.Any())
            {
                foreach (Pawn item in pawns)
                {
                    list.Add(item);
                }
            }
            SendStandardLetter(letterLabel, letterText, GetLetterDef(), parms, list);
            if (parms.controllerPawn == null || parms.controllerPawn.Faction != Faction.OfPlayer)
            {
                parms.raidStrategy.Worker.MakeLords(parms, pawns);
            }
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.EquippingWeapons, OpportunityType.Critical);
            if (!PlayerKnowledgeDatabase.IsComplete(ConceptDefOf.ShieldBelts))
            {
                for (int j = 0; j < pawns.Count; j++)
                {
                    Pawn pawn = pawns[j];
                    if (pawn.apparel != null && pawn.apparel.WornApparel.Any((Apparel ap) => ap.def == ThingDefOf.Apparel_ShieldBelt))
                    {
                        LessonAutoActivator.TeachOpportunity(ConceptDefOf.ShieldBelts, OpportunityType.Critical);
                        break;
                    }
                }
            }
            if (DebugSettings.logRaidInfo)
            {
                Log.Message($"Raid: {parms.faction.Name} ({parms.faction.def.defName}) {parms.raidArrivalMode.defName} {parms.raidStrategy.defName} c={parms.spawnCenter} p={parms.points}");
            }
            Find.TickManager.slower.SignalForceNormalSpeedShort();
            Find.StoryWatcher.statsRecord.numRaidsEnemy++;
            parms.target.StoryState.lastRaidFaction = parms.faction;
            return true;
        }

        public new bool TryGenerateRaidInfo(IncidentParms parms, out List<Pawn> pawns, bool debugTest = false)
        {
            List<Site> hefSites;
            List<SitePartDef> hefSiteDefs;

            pawns = null;

            ResolveRaidPoints(parms);

            if (!TryResolveRaidFaction(parms))
                return false;

            hefSites = HEF_Utils.FindHEFSitesFor(parms.faction);
            hefSiteDefs = HEF_Utils.FindDefsForSites(hefSites);

            PawnGroupKindDef combat = PawnGroupKindDefOf.Combat;

            //Due to the Expansion Site system, it will be easiest to select the PawnGroupMaker first, as there is otherwise a high probability that selected Strategy/ArrivalMode will have no legal PawnGroup
            if (!TryResolvePawnGroup(parms, hefSiteDefs, out TaggedPawnGroupMaker groupMaker))
                return false;

            ResolveRaidStrategy(parms, groupMaker);
            ResolveRaidArriveMode(parms, groupMaker);
            ResolveRaidAgeRestriction(parms);

            if (!debugTest)
            {
                parms.raidStrategy.Worker.TryGenerateThreats(parms);
            }
            if (!debugTest && !parms.raidArrivalMode.Worker.TryResolveRaidSpawnCenter(parms))
            {
                return false;
            }
            float points = parms.points;
            parms.points = AdjustedRaidPoints(parms.points, parms.raidArrivalMode, parms.raidStrategy, parms.faction, combat, parms.raidAgeRestriction);
            if (!debugTest)
            {
                pawns = parms.raidStrategy.Worker.SpawnThreats(parms);
            }
            if (pawns == null)
            {
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
            }
            parms.pawnCount = pawns.Count;
            //PostProcessSpawnedPawns(parms, pawns); //TODO re-enable this for 1.5
            if (debugTest)
            {
                parms.target.StoryState.lastRaidFaction = parms.faction;
            }
            else
            {
                GenerateRaidLoot(parms, points, pawns);
            }
            return true;
        }

        protected override bool TryResolveRaidFaction(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            //vanilla early return for if the faction was already chosen via debug tool
            if (parms.faction != null && parms.faction.HostileTo(Faction.OfPlayer))
            {
                return true;
            }

            //AMP factions will always have Combat groups, so we don't need to use PawnGroupMakerUtility to select a faction that has one

            //get hostile AMP factions
            List<Faction> possibleFactions = (Find.FactionManager.AllFactions.Where(f => f.def.HasModExtension<PawnGroupMakerExtension>() && f.HostileTo(Faction.OfPlayer))).ToList();

            return possibleFactions.TryRandomElement(out parms.faction);
        }

        protected bool TryResolvePawnGroup(IncidentParms parms, List<SitePartDef> siteDefs, out TaggedPawnGroupMaker groupMaker)
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

        public void ResolveRaidStrategy(IncidentParms parms, TaggedPawnGroupMaker groupMaker)
        {
            if (parms.raidStrategy != null)
                return;

            parms.raidStrategy = groupMaker.allowedRaidStrategies.RandomElement(); //TODO consider using point curve weight
        }

        public void ResolveRaidArriveMode(IncidentParms parms, TaggedPawnGroupMaker groupMaker)
        {
            if (parms.raidArrivalMode != null)
                return;

            parms.raidArrivalMode = groupMaker.allowedArrivalModes.RandomElement();
        }

        public static new float AdjustedRaidPoints(float points, PawnsArrivalModeDef raidArrivalMode, RaidStrategyDef raidStrategy, Faction faction, PawnGroupKindDef groupKind, RaidAgeRestrictionDef ageRestriction = null)
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

