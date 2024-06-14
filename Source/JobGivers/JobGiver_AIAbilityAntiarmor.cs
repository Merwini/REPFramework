//using RimWorld;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Reflection;
//using System.Text;
//using Verse;
//using Verse.AI;

//namespace rep.heframework
//{
//    public class JobGiver_AIAbilityAntiarmor : JobGiver_AIAbilityFight
//    {
//        private AbilityDef ability;

//        private float sharpArmorThreshold;

//        private int chargeCount;

//        protected override bool ExtraTargetValidator(Pawn pawn, Thing target)
//        {
//            Log.Warning("extra called");
//            if (base.ExtraTargetValidator(pawn, target))
//            {
//                if (target.GetStatValue(StatDefOf.ArmorRating_Sharp) >= sharpArmorThreshold)
//                {
//                    Log.Warning("target armor value is " + pawn.GetStatValue(StatDefOf.ArmorRating_Sharp).ToString());
//                    return true;
//                }
//            }
//            return false;
//        }

//        protected override Job TryGiveJob(Pawn pawn)
//        {
//            if (chargeCount == 0 || pawn.abilities.GetAbility(ability).OnCooldown)
//            {
//                return null;
//            }
//            //TODO

//            return null;
//        }
//    }
//}