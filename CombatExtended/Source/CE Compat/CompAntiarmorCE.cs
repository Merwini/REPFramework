using CombatExtended;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using rep.heframework;

namespace rep.heframework.cecompat
{
    public class CompAntiarmorCE : CompAntiarmor
    {
        public CompProperties_AntiarmorCE Props
        {
            get
            {
                return (CompProperties_AntiarmorCE)props;
            }
        }

        public override float GetPawnOverallArmor(Pawn pawn, StatDef stat)
        {
            //from RebuildArmorCache(Dictionary<BodyPartRecord, float> armorCache, StatDef stat)
            Dictionary<BodyPartRecord, float> armorCache = new Dictionary<BodyPartRecord, float>();
            float naturalArmor = pawn.GetStatValue(stat);
            List<Apparel> wornApparel = pawn.apparel?.WornApparel;
            foreach (BodyPartRecord part in pawn.RaceProps.body.AllParts)
            {
                //TODO: 1.5 should be Neck
                if (part.depth == BodyPartDepth.Outside && (part.coverage >= 0.1 || (part.def == BodyPartDefOf.Eye || part.def == BodyPartDefOf.Eye)))
                {
                    float armorValue = part.IsInGroup(CE_BodyPartGroupDefOf.CoveredByNaturalArmor) ? naturalArmor : 0f;
                    if (wornApparel != null)
                    {
                        foreach (var apparel in wornApparel)
                        {
                            if (apparel.def.apparel.CoversBodyPart(part))
                            {
                                armorValue += apparel.PartialStat(stat, part);
                            }
                        }
                    }
                    armorCache[part] = armorValue;
                }
            }

            //from TryDrawOverallArmor(Dictionary<BodyPartRecord, float> armorCache, ref float curY, float width, StatDef stat, string label, string unit)
            float averageArmor = 0;
            float bodyCoverage = 0;
            foreach (var bodyPartValue in armorCache)
            {
                BodyPartRecord part = bodyPartValue.Key;
                float armorValue = bodyPartValue.Value;
                averageArmor += armorValue * part.coverage;
                bodyCoverage += part.coverage;
            }
            averageArmor /= bodyCoverage;

            return averageArmor;
        }
    }
}