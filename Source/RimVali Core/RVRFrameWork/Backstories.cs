﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimValiCore.RVR
{
    // TODO: Rename these
    public class BodyPartToAffect
    {
        public HediffDef hediffDef;
        public BodyPartDef bodyPartDef = BodyPartDefOf.Torso;
    }

    public class skillGains
    {
        public SkillDef skill;
        public int amount;
    }

    public class traitList
    {
        public TraitDef def;
        public int degree;
    }
    public static class BackstoryTagManager
    {
        private static Dictionary<Backstory, List<string>> tags = new Dictionary<Backstory, List<string>>();

        public static List<string> GetTags(this Backstory story)
        {
            if (tags.ContainsKey(story))
            {
                return tags[story];
            }
            return new List<string>();
        }

        public static void SetTags(this Backstory story, List<string> setTags)
        {
            tags[story] = setTags;
        }
    }

    public class RVRBackstory : Def
    {
        public string storyDesc;
        public string title;
        public string femaleTitle;
        public string shortTitle;
        public string shortFemaleTitle;

        public bool canSpawnMale = true;
        public bool canSpawnFemale = true;

        public bool shuffable = true;

        //These stack!
        //A global chance of 50 and a female chance of 50 would be 25 for female pawns.
        public int femaleChance = 100;

        public int maleChance = 100;
        public int globalChance = 100;

        public BackstorySlot backstorySlot = BackstorySlot.Adulthood;

        public List<string> spawnInCategories = new List<string>();
        public List<skillGains> skillGains = new List<skillGains>();
        public List<traitList> forcedTraits = new List<traitList>();
        public List<traitList> disabledTraits = new List<traitList>();
        public List<WorkTags> disabledWorkTypes = new List<WorkTags>();
        public List<WorkTags> enabledWorkTypes = new List<WorkTags>();
        public List<string> tags = new List<string>();

        public string linkedStoryIdentifier;

        public List<Colors> colorGenOverrides = new List<Colors>();
        public bool hasButcherThoughtOverrides = false;

        public butcherAndHarvestThoughts butcherAndHarvestThoughtOverrides = new butcherAndHarvestThoughts();
        public BodyDef bodyDefOverride;
        public BodyTypeDef bodyType;

        //Not accessible in XML, dont use them.
        public Backstory story;

        private readonly List<TraitEntry> traitsToForce = new List<TraitEntry>();
        private readonly List<TraitEntry> traitsToDisable = new List<TraitEntry>();

        public bool CanSpawn(Pawn pawn)
        {
            if (backstorySlot == BackstorySlot.Adulthood)
            {
                if (linkedStoryIdentifier != null && !(pawn.story.childhood.identifier == linkedStoryIdentifier))
                {
                    //Log.Message("Story can't spawn: linked story not avalible");
                    return false;
                }
            }
            else
            {
                if (linkedStoryIdentifier != null && (pawn.story.adulthood != null && (pawn.story.adulthood.identifier) != linkedStoryIdentifier))
                {
                    //Log.Message("Story can't spawn: linked story not avalible");
                    return false;
                }
            }
            if (Rand.Range(0, 100) < globalChance)
            {
                if (!canSpawnFemale && pawn.gender == Gender.Female)
                {
                    return false;
                }
                if (!canSpawnMale && pawn.gender == Gender.Male)
                {
                    return false;
                }
                if (pawn.gender == Gender.Female)
                {
                    if (Rand.Range(0, 100) < femaleChance)
                    {
                        //Log.Message("Story can't spawn: chance roll (female)");
                        return true;
                    }
                    return false;
                }
                else
                {
                    if (Rand.Range(0, 100) < maleChance)
                    {
                        return true;
                    }
                    //Log.Message("Story can't spawn: chance roll (male)");
                    return false;
                }
            }
            // Log.Message("Story can't spawn: chance roll (global)");
            return false;
        }

        public override void ResolveReferences()
        {
            Dictionary<SkillDef, int> skills = new Dictionary<SkillDef, int>();
            base.ResolveReferences();
            foreach (skillGains skillGains in skillGains)
            {
                if (!skills.ContainsKey(skillGains.skill))
                {
                    skills.Add(skillGains.skill, skillGains.amount);
                }
            }
            foreach (traitList traitItem in forcedTraits)
            {
                traitsToForce.Add(new TraitEntry(traitItem.def, traitItem.degree));
            }
            story = new Backstory
            {
                slot = backstorySlot,
                title = title,
                titleFemale = femaleTitle,
                titleShort = shortTitle,
                titleShortFemale = shortFemaleTitle,
                identifier = defName,
                baseDesc = storyDesc,
                spawnCategories = spawnInCategories,
                skillGainsResolved = skills,
                forcedTraits = traitsToForce,
                disallowedTraits = traitsToDisable,
                
                workDisables = ((Func<WorkTags>)delegate
                {
                    WorkTags work = WorkTags.None;
                    Enum.GetValues(typeof(WorkTags)).Cast<WorkTags>().Where(tag =>
                    ((!enabledWorkTypes.NullOrEmpty() && !enabledWorkTypes.Contains(tag))
                     || disabledWorkTypes.Contains(tag))
                     && (!(disabledWorkTypes.Contains(WorkTags.AllWork)) && !(tag == WorkTags.AllWork))
                     ).ToList().ForEach(tag => work |= tag);
                    return work;
                })(),
                shuffleable = shuffable
            };
            BackstoryTagManager.SetTags(story, tags);
            BackstoryDatabase.AddBackstory(story);
            //Log.Message("created story: " + this.defName);
        }
    }
}