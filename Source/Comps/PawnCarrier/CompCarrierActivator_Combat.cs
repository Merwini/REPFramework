using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace rep.heframework
{
    class CompCarrierActivator_Combat : CompCarrierActivator
    {
        public override void CompTick()
        {
            base.CompTick();
            if (parent.IsHashIntervalTick(300))
            {
                if (((Pawn)parent).TargetCurrentlyAimingAt != null)
                {
                    ToggleSpawningOn();
                }
                else
                {
                    ToggleSpawningOff();
                }
            }
        }
    }
}
