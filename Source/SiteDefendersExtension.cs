using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Reflection;
using System.Text;
using Verse;

namespace rep.heframework
{
    public class SiteDefendersExtension : DefModExtension
    {
        #region Fields
        public List<FactionGroupLink> factionsToPawnGroups;
        #endregion
    }

    public class FactionGroupLink
    {
        //TODO maybe pre-resolve references like AmmoLinks
        public string faction;

        public List<string> pawnGroups;
    }
}