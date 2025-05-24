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
    public class SitePartWorker_ExpansionHE : SitePartWorker_Outpost
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
				groupKind = PawnGroupKindDefOf.Combat,
				points = parms.threatPoints,
				inhabitants = true,
				seed = OutpostSitePartUtility.GetPawnGroupMakerSeed(parms)
			}, site).Count();
		}

		public IEnumerable<PawnKindDef> GeneratePawnKindsExample(PawnGroupMakerParms parms, Site site)
		{
			if (parms.groupKind == null)
			{
				Log.Error("Tried to generate pawn kinds with null pawn group kind def. parms: \n" + parms);
				yield break;
			}
			if (parms.faction == null)
			{
				Log.Error("Tried to generate pawn kinds with null faction. parms: \n" + parms);
				yield break;
			}
			if (!TryGetPawnGroupMakerForExpansion(parms, out var pawnGroupMaker, site))
			{
				yield break;
			}
			foreach (PawnKindDef item in pawnGroupMaker.GeneratePawnKindsExample(parms))
			{
				yield return item;
			}
		}

		public bool TryGetPawnGroupMakerForExpansion(PawnGroupMakerParms parms, out PawnGroupMaker pawnGroupMaker, Site site)
		{
			pawnGroupMaker = null;

			//Check that the site's faction is HE
			FactionDef factionDef = parms.faction.def;

			PawnGroupMakerExtensionHE factExtension = (PawnGroupMakerExtensionHE)parms.faction.def.GetModExtension<PawnGroupMakerExtensionHE>();
			if (factExtension == null)
            {
				Log.Error($"Using HE_SitePartWorker_Expansion on site for non-HE faction {parms.faction.Name}. Destroying site.");
                site.Destroy();
				return false;
            }

			if (factExtension.taggedPawnGroupMakers.NullOrEmpty())
            {
				Log.Error($"Using HE_SitePartWorker_Expansion on site for faction {parms.faction.Name} with misconfigured PawnGroupMakerExtension. Faction has no TaggedPawnGroupMakers");
				site.Destroy();
				return false;
			}

			//Check that the site is HE
			SitePartDef mainPart = site.MainSitePartDef;
			WorldObjectExtensionHE siteExtension = (WorldObjectExtensionHE)mainPart.GetModExtension<WorldObjectExtensionHE>();
			if (siteExtension == null)
			{
				Log.Error($"Using HE_SiteWorker_Expansion on non-HE site {site.Label}. Destroying site.");
				site.Destroy();
				return false;
            }

			//Check that the site's extension is properly configured
			if (siteExtension.factionsToPawnGroups.NullOrEmpty())
            {
				//not error, in case it is intentionally left with no defenders
				Log.Warning($"Site {site.Label} has possibly misconfigured SiteDefendersExtension. No factionsToPawnGroups entries.");
				return false;
            }

			List<TaggedPawnGroupMaker> possibleGroups = new List<TaggedPawnGroupMaker>();

			foreach (string str in siteExtension.factionsToPawnGroups.FirstOrDefault(link => link.Faction == factionDef).PawnGroups)
            {
				TaggedPawnGroupMaker possibleGroup = factExtension.taggedPawnGroupMakers.FirstOrDefault(tpgm => tpgm.groupName == str);
				if (possibleGroup != null)
                {
					possibleGroups.Add(possibleGroup);
				}
			}

			if (possibleGroups.Count == 0)
            {
				//not error, in case it is intentionally left with no defenders
				Log.Warning($"Site {site.Label} has possibly misconfigured SiteDefendersExtension. No pawnGroupMakers could be selected from factionsToPawnGroups entry.");
				return false;
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