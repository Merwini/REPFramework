using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace rep.heframework
{
    public abstract class CompCarrierActivator : ThingComp
    {
        CompPawnCarrier compCarrier;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            compCarrier = parent.TryGetComp<CompPawnCarrier>();
            if (compCarrier == null)
            {
                Log.Error("Misconfigured comps on def " + parent.def.defName + ". Has CompCarrierActivator but no CompPawnCarrier.");
            }
        }

        public virtual void ToggleSpawningOn()
        {
            compCarrier.SpawnUnpause();
        }

        public virtual void ToggleSpawningOff()
        {
            compCarrier.SpawnPause();
        }
    }
}