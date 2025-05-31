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
    public class WorldObjectExtensionHE : DefModExtension
    {
        public List<FactionGroupLink> factionsToPawnGroups;

        public MapGeneratorDef mapGenerator;

        public float threatPointModifier = 0f;

        public FloatRange spawnThreatPointRange = new FloatRange(0, 1000000);

        public FloatRange defenderThreatPointsRange = new FloatRange(240f, 20000f);

        public float settlementThreatPoints = 6000f;

        public SimpleCurve threatPointCurve;

        public List<LootPerPawnLink> lootLinks;

        public int minimumTileDistance = 2;

        public int maximumTileDistance = 8;

        public int maximumSiteCount = 10;

        public IncidentDef fireIncidentOnSpawn = null;

        #region ArtilleryStuff

        public ThingDef artilleryProjectile;

        public int shellsPerBarrage = 3;

        public int numberOfBarrages = 4;

        public float forcedMissRadius = 9f;

        public int ticksBetweenShells = 300; // 5 seconds

        public float shellVariability = 0f;

        public int ticksBetweenBarrages = 15000; // quarter of an in-game day

        public float barrageVariability = 0f;

        public bool doWaveRaids = false;

        public string waveRaidGroupName = null;

        public float waveRaidPointMultiplier = 0.25f;

        public bool doFinalRaid = false;

        public string finalRaidGroupName = null;

        public float finalRaidPointMultiplier = 1f;

        #endregion
    }

    public class FactionGroupLink
    {
        //TODO maybe pre-resolve references like AmmoLinks
        private FactionDef faction;

        private List<string> pawnGroups;

        public FactionDef Faction => faction;

        public List<string> PawnGroups => pawnGroups;

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