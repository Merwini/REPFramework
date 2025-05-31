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
        IncidentParms parms;
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
            if (ticksUntilNextBarrage <= 0)
            {
                StartBarrage();
            }

            if (ticksUntilNextShell <= 0)
            {
                FireShell();
            }
        }

        public bool StartArtillerySiege(Faction siegingFaction, Site sourceSite, ThingDef artilleryProjectile, IncidentParms parms, int shellsPerBarrage = 1, int numberOfBarrages = 1, float forcedMissRadius = 9, int ticksBetweenShells = 300, float shellVariability = 0, int ticksBetweenBarrages = 60000, float barrageVariability = 0, List<IntVec3> targetCells = null, bool doWaveRaids = false, bool doFinalRaid = false, TaggedPawnGroupMaker waveRaidGroup = null, float waveRaidPoints = 0, TaggedPawnGroupMaker finalRaidGroup = null, float finalRaidPoints = 0)
        {
            //TODO validate inputs are not illegal, like <1 ticks between shells/barrages

            // Don't start a siege if one is already in progress, or if one of the required arguments is null
            if (siegeInProgress || siegingFaction == null || sourceSite == null || artilleryProjectile == null || parms == null)
                return false;

            this.siegingFaction = siegingFaction;
            this.sourceSite = sourceSite;
            this.artilleryProjectile = artilleryProjectile;
            this.parms = parms;

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
            barragesLeftInSiege = numberOfBarrages;
            this.targetCells = ValidateTargets(originalTargetCells);

            if (targetCells.NullOrEmpty())
            {
                Log.Warning("Failed to start siege due to no target cells selected");
                return false;
            }

            siegeInProgress = true;
            DoSiegeLetter(parms);

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
            barragesLeftInSiege--;
            shellsLeftInBarrage = shellsPerBarrage;
            SetBarrageCooldown();
            SetShellCooldown();
            Messages.Message($"Take cover! {siegingFaction.Name} is firing an artillery barrage!", MessageTypeDefOf.NegativeEvent);
        }

        public void EndBarrage()
        {
            barrageInProgress = false;

            DoRaid();

            if (barragesLeftInSiege == 0)
            {
                EndArtillerySiege();
            }
            //else so that it doesn't do both alerts, since EndSiege has its own
            else
            {
                Messages.Message($"The artillery barrage is ending.", MessageTypeDefOf.NeutralEvent);
            }

            if (newTargetsCached && !cachedTargetCells.NullOrEmpty())
            {
                UpdateTargetCells(cachedTargetCells);
            }
        }

        private void DoRaid()
        {
            HE_IncidentParms heParms;
            if (doWaveRaids || doFinalRaid)
            {
                heParms = PrepRaidParms();
            }
            else
            {
                return;
            }

            if (doWaveRaids && barragesLeftInSiege != 0)
            {
                DoWaveRaid(heParms);
            }
            else if (doFinalRaid && barragesLeftInSiege == 0)
            {
                DoFinalRaid(heParms);
            }
        }

        private HE_IncidentParms PrepRaidParms()
        {
            HE_IncidentParms heParms = new HE_IncidentParms();
            HE_Utils.CopyFields(parms, heParms);
            return heParms;
        }

        private void DoWaveRaid(HE_IncidentParms heParms)
        {
            if (waveRaidPoints <= 0)
            {
                Log.Error($"MapComponent_ArtillerySiege tried to send a wave raid between barrages with 0 or negative raid points for faction {siegingFaction}");
                return;
            }

            heParms.points = waveRaidPoints;
            if (waveRaidGroup != null)
            {
                heParms.taggedGroupMaker = waveRaidGroup;
            }
            else
            {
                HE_Utils.TryResolveTaggedPawnGroup(heParms, HE_Utils.FindDefCountsForSites(HE_Utils.FindHESitesFor(parms.faction)).Keys.ToList(), out TaggedPawnGroupMaker groupMaker);
                heParms.taggedGroupMaker = groupMaker;
            }

            // With a TaggedPawnGroupMaker in the HE_IncidentParms, this framework's raid generation detour should handle the rest
            IncidentDefOf.RaidEnemy.Worker.TryExecuteWorker(heParms);
        }

        private void DoFinalRaid(HE_IncidentParms heParms)
        {
            if (finalRaidPoints <= 0)
            {
                Log.Error($"MapComponent_ArtillerySiege tried to send a final raid with 0 or negative raid points for faction {siegingFaction}");
                return;
            }

            heParms.points = finalRaidPoints;
            heParms.taggedGroupMaker = finalRaidGroup;

            if (finalRaidGroup != null)
            {
                heParms.taggedGroupMaker = finalRaidGroup;
            }
            else
            {
                HE_Utils.TryResolveTaggedPawnGroup(heParms, HE_Utils.FindDefCountsForSites(HE_Utils.FindHESitesFor(parms.faction)).Keys.ToList(), out TaggedPawnGroupMaker groupMaker);
                heParms.taggedGroupMaker = groupMaker;
            }

            // With a TaggedPawnGroupMaker in the HE_IncidentParms, this framework's raid generation detour should handle the rest
            IncidentDefOf.RaidEnemy.Worker.TryExecuteWorker(heParms);
        }

        public virtual void FireShell()
        {
            try
            {
                Projectile shell = SpawnShell();
                IntVec3 target = ChooseTarget();
                LaunchShell(shell, target);
                shellsLeftInBarrage--;
                if (shellsLeftInBarrage == 0)
                {
                    EndBarrage();
                }
            }
            catch(Exception ex)
            {
                Log.Error("Exception while firing artillery shell. To prevent devestation, ending the siege immediately. Exception is:\n" + ex.ToString());
                EndBarrage();
                EndArtillerySiege();
            }
        }

        Projectile SpawnShell()
        {
            // Shell ThingDef should have already been validated in StartArtillerySiege
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

        // Virtual for CE override?
        public virtual void LaunchShell(Projectile shell, IntVec3 target)
        {
            shell.Launch(
                launcher: shell, 
                intendedTarget: target,
                usedTarget: target,
                hitFlags: ProjectileHitFlags.All);
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

        public void ForceBarrageTimer(int ticks = 1)
        {
            if (ticksBetweenShells >= 1)
            {
                ticksUntilNextBarrage = ticks;
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
            if (map == null || map.Tile == -1 || sourceWorldTile == -1)
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

        // TODO optimize this, it's really performance-heavy. Although it only runs once, so maybe it's okay? Need to see how long it takes in practice.
        public List<IntVec3> ValidateTargets(List<IntVec3> initialTargets)
        {
            List<IntVec3> newTargets = new List<IntVec3>();

            // Don't target cells under mountain
            if (initialTargets != null)
            {
                foreach (IntVec3 c in initialTargets)
                {
                    if (c.InBounds(map) && map.roofGrid.RoofAt(c) != RoofDefOf.RoofRockThick)
                    {
                        newTargets.Add(c);
                    }
                }
            }

            // If a null or empty list was passed, or list was made empty by removing impenetrable targets, try to populate the list
            if (newTargets.Count == 0)
            {
                List<Room> playerRooms = new List<Room>();

                // Try to find some rooms owned by the player with penetrable roof
                foreach (Room room in map.regionGrid.allRooms)
                {
                    if (!room.TouchesMapEdge &&
                        room.Owners.Any(p => p.Faction == Faction.OfPlayer))
                    {
                        bool hasPenetrableRoof = false;

                        foreach (IntVec3 cell in room.Cells.Take(100))
                        {
                            RoofDef roof = map.roofGrid.RoofAt(cell);
                            if (roof == RoofDefOf.RoofConstructed || roof == RoofDefOf.RoofRockThin)
                            {
                                hasPenetrableRoof = true;
                                break;
                            }
                        }

                        if (hasPenetrableRoof)
                        {
                            playerRooms.Add(room);
                            if (playerRooms.Count >= 10)
                                break;
                        }
                    }
                }

                // Pick a cell from each found room to target
                foreach (Room room in playerRooms)
                {
                    IntVec3 randCell = room.Cells
                        .Where(c =>
                        {
                            RoofDef roof = map.roofGrid.RoofAt(c);
                            return roof == RoofDefOf.RoofConstructed || roof == RoofDefOf.RoofRockThin;
                        })
                        .RandomElementWithFallback(IntVec3.Invalid);

                    if (randCell.IsValid)
                    {
                        newTargets.Add(randCell);
                    }
                }
            }

            // If no cells were selected, probably because no legal Rooms were found, try to target 10 random player-owned structures
            if (newTargets.Count == 0)
            {
                List<Thing> ownedStructures = map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                    .Where(t => t.Faction == Faction.OfPlayer &&
                                t.Position.InBounds(map) &&
                                map.roofGrid.RoofAt(t.Position) != RoofDefOf.RoofRockThick)
                    .OrderBy(_ => Rand.Value)
                    .Take(10)
                    .ToList();

                foreach (Thing structure in ownedStructures)
                {
                    newTargets.Add(structure.Position);
                }
            }

            return newTargets;
        }

        void DoSiegeLetter(IncidentParms parms)
        {
            TaggedString baseLabel = "HE_LetterLabelArtillerySiege".Translate();
            TaggedString baseText = "HE_LetterTextArtillerySiege".Translate();

            TaggedString waveText = doWaveRaids ? "HE_LetterWaveArtillerySiege".Translate() : TaggedString.Empty;
            TaggedString finalRaidText = doFinalRaid ? "HE_LetterRaidArtillerySiege".Translate() : TaggedString.Empty;
            TaggedString barrageText = (numberOfBarrages >= 0) ? new TaggedString(numberOfBarrages.ToString()) : "HE_Unlimited".Translate();

            NamedArgument[] textArgs = new NamedArgument[]
            {
            siegingFaction.Name,      // {0}
            barrageText,              // {1}
            waveText,                 // {2}
            finalRaidText             // {3}
            };

            IncidentWorker.SendIncidentLetter(
                baseLabel,
                baseText,
                LetterDefOf.ThreatBig,
                parms,
                sourceSite,
                null,
                textArgs
            );
        }

        //TODO ExposeData()
    }
}
