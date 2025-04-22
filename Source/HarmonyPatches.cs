using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorld.BaseGen;
using Verse;
using Verse.AI;
using System.Reflection;
using System.Reflection.Emit;

namespace rep.heframework
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("rep.heframework");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(IncidentWorker_Raid), "TryGenerateRaidInfo")]
        public static class IncidentWorker_Raid_TryGenerateRaidInfo_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                MethodInfo methodHelper = typeof(HarmonyPatches).GetMethod("IncidentWorker_Raid_Helper", BindingFlags.Public | BindingFlags.Static);
                var codes = new List<CodeInstruction>(instructions);
                int jumpIndex = -1;
                int continueIndex = -1;
                bool foundIndex = false;

                Label myCode = generator.DefineLabel();
                Label continueLabel = generator.DefineLabel();

                for (int i = 0; i < codes.Count; i++)
                {
                    if (foundIndex)
                        break;

                    //find the TryResolveRaidFaction method call
                    if (!foundIndex
                        && codes[i].opcode == OpCodes.Callvirt
                        && codes[i].operand.ToString().Contains("TryResolveRaidFaction"))
                    {
                        //move forward from there to find codes
                        for (int j = i; j < codes.Count; j++)
                        {
                            //instruction to jump over the return
                            if (codes[j].opcode == OpCodes.Brtrue_S)
                            {
                                jumpIndex = j;
                            }

                            if (codes[j].opcode == OpCodes.Ldfld)
                            {
                                continueIndex = j - 1; //this will be the ldarg.1 at the start of PawnGroupKindDef groupKind = parms.pawnGroupKind ?? PawnGroupKindDefOf.Combat;
                                foundIndex = true;
                                break;
                            }
                        }
                    }
                }

                if (jumpIndex >= 0 && continueIndex >= 0)
                {
                    //make new instructions
                    List<CodeInstruction> newCodes = new List<CodeInstruction>()
                    {
                        new CodeInstruction(OpCodes.Ldarg_1),
                        new CodeInstruction(OpCodes.Ldarg_2),
                        new CodeInstruction(OpCodes.Ldarg_3),
                        new CodeInstruction(OpCodes.Call, methodHelper),
                        new CodeInstruction(OpCodes.Brfalse_S, continueLabel),
                        new CodeInstruction(OpCodes.Ldc_I4_1),
                        new CodeInstruction(OpCodes.Ret)
                    };

                    //label the start of my code
                    newCodes[0].labels.Add(myCode);

                    //label continue point if my method returns false
                    codes[continueIndex].labels.Add(continueLabel);

                    //change the instruction that jumps to PawnGroupKindDef groupKind = parms.pawnGroupKind ?? PawnGroupKindDefOf.Combat to jump to my code instead
                    codes[jumpIndex] = new CodeInstruction(OpCodes.Brtrue_S, myCode);

                    //insert new instructions
                    codes.InsertRange(continueIndex, newCodes);
                }

                return codes.AsEnumerable();
            }
        }

        public static bool IncidentWorker_Raid_Helper(IncidentParms parms, out List<Pawn> pawns, bool debugTest = false)
        {
            PawnGroupMakerExtension ext = parms.faction.def.GetModExtension<PawnGroupMakerExtension>();
            if (ext == null)
            {
                pawns = null;
                return false;
            }
            else
            {
                HEF_Utils.TryGenerateExtendedRaidInfo(parms, out pawns, debugTest);
                return true;
            }
        }

        [HarmonyPatch(typeof(MapParent), "MapGeneratorDef", MethodType.Getter)]
        public static class MapParent_MapGeneratorDef_Patch
        {
            static void Postfix(MapParent __instance, ref MapGeneratorDef __result)
            {
                if (__instance is Site site)
                {
                    WorldObjectExtension extension = (WorldObjectExtension)site.MainSitePartDef?.modExtensions?.FirstOrDefault(x => x is WorldObjectExtension);

                    if (extension != null && extension.mapGenerator != null)
                    {
                        __result = extension.mapGenerator;
                    }
                }
                
            }
        }

        [HarmonyPatch(typeof(Settlement), "MapGeneratorDef", MethodType.Getter)]
        public static class Settlement_MapGeneratorDef_Postfix
        {
            static void Postfix(Settlement __instance, ref MapGeneratorDef __result)
            {
                WorldObjectExtension extension = (WorldObjectExtension)__instance.Faction?.def.modExtensions?.FirstOrDefault(x => x is WorldObjectExtension);
                if (extension != null && extension.mapGenerator != null)
                {
                    __result = extension.mapGenerator;
                }
            }
        }

        [HarmonyPatch(typeof(Site), "ShouldRemoveMapNow")]
        public static class Site_ShouldRemoveMapNow_Postfix
        {
            static void Postfix(Site __instance, ref bool alsoRemoveWorldObject)
            {
                if (__instance.parts.Any(p => p.def.Worker is HEF_SitePartWorker_Expansion) && GenHostility.AnyHostileActiveThreatToPlayer(__instance.Map, countDormantPawnsAsHostile: true))
                {
                    alsoRemoveWorldObject = false;
                }
            }
        }

        [HarmonyPatch(typeof(SymbolResolver_PawnGroup), "Resolve")]
        public static class SymbolResolver_PawnGroup_Resolve_Prefix
        {
            static bool Prefix()
            {
                Map map = BaseGen.globalSettings.map;
                if (map.generatorDef.genSteps.Any(g => g.defName == "HEF_TaggedPawnGroup"))
                {
                    return false;
                }
                return true;
            }
  
        }

    }
}
