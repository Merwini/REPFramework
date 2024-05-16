using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using CombatExtended;
using UnityEngine;

namespace rep.heframework.cecompat
{

	public class DamageWorker_SelfDestructCE : DamageWorker
	{
		public override DamageResult Apply(DamageInfo dinfo, Thing victim)
		{
			DamageResult result = base.Apply(dinfo, victim);
			Thing kamikaze = dinfo.Instigator;
			IntVec3 c = kamikaze.Position;
			ThingDef projectileDef = dinfo.Def.explosionCellMote;

			var projectile = (ProjectileCE)ThingMaker.MakeThing(projectileDef);
			GenSpawn.Spawn(projectile, c, kamikaze.Map);
			projectile.Launch(
				launcher: kamikaze,
				origin: new Vector2(c.x, c.y),
				shotAngle: 0,
				shotRotation: 0,
				shotSpeed: 1000
				);
			projectile.Impact(victim);
			kamikaze.Kill();

			return result;
		}
	}
}

