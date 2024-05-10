using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace rep.heframework
{
    public static class HEF_Utils
    {
        public static List<Faction> ReturnHEFFactions()
        {
            List<Faction> factions = Find.FactionManager.AllFactions.Where(f => (f.def.HasModExtension<PawnGroupMakerExtension>())).ToList();

            return factions;
        }

        public static List<Faction> ReturnHostileHEFFactions()
        {
            List<Faction> hostileFactions = ReturnHEFFactions().Where(f => f.HostileTo(Find.FactionManager.OfPlayer)).ToList();

            return hostileFactions;
        }

        public static List<Site> FindHEFSitesFor(Faction fact)
        {
            List<Site> hefSites = new List<Site>();

            List<Site> worldSites = Find.WorldObjects.Sites;
            foreach (Site site in worldSites)
            {
                if (site == null)
                    continue;
                if (site.Faction != fact)
                    continue;
                if (site.MainSitePartDef == null)
                    continue;
                if (site.MainSitePartDef.HasModExtension<SiteDefendersExtension>())
                {
                    hefSites.Add(site);
                    continue;
                }
            }

            return hefSites;
        }

        public static List<SitePartDef> FindExistingHEFSiteDefsFor(Faction fact)
        {
            List<Site> hefSites = FindHEFSitesFor(fact);
            List<SitePartDef> hefSitePartDefs = FindDefsForSites(hefSites);
            
            return hefSitePartDefs;
        }

        public static List<SitePartDef> FindEligibleHEFSiteDefsFor(Faction fact)
        {
            HashSet<SitePartDef> eligibleDefs = new HashSet<SitePartDef>();

            foreach (SitePartDef def in DefDatabase<SitePartDef>.AllDefs)
            {
                SiteDefendersExtension extension = def.GetModExtension<SiteDefendersExtension>();
                if (extension != null && extension.factionsToPawnGroups.Any(x => x.faction == fact.def.defName))
                {
                    eligibleDefs.Add(def);
                }
            }

            List<SitePartDef> usedDefs = FindExistingHEFSiteDefsFor(fact);

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
                        tags.Add(tag);
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