using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using Verse.AI.Group;

namespace rep.heframework
{
    public class CompPawnCarrier : ThingComp
    {
        public CompProperties_PawnCarrier Props
        {
            get
            {
                return (CompProperties_PawnCarrier)props;
            }
        }

        private bool canSpawnPawns = false;

        private int ticksUntilNextPawnRelease;

        private List<Pawn> pawnsInStorage = new List<Pawn>();

        CompCarrierActivator compActivator;

        Lord lord;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            if (!respawningAfterLoad)
            {
                GenerateInitialPawns();
                ticksUntilNextPawnRelease = Props.ticksBetweenPawnRelease;
            }

            if (Props.autonomous)
            {
                compActivator = parent.TryGetComp<CompCarrierActivator>();
                if (compActivator == null)
                {
                    Log.Error("Misconfigured comps on def " + parent.def.defName + ". Is set to be autonomous but has no CompCarrierActivator.");
                }
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (!canSpawnPawns)
            {
                return;
            }
            if (ticksUntilNextPawnRelease > 0)
            {
                ticksUntilNextPawnRelease--;
                return;
            }

            TryReleasePawn(Props.releaseAtOnce);
            ticksUntilNextPawnRelease = Props.ticksBetweenPawnRelease;
        }


        private void GenerateInitialPawns()
        {
            //infinite spawners only need one in storage at a time
            if (Props.infinitePawnSupply)
            {
                TryAddPawn();
                return;
            }

            //if random, fill to capacity with randomly selected kinds
            if (Props.randomPawnKinds)
            {
                for (int i = 0; i < Props.maxCapacity; i++)
                {
                    TryAddPawn(Props.pawnKindsCarried.RandomElement());
                }
            }

            //if not random, fill with exactly the kinds in the list
            else
            {
                for (int i = 0; i < Props.pawnKindsCarried.Count; i++)
                {
                    TryAddPawn(Props.pawnKindsCarried[i]);
                }
            }
        }

        public bool TryAddPawn(Pawn pawn, bool force = false)
        {
            if (pawn == null)
            {
                Log.Error("Tried to add null pawn to PawnCarrier " + parent.Label);
                return false;
            }

            if ((pawnsInStorage.Count < Props.maxCapacity) || force == true)
            {
                AddPawn(pawn);
                return true;
            }

            return false;
        }

        public bool TryAddPawn(PawnKindDef kind = null, bool force = false)
        {
            //if no PawnKind is specified, pick one at random
            if (kind == null)
            {
                kind = Props.pawnKindsCarried.RandomElement();
            }
            Pawn pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, faction: parent.Faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true));

            return TryAddPawn(pawn, force);
        }

        private bool AddPawn(Pawn pawn)
        {
            pawnsInStorage.Add(pawn);
            return true;
            //TODO more logic
        }

        public bool TryReleasePawn(int number)
        {
            bool releasedAtLeastOne = false; //TODO maybe this should return if full amount is released instead of at least 1?
            for (int i = 0; i < number; i++)
            {
                if (TryReleasePawn())
                {
                    releasedAtLeastOne = true;
                }
                else
                {
                    break;
                }
            }

            return releasedAtLeastOne;
        }

        public bool TryReleasePawn(PawnKindDef kind = null)
        {
            //early return if empty
            if (pawnsInStorage.Count == 0)
            {
                return false;
            }

            int releaseIndex = -1;

            //if no PawnKind is specified, release the last pawn on the list which has slightly better performance than the first
            if (kind == null)
            {
                releaseIndex = pawnsInStorage.Count - 1;
            }
            else
            {
                for (int i = pawnsInStorage.Count; i <= 0; i--)
                {
                    if (pawnsInStorage[i].kindDef == kind)
                    {
                        releaseIndex = i;
                        break;
                    }
                }
                if (releaseIndex == -1)
                {
                    //if no pawn has the right kind, nothing is released
                    return false;
                }
            }

            return ReleasePawn(releaseIndex);
        }

        //returns a bool in case I add more logic later
        private bool ReleasePawn(int index)
        {
            //moved this here to give the parent a chance to be assigned a lord before checking for one
            if (lord == null)
            {
                lord = GetOrMakeLord();
            }

            Pawn pawn = pawnsInStorage[index];
            GenSpawn.Spawn(pawn, ReturnSpawnCell, parent.Map);
            lord.AddPawn(pawn);
            pawnsInStorage.RemoveAt(index);

            //if it should have an infinite supply, re-stock the pawn
            if (Props.infinitePawnSupply)
            {
                TryAddPawn();
            }

                return true;
        }

        public void SpawnPause()
        {
            canSpawnPawns = false;
        }

        public void SpawnUnpause()
        {
            canSpawnPawns = true;
            //TODO immobilize?
        }

        //Will try to return the parent's lord if configured to do so.
        //If not, will try to return an existing lord for its faction of the configured type
        //If none are found, will return a new lord of that type
        public Lord GetOrMakeLord()
        {
            Lord lord;

            if (Props.joinParentLord)
            {
                ((Pawn)parent).TryGetLord(out lord);
            }
            else
            {
                lord = null;
                for (int i = 0; i < parent.Map.lordManager.lords.Count; i++)
                {
                    if (parent.Map.lordManager.lords[i].faction == parent.Faction && parent.Map.lordManager.lords[i].GetType() == Props.lordJobType)
                    {
                        lord = parent.Map.lordManager.lords[i];
                        break;
                    }
                }
            }

            if (lord == null)
            {
                object[] lordJobProps = GetLordJobProps(Props.lordJobType);
                if (lordJobProps == null)
                {
                    Log.Error("Tried to make unsupported LordJob for CompPawnCarrier on " + parent.Label);
                }

                lord = LordMaker.MakeNewLord(parent.Faction, Activator.CreateInstance(Props.lordJobType, lordJobProps) as LordJob, parent.Map);
            }

            return lord;
        }



        //not needed, just call the TryReleasePawn(int) method
        //public bool TryReleaseAllPawns()
        //{
        //    return ReleaseAllPawns();
        //}

        //private bool ReleaseAllPawns()
        //{
        //    if (Props.infinitePawnSupply)
        //    {
        //        return false;
        //    }

        //    while (pawnsInStorage.Count != 0)
        //    {
        //        TryReleasePawn();
        //    }

        //    return true;
        //}

        public override void Notify_Downed()
        {
            base.Notify_Downed();
            if (Props.releaseAllOnDowned)
            {
                TryReleasePawn(pawnsInStorage.Count);
            }
            else
            {
                TryReleasePawn(Props.releaseNumberOnDowned);
                //TODO clean up pawns not released
            }
        }

        public override void Notify_Killed(Map prevMap, DamageInfo? dinfo = null)
        {
            base.Notify_Killed(prevMap, dinfo);
            if (Props.releaseAllOnKilled)
            {
                TryReleasePawn(pawnsInStorage.Count);
            }
            else
            {
                TryReleasePawn(Props.releaseNumberOnKilled);
                //TODO clean up pawns not released
            }
        }

        public object[] GetLordJobProps(Type type)
        {
            if (type == typeof(LordJob_AssaultColony))
            {
                return new object[] {
                /* Faction assaulterFaction */ parent.Faction,
                /* bool canKidnap */ Props.canKidnap,
                /* bool canTimeoutOrFlee */ Props.canTimeOutOrFlee,
                /* bool sappers */ Props.sappers,
                /* bool useAvoidGridSmart */ Props.useAvoidGridSmart,
                /* bool canSteal */ Props.canSteal,
                /* bool breachers */ Props.breachers,
                /* bool canPickUpOpportunisticWeapons */ Props.canPickUpOpportunisticWeapons
                };
            }

            //TODO more LordJobs
            return null;
        }

        private IntVec3 ReturnSpawnCell
        {
            get
            {
                int x = parent.Position.x;
                int z = parent.Position.z;
                if (Props.spawnBasedOnFacing)
                {
                    if (parent.Rotation == Rot4.North)
                    {
                        x += Props.spawnXOffsetFacingNorth;
                        z += Props.spawnZOffsetFacingNorth;
                    }
                    else if (parent.Rotation == Rot4.South)
                    {
                        x -= Props.spawnXOffsetFacingNorth;
                        z -= Props.spawnZOffsetFacingNorth;
                    }
                    else if (parent.Rotation == Rot4.East)
                    {
                        x += Props.spawnZOffsetFacingNorth;
                        z -= Props.spawnXOffsetFacingNorth;
                    }
                    else if (parent.Rotation == Rot4.West)
                    {
                        x -= Props.spawnZOffsetFacingNorth;
                        z += Props.spawnXOffsetFacingNorth;
                    }
                }
                else
                {
                    x += Props.spawnXOffsetFacingNorth;
                    z += Props.spawnZOffsetFacingNorth;
                }

                return new IntVec3(x, 0, z);
            }
        }

        //TODO integration with Smokescreen to pop smoke when unloading begins

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref canSpawnPawns, "canSpawnPawns", false);
            Scribe_Values.Look(ref ticksUntilNextPawnRelease, "ticksUntilNextPawnRelease", 0);
            Scribe_Collections.Look(ref pawnsInStorage, "pawnsInStorage", LookMode.Reference);

            Scribe_References.Look(ref lord, "lord");
        }
    }
}