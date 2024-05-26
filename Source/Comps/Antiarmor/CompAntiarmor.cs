using RimWorld;
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
    public class CompAntiarmor : ThingComp
    {
        public CompProperties_Antiarmor Props
        {
            get
            {
                return (CompProperties_Antiarmor)props;
            }
        }

        LocalTargetInfo tempTarget;

        public int nextFireOffsetIndex;

        public int projectilesRemaining;

        public int ticksUntilReady;

        public Ability ability;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            ability = AbilityUtility.MakeAbility(Props.ability, (Pawn)parent);
            ticksUntilReady = 0;
            projectilesRemaining = Props.magazineSize;
            nextFireOffsetIndex = 0;
        }

        public override void CompTick()
        {
            base.CompTick();
            if ((projectilesRemaining != 0) && (ticksUntilReady > 0))
            {
                ticksUntilReady--;
            }

            if (parent.IsHashIntervalTick(300) && ShouldFireNow())
            {
                FireProjectile();
            }
        }

        public bool ShouldFireNow()
        {
            //return false if not ready to fire
            if (projectilesRemaining == 0 || ticksUntilReady != 0)
            {
                return false;
            }

            //return true if has a target, and target is above armor threshold
            if (((Pawn)parent).TargetCurrentlyAimingAt != null && ((Pawn)parent).TargetCurrentlyAimingAt.TryGetPawn(out Pawn pawn))
            {
                if (pawn == null)
                {
                    return false;
                }

                float armorValue = GetPawnOverallArmor(pawn, StatDefOf.ArmorRating_Sharp);
                Log.Warning("target is " + pawn.Name);
                Log.Warning("target armor value is " + armorValue.ToString());
                if (armorValue >= Props.armorSharpThreshold)
                {
                    tempTarget = ((Pawn)parent).TargetCurrentlyAimingAt;
                    return true;

                }
            }

            //default
            return false;
        }

        //virtual so I can override for the CE version
        //this uses modified logic from calculating a pawn's armor in ITab_Pawn_Gear
        public virtual float GetPawnOverallArmor(Pawn pawn, StatDef stat)
        {
            float num = 0f;
            float num2 = Mathf.Clamp01(pawn.GetStatValue(stat) / 2f);
            List<BodyPartRecord> allParts = pawn.RaceProps.body.AllParts;
            List<Apparel> list = ((pawn.apparel != null) ? pawn.apparel.WornApparel : null);
            for (int i = 0; i < allParts.Count; i++)
            {
                float num3 = 1f - num2;
                if (list != null)
                {
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (list[j].def.apparel.CoversBodyPart(allParts[i]))
                        {
                            float num4 = Mathf.Clamp01(list[j].GetStatValue(stat) / 2f);
                            num3 *= 1f - num4;
                        }
                    }
                }
                num += allParts[i].coverageAbs * (1f - num3);
            };

            return num;
        }

        public void FireProjectile()
        {
            LaunchProjectileAt(GetNextFireOffset());
            projectilesRemaining--;
            ticksUntilReady = Props.ticksBetweenShots;
        }

        //virtual so I can override for the CE version
        public virtual void LaunchProjectileAt(Vector3 offset)
        {
            ability.QueueCastingJob(tempTarget, null);
        }

        public Vector3 GetNextFireOffset()
        {
            //TODO someday?
            //Calculate an offset based on weapon position and next entry in offset list
            //Advance offset index by 1, or go back to 0 if at the end of list
            //if (!Props.fireOffsets.NullOrEmpty())
            //{

            //}

            return Vector3.zero;
        }
    }
}