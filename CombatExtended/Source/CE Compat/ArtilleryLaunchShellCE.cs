using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using CombatExtended;

namespace rep.heframework.cecompat
{
    [StaticConstructorOnStartup]
    class ArtilleryLaunchShellCE
    {
        static ArtilleryLaunchShellCE()
        {
            Func<ThingDef, ThingWithComps> spawnFunc = new Func<ThingDef, ThingWithComps>(SpawnArtilleryProjectileCE);
            Action<ThingWithComps, IntVec3> launchAction = new Action<ThingWithComps, IntVec3>(LaunchArtilleryShellCE);

            HE_Settings.artillarySpawnDelegate.Add(typeof(ProjectilePropertiesCE), spawnFunc);
            HE_Settings.artilleryLaunchDelegate.Add(typeof(ProjectileCE), launchAction);
        }

        public static ThingWithComps SpawnArtilleryProjectileCE(ThingDef def)
        {
            ProjectileCE projectileCE = ThingMaker.MakeThing(def) as ProjectileCE;

            return projectileCE;
        }

        public static void LaunchArtilleryShellCE(ThingWithComps thing, IntVec3 target)
        {
            if (thing is ProjectileCE shellCE)
            {
                shellCE.Launch(
                            launcher: shellCE,
                            intendedTarget: target,
                            usedTarget: target,
                            hitFlags: ProjectileHitFlags.All);
            }
            else
            {
                Log.Error($"LaunchArtilleryShellCE tried to launch non-CE projectile. label: {thing.Label}, defName: {thing.def?.defName}, type: {thing.GetType()}");
            }
        }
    }
}
