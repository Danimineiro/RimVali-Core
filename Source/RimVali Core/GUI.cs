using System;
using System.Collections.Generic;
using System.Linq;
using RimValiCore.Windows;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;
using RimValiCore.RVR;
using Verse.Sound;

namespace RimValiCore
{
    
    [HarmonyPatch(typeof(Page_ConfigureStartingPawns), "DoWindowContents")]
    public static class ConfigurePatch
    {
        [HarmonyPostfix]
        public static void Patch()
        {
            bool button = Widgets.ButtonText(new Rect(new Vector2(870, 0), new Vector2(100, 30)), "RVC_EditPawn".Translate());
            if (button)
            {
                Find.WindowStack.Add(new EditorWindow());
            }
        }
    }

    public class EditorWindow : Window
    {
        private const float RectInnerFieldHeight = 40f;

        private readonly List<Pawn> pawns = Find.GameInitData.startingAndOptionalPawns;

        private readonly Rect RectWindowMain = new Rect(0f, 0f, 1000f, 400f);
        private readonly Rect RectWindowSub;
        private readonly Rect RectWindowEdit;
        private readonly Rect RectPawnSelectOuter;
        private readonly Rect RectColorSelectOuter;

        private readonly Rect[] RectEditSections;
        private readonly Rect[] RectNamingRects;
        private readonly Rect RectColoringPart;
        private readonly Rect RectPawnBig;
        private readonly Rect RectInfoBox;

        private Dictionary<string, ColorSet> colorSets = new Dictionary<string, ColorSet>();
        private Rect[] RectColorFields;
        private Rect[] RectMaskFields;
        private Rect RectMaskSelectInner;
        private Rect RectColorSelectInner;
        private Rect RectPawnSelectInner;
        private Vector2 PawnSelectorScroll;
        private Vector2 ColorSelectorScroll;
        private Vector2 MaskSelectorScroll;
        private InfoBoxStatus infoBoxStatus = InfoBoxStatus.Expanded;
        private float infoBoxExpansion = 1f;
        private bool calculatedInnerMask = false;

        private Pawn selectedPawn;
        private List<RenderableDef> renderableDefs;

        public override Vector2 InitialSize => RectWindowMain.size;

        protected override float Margin => 0f;

        public EditorWindow()
        {
            doCloseX = true;
            closeOnClickedOutside = true;

            SelectedPawn = Find.GameInitData.startingAndOptionalPawns[0];

            RectWindowSub = RectWindowMain.ContractedBy(25f);
            RectPawnSelectOuter = RectWindowSub.LeftPartPixels(172f);
            RectPawnSelectInner = RectPawnSelectOuter.LeftPartPixels(172f - 17f);

            RectPawnSelectInner.height = 55f * pawns.Count;
            if (RectPawnSelectInner.height < RectPawnSelectOuter.height)
            {
                RectPawnSelectInner.width += 17f;
            }

            RectWindowEdit = RectWindowSub.RightPartPixels(RectWindowSub.width - RectPawnSelectOuter.width - 5f);

            RectEditSections = RectWindowEdit.DivideVertical(2).ToArray();
            RectColoringPart = RectEditSections[0];
            RectNamingRects = RectEditSections[1].TopPartPixels(39f).ContractVertically(5).DivideHorizontal(3).ToArray();
            RectColorSelectOuter = RectColoringPart.RightPartPixels(300f).ContractVertically(5);
            RectColorSelectOuter.width -= 5;

            CalcInnerColorRect();

            RectPawnBig = RectColoringPart.LeftPartPixels(RectEditSections[0].height);
            RectInfoBox = RectPawnBig.MoveRect(new Vector2(RectPawnBig.width, 0f)).ContractedBy(5f);
            RectInfoBox.width = RectColoringPart.width - RectPawnBig.width - RectColorSelectOuter.width - 15f;

            //Saftey check!
            if(RimValiCoreMod.Settings.savedColors==null)
                RimValiCoreMod.Settings.savedColors = new List<Color>() {Color.black, Color.black, Color.black, Color.black, Color.black, Color.black, Color.black, Color.black, Color.black, Color.black};
        }

        public override void DoWindowContents(Rect _)
        {
            DrawPawnSelectionArea();
            DrawPawn();
            DrawInfoBoxExpansionButton();
            DoInfoBoxExpansion();

            DrawColorSelection();
            DrawNameEdit();
            DrawMaskBox();
            DrawInfoBox();
        }

        /// <summary>
        /// Recalculates the height and width of the inner scroll view for the color picking part
        /// </summary>
        private void CalcInnerColorRect()
        {
            List<Rect> rectList = new List<Rect>();

            RectColorSelectInner = new Rect(RectColorSelectOuter)
            {
                height = colorSets.Count * RectInnerFieldHeight
            };

            if (HasOpenColorField) RectColorSelectInner.height += RectInnerFieldHeight * 3f;

            if (RectColorSelectInner.height > RectColorSelectOuter.height) RectColorSelectInner.width -= 17f;

            for (int i = 0; i < colorSets.Count; i++)
            {
                Vector2 mod = new Vector2(0f, RectInnerFieldHeight * i + ((HasOpenColorField && (i > OpenColorField)) ? RectInnerFieldHeight * 3f : 0f));
                Rect tempRect = RectColorSelectInner.TopPartPixels(RectInnerFieldHeight).MoveRect(mod);
                tempRect.height -= 5f;

                rectList.Add(tempRect);
            }

            RectColorFields = rectList.ToArray();
        }

        private void CalcInnerMaskRect(Rect inRect)
        {
            List<Rect> rectList = new List<Rect>();

            RectMaskSelectInner = new Rect(RectInfoBox)
            {
                width = (RectInfoBox.width - 17f) * (1f - infoBoxExpansion),
                height = ((RimValiRaceDef)SelectedPawn.def).renderableDefs.Count * RectInnerFieldHeight - 5f
            };

            for (int i = 0; i < ((RimValiRaceDef)SelectedPawn.def).renderableDefs.Count; i++)
            {
                Vector2 mod = new Vector2(0f, RectInnerFieldHeight * i /**+ ((HasOpenColorField && (i > OpenColorField)) ? RectInnerFieldHeight * 3f : 0f)**/);
                Rect tempRect = inRect.TopPartPixels(RectInnerFieldHeight).MoveRect(mod);
                tempRect.height -= 5f;

                rectList.Add(tempRect);
            }

            RectMaskFields = rectList.ToArray();
        }

        private bool HasOpenColorField => OpenColorField > -1;

        private int OpenColorField { get; set; } = -1;

        private int OpenMaskField { get; set; } = -1;

        private Pawn SelectedPawn
        {
            get => selectedPawn;
            set
            {
                selectedPawn = value;

                if (SelectedPawn.def is RimValiRaceDef def && SelectedPawn.GetComp<ColorComp>() is ColorComp comp)
                {
                    colorSets = comp.colors;
                    renderableDefs = def.renderableDefs;
                }
                else
                {
                    colorSets = new Dictionary<string, ColorSet>();
                }

                OpenColorField = -1;
                CalcInnerColorRect();
            }
        }
        protected Color BGColor
        {
            get
            {
                float num = Pulser.PulseBrightness(0.5f, Pulser.PulseBrightness(0.5f, 0.6f));
                return new Color(num, num, num, 0.2f) * Color.yellow;
            }
        }

        /// <summary>
        /// Does the info box expansion
        /// </summary>
        private void DoInfoBoxExpansion()
        {
            switch (infoBoxStatus)
            {
                case InfoBoxStatus.Expanded:
                    infoBoxExpansion = 1f;
                    break;
                case InfoBoxStatus.Expanding:
                    infoBoxExpansion += 0.01f;
                    break;
                case InfoBoxStatus.Collapsing:
                    infoBoxExpansion -= 0.01f;
                    break;
                case InfoBoxStatus.Collapsed:
                    infoBoxExpansion = 0f;
                    break;
            }

            if (infoBoxExpansion <= 0f)
            {
                infoBoxExpansion = 0f;
                infoBoxStatus = InfoBoxStatus.Collapsed;
                return;
            }

            if (infoBoxExpansion > 1f)
            {
                infoBoxExpansion = 1f;
                infoBoxStatus = InfoBoxStatus.Expanded;
            }
        }

        /// <summary>
        /// Draws the button that controls the info box expansion
        /// </summary>
        private void DrawInfoBoxExpansionButton()
        {
            if (!(SelectedPawn.def is RimValiRaceDef def && !def.renderableDefs.NullOrEmpty())) return;

            Widgets.DrawHighlightIfMouseover(RectPawnBig);

            TooltipHandler.TipRegion(RectPawnBig, "RVC_TipExpandMask".Translate());
            Widgets.DrawTextureFitted(RectPawnBig.TopPartPixels(RectInnerFieldHeight - 11f).RightPartPixels(RectInnerFieldHeight - 11f), (infoBoxStatus == InfoBoxStatus.Expanded || infoBoxStatus == InfoBoxStatus.Expanding) ? TexButton.Collapse : TexButton.Reveal, 1f);

            if (Widgets.ButtonInvisible(RectPawnBig))
            {
                switch (infoBoxStatus)
                {
                    case InfoBoxStatus.Expanded:
                    case InfoBoxStatus.Expanding:
                        infoBoxStatus = InfoBoxStatus.Collapsing;
                        SoundDefOf.TabOpen.PlayOneShotOnCamera();
                        break;
                    case InfoBoxStatus.Collapsing:
                    case InfoBoxStatus.Collapsed:
                        infoBoxStatus = InfoBoxStatus.Expanding;
                        SoundDefOf.TabClose.PlayOneShotOnCamera();
                        break;
                }
            }
        }

        private void DrawMaskBox()
        {
            if (infoBoxStatus == InfoBoxStatus.Expanded) return;

            Rect RectDynMaskBox = new Rect(RectInfoBox)
            {
                width = (RectInfoBox.width - 17f) * (1f - infoBoxExpansion)
            };

            if (infoBoxStatus != InfoBoxStatus.Collapsed)
            {
                CalcInnerMaskRect(RectDynMaskBox);
                calculatedInnerMask = false;
            }

            if (infoBoxStatus == InfoBoxStatus.Collapsed && !calculatedInnerMask)
            {
                CalcInnerMaskRect(RectDynMaskBox);
                calculatedInnerMask = true;
            }

            Widgets.BeginScrollView(RectInfoBox, ref MaskSelectorScroll, RectMaskSelectInner);

            Text.Anchor = TextAnchor.MiddleLeft;

            for (int i = 0; i < RectMaskFields.Length; i++)
            {
                Rect rectTemp = RectMaskFields[i];
                Rect rectExpandCollapseIcon = rectTemp.LeftPartPixels(rectTemp.height);

                Widgets.DrawBox(rectTemp, 2);
                Widgets.DrawHighlight(rectTemp);
                Widgets.DrawHighlightIfMouseover(rectTemp);
                Widgets.Label(rectTemp.RightPartPixels(rectTemp.width - rectTemp.height - 5f), renderableDefs[i].defName);
                Widgets.DrawTextureFitted(rectExpandCollapseIcon.ContractedBy(11f), i == OpenMaskField ? TexButton.Collapse : TexButton.Reveal, 1f);

                if (Widgets.ButtonInvisible(rectTemp))
                {
                    if (i == OpenMaskField)
                    {
                        OpenMaskField = -1;
                        SoundDefOf.TabClose.PlayOneShotOnCamera();
                    }
                    else
                    {
                        if (OpenMaskField != -1) SoundDefOf.TabClose.PlayOneShotOnCamera();

                        OpenMaskField = i;
                        SoundDefOf.TabOpen.PlayOneShotOnCamera();
                    }

                    CalcInnerMaskRect(RectDynMaskBox);
                }
            }

            Text.Anchor = TextAnchor.UpperLeft;

            Widgets.EndScrollView();
        }

        /// <summary>
        /// Creates a small box with information for the user
        /// </summary>
        private void DrawInfoBox()
        {
            if (infoBoxStatus == InfoBoxStatus.Collapsed) return;
            
            Rect RectDynInfoBox = new Rect(RectInfoBox)
            {
                width = RectInfoBox.width * infoBoxExpansion
            };

            RectDynInfoBox.x = RectInfoBox.xMax - RectDynInfoBox.width;

            Widgets.DrawHighlight(RectDynInfoBox);
            Widgets.DrawBox(RectDynInfoBox, 2);
            Widgets.Label(RectDynInfoBox.ContractedBy(2f + 5f), 
                $"{"RVC_Tutorial".Translate($"<color=green>{SelectedPawn.def.label}</color>")}" +
                $"\n\n<color=orange>{"RVC_WarningColorEdit".Translate()}</color>");
        }

        /// <summary>
        /// Draws the color selection ScrollView
        /// </summary>
        private void DrawColorSelection()
        {
            Widgets.BeginScrollView(RectColorSelectOuter, ref ColorSelectorScroll, RectColorSelectInner);
            int pos = 0;
            foreach (KeyValuePair<string, ColorSet> kvp in colorSets)
            {
                string name = $"{SelectedPawn.def.defName}_ColorSet_{kvp.Key}".Translate();
                Rect rectTemp = RectColorFields[pos];
                Rect rectExpandCollapseIcon = rectTemp.LeftPartPixels(rectTemp.height);

                Text.Anchor = TextAnchor.MiddleLeft;

                Widgets.DrawLightHighlight(rectTemp);
                Widgets.DrawHighlightIfMouseover(rectTemp);
                Widgets.DrawBox(rectTemp, 2);
                Widgets.Label(rectTemp.RightPartPixels(rectTemp.width - rectTemp.height - 5f), name);
                Widgets.DrawTextureFitted(rectExpandCollapseIcon.ContractedBy(11f), pos == OpenColorField ? TexButton.Collapse : TexButton.Reveal, 1f);

                //Open or Close a Color Field
                if (Widgets.ButtonInvisible(rectTemp))
                {
                    if (pos == OpenColorField)
                    {
                        OpenColorField = -1;
                        SoundDefOf.TabClose.PlayOneShotOnCamera();
                    }
                    else
                    {
                        if (HasOpenColorField) SoundDefOf.TabClose.PlayOneShotOnCamera();

                        OpenColorField = pos;
                        SoundDefOf.TabOpen.PlayOneShotOnCamera();
                    }

                    CalcInnerColorRect();
                }

                //Draw the three recoloring options and buttons that allow to change them
                if (pos == OpenColorField)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float indent = 15f;

                        string colorName = $"{SelectedPawn.def.defName}_{kvp.Key}_{(Count)i}".Translate();
                        Rect rectColorField = rectTemp.MoveRect(new Vector2(indent, RectInnerFieldHeight * (i + 1)));
                        rectColorField.width -= indent + 1f;

                        Rect rectColorLabel = rectColorField.RightPartPixels(rectColorField.width - 5f);
                        Rect rectColorColor = rectColorField.RightPartPixels(rectColorLabel.width - 100f - 5f);

                        Widgets.DrawLightHighlight(rectColorField);
                        Widgets.DrawBoxSolidWithOutline(rectColorColor, kvp.Value.Colors[i], new Color(255f, 255f, 255f, 0.5f), 3);
                        Widgets.DrawHighlightIfMouseover(rectColorColor);
                        Widgets.DrawBox(rectColorField);
                        Widgets.Label(rectColorLabel, colorName);

                        if (Widgets.ButtonInvisible(rectColorColor))
                        {
                            int k = i; //save the current i to k so that the value of i isn't overridden during the for loop
                            Find.WindowStack.Add(new ColorPickerWindow(color => SetColor(color, kvp, k), (newSavedColors) => { RimValiCoreMod.Settings.savedColors = newSavedColors.ToList(); RimValiCoreMod.Settings.Write(); }, kvp.Value.Colors[k], RimValiCoreMod.Settings.savedColors.ToArray()));
                        }
                        TooltipHandler.TipRegion(rectColorColor, $"RVC_EditColor".Translate());
                    }
                }
                RimValiUtility.ResetTextAndColor();

                pos++;
            }

            RimValiUtility.ResetTextAndColor();
            Widgets.EndScrollView();
        }

        /// <summary>
        /// Draws the name editor fields
        /// </summary>
        private void DrawNameEdit()
        {
            if (SelectedPawn.Name is NameTriple name)
            {
                string first = Widgets.TextField(RectNamingRects[0], name.First, 25, CharacterCardUtility.ValidNameRegex);

                if (name.First.Equals(name.Nick) || name.Last.Equals(name.Nick)) GUI.color = new Color(255f, 255f, 255f, 0.4f);
                string nick = Widgets.TextField(RectNamingRects[1].ContractHorizontally(5), name.Nick, 25, CharacterCardUtility.ValidNameRegex);
                GUI.color = Color.white;

                string last = Widgets.TextField(RectNamingRects[2], name.Last, 25, CharacterCardUtility.ValidNameRegex);

                SelectedPawn.Name = new NameTriple(first, nick, last);
            }
            else
            {
                //Fixes names that aren't NameTriple based by converting them into one
                string[] fullName = SelectedPawn.Name.ToString().Split(' ');

                string first = fullName[0];
                string nick = "";
                string last = "";

                if (fullName.Length > 1)
                {
                    last = fullName[fullName.Length - 1];

                    for (int i = 1; i < fullName.Length - 1; i++)
                    {
                        nick += $"{fullName[i]} ";
                    }

                    if (nick.EndsWith(" "))
                    {
                        nick.Substring(0, nick.Length - 1);
                    }
                }

                SelectedPawn.Name = new NameTriple(first, nick, last);
            }
        }

        /// <summary>
        /// Sets a new <see cref="Color"/> <paramref name="color"/> into the given <see cref="ColorSet"/> provided in the <see cref="KeyValuePair"/> <paramref name="kvp"/> into the <paramref name="index"/>
        /// </summary>
        /// <param name="color">The new <see cref="Color"/></param>
        /// <param name="kvp">A <see cref="KeyValuePair"/> with <see cref="string"/> and <see cref="ColorSet"/></param>
        /// <param name="index"></param>
        private void SetColor(Color color, KeyValuePair<string, ColorSet> kvp, int index)
        {
            Color[] colors = kvp.Value.Colors;
            colors[index] = color;
            kvp.Value.Colors = colors;
            SelectedPawn.Drawer.renderer.graphics.ResolveAllGraphics();
        }

        /// <summary>
        /// Draws an image of the selected Pawn
        /// </summary>
        private void DrawPawn()
        {
            Widgets.DrawBox(RectColoringPart);
            RenderTexture image = PortraitsCache.Get(SelectedPawn, new Vector2(1024f, 1024f), Rot4.South, new Vector3(0f, 0f, 0.14f), cameraZoom: 2f, supersample: false);
            GUI.DrawTexture(RectPawnBig, image, ScaleMode.StretchToFill);
        }

        /// <summary>
        /// Draws a list of pawns that can be edited, highlighting the currently selected pawn yellow
        /// </summary>
        private void DrawPawnSelectionArea()
        {
            Widgets.BeginScrollView(RectPawnSelectOuter, ref PawnSelectorScroll, RectPawnSelectInner);
            GUI.BeginGroup(RectPawnSelectInner);

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                Rect rectPawnBox = new Rect(5f, 55f * i, RectPawnSelectInner.width - 10f, 50f);
                Rect rectPawnContent = rectPawnBox.ContractedBy(5f);
                rectPawnContent.height += 5f;

                Rect rectPawnPortraitArea = rectPawnContent.RightPartPixels(rectPawnContent.height);

                RenderTexture image = PortraitsCache.Get(pawn, new Vector2(256f, 256f), Rot4.South, new Vector3(0f, 0f, 0.25f), stylingStation: true, cameraZoom: 2.5f, supersample: false);

                Widgets.DrawBox(rectPawnBox);
                Widgets.DrawHighlight(rectPawnBox);
                Widgets.DrawHighlightIfMouseover(rectPawnBox);

                Text.Font = GameFont.Tiny;

                if (pawn.Name is NameTriple name && name.Nick is string nick)
                {
                    Widgets.Label(rectPawnContent, nick);
                }
                else
                {
                    Widgets.Label(rectPawnContent, pawn.Name.ToString());
                }

                Text.Anchor = TextAnchor.LowerLeft;

                Widgets.Label(rectPawnContent.MoveRect(new Vector2(0f, -5f)), pawn.story.TitleCap);

                GUI.color = new Color(1f, 1f, 1f, 0.2f);
                Widgets.DrawTextureFitted(rectPawnPortraitArea, image, 1f);

                RimValiUtility.ResetTextAndColor();

                if (SelectedPawn == pawn)
                {
                    Widgets.DrawBoxSolid(rectPawnBox, new Color(181f, 141f, 0f, 0.2f));
                }

                if (Widgets.ButtonInvisible(rectPawnBox))
                {
                    SelectedPawn = pawn;
                }
            }

            GUI.EndGroup();
            Widgets.EndScrollView();
        }

        /// <summary>
        /// Used to differentiate the language keys
        /// </summary>
        private enum Count
        {
            First,
            Second,
            Third
        }

        /// <summary>
        /// Defines in what state the InfoBox is
        /// </summary>
        private enum InfoBoxStatus
        {
            Expanded,
            Collapsing,
            Collapsed,
            Expanding
        }
    }
}
