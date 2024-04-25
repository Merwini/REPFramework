using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
//using LudeonTK;

namespace rep.framework
{
    public static class DebugActions
    {
        //TODO 1.5
        /*
        [DebugAction("Army Modernization Project", "List AMP Factions", false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static void ListAmpFactions()
        {
            List<Faction> ampFacts = AMP_Utils.ReturnAMPFactions();
            if (ampFacts.NullOrEmpty())
            {
                Log.Warning("No AMP Factions found");
            }
            else
            {
                foreach (Faction fact in ampFacts)
                {
                    Log.Message(fact.Name);
                }
            }
        }

        [DebugAction("Army Modernization Project", "List AMP Sites for:", false, false, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static void ListAmpSitesFor() 
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            List<Faction> ampFacts = AMP_Utils.ReturnAMPFactions();

            foreach (Faction f in ampFacts)
            {
                Faction faction = f;
                list.Add(new DebugMenuOption(faction.Name + " (" + faction.def.defName + ")", DebugMenuOptionMode.Action, delegate
                {
                    List<Site> ampSites = AMP_Utils.FindAMPSitesFor(faction);
                    if (ampSites.NullOrEmpty())
                    {
                        Log.Warning("Selected faction has no active AMP sites");
                    }
                    else
                    {
                        foreach (Site site in ampSites)
                        {
                            Log.Message(site.MainSitePartDef.defName.ToString()); //TODO more info like age?
                        }
                    }
                }));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        //TODO Spawn amp raid for faction based on its current sites

        //TODO Spawn selected amp raid for faction

        //TODO Spawn random amp site for faction

        //TODO Spawn selected amp site for faction
        */
    }
}