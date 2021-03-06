﻿using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Linq;
using System.Text;
using Harmony;
using UnityEngine;
using static BootDialog.PostBootDialog;
using System.Reflection;
using System.IO;

namespace CustomizePlants
{
    public static class PLANTS
    {
        public static readonly string[] NAMES = {
            BasicSingleHarvestPlantConfig.ID,
            SeaLettuceConfig.ID,
            BasicFabricMaterialPlantConfig.ID,
            BeanPlantConfig.ID,
            BulbPlantConfig.ID,
            CactusPlantConfig.ID,
            ColdWheatConfig.ID,
            EvilFlowerConfig.ID,
            ForestTreeBranchConfig.ID,
            ForestTreeConfig.ID,
            GasGrassConfig.ID,
            LeafyPlantConfig.ID,
            MushroomPlantConfig.ID,
            PrickleFlowerConfig.ID,
            PrickleGrassConfig.ID,
            SaltPlantConfig.ID,
            SpiceVineConfig.ID,
            SwampLilyConfig.ID,
            ColdBreatherConfig.ID,
            OxyfernConfig.ID,
            SuperWormPlantConfig.ID,
            WormPlantConfig.ID,
            SwampHarvestPlantConfig.ID,
            ToePlantConfig.ID,
            WineCupsConfig.ID,
            CritterTrapPlantConfig.ID,
            CylindricaConfig.ID,
            FilterPlantConfig.ID
        };

        public static readonly Type[] CLASSES = {
            typeof(BasicSingleHarvestPlantConfig),
            typeof(SeaLettuceConfig),
            typeof(BasicFabricMaterialPlantConfig),
            typeof(BeanPlantConfig),
            typeof(BulbPlantConfig),
            typeof(CactusPlantConfig),
            typeof(ColdWheatConfig),
            typeof(EvilFlowerConfig),
            typeof(ForestTreeConfig),
            typeof(ForestTreeBranchConfig),
            typeof(GasGrassConfig),
            typeof(LeafyPlantConfig),
            typeof(MushroomPlantConfig),
            typeof(PrickleFlowerConfig),
            typeof(PrickleGrassConfig),
            typeof(SaltPlantConfig),
            typeof(SpiceVineConfig),
            typeof(SwampLilyConfig),
            typeof(ColdBreatherConfig),
            typeof(OxyfernConfig),
            typeof(SuperWormPlantConfig),
            typeof(WormPlantConfig),
            typeof(SwampHarvestPlantConfig),
            typeof(ToePlantConfig),
            typeof(WineCupsConfig),
            typeof(CritterTrapPlantConfig),
            typeof(CylindricaConfig),
            typeof(FilterPlantConfig)
        };
    }
    
    [HarmonyPatch(typeof(KMod.Mod), "Load")]
    public static class OnLoadPatch
	{
        public static bool IsPatched = false;

        public static void Prefix()
        {
            if (IsPatched) return;
            IsPatched = true;

            var harmony = HarmonyInstance.Create("com.fumihiko.oni.customizeplants");
            var postfix = typeof(OnLoadPatch).GetMethod("PlantPostfix");

            foreach (Type type in PLANTS.CLASSES)
            {
                var original = type.GetMethod("CreatePrefab");
                harmony.Patch(original, prefix: null, postfix: new HarmonyMethod(postfix));

                //FumLib.FumTools.PrintAllPatches(type, "CreatePrefab");
            }

            if (CustomizePlantsState.StateManager.State.ModPlants != null)
            {
                foreach (string config in CustomizePlantsState.StateManager.State.ModPlants)
                {
                    try
                    {
                        int cStart = config.IndexOf(' ') + 1;
                        int cLength = config.IndexOf(',', cStart) - cStart;
                        string nameDll = config.Substring(cStart, cLength) + ".dll";

                        if (nameDll == "Fervine-merged.dll") nameDll = "Fervine*.dll";

                        string[] dlls = Directory.GetFiles(Config.Helper.ModsDirectory, nameDll, SearchOption.AllDirectories);

                        if (dlls.Length == 0) throw new FileNotFoundException("ModPlants: could not find mod: " + nameDll);
                        
                        foreach (string dll in dlls)
                        {
                            Debug.Log("ModPlants: loading external dll: " + dll);
                            Assembly.LoadFile(dll);
                        }

                        Type type = Type.GetType(config, true);
                        MethodInfo original = type.GetMethod("CreatePrefab");
                        if (original == null) throw new NullReferenceException("ModPlants: CreatePrefab is NULL");
                        harmony.Patch(original, prefix: null, postfix: new HarmonyMethod(postfix));
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e.Message);
                        Debug.LogWarning(ToDialog("ModPlants: " + config + " is not a valid qualifier."));
                    }
                }
            }
        }
        
        public static void PlantPostfix(GameObject __result)
        {
            PlantHelper.ProcessPlant(__result);
        }

    }
    
}
