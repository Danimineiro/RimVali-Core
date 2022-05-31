using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace RimValiCore.QLine
{
    public class QuestStage
    {
        public string label;
        public string description;
        public List<QuestStageButtonDecision> buttons;

        public QuestStageButtonDecision this[int index] 
        { 
            get => buttons[index]; 
            set => buttons[index] = value; 
        }

        public string LabelCap => label.CapitalizeFirst();

        public override string ToString()
        {
            return $"[QuestStage] label: {label}, description: {description}, buttons:\n    {string.Join("\n    ", buttons)}";
        }
    }

    public class QuestStageButtonDecision
    {
        private readonly string buttonText;
        private readonly Action buttonAction;
        private readonly Requirements requirements;

        public QuestStageButtonDecision(string buttonText, Action buttonAction)
        {
            this.buttonText = buttonText;
            this.buttonAction = buttonAction;
        }

        public QuestStageButtonDecision(string buttonText, Action buttonAction, Requirements requirements) : this(buttonText, buttonAction)
        {
            this.requirements = requirements;
        }

        public string ButtonText => buttonText;

        public bool IsAvailable => Requirements?.AreFulFilled ?? true;

        public string DisableReason => Requirements.AllReasons().Join(reason => $"{reason.Reason}: {reason.ShouldDisable}", "\n");

        public Action ButtonAction => buttonAction;

        public Requirements Requirements => requirements;

        public override string ToString() => $"[QuestStageButtonDecision] buttonText: {buttonText}, hasAction: {buttonAction != null}";
    }

    public class Requirements
    {
        private readonly RequirementMode mode;
        private readonly List<Requirements> innerRequirements;
        private readonly List<DisableReason> disableReasons;
        private readonly string labelOverride;
        private readonly bool valid;
        private readonly int countForXMustBeTrue;
        private float? longestStringLength;
        private int? numberOfRequirements;
        private int? count;

        public int CountForXMustBeTrue => countForXMustBeTrue;

        public List<Requirements> InnerRequirements => innerRequirements ?? new List<Requirements>();

        public List<DisableReason> DisableReasons => disableReasons ?? new List<DisableReason>();

        public bool Valid => valid;

        public int ToolTipSpacesNeeded => (int)(count ?? (count = AllReasons().Count() + numberOfRequirements));

        public float LongestStringLength
        {
            get => (float)(longestStringLength ?? (longestStringLength = DetermineLongestStringLength()));
            private set => longestStringLength = value;
        }

        public string RequirementModeLabel => labelOverride ?? $"<color=green>{$"##RVC_{mode}"}:</color>";

        public bool AreFulFilled
        {
            get
            {
                if (!valid)
                {
                    Log.Error("Can't validate invalid Requirements object!");
                    return false;
                }

                bool fulfilled = true;

                switch (mode)
                {
                    case RequirementMode.AllTrue:
                        fulfilled = (innerRequirements?.All(requirement => requirement.AreFulFilled) ?? true) && (disableReasons?.All(reason => !reason.ShouldDisable) ?? true);
                        break;

                    case RequirementMode.AllFalse:
                        fulfilled = (innerRequirements?.All(requirement => !requirement.AreFulFilled) ?? true) && (disableReasons?.All(reason => reason.ShouldDisable) ?? true);
                        break;

                    case RequirementMode.AtLeastXMustBeTrue:
                        fulfilled = CountForXMustBeTrue <= (innerRequirements?.Sum(requirement => requirement.AreFulFilled ? 1 : 0) ?? 0) + (disableReasons?.Sum(reason => reason.ShouldDisable ? 0 : 1) ?? 0);
                        break;
                }

                return fulfilled;
            }
        }

        public Requirements(RequirementMode mode, List<Requirements> innerRequirements = null, List<DisableReason> disableReasons = null, string labelOverride = null, int countForXMustBeTrue = -1)
        {
            this.countForXMustBeTrue = countForXMustBeTrue;
            this.innerRequirements = innerRequirements;
            this.disableReasons = disableReasons;
            this.labelOverride = labelOverride;
            this.mode = mode;
            
            valid = ErrorCheck();
        }

        public float DetermineLongestStringLength()
        {
            float length = 0;
            float lengthMod = Windows.GUIUtils.RectExtensions.LayerOffset;

            if (!disableReasons.NullOrEmpty())
            {
                length = disableReasons.Max(reason => Text.CalcSize(reason.Reason).x + lengthMod);
                length = Math.Max(length, Text.CalcSize(RequirementModeLabel).x);
            }

            if (!innerRequirements.NullOrEmpty())
            {
                foreach (Requirements requirement in innerRequirements)
                {
                    length = Math.Max(length, requirement.DetermineLongestStringLength() + lengthMod);
                }
            }

            return length;
        }

        public IEnumerable<DisableReason> AllReasons()
        {
            if (!disableReasons.NullOrEmpty())
            {
                foreach (DisableReason reason in disableReasons)
                {
                    yield return reason;
                }
            }

            int tempCounter = 1;
            if (!innerRequirements.NullOrEmpty())
            {
                foreach (Requirements requirement in innerRequirements)
                {
                    tempCounter++;
                    foreach (DisableReason reason in requirement.AllReasons())
                    {
                        yield return reason;
                    }
                }
            }

            numberOfRequirements = tempCounter;
            yield break;
        }

        private bool ErrorCheck()
        {
            bool valid = true;

            if (innerRequirements.NullOrEmpty() && disableReasons.NullOrEmpty())
            {
                Log.Error("innerRequirements can't be NullOrEmpty if disableReason is NullOrEmpty!");
                valid = false;
            }

            if (mode == RequirementMode.AtLeastXMustBeTrue && countForXMustBeTrue <= 0)
            {
                Log.Error($"mode is {mode}, but countForXMustBeTrue is 0 or less ({countForXMustBeTrue})!");
                valid = false;
            }

            return valid;
        }
    }

    public enum RequirementMode
    {
        AllTrue = 0,
        AllFalse = 1,
        AtLeastXMustBeTrue = 2
    }

    public class DisableReason
    {
        private readonly Func<bool> shouldDisable = () => false;
        private readonly Func<string> reason = () => "No Reason Given";

        public DisableReason(Func<bool> shouldDisable, Func<string> reason)
        {
            this.reason = reason;
            this.shouldDisable = shouldDisable;
        }

        public bool ShouldDisable => shouldDisable();

        public string Reason => reason();
    }
}
