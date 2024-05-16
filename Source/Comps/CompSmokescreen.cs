using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace rep.heframework
{
    public class CompSmokescreen : ThingComp
    {
        public CompProperties_Smokescreen Props
        {
            get
            {
                return (CompProperties_Smokescreen)props;
            }
        }

        public int ticksUntilReady;

        public int chargesLeft;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            ticksUntilReady = 0;
            chargesLeft = Props.charges;
        }

        public override void PostPostApplyDamage(DamageInfo dinfo, float totalDamageDealt)
        {
            base.PostPostApplyDamage(dinfo, totalDamageDealt);
            if (ShouldPopSmoke(dinfo.Amount))
            {
                IntVec3 damageSource;
                if (dinfo.Instigator != null && !Props.alwaysLaunchForward)
                {
                    damageSource = dinfo.Instigator.Position;
                }
                else
                {
                    damageSource = FacingOffset;
                }
                PopSmoke(damageSource);
            }
        }

        public override void CompTick()
        {
            base.CompTick();
            if ((chargesLeft != 0) && (ticksUntilReady < 0))
            {
                ticksUntilReady--;
            }
        }

        public bool ShouldPopSmoke(float damage)
        {
            return (chargesLeft > 0 && ticksUntilReady == 0 && damage > Props.damageThreshold);
        }

        public bool PopSmoke(IntVec3 targetSource)
        {
            foreach (IntVec3 target in CalcShotTargets(targetSource))
            {
                Projectile projectile = (Projectile)ThingMaker.MakeThing(Props.smokeProjectile, null);
                GenSpawn.Spawn(projectile, parent.Position, parent.Map);
                projectile.Launch(
                    launcher: parent,
                    usedTarget: target,
                    intendedTarget: target,
                    hitFlags: ProjectileHitFlags.IntendedTarget
                    );
            }
            chargesLeft--;
            ticksUntilReady = Props.cooldown;
            return true;
        }

        //public List<float> CalcShotRotations(IntVec3 damageSource)
        //{
        //    List<float> rotations = new List<float>();

        //    Vector2 w = new Vector2(damageSource.x - parent.Position.x, damageSource.z - parent.Position.z);
        //    float shotRotation = (-90 + Mathf.Rad2Deg * Mathf.Atan2(w.y, w.x)) % 360;

        //    rotations.Add(shotRotation);
        //    Log.Message(shotRotation.ToString());

        //    return rotations;
        //}

        public List<IntVec3> CalcShotTargets(IntVec3 targetSource)
        {
            List<IntVec3> targetCells = new List<IntVec3>();

            // Calculate angle between parent position and target source
            float angleToTarget = Mathf.Atan2(targetSource.z - parent.Position.z, targetSource.x - parent.Position.x);

            // Calculate spread angle between projectiles
            float spreadAngle = Mathf.Deg2Rad * Props.projectileSpreadDegrees;

            // Calculate number of cells to allocate
            int numCells = Props.projectileCount;

            // Calculate positions for each projectile
            int middleIndex = numCells / 2;
            for (int i = 0; i < numCells; i++)
            {
                float offsetAngle = (i - middleIndex) * spreadAngle;

                // If even number of projectiles, adjust middle positions
                if (numCells % 2 == 0 && i == middleIndex)
                {
                    offsetAngle -= spreadAngle / 2f;
                }

                // Calculate position for projectile
                float x = Mathf.Clamp(parent.Position.x + Props.range * Mathf.Cos(angleToTarget + offsetAngle), 0, parent.Map.Size.x);
                float z = Mathf.Clamp(parent.Position.z + Props.range * Mathf.Sin(angleToTarget + offsetAngle), 0, parent.Map.Size.z);
                IntVec3 targetCell = new IntVec3(Mathf.RoundToInt(x), 0, Mathf.RoundToInt(z));

                // Add target cell to array
                targetCells.Add(targetCell);
            }

            return targetCells;
        }

        public IntVec3 FacingOffset
        {
            get
            {
                int x = parent.Position.x;
                int z = parent.Position.z;

                if (parent.Rotation == Rot4.North)
                {
                    z += 1;
                }
                else if (parent.Rotation == Rot4.South)
                {
                    z -= 1;
                }
                else if (parent.Rotation == Rot4.East)
                {
                    x += 1;
                }
                else if (parent.Rotation == Rot4.West)
                {
                    x -= 1;
                }

                x = Mathf.Clamp(0, x, parent.Map.Size.x);
                z = Mathf.Clamp(0, z, parent.Map.Size.z);

                return new IntVec3(x, 0, z);
            }
        }

        public IntVec3 AdjustForFacing(IntVec3 vec)
        {
            //TODO 
            return new IntVec3(0, 0, 0);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksUntilReady, "ticksUntilRead", 0);
            Scribe_Values.Look(ref chargesLeft, "chargesLeft", 0);
        }

        //TODO integration with PawnCarrier to toggle spawn when smoke is popped
    }
}