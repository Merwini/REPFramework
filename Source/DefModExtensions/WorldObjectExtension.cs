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

        public List<LootPerPawnLink> lootLinks;

        #endregion
    }

    public class FactionGroupLink
    {
        //TODO maybe pre-resolve references like AmmoLinks
        public FactionDef faction;

        public List<string> pawnGroups;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "faction", xmlRoot.Name);

            pawnGroups = new List<string>();

            foreach (var group in xmlRoot.InnerText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                pawnGroups.Add(group);
            }
        }
    }

    public class LootPerPawnLink
    {
        public ThingDef thingDef;

        public int thingPerPawn;

        public void LoadDataFromXmlCustom(XmlNode xmlRoot)
        {
            DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "thingDef", xmlRoot.Name);
            thingPerPawn = ParseHelper.FromString<int>(xmlRoot.FirstChild.Value);
        }
    }
}