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
    public class GenStep_LootPerPawn : GenStep
    {
        public override int SeedPart => 753257316;

        public override void Generate(Map map, GenStepParams parms)
        {
            //Doing warnings instead of errors for now, because this GenStep is less critical to play experience

            Faction faction = GetMapFaction(map, parms);
            if (faction == null)
            {
                Log.Warning("GenStep_LootPerPawn for new map failed to find faction. Unable to adjust loot.");
                return;
            }

            WorldObjectExtensionHEF extension = HEF_Utils.GetWorldObjectExtension(faction.def, parms);
            if (extension == null)
            {
                Log.Warning("GenStep_LootPerPawn for new map failed to find WorldObjectExtension. Unable to adjust loot.");
                return;
            }

            Dictionary<ThingDef, int> lootLinkDict = new Dictionary<ThingDef, int>();
            if (!PopulateDictionary(lootLinkDict, extension))
            {
                Log.Warning("GenStep_LootPerPawn for new map could not make a Dictionary from the WorldObjectExtension. Unable to adjust loot.");
                return;
            }

            Dictionary<ThingDef, List<Thing>> thingDict = new Dictionary<ThingDef, List<Thing>>();
            int pawnsCount = 0;
            //maybe use out instead of ref and initialize in method? does it matter?
            CheckThingsInMap(lootLinkDict, map, thingDict, ref pawnsCount);

            if (thingDict.Count == 0)
            {
                Log.Warning("GenStep_LootPerPawn for new map found no Things configured to be adjusted. Was this intentional?");
                return; //return because pointless to continue if there is nothing to adjust
            }
            //else if so that this will only warn if Things were found but no pawns were
            else if (pawnsCount == 0)
            {
                Log.Warning("GenStep_LootPerPawn for new map found 0 pawns on the map. Loot adjustment will zero out the loot. Was this intentional?");
                //no return, in case the map intentionally has no pawns
            }

            AdjustLoots(lootLinkDict, ref thingDict, pawnsCount);
        }

        public Faction GetMapFaction(Map map, GenStepParams parms)
        {
            //TODO move this into HEF_Utils and make GenStep_TaggedPawnGroup use it
            Faction faction;
            faction = parms.sitePart?.site?.Faction;

            if (faction == null)
            {
                faction = map.ParentFaction;
            }
            return faction;
        }

        public bool PopulateDictionary(Dictionary<ThingDef, int> lootLinkDict, WorldObjectExtensionHEF extension)
        {
            if (extension == null || extension.lootLinks.NullOrEmpty())
            {
                return false;
            }

            foreach (LootPerPawnLink link in extension.lootLinks)
            {
                lootLinkDict.Add(link.thingDef, link.thingPerPawn);
            }

            return true;
        }

        public bool CheckThingsInMap(Dictionary<ThingDef, int> linkDict, Map map, Dictionary<ThingDef, List<Thing>> thingDict, ref int pawnCount)
        {
            foreach (Thing thing in map.listerThings.AllThings)
            {
                if (thing is Pawn pawn)
                {
                    pawnCount++;
                }
                else if (linkDict.ContainsKey(thing.def))
                {
                    if (!thingDict.ContainsKey(thing.def))
                    {
                        thingDict[thing.def] = new List<Thing>();
                    }

                    thingDict[thing.def].Add(thing);
                }
            }
            return true;
        }

        public bool AdjustLoots(Dictionary<ThingDef, int> linkDict, ref Dictionary<ThingDef, List<Thing>> thingDict, int pawnCount)
        {
            foreach (var link in linkDict)
            {
                if (!thingDict.ContainsKey(link.Key))
                {
                    continue;
                }

                //I don't think the count would ever be 0, or the entry wouldn't exist, but it costs little to verify
                if (thingDict[link.Key].Count == 0)
                {
                    continue;
                }

                int lootTarget = link.Value * pawnCount;

                int lootPerThing = (int)(lootTarget / thingDict[link.Key].Count); 

                if (lootPerThing > link.Key.stackLimit)
                {
                    lootPerThing = link.Key.stackLimit;
                }

                foreach (Thing thing in thingDict[link.Key])
                {
                    thing.stackCount = lootPerThing;
                }
            }

            return true;
        }
    }
}