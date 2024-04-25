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

namespace rep.framework
{
    public class AMP_SitePartWorker_Expansion : SitePartWorker_Outpost
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
			SitePartDef mainPart = site.MainSitePartDef;
			AMP_FactionDef factionDef;
			if (parms.faction.def is AMP_FactionDef)
            {
				factionDef = (AMP_FactionDef)parms.faction.def;
            }
			else
            {
				Log.Error("Using AMP_SiteWorker_Expansion on site for non-AMP faction " + parms.faction.Name);
				pawnGroupMaker = null;
				return false;
            }

			if (!mainPart.tags.Any(t => t.StartsWith("AMP_")))
			{
				Log.Error("Using AMP_SiteWorker_Expansion on non-AMP site " + site.Label);
				pawnGroupMaker = null;
				return false;
            }

			List<PawnGroupMaker> possibleGroups = new List<PawnGroupMaker>();

			foreach (AMP_PawnGroupMaker apgm in factionDef.pawnGroupMakers)
			{
				foreach (String tag in apgm.requiredSiteTags)
                {
					if (mainPart.tags.Contains(tag))
                    {
						possibleGroups.Add(apgm);
                    }
                }
			}
			if (possibleGroups.Count == 0)
            {
				possibleGroups.Add(parms.faction.def.pawnGroupMakers.First());
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