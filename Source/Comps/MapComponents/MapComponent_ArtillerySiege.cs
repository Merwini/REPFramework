using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace rep.heframework
{
    public class MapComponent_ArtillerySiege : MapComponent
    {
        // Fields directly set by StartArtillerySiege arguments. Are not modified during siege.
        int shellsPerBarrage;
        int numberOfBarrages;
        float forcedMissRadius;
        int ticksBetweenShells;
        int ticksBetweenBarrages;
        float barrageVariability;
        float shellVariability;

        Faction siegingFaction;
        Site sourceSite;
        ThingDef artilleryProjectile;
        List<IntVec3> originalTargetCells;

        bool doWaveRaids;
        bool doFinalRaid;
        TaggedPawnGroupMaker waveRaidGroup;
        TaggedPawnGroupMaker finalRaidGroup;
        float waveRaidPoints;
        float finalRaidPoints;

        // Fields calculated or set by comp. Can be modified during siege.
        bool siegeInProgress = false;
        bool barrageInProgress = false;
        int ticksUntilNextBarrage;
        int ticksUntilNextShell;
        int barragesLeftInSiege;
        int shellsLeftInBarrage;
        IntVec3 artilleryOriginCell;

        List<IntVec3> targetCells;
        bool newTargetsCached;
        List<IntVec3> cachedTargetCells;

        public bool SiegeInProgress => siegeInProgress;
        public bool BarrageInProgress => barrageInProgress;
        

        public MapComponent_ArtillerySiege(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            if (!siegeInProgress)
                return;

            if (!barrageInProgress)
                ticksUntilNextBarrage--;
            else
                ticksUntilNextShell--;

            // On rare ticks or right before starting a barrage, check that the site still exists. If not, end the siege
            if (Find.TickManager.TicksGame % 250 == 0 || ticksUntilNextBarrage == 0)
            {
                if (!CheckIfSiteStillExists())
                {
                    EndArtillerySiege();
                    return;
                }
            }

            // StartBarrage() resets the ticks, so don't need to validate that a barrage isn't already in progress
            if (ticksUntilNextBarrage == 0)
            {
                StartBarrage();
            }

            if (ticksUntilNextShell == 0)
            {
                FireShell();
            }
        }

        public bool StartArtillerySiege(Faction siegingFaction, Site sourceSite, ThingDef artilleryProjectile, int shellsPerBarrage = 1, int numberOfBarrages = 1, float forcedMissRadius = 9, int ticksBetweenShells = 300, float shellVariability = 0, int ticksBetweenBarrages = 60000, float barrageVariability = 0, List<IntVec3> targetCells = null, bool doWaveRaids = false, bool doFinalRaid = false, TaggedPawnGroupMaker waveRaidGroup = null, float waveRaidPoints = 0, TaggedPawnGroupMaker finalRaidGroup = null, float finalRaidPoints = 0)
        {
            //TODO validate inputs are not illegal, like <1 ticks between shells/barrages

            // Don't start a siege if one is already in progress, or if one of the required arguments is null
            if (siegeInProgress || siegingFaction == null || sourceSite == null || artilleryProjectile == null)
                return false;

            this.siegingFaction = siegingFaction;
            this.sourceSite = sourceSite;
            this.artilleryProjectile = artilleryProjectile;

            this.shellsPerBarrage = shellsPerBarrage;
            this.numberOfBarrages = numberOfBarrages;
            this.forcedMissRadius = forcedMissRadius;
            this.ticksBetweenShells = ticksBetweenShells;
            this.ticksBetweenBarrages = ticksBetweenBarrages;
            this.shellVariability = shellVariability;
            this.barrageVariability = barrageVariability;

            this.originalTargetCells = targetCells;
            this.doWaveRaids = doWaveRaids;
            this.waveRaidGroup = waveRaidGroup;
            this.waveRaidPoints = waveRaidPoints;
            this.doFinalRaid = doFinalRaid;
            this.finalRaidGroup = finalRaidGroup;
            this.finalRaidPoints = finalRaidPoints;

            //more stuff
            artilleryOriginCell = GetEdgeCellTowardsWorldTile(map, sourceSite.Tile);
            ticksUntilNextBarrage = CalcCooldownVariable(ticksBetweenBarrages, barrageVariability);

            siegeInProgress = true;

            //TODO decide if letter warning of siege should be done here or by the incident that starts the siege

            return true;
        }

        public void EndArtillerySiege()
        {
            siegeInProgress = false;
            Messages.Message($"The artillery siege by {siegingFaction.Name} has come to an end.", MessageTypeDefOf.NegativeEvent);
        }

        public void StartBarrage()
        {
            barrageInProgress = true;
            shellsLeftInBarrage = shellsPerBarrage;
            SetBarrageCooldown();
            SetShellCooldown();
            Messages.Message($"Take cover! {siegingFaction.Name} is firing an artillery barrage!", MessageTypeDefOf.NegativeEvent);
        }

        public void EndBarrage()
        {
            barrageInProgress = false;
            DoRaid();
            //TODO check if barrages left == 0, end Siege if so
            if (newTargetsCached && !cachedTargetCells.NullOrEmpty())
            {
                UpdateTargetCells(cachedTargetCells);
            }
            Messages.Message($"The artillery barrage is ending.", MessageTypeDefOf.NeutralEvent);
        }

        public void DoRaid()
        {
            if (doWaveRaids && barragesLeftInSiege != 0)
            {
                //TODO spawn raid using wave group
            }
            else if (doFinalRaid && barragesLeftInSiege == 0)
            {
                //TODO spawn raid using final group
            }
        }

        //TODO virtual for CE version? Do I need one, or will patching in a CE-compatible ThingDef for the projectile be good?
        public virtual void FireShell()
        {
            //TODO spawn shell, choose target, launch shell, reset timer
            Projectile shell = SpawnShell();
            IntVec3 target = ChooseTarget();
            shell.Launch
        }

        Projectile SpawnShell()
        {
            //Shell ThingDef should have been validated in StartArtillerySiege
            Projectile shell = (Projectile)ThingMaker.MakeThing(artilleryProjectile);
            GenSpawn.Spawn(shell, artilleryOriginCell, map);

            return shell;
        }

        IntVec3 ChooseTarget()
        {
            IntVec3 aimTarget = targetCells.RandomElement();
            int maxExclusive = GenRadial.NumCellsInRadius(forcedMissRadius);
            int num = Rand.Range(0, maxExclusive);

            IntVec3 missTarget = aimTarget + GenRadial.RadialPattern[num];

            int newX = Mathf.Clamp(missTarget.x, 0, map.Size.x - 1);
            int newZ = Mathf.Clamp(missTarget.z, 0, map.Size.z - 1);

            return new IntVec3(newX, 0, newZ);
        }

        public void UpdateTargetCells(List<IntVec3> newTargets)
        {
            if (!siegeInProgress || newTargets.NullOrEmpty())
                return;

            if (barrageInProgress)
            {
                newTargetsCached = true;
                cachedTargetCells = newTargets;
                return;
            }

            targetCells.Clear();
            foreach (IntVec3 c in newTargets)
            {
                targetCells.Add(c);
            }
        }

        private void SetBarrageCooldown()
        {
            ticksUntilNextBarrage = CalcCooldownVariable(ticksBetweenBarrages, barrageVariability);
        }

        private void SetShellCooldown()
        {
            ticksUntilNextShell = CalcCooldownVariable(ticksBetweenShells, shellVariability);
        }

        public int CalcCooldownVariable(int defaultCooldown, float variability)
        {
            float min = defaultCooldown * (1f - variability);
            float max = defaultCooldown * (1f + variability);

            return Mathf.RoundToInt(Rand.Range(min, max));
        }

        public bool CheckIfSiteStillExists()
        {
            return (sourceSite != null && !sourceSite.Destroyed);
        }

        public static IntVec3 GetEdgeCellTowardsWorldTile(Map map, int sourceWorldTile)
        {
            if (map == null || map.Tile != -1 || sourceWorldTile != -1)
            {
                Log.Warning("Invalid world tile or map has no tile.");
                return CellFinder.RandomEdgeCell(map);
            }

            // Find the angle between the settlement map and the Site responsible for the artillery
            WorldGrid grid = Find.WorldGrid;
            Vector2 mapLongLat = grid.LongLatOf(map.Tile);
            Vector2 sourceLongLat = grid.LongLatOf(sourceWorldTile);
            Vector2 dir = (sourceLongLat - mapLongLat).normalized;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            angle = (angle + 360f) % 360f;

            // Pick a random cell along the map edge matching that angle
            // I think an unpassable cell will be fine since artillery flies overhead
            // Will anyone notice if the origin cell doesn't exactly match the angle? Probably not worth it to be more precise
            IntVec3 cell;
            if (angle >= 45f && angle < 135f)
            {
                // North
                int x = Rand.Range(0, map.Size.x);
                cell = new IntVec3(x, 0, map.Size.z - 1);
            }
            else if (angle >= 135f && angle < 225f)
            {
                // West
                int z = Rand.Range(0, map.Size.z);
                cell = new IntVec3(0, 0, z);
            }
            else if (angle >= 225f && angle < 315f)
            {
                // South
                int x = Rand.Range(0, map.Size.x);
                cell = new IntVec3(x, 0, 0);
            }
            else
            {
                // East
                int z = Rand.Range(0, map.Size.z);
                cell = new IntVec3(map.Size.x - 1, 0, z);
            }

            return cell;
        }
    }
}
