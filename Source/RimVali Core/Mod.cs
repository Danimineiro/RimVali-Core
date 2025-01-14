﻿using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimValiCore
{
    public class RVCModSettings : ModSettings
    {
        public bool expMode;
        public bool smartPawnScaling;
        public bool QL_DecisionWindow_ShowDebug;
        public int textureSizeScaling;
        public int smallestTexSize;
        public List<Color> savedColors;

        public RVCModSettings()
        {
            expMode = false;
            smartPawnScaling = true;
            QL_DecisionWindow_ShowDebug = false;
            textureSizeScaling = 10;
            smallestTexSize = 200;
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref expMode, "mode", false, false);
            Scribe_Values.Look(ref smartPawnScaling, "SmartScale", true, false);
            Scribe_Values.Look(ref QL_DecisionWindow_ShowDebug, "QL_DecisionWindow_ShowDebug", false, false);
            Scribe_Values.Look(ref textureSizeScaling, "texSS", 10, false);
            Scribe_Values.Look(ref smallestTexSize, "STS", 200, false);
            Scribe_Collections.Look(ref savedColors, "savedColors");
            base.ExposeData();
        }
    }

    public class RimValiCoreMod : Mod
    {
        public override string SettingsCategory()
        {
            return "RimValiCore";
        }

        private static RVCModSettings settings;
        public static RVCModSettings Settings => settings;
        public ModContentPack mod;

        public RimValiCoreMod(ModContentPack content) : base(content)
        {
            try
            {
                // Log.Message("Starting loading patch");
                // new Patcher(new Harmony("RimValiCore.Loading"));
            }
            catch (Exception e)
            {
                Log.Error(e.Message);
            }
            RimValiUtility.dir = content.RootDir.ToString();
            mod = content;
            settings = GetSettings<RVCModSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(inRect);
            ls.CheckboxLabeled("Experimental mode: ", ref settings.expMode, "Only enable if you are willing to risk damage to saves and other such things!");
            ls.CheckboxLabeled("Smart pawn scaling: ", ref settings.smartPawnScaling);
            ls.Label($"Smart texture scaling: {settings.textureSizeScaling}");
            settings.textureSizeScaling = (int)ls.Slider(settings.textureSizeScaling, 1, 100);
            ls.Label($"Smallest texture size: {settings.smallestTexSize}");
            settings.smallestTexSize = (int)ls.Slider(settings.smallestTexSize, 100, 8000);
            ls.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}
