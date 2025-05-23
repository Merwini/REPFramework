using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace rep.heframework
{
    public static class HEDebugActions
    {
        internal const string DebugPrefix = "HEDebugForced_";

        //TODO 1.5
        [DebugAction("RimWorld Enhancement Project", "List HE Factions", false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static void ListHEFactions()
        {
            List<Faction> heFacts = HEF_Utils.ReturnHEFFactions();
            if (heFacts.NullOrEmpty())
            {
                Log.Warning("No HE Factions found");
            }
            else
            {
                foreach (Faction fact in heFacts)
                {
                    Log.Message(fact.Name);
                }
            }
        }

        [DebugAction("RimWorld Enhancement Project", "List HE Sites for...", false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static void ListHESitesFor() 
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            List<Faction> heFacts = HEF_Utils.ReturnHEFFactions();

            foreach (Faction f in heFacts)
            {
                Faction faction = f;
                list.Add(new DebugMenuOption(faction.Name + " (" + faction.def.defName + ")", DebugMenuOptionMode.Action, delegate
                {
                    List<Site> heSites = HEF_Utils.FindHEFSitesFor(faction);
                    if (heSites.NullOrEmpty())
                    {
                        Log.Warning("Selected faction has no active HE sites");
                    }
                    else
                    {
                        foreach (Site site in heSites)
                        {
                            Log.Message(site.MainSitePartDef.defName.ToString()); //TODO more info like age?
                        }
                    }
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        [DebugAction("RimWorld Enhancement Project", "Execute HE raid with groupName...", false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static void ExecuteRaidGroupName()
        {
            //List HE factions, pick one
            //List tags for tagged PawnGroupMakers for that faction
            //Hide that tag somewhere in parms and force the raid
            //Rewrite TryGenerateExtendedRaidInfo to check whatever field I hide the tag in

            StorytellerComp storytellerComp = Find.Storyteller.storytellerComps.First((StorytellerComp x) => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain);
            IncidentParms parms = storytellerComp.GenerateParms(IncidentCategoryDefOf.ThreatBig, Find.CurrentMap);
            parms.forced = true;
            List<DebugMenuOption> factionList = new List<DebugMenuOption>();
            foreach (Faction allHEFaction in HEF_Utils.ReturnHEFFactions())
            {
                Faction localFac = allHEFaction;
                factionList.Add(new DebugMenuOption(localFac.Name + " (" + localFac.def.defName + ")", DebugMenuOptionMode.Action, delegate
                {
                    parms.faction = localFac;
                    PawnGroupMakerExtensionHEF extension = localFac.def.GetModExtension<PawnGroupMakerExtensionHEF>(); //faction would not have been an option if this was null, no check needed
                    List<DebugMenuOption> tagList = new List<DebugMenuOption>();
                    foreach (TaggedPawnGroupMaker tpgm in extension.taggedPawnGroupMakers.Where(x => x.groupName != null))
                    {
                        string localTag = tpgm.groupName;
                        tagList.Add(new DebugMenuOption(localTag, DebugMenuOptionMode.Action, delegate
                        {
                            parms.questTag = DebugPrefix + localTag; //storing this here should be safe, as it will be otherwise unused by this raid generation
                            DebugActionsIncidents.DoRaid(parms);
                        }));
                    }
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(tagList));
                }));
            }
            Find.WindowStack.Add(new Dialog_DebugOptionListLister(factionList));
        }

        //TODO Spawn random amp site for faction

        //TODO Spawn selected amp site for faction
    }
}