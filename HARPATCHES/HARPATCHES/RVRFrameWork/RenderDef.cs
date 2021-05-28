﻿using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using Unity;
using UnityEngine;
using System;

namespace RimValiCore.RVR
{
    #region texture types
    public class BaseTex
    {
        public string tex;
        public string femaleTex;
    }

    public class HediffTex : BaseTex
    {
        public HediffDef hediff;
    }
    public class BackstoryTex : BaseTex
    {
        public string backstoryTitle;
    }

    public class HediffStoryTex : BaseTex
    {
        public string backstoryTitle;
        public HediffDef hediffDef;
    }
    #endregion
    public class BodyPartGraphicPos
    {
        public Vector2 position = new Vector2(0f, 0f);
        public float layer = 1f;
        public Vector2 size = new Vector2(1f, 1f);

    }
    public class RenderableDef : Def
    {
       
        public Graphic graphic;

        #region backstory checks
        public bool StoryIsName(Backstory story, string title)
        {
            //I have to check if everything is null so we get this mess, otherwise sometimes a null reference exception occurs.
            //There probably is a cleaner way of doing this I'm not aware of.
            return ((story.untranslatedTitle != null && story.untranslatedTitle == title)
                        || ((story.untranslatedTitle != null && story.untranslatedTitle == title)
                        || (story.untranslatedTitleShort != null && story.untranslatedTitleShort == title)
                        ||(story.untranslatedTitleFemale != null && story.untranslatedTitleFemale == title)
                        //This does not need to be checked, as it literally cannot ever be null.
                        || story.identifier == title
                        || (story.titleShort != null && story.titleShort == title)
                        || (story.titleFemale != null && story.titleFemale == title)
                        || (story.titleShortFemale != null && story.titleShortFemale == title
                        //Same here.
                        || story.title == title)));
            //Now we hope Tynan never changes backstories. Ever. Or else this thing breaks.
        }
        #endregion
        #region get index
        public int GetMyIndex(Pawn pawn)
        {
            if(pawn.def is RimValiRaceDef)
            {
                colorComp comp = pawn.TryGetComp<colorComp>();
                foreach(string str in comp.renderableDefIndexes.Keys)
                {
                    if (str == defName || (linkIndexWithDef != null && linkIndexWithDef.defName == str))
                    {
                        return comp.renderableDefIndexes[str];
                    }
                }
            }
            return 0;
        }
        #endregion

        #region get texture
        public string texPath(Pawn pawn)
        {
            return texPath(pawn, GetMyIndex(pawn));
        }
        public string texPath(Pawn pawn, int index)
        {
            
            string path = textures[index].tex;

            if (textures[index].femaleTex != null && pawn.gender == Gender.Female)
            {
                path = textures[index].femaleTex;
            }

            //HediffStory gets highests priority here, by being lowest on this set
            Backstory adulthood = null;
            if (pawn.story.adulthood != null)
            {
                adulthood = pawn.story.adulthood;
            }
            Backstory childhood = pawn.story.childhood;
            foreach (BackstoryTex backstoryTex in backstoryTextures)
            {
                //Log.Message(backstoryTex.backstoryTitle);
                if ((adulthood != null && StoryIsName(adulthood, backstoryTex.backstoryTitle)) || StoryIsName(childhood, backstoryTex.backstoryTitle))
                {

                    if (backstoryTex.femaleTex != null && pawn.gender == Gender.Female)
                    {
                        path = backstoryTex.femaleTex;
                    }
                    path = backstoryTex.tex;
                }
            }
            foreach (HediffTex hediffTex in hediffTextures)
            {
                foreach (BodyPartRecord bodyPartRecord in pawn.def.race.body.AllParts)
                {
                    BodyPartDef def = bodyPartRecord.def;
                    if (def.defName.ToLower() == bodyPart.ToLower() || def.label.ToLower() == bodyPart.ToLower())
                    {
                        if (pawn.health.hediffSet.HasHediff(hediffTex.hediff, bodyPartRecord, false))
                        {

                            if (hediffTex.femaleTex != null && pawn.gender == Gender.Female)
                            {
                                path = hediffTex.femaleTex;
                            }
                            path = hediffTex.tex;
                        }
                    }
                }

            }

            foreach (HediffStoryTex hediffStoryTex in hediffStoryTextures)
            {
                if ((adulthood != null && StoryIsName(adulthood, hediffStoryTex.backstoryTitle)) || StoryIsName(childhood, hediffStoryTex.backstoryTitle))
                {
                    foreach (BodyPartRecord bodyPartRecord in pawn.def.race.body.AllParts)
                    {
                        BodyPartDef def = bodyPartRecord.def;
                        if (def.defName.ToLower() == bodyPart.ToLower() || def.label.ToLower() == bodyPart.ToLower())
                        {
                            if (pawn.health.hediffSet.HasHediff(hediffStoryTex.hediffDef, bodyPartRecord, false))
                            {

                                if (def.defName.ToLower() == bodyPart.ToLower() || def.label.ToLower() == bodyPart.ToLower())
                                {
                                    if (pawn.health.hediffSet.HasHediff(hediffStoryTex.hediffDef, bodyPartRecord, false))
                                    {
                                        if (hediffStoryTex.femaleTex != null && pawn.gender == Gender.Female)
                                        {
                                            path = hediffStoryTex.femaleTex;
                                        }
                                        else
                                        {
                                            path = hediffStoryTex.tex;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return path;

        }
        #endregion
        public List<BaseTex> textures;
        public string rottingTex;
        public string dessicatedTex;

        public string bodyPart = null;

        public RenderableDef linkIndexWithDef;

        public bool showsInBed = true;
        public bool showsIfDessicated = true;
        public bool showsIfRotted = true;
        public string useColorSet;
        public BodyPartGraphicPos east = new BodyPartGraphicPos();
        public BodyPartGraphicPos north = new BodyPartGraphicPos();
        public BodyPartGraphicPos south = new BodyPartGraphicPos();
        public BodyPartGraphicPos west;

        public List<BackstoryTex> backstoryTextures = new List<BackstoryTex>();
        public List<HediffTex> hediffTextures = new List<HediffTex>();
        public List<HediffStoryTex> hediffStoryTextures = new List<HediffStoryTex>();
        #region portrait check
        public bool CanShowPortrait(Pawn pawn)
        {
            if (bodyPart == null)
            {
                return true;
            }
            IEnumerable<BodyPartRecord> bodyParts = pawn.health.hediffSet.GetNotMissingParts();
            //Log.Message(bodyParts.Any(x => x.def.defName.ToLower() == "left lower ear" || x.untranslatedCustomLabel.ToLower() == "left lower ear".ToLower()).ToString());
            try
            {
                if (bodyParts.Any(x => x.def.defName.ToLower() == bodyPart.ToLower() || x.Label.ToLower() == bodyPart.ToLower()))
                {
                    if (!pawn.Spawned)
                    {
                        return true;
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                //Log.Message(e.ToString(), true);
                return true;
            }
        }
        #endregion

        #region general show check

        public bool CanShow(Pawn pawn, RotDrawMode mode)
        {
            if ((mode == RotDrawMode.Rotting && !showsIfRotted) || (mode == RotDrawMode.Dessicated && !showsIfDessicated))
            {
                return false;
            }
            return CanShow(pawn);
        }
        public bool CanShow(Pawn pawn)
        {
            IEnumerable<BodyPartRecord> bodyParts = pawn.health.hediffSet.GetNotMissingParts();
            //Log.Message(bodyParts.Any(x => x.def.defName.ToLower() == "left lower ear" || x.untranslatedCustomLabel.ToLower() == "left lower ear".ToLower()).ToString());
            try
            {
                if (bodyPart==null || bodyParts.Any(x => x.def.defName.ToLower() == bodyPart.ToLower() || x.Label.ToLower() == bodyPart.ToLower()) )
                {
                    if (!this.showsInBed && pawn.InBed() && !pawn.CurrentBed().def.building.bed_showSleeperBody)
                    {
                        return false;
                    }
                    return true;
                  
                }
                return false;
                
            }catch {
                //Log.Message(e.ToString(), true);
                return true;
            }
        }
        #endregion
    }
}