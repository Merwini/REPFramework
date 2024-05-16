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
	public class DamageWorker_SelfDestruct : DamageWorker
	{
		public override DamageResult Apply(DamageInfo dinfo, Thing victim)
		{
			DamageResult result = base.Apply(dinfo, victim);
			Thing kamikaze = dinfo.Instigator;
			IntVec3 c = kamikaze.Position;
			ThingDef projectileDef = dinfo.Def.explosionCellMote;

			Projectile projectile = (Projectile)ThingMaker.MakeThing(projectileDef);
			GenSpawn.Spawn(projectile, c, kamikaze.Map);
			projectile.Launch(
					launcher: kamikaze,
					usedTarget: victim,
					intendedTarget: victim,
					hitFlags: ProjectileHitFlags.IntendedTarget
					);
			kamikaze.Kill();

			return result;
		}
	}
}