using RimWorld;
using RimWorld.Planet;
using RimWorld.QuestGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using Verse.Grammar;

namespace rep.heframework
{
    public class HEF_SitePartWorker_Expansion : SitePartWorker_Outpost
    {
		public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
		{
			base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
			int enemiesCount = GetEnemiesCount(part.site, part.parms);
			outExtraDescriptionRules.Add(new Rule_String("enemiesCount", enemiesCount.ToString()));
			outExtraDescriptionRules.Add(new Rule_String("enemiesLabel", GetEnemiesLabel(part.site, enemiesCount)));
		}

		public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
		{
			return def.label + ": " + "KnownSiteThreatEnemyCountAppend".Translate(GetEnemiesCount(site, sitePart.parms), "Enemies".Translate());
		}

		protected new int GetEnemiesCount(Site site, SitePartParams parms)
        {
			return GeneratePawnKindsExample(new PawnGroupMakerParms
			{
				tile = site.Tile,
				faction = site.Faction,
				groupKind = PawnGroupKindDefOf.Settlement,
				points = parms.threatPoints,
				inhabitants = true,
				seed = OutpostSitePartUtility.GetPawnGroupMakerSeed(parms)
			}, site).Count();
		}

		public IEnumerable<PawnKindDef> GeneratePawnKindsExample(PawnGroupMakerParms parms, Site site)
		{
			if (parms.groupKind == null)
			{
				Log.Error("Tried to generate pawn kinds with null pawn group kind def. parms=" + parms);
				yield break;
			}
			if (parms.faction == null)
			{
				Log.Error("Tried to generate pawn kinds with null faction. parms=" + parms);
				yield break;
			}
			if (!TryGetPawnGroupMakerForExpansion(parms, out var pawnGroupMaker, site))
			{
				Log.Error(string.Concat("Faction ", parms.faction, " of def ", parms.faction.def, " has no usable PawnGroupMakers for parms ", parms));
				yield break;
			}
			foreach (PawnKindDef item in pawnGroupMaker.GeneratePawnKindsExample(parms))
			{
				yield return item;
			}
		}

		public bool TryGetPawnGroupMakerForExpansion(PawnGroupMakerParms parms, out PawnGroupMaker pawnGroupMaker, Site site)
        {
			//Check that the site's faction is HEF
			FactionDef factionDef;
			PawnGroupMakerExtension factExtension = (PawnGroupMakerExtension)parms.faction.def.modExtensions?.FirstOrDefault(x => x is PawnGroupMakerExtension);
			if (factExtension != null)
            {
				factionDef = parms.faction.def;
            }
			else
            {
				Log.Error("Using HEF_SitePartWorker_Expansion on site for non-HEF faction " + parms.faction.Name);
				pawnGroupMaker = null;
				return false;
            }

			//Check that the site is HEF
			SitePartDef mainPart = site.MainSitePartDef;
			SiteDefendersExtension siteExtension = (SiteDefendersExtension)mainPart.modExtensions?.FirstOrDefault(x => x is SiteDefendersExtension);
			if (siteExtension == null)
			{
				Log.Error("Using HEF_SiteWorker_Expansion on non-HEF site " + site.Label);
				pawnGroupMaker = null;
				return false;
            }

			//Check that the site's extension is properly configured
			if (siteExtension.factionsToPawnGroups.NullOrEmpty())
            {
				Log.Error("Site " + site.Label + " has misconfigured SiteDefendersExtension. No factionsToPawnGroups.");
				pawnGroupMaker = null;
				return false;
            }
			int factionIndex = -1;
			for (int i = 0; i < siteExtension.factionsToPawnGroups.Count; i++)
            {
				if (siteExtension.factionsToPawnGroups[i].faction == factionDef.defName)
                {
					factionIndex = i;
					break;
                }
			}
			if (factionIndex == -1)
            {
				Log.Error("Site " + site.Label + " has misconfigured SiteDefendersExtension. Site faction has no factionsToPawnGroups entry.");
				pawnGroupMaker = null;
				return false;
			}
			//TODO checks to make sure that pawnGroups for the given faction exist

			List<TaggedPawnGroupMaker> possibleGroups = new List<TaggedPawnGroupMaker>();

			foreach (string str in siteExtension.factionsToPawnGroups[factionIndex].pawnGroups)
            {
				possibleGroups.Add(factExtension.taggedPawnGroupMakers.FirstOrDefault(tpgm => tpgm.groupName == str));
            }

			if (possibleGroups.Count == 0)
            {
				Log.Error("Site " + site.Label + " has misconfigured SiteDefendersExtension. No pawnGroupMakers could be selected from factionsToPawnGroups entry.");
            }

			pawnGroupMaker = possibleGroups.RandomElement();
			if (pawnGroupMaker == null)
            {
				Log.Error("Failed to assign PawnGroupMaker for site " + site.Label);
				return false;
            }

			return true;
        }
	}


}