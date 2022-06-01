using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Verse;

namespace RimValiCore.QLine
{
    /// <summary>
    ///     This class defines a single Quest stage
    /// </summary>
    public class QuestStage
    {
        public string label;
        public string description;
        public List<QuestStageButtonDecision> buttons;

        /// <summary>
        ///     shortcut for <see cref="QuestStage.buttons"/>
        /// </summary>
        /// <param name="index">the index</param>
        /// <returns>the <see cref="QuestStageButtonDecision"/> at the <paramref name="index"/>s position</returns>
        public QuestStageButtonDecision this[int index] 
        { 
            get => buttons[index]; 
            set => buttons[index] = value; 
        }

        /// <summary>
        ///     Returns the count of buttons that can be displayed
        /// </summary>
        public int DisplayableButtons => buttons.Sum(button => button.ShouldDisplay ? 1 : 0);

        /// <summary>
        ///     The capitalized <see cref="label"/> of this object
        /// </summary>
        public string LabelCap => label.CapitalizeFirst();

        /// <returns>The debug string of this object</returns>
        public override string ToString()
        {
            return $"[{label}], description: {description}, buttons:\n    {string.Join("\n    ", buttons)}";
        }
    }

    /// <summary>
    ///     Defines one button for a <see cref="QuestStage"/>
    /// </summary>
    public class QuestStageButtonDecision
    {
        private readonly string buttonText;
        private readonly Action buttonAction;
        private readonly Func<bool> shouldDisplay = () => true;
        private readonly Requirements requirements;

        /// <summary>
        ///     Constructs a <see cref="QuestStageButtonDecision"/> without the use of <see cref="Requirements"/>
        /// </summary>
        /// <param name="buttonText">the text displayed on the button</param>
        /// <param name="buttonAction">the action executed </param>
        /// <param name="shouldDisplay"
        public QuestStageButtonDecision(string buttonText, Action buttonAction, Func<bool> shouldDisplay = null)
        {
            this.buttonText = buttonText;
            this.buttonAction = buttonAction;
            this.shouldDisplay = shouldDisplay ?? this.shouldDisplay;
        }

        /// <summary>
        ///     Constructs a <see cref="QuestStageButtonDecision"/> with the use of <see cref="RimValiCore.QLine.Requirements"/>
        /// </summary>
        /// <param name="buttonText">the text displayed on the button</param>
        /// <param name="buttonAction">the action executed </param>
        /// <param name="requirements">the <see cref="RimValiCore.QLine.Requirements"/>that need to be fulfilled</param>
        public QuestStageButtonDecision(string buttonText, Action buttonAction, Requirements requirements, Func<bool> shouldDisplay = null) : this(buttonText, buttonAction, shouldDisplay)
        {
            this.requirements = requirements;
        }

        /// <summary>
        ///     The text this button displays
        /// </summary>
        public string ButtonText => buttonText;

        /// <summary>
        ///     If the button can be pressed and activated
        /// </summary>
        public bool IsAvailable => Requirements?.AreFulFilled ?? true;

        public bool ShouldDisplay => shouldDisplay();

        /// <summary>
        ///     Lists all reasons why this button might be disabled
        ///     mostly for debug use
        /// </summary>
        public string DisableReason => Requirements.AllReasons().Join(reason => $"{reason.Reason}: {reason.ShouldDisable}", "\n");

        /// <summary>
        ///     The action this button does
        /// </summary>
        public Action ButtonAction => buttonAction;

        /// <summary>
        ///     The <see cref="RimValiCore.QLine.Requirements"/> the button must fulfill in oder to be activatable
        /// </summary>
        public Requirements Requirements => requirements;

        /// <returns>This button as a debug string</returns>
        public override string ToString() => $"[{buttonText}], hasAction: {buttonAction != null}, shouldDisplay: {ShouldDisplay}";
    }

    /// <summary>
    ///     A class that defines requirements for an action to take place.
    ///     Desinged to be used in user interfaces, as it also is intended to explain the requirements.
    /// </summary>
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

        /// <summary>
        ///     Lists the next layer of <see cref="Requirements"/> inside this object
        /// </summary>
        public List<Requirements> InnerRequirements => innerRequirements ?? new List<Requirements>();

        /// <summary>
        ///     Lists all <see cref="DisableReason"/> this object contains
        /// </summary>
        public List<DisableReason> DisableReasons => disableReasons ?? new List<DisableReason>();

        /// <summary>
        ///     The amount of lines this <see cref="Requirements"/> needs in order to be displayed using <see cref="Windows.GUIUtils.RectExtensions.DrawRequirements"/>
        /// </summary>
        public int ToolTipLinesNeeded => (int)(count ?? (count = AllReasons().Count() + numberOfRequirements));

        /// <summary>
        ///     The label that appears above every condition that must be fulfilled, explaining how they interact
        /// </summary>
        public string RequirementModeLabel => labelOverride ?? $"QLine_{mode}".Translate(CountForXMustBeTrue);

        /// <summary>
        ///     Used when this objects <see cref="mode"/> is set to <see cref="RequirementMode.AtLeastXMustBeTrue"/>. Defines how many conditions must be true for the <see cref="Requirements"/> to be true
        /// </summary>
        public int CountForXMustBeTrue => countForXMustBeTrue;

        /// <summary>
        ///     Is true when the object is set up properly
        /// </summary>
        public bool Valid => valid;

        /// <summary>
        ///     The length of the longest string, as determined by <see cref="Text.CalcSize"/>
        /// </summary>
        public float LongestStringLength
        {
            get => (float)(longestStringLength ?? (longestStringLength = DetermineLongestStringLength()));
            private set => longestStringLength = value;
        }

        /// <summary>
        ///     Determines if all the <see cref="Requirements"/> are fulfilled, results differ depending on <see cref="mode"/>
        /// </summary>
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

        /// <summary>
        ///     Sets the values for a new <see cref="Requirements"/> object. It is required to set either a inner list of <see cref="Requirements"/> using <paramref name="innerRequirements"/>
        ///     or setting a List of <see cref="DisableReason"/>s using <paramref name="disableReasons"/>. If <paramref name="mode"/> is set to <see cref="RequirementMode.AtLeastXMustBeTrue"/>,
        ///     then <paramref name="countForXMustBeTrue"/> must be 1 or more
        /// </summary>
        /// <param name="mode">the <see cref="RequirementMode"/>this validates in</param>
        /// <param name="innerRequirements">an inner layer of <see cref="Requirements"/></param>
        /// <param name="disableReasons">single conditions</param>
        /// <param name="labelOverride">if set, replaces the output from <see cref="RequirementModeLabel"/></param>
        /// <param name="countForXMustBeTrue">used with <see cref="RequirementMode.AtLeastXMustBeTrue"/></param>
        public Requirements(RequirementMode mode, List<Requirements> innerRequirements = null, List<DisableReason> disableReasons = null, string labelOverride = null, int countForXMustBeTrue = -1)
        {
            this.countForXMustBeTrue = countForXMustBeTrue;
            this.innerRequirements = innerRequirements;
            this.disableReasons = disableReasons;
            this.labelOverride = labelOverride;
            this.mode = mode;
            
            valid = ErrorCheck();
        }

        /// <summary>
        ///     Determines the longest string in this <see cref="Requirements"/>, including it's inner layers, using <see cref="Text.CalcSize"/>
        /// </summary>
        /// <returns>a float with the length</returns>
        private float DetermineLongestStringLength()
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

        /// <summary>
        ///     Lists all <see cref="DisableReason"/>s in this <see cref="Requirements"/> object and it's inner layers
        /// </summary>
        /// <returns>an <see cref="IEnumerable{T}"/> where T is <see cref="DisableReason"/></returns>
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

        /// <summary>
        ///     Checks this object for errors
        /// </summary>
        /// <returns>if this object is legal or not</returns>
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

    /// <summary>
    ///     This class defines a reason and a condition. If the condition is true, it should disable something
    /// </summary>
    public class DisableReason
    {
        private readonly Func<bool> shouldDisable = () => false;
        private readonly Func<string> reason = () => "No Reason Given";

        public DisableReason(Func<bool> shouldDisable, Func<string> reason)
        {
            this.reason = reason;
            this.shouldDisable = shouldDisable;
        }

        /// <summary>
        ///     Determines if this should disable something
        /// </summary>
        public bool ShouldDisable => shouldDisable();

        /// <summary>
        ///     The reason why something should be disabled
        /// </summary>
        public string Reason => reason();
    }
}
