﻿using RimWorld;
using System.Collections.Generic;
using Verse;
namespace AvaliMod
{
    public class MultiPartBionic : HediffComp
    {
        private int onItem = 0;
        private int hediffsFound;
        private string output;
        private bool triggeredTextAdd = false;
        private bool triggeredTextRemove = false;
        private BodyPartDef bodyPart;

        public MultiPartBionicCompProperties Props
        {
            get
            {
                return (MultiPartBionicCompProperties)this.props;
            }
        }
        private string hediffsLeft
        {
            get
            {
                return this.Props.stringForHediffsLeft;
            }
        }

        private bool displayTextWhenChanged
        {
            get
            {
                return this.Props.displayTextWhenChanged;
            }
        }
        private List<BodyPartDef> bodyParts
        {
            get
            {
                return this.Props.bodyPartsToAffect;
            }
        }
        private List<HediffDef> otherHediffs
        {
            get
            {
                return this.Props.otherHediffs;
            }
        }
        private List<HediffDef> hediffsToAdd
        {
            get
            {
                return this.Props.hediffsToAdd;
            }
        }
        private string textOnAdd
        {
            get
            {
                return this.Props.textOnAdd;
            }
        }
        private string textOnRemove
        {
            get
            {
                return this.Props.textOnRemove;
            }
        }
        private int timeToFade
        {
            get
            {
                return this.Props.timeToFade;
            }
        }
        private List<BodyPartDef> bodyPartsMustBeOn
        {
            get
            {
                return this.Props.bodyPartsMustBeOn;
            }
        }
        public override string CompTipStringExtra
        {
            get
            {
                Pawn pawn = this.parent.pawn;

                output = hediffsLeft;
                foreach (HediffDef hediffDef in otherHediffs)
                {
                    if (!(pawn.health.hediffSet.HasHediff(hediffDef)))
                    {
                        if ((onItem + 1) >= otherHediffs.Count)
                        {
                            output = output + hediffDef.label + ". ";
                        }
                        else
                        {
                            output = output + hediffDef.label + ", ";
                        }
                    }
                }
                return output;
            }
        }
        public override void CompPostTick(ref float severityAdjustment)
        {
            Pawn pawn = this.parent.pawn;
            if (pawn.Spawned)
            {
                hediffsFound = 0;
                foreach (HediffDef hediff in otherHediffs)
                {
                    if (onItem <= bodyPartsMustBeOn.Count)
                    {
                        bodyPart = bodyPartsMustBeOn[onItem];
                    }
                    else
                    {
                        bodyPart = bodyPartsMustBeOn[bodyPartsMustBeOn.Count];
                    }
                    BodyPartRecord bodyPartRecord = pawn.RaceProps.body.GetPartsWithDef(bodyPart).RandomElement<BodyPartRecord>();
                    if (pawn.health.hediffSet.HasHediff(otherHediffs[onItem], bodyPartRecord, false))
                    {
                        hediffsFound += 1;
                    }
                }
                if (hediffsFound == otherHediffs.Count)
                {
                    foreach (HediffDef hediffDef in hediffsToAdd)
                    {
                        if (onItem <= bodyParts.Count)
                        {
                            bodyPart = bodyParts[onItem];
                        }
                        else
                        {
                            bodyPart = bodyParts[bodyParts.Count];
                        }
                        BodyPartRecord bodyPartRecord = pawn.RaceProps.body.GetPartsWithDef(bodyPart).RandomElement<BodyPartRecord>();
                        Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn, null);
                        if (!pawn.health.hediffSet.HasHediff(hediffDef, false))
                        {
                            pawn.health.AddHediff(hediff, bodyPartRecord);
                            Log.Message("Added hediff: " + hediffDef.defName.ToString() + " to pawn " + hediff.pawn.ToString());
                            if (displayTextWhenChanged & !(triggeredTextAdd))
                            {
                                triggeredTextAdd = true;
                                triggeredTextRemove = false;
                                MoteMaker.ThrowText(pawn.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Blueprint), pawn.Map, textOnAdd, timeToFade);
                            }
                        }
                        onItem += 1;

                    }
                }
                else
                {
                    hediffsFound = 0;
                    foreach (HediffDef hediffDef in otherHediffs)
                    {
                        if (pawn.health.hediffSet.HasHediff(hediffDef))
                        {
                            hediffsFound += 1;
                        }
                        onItem += 1;
                    }
                    if (!(hediffsFound == otherHediffs.Count))
                    {
                        foreach (HediffDef hediffDef in hediffsToAdd)
                        {
                            Hediff hediff = HediffMaker.MakeHediff(hediffDef, pawn, null);
                            if (pawn.health.hediffSet.HasHediff(hediffDef))
                            {
                                pawn.health.RemoveHediff(pawn.health.hediffSet.GetFirstHediffOfDef(hediffDef));
                                Log.Message("Removed hediff: " + hediffDef.defName.ToString() + " from pawn " + hediff.pawn.ToString());
                                if (displayTextWhenChanged & !(triggeredTextRemove))
                                {
                                    triggeredTextRemove = true;
                                    triggeredTextAdd = false;
                                    MoteMaker.ThrowText(pawn.Position.ToVector3ShiftedWithAltitude(AltitudeLayer.Blueprint), pawn.Map, textOnRemove, timeToFade);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}