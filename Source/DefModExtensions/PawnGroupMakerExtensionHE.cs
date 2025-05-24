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
    public class PawnGroupMakerExtensionHE : DefModExtension
    {
        #region Fields
        public bool alwaysUseHighestTier = false;

        public List<TaggedPawnGroupMaker> taggedPawnGroupMakers;


        #endregion
    }
}