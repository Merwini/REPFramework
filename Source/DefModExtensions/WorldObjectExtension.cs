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
    public class WorldObjectExtension : DefModExtension
    {
        #region Fields
        public List<FactionGroupLink> factionsToPawnGroups;

        public MapGeneratorDef mapGenerator;

        public FloatRange threatPointsRange = new FloatRange(240f, 10000f);

        public float maximumThreatPoints = 10000f;

        public SimpleCurve threatPointCurve;
        #endregion
    }

    public class FactionGroupLink
    {
        //TODO maybe pre-resolve references like AmmoLinks
        public FactionDef faction;

        public List<string> pawnGroups;

    }
}