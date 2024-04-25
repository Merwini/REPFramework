using CombatExtended;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace rep.framework
{
    public static class AMP_Utils
    {
        public static List<Faction> ReturnAMPFactions()
        {
            List<Faction> amps = Find.FactionManager.AllFactions.Where(f => (f.def == AMP_DefOf.AMP_FactionFriendly || f.def == AMP_DefOf.AMP_FactionHostile)).ToList();

            return amps;
        }

        public static List<Faction> ReturnHostileAMPFactions()
        {
            List<Faction> hamps = ReturnAMPFactions().Where(f => f.HostileTo(Find.FactionManager.OfPlayer)).ToList();

            return hamps;
        }

        public static List<Site> FindAMPSitesFor(Faction fact)
        {
            List<Site> ampSites = new List<Site>();

            List<Site> worldSites = Find.WorldObjects.Sites;
            foreach (Site site in worldSites)
            {
                if (site == null)
                    continue;
                if (site.Faction != fact)
                    continue;
                if (site.MainSitePartDef == null)
                    continue;
                if (site.MainSitePartDef.defName.StartsWith("AMP_"))
                {
                    ampSites.Add(site);
                    continue;
                }
            }

            return ampSites;
        }

        public static List<SitePartDef> FindExistingAMPSiteDefsFor(Faction fact)
        {
            List<Site> ampSites = FindAMPSitesFor(fact);
            List<SitePartDef> ampSitePartDefs = FindDefsForSites(ampSites);
            
            return ampSitePartDefs;
        }

        public static List<SitePartDef> FindEligibleAMPSiteDefsFor(Faction fact)
        {
            HashSet<SitePartDef> eligibleDefs = new HashSet<SitePartDef>();

            foreach (SitePartDef def in DefDatabase<SitePartDef>.AllDefs)
            {
                if (def.defName.StartsWith("AMP_"))
                {
                    Log.Message("Found AMP SPD " + def.defName);
                    eligibleDefs.Add(def);
                }
            }

            List<SitePartDef> usedDefs = FindExistingAMPSiteDefsFor(fact);

            if (!usedDefs.NullOrEmpty())
            {
                foreach (SitePartDef def in usedDefs)
                {
                    eligibleDefs.Remove(def);
                }
            }

            return eligibleDefs.ToList();
        }

        public static List<SitePartDef> FindDefsForSites(List<Site> sites)
        {
            List<SitePartDef> defs = new List<SitePartDef>();

            if (!sites.NullOrEmpty())
            {
                foreach (Site site in sites)
                {
                    defs.Add(site.MainSitePartDef);
                }
            }

            return defs;
        }

        public static List<string> FindStringsForDefs(List<SitePartDef> defs)
        {
            List<string> tags = new List<string>();

            if (!defs.NullOrEmpty())
            {
                foreach (SitePartDef def in defs)
                {
                    //the list is initialized empty in the class, so no null check needed
                    foreach (string tag in def.tags)
                    {
                        if (tag.StartsWith("AMP_PGM_"))
                        {
                            tags.Add(tag);
                        }
                    }
                }
            }

            return tags;
        }

        public static bool CheckIfAllTagsPresent(List<string> pgmTags, List<string> siteTags)
        {
            foreach (string str in pgmTags)
            {
                if (!siteTags.Contains(str))
                {
                    return false;
                }
            }
            return true;
        }
    }
}