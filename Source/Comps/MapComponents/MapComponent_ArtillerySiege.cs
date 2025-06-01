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
        int ticksUntilFirstBarrage;
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
        IntVec2 artilleryDirection;

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

        public bool StartArtillerySiege(Faction siegingFaction, Site sourceSite, IncidentParms parms, WorldObjectExtensionHE extension)
        {
            // Unfixable missing data
            if (siegeInProgress || siegingFaction == null || sourceSite == null || parms == null || extension == null || extension.artilleryProjectile == null)
                return false;

            // Fixable misconfigurations
            int shellsPerBarrage = Mathf.Max(1, extension.shellsPerBarrage);
            if (extension.shellsPerBarrage < 1)
                Log.Warning("Artillery siege config had shellsPerBarrage < 1. Defaulting to 1.");

            int numberOfBarrages = extension.numberOfBarrages == 0 ? 1 : extension.numberOfBarrages;
            if (extension.numberOfBarrages == 0)
                Log.Warning("Artillery siege config had 0 barrages. Defaulting to 1.");

            int ticksBetweenShells = extension.ticksBetweenShells < 1 ? 60 : extension.ticksBetweenShells;
            if (extension.ticksBetweenShells < 1)
                Log.Warning("Artillery siege config had ticksBetweenShells < 1. Defaulting to 60.");

            int ticksBetweenBarrages = extension.ticksBetweenBarrages < 1 ? 60000 : extension.ticksBetweenBarrages;
            if (extension.ticksBetweenBarrages < 1)
                Log.Warning("Artillery siege config had ticksBetweenBarrages < 1. Defaulting to 60000.");


            this.siegingFaction = siegingFaction;
            this.sourceSite = sourceSite;
            this.artilleryProjectile = extension.artilleryProjectile;
            this.parms = parms;

            this.shellsPerBarrage = shellsPerBarrage;
            this.numberOfBarrages = numberOfBarrages;
            this.forcedMissRadius = extension.forcedMissRadius;
            this.ticksUntilFirstBarrage = extension.ticksUntilFirstBarrage;
            this.ticksBetweenShells = ticksBetweenShells;
            this.ticksBetweenBarrages = ticksBetweenBarrages;
            this.shellVariability = extension.shellVariability;
            this.barrageVariability = extension.barrageVariability;

            this.originalTargetCells = null;
            this.doWaveRaids = extension.doWaveRaids;
            this.doFinalRaid = extension.doFinalRaid;

            PawnGroupMakerExtensionHE factionExt = siegingFaction.def.GetModExtension<PawnGroupMakerExtensionHE>();
            if (factionExt != null)
            {
                if (doWaveRaids)
                {
                    waveRaidGroup = factionExt.taggedPawnGroupMakers?.FirstOrDefault(x =>
                        string.Equals(x.groupName, extension.waveRaidGroupName, StringComparison.OrdinalIgnoreCase));
                    waveRaidPoints = parms.points * extension.waveRaidPointMultiplier;
                }

                if (doFinalRaid)
                {
                    finalRaidGroup = factionExt.taggedPawnGroupMakers?.FirstOrDefault(x =>
                        string.Equals(x.groupName, extension.finalRaidGroupName, StringComparison.OrdinalIgnoreCase));
                    finalRaidPoints = parms.points * extension.finalRaidPointMultiplier;
                }
            }

            artilleryDirection = GetEdgeCellTowardsWorldTile(map, sourceSite.Tile);
            ticksUntilNextBarrage = ticksUntilFirstBarrage;
            barragesLeftInSiege = numberOfBarrages;
            this.targetCells = ValidateTargets(originalTargetCells);

            if (targetCells.NullOrEmpty())
            {
                Log.Warning("Failed to start siege due to no valid target cells.");
                return false;
            }

            siegeInProgress = true;
            DoSiegeLetter(parms);

            if (HE_Settings.debugLogging)
            {
                Log.Message($"Starting siege on map {map.Parent.Label}. Sieging faction: {siegingFaction}. Projectile: {artilleryProjectile.defName}. Shells per barrage: {shellsPerBarrage}. Number of barrages: {numberOfBarrages}. Ticks between shells: {ticksBetweenShells}. Ticks between barrages: {ticksBetweenBarrages}. Ticks until first barrage: {ticksUntilFirstBarrage}.");
            }

            return true;
        }

        public void EndArtillerySiege()
        {
            siegeInProgress = false;
            Messages.Message("HE_AlertSiegeEnded".Translate(siegingFaction.Name), MessageTypeDefOf.NegativeEvent);
        }

        public void StartBarrage()
        {
            barrageInProgress = true;
            barragesLeftInSiege--;
            shellsLeftInBarrage = shellsPerBarrage;
            SetBarrageCooldown();
            SetShellCooldown();
            Messages.Message("HE_AlertBarrageStarted".Translate(siegingFaction.Name), MessageTypeDefOf.NegativeEvent);

            if (HE_Settings.debugLogging)
            {
                Log.Message($"Starting barrage. Barrages left in siege: {barragesLeftInSiege}. Shells in barrage: {shellsLeftInBarrage}. Barrage cooldown set to: {ticksUntilNextBarrage}. Shell cooldown set to: {ticksUntilNextShell}");
            }
        }

        public void EndBarrage()
        {
            barrageInProgress = false;

            try
            {
                DoRaid();
            }
            catch (Exception ex)
            {
                Log.Error("Exception while spawning raid for artillery siege. Exception is:\n" + ex.ToString());
            }

            if (barragesLeftInSiege == 0)
            {
                EndArtillerySiege();
            }
            //else so that it doesn't do both alerts, since EndSiege has its own
            else
            {
                Messages.Message($"HE_AlertBarrageEnded".Translate(), MessageTypeDefOf.NeutralEvent);
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
            heParms.target = map;
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
            Projectile shell = null;
            IntVec3 target = IntVec3.Zero;
            try
            {
                target = ChooseTarget();
                shell = SpawnShell(target);
                LaunchShell(shell, target);
                shellsLeftInBarrage--;
                if (shellsLeftInBarrage <= 0)
                {
                    EndBarrage();
                }
                else
                {
                    SetShellCooldown();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Exception while firing artillery shell. To prevent devestation, ending the siege immediately. Exception is:\n" + ex.ToString());
                EndBarrage();
                EndArtillerySiege();
            }
            finally
            {
                if (HE_Settings.debugLogging)
                {
                    Log.Message($"Fired shell {(shell != null ? shell.def.defName : null)} at target {target}. Shells left in barrage: {shellsLeftInBarrage}. Cooldown reset to: {ticksUntilNextShell}");
                }
            }
        }

        Projectile SpawnShell(IntVec3 target)
        {
            IntVec3 spawnCell;

            if (artilleryDirection == IntVec2.North)
            {
                spawnCell = new IntVec3(target.x, 0, map.Size.z - 1);
            }
            else if (artilleryDirection == IntVec2.South)
            {
                spawnCell = new IntVec3(target.x, 0, 0);
            }
            else if (artilleryDirection == IntVec2.East)
            {
                spawnCell = new IntVec3(map.Size.x - 1, 0, target.z);
            }
            else if (artilleryDirection == IntVec2.West)
            {
                spawnCell = new IntVec3(0, 0, target.z);
            }
            else
            {
                Log.Warning("SpawnShell Invalid artilleryDirection; defaulting to map center.");
                spawnCell = new IntVec3(map.Size.x / 2, 0, map.Size.z - 1);
            }

            // Shell ThingDef should have already been validated in StartArtillerySiege
            Projectile shell = ThingMaker.MakeThing(artilleryProjectile) as Projectile;
            if (shell == null)
            {
                Log.Error("Failed to cast artillery projectile to Projectile.");
                return null;
            }

            GenSpawn.Spawn(shell, spawnCell, map);
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

            targetCells = newTargets.ToList();
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

        public static IntVec2 GetEdgeCellTowardsWorldTile(Map map, int sourceWorldTile)
        {
            if (map == null || map.Tile == -1 || sourceWorldTile == -1)
            {
                Log.Warning("GetEdgeCellTowardsWorldTile: Invalid world tile or map has no tile.");
                return IntVec2.North;
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
            if (angle >= 45f && angle < 135f)
            {
                return IntVec2.North;
            }
            else if (angle >= 135f && angle < 225f)
            {
                return IntVec2.West;
            }
            else if (angle >= 225f && angle < 315f)
            {
                return IntVec2.South;
            }
            else
            {
                return IntVec2.East;
            }
        }

        // TODO gauge performance of this on a real map
        public List<IntVec3> ValidateTargets(List<IntVec3> initialTargets)
        {
            List<IntVec3> newTargets = FilterInvalidTargetCells(initialTargets);

            if (newTargets.Count == 0)
            {
                newTargets = GetTargetCellsFromPlayerRooms();
            }

            if (newTargets.Count == 0)
            {
                newTargets = GetTargetCellsFromPlayerStructures();
            }

            return newTargets;
        }

        private List<IntVec3> FilterInvalidTargetCells(List<IntVec3> cells)
        {
            List<IntVec3> result = new List<IntVec3>();
            if (cells == null)
                return result;

            foreach (IntVec3 c in cells)
            {
                if (c.InBounds(map) && map.roofGrid.RoofAt(c) != RoofDefOf.RoofRockThick)
                {
                    result.Add(c);
                }
            }
            return result;
        }

        // Tries to return a list of coordinates, each corresponding to a cell chosen at random from a room owned by the player that can be penetrated by mortar shells
        // TODO reevaluate magic numbers
        private List<IntVec3> GetTargetCellsFromPlayerRooms()
        {
            List<IntVec3> result = new List<IntVec3>();
            List<Room> playerRooms = new List<Room>();

            foreach (Room room in map.regionGrid.allRooms)
            {
                if (!room.TouchesMapEdge &&
                    room.Owners.Any(p => p.Faction == Faction.OfPlayer))
                {
                    bool hasPenetrableRoof = room.Cells.Take(100).Any(cell =>
                    {
                        RoofDef roof = map.roofGrid.RoofAt(cell);
                        return roof == RoofDefOf.RoofConstructed || roof == RoofDefOf.RoofRockThin;
                    });

                    if (hasPenetrableRoof)
                    {
                        playerRooms.Add(room);
                        if (playerRooms.Count >= 10)
                            break;
                    }
                }
            }

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
                    result.Add(randCell);
                }
            }

            return result;
        }

        // Used if no coordinates were found by GetTargetCellsFromPlayerRooms, tries to target outdoor structures owned by the player
        private List<IntVec3> GetTargetCellsFromPlayerStructures()
        {
            return map.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial)
                .Where(t => t.Faction == Faction.OfPlayer &&
                            t.Position.InBounds(map) &&
                            map.roofGrid.RoofAt(t.Position) != RoofDefOf.RoofRockThick)
                .OrderBy(_ => Rand.Value)
                .Take(10)
                .Select(t => t.Position)
                .ToList();
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

        public override void ExposeData()
        {
            Scribe_Values.Look(ref siegeInProgress, "siegeInProgress", false);
            Scribe_Values.Look(ref barrageInProgress, "barrageInProgress", false);
            Scribe_Values.Look(ref shellsPerBarrage, "shellsPerBarrage", 1);
            Scribe_Values.Look(ref numberOfBarrages, "numberOfBarrages", 1);
            Scribe_Values.Look(ref forcedMissRadius, "forcedMissRadius", 9f);
            Scribe_Values.Look(ref ticksUntilFirstBarrage, "ticksUntilFirstBarrage", 180000);
            Scribe_Values.Look(ref ticksBetweenShells, "ticksBetweenShells", 300);
            Scribe_Values.Look(ref ticksBetweenBarrages, "ticksBetweenBarrages", 60000);
            Scribe_Values.Look(ref barrageVariability, "barrageVariability", 0f);
            Scribe_Values.Look(ref shellVariability, "shellVariability", 0f);
            Scribe_Values.Look(ref ticksUntilNextBarrage, "ticksUntilNextBarrage", 0);
            Scribe_Values.Look(ref ticksUntilNextShell, "ticksUntilNextShell", 0);
            Scribe_Values.Look(ref barragesLeftInSiege, "barragesLeftInSiege", 0);
            Scribe_Values.Look(ref shellsLeftInBarrage, "shellsLeftInBarrage", 0);

            Scribe_Values.Look(ref newTargetsCached, "newTargetsCached", false);
            Scribe_Collections.Look(ref targetCells, "targetCells", LookMode.Value);
            Scribe_Collections.Look(ref cachedTargetCells, "cachedTargetCells", LookMode.Value);
            Scribe_Collections.Look(ref originalTargetCells, "originalTargetCells", LookMode.Value);
            Scribe_Values.Look(ref artilleryDirection, "artilleryDirection");

            Scribe_References.Look(ref siegingFaction, "siegingFaction");
            Scribe_References.Look(ref sourceSite, "sourceSite");
            Scribe_Defs.Look(ref artilleryProjectile, "artilleryProjectile");
            Scribe_Deep.Look(ref parms, "incidentParms");

            Scribe_Values.Look(ref doWaveRaids, "doWaveRaids", false);
            Scribe_Values.Look(ref doFinalRaid, "doFinalRaid", false);
            Scribe_Values.Look(ref waveRaidPoints, "waveRaidPoints", 0f);
            Scribe_Values.Look(ref finalRaidPoints, "finalRaidPoints", 0f);
            Scribe_Deep.Look(ref waveRaidGroup, "waveRaidGroup");
            Scribe_Deep.Look(ref finalRaidGroup, "finalRaidGroup");

            string waveGroupName = waveRaidGroup?.groupName;
            string finalGroupName = finalRaidGroup?.groupName;
            Scribe_Values.Look(ref waveGroupName, "waveRaidGroupName");
            Scribe_Values.Look(ref finalGroupName, "finalRaidGroupName");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                waveRaidGroup = null;
                if (!waveGroupName.NullOrEmpty() && siegingFaction != null)
                {
                    PawnGroupMakerExtensionHE ext = siegingFaction.def.GetModExtension<PawnGroupMakerExtensionHE>();
                    if (ext != null)
                    {
                        waveRaidGroup = ext.taggedPawnGroupMakers?.FirstOrDefault(g => g.groupName == waveGroupName);
                    }
                }

                finalRaidGroup = null;
                if (!finalGroupName.NullOrEmpty() && siegingFaction != null)
                {
                    PawnGroupMakerExtensionHE ext = siegingFaction.def.GetModExtension<PawnGroupMakerExtensionHE>();
                    if (ext != null)
                    {
                        finalRaidGroup = ext.taggedPawnGroupMakers?.FirstOrDefault(g => g.groupName == finalGroupName);
                    }
                }
            }
        }
    }
}
