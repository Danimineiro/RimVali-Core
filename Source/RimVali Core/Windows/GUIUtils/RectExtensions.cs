﻿using RimValiCore.QLine;
using RimWorld;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimValiCore.Windows.GUIUtils
{
    public static class RectExtensions
    {
        /// <summary>
        ///     Creates a copy of this <see cref="Rect" /> moved by a <see cref="Vector2" />
        /// </summary>
        /// <param name="rect">the <see cref="Rect" /> to move</param>
        /// <param name="vec">the distance to move <paramref name="rect" /></param>
        /// <returns>A copy of <paramref name="rect" />, moved by the distance specified in <paramref name="vec" /></returns>
        public static Rect MoveRect(this Rect rect, Vector2 vec)
        {
            Rect newRect = new Rect(rect);
            newRect.position += vec;
            return newRect;
        }

        /// <summary>
        ///     Creates a copy of a <see cref="Rect" /> with its left edge moved by <paramref name="scaleBy" />, while
        ///     maintaining its width.
        /// </summary>
        /// <param name="rect">The <see cref="Rect" /> to modify</param>
        /// <param name="scaleBy">The amount of units to scale <paramref name="rect" /> by</param>
        /// <returns>A copy of <paramref name="rect" /> with its left edge moved to the left by <paramref name="scaleBy" /> units</returns>
        public static Rect ScaleX(this Rect rect, float scaleBy)
        {
            Rect newRect = new Rect(rect);
            newRect.xMin -= scaleBy;
            return newRect;
        }

        /// <summary>
        ///     Devides a <see cref="Rect"/> <paramref name="rect"/> vertically into <see cref="int"/> <paramref name="times"/> amount of pieces
        /// </summary>
        /// <param name="rect">the initial <see cref="Rect"/> that is to be devided</param>
        /// <param name="times">the amount of times it should be devided</param>
        /// <returns>An <see cref="IEnumerable{T}"/> with <paramref name="times"/> amount of pieces </returns>
        public static IEnumerable<Rect> DivideVertical(this Rect rect, int times)
        {
            for (int i = 0; i < times; i++)
            {
                yield return rect.TopPartPixels(rect.height / times).MoveRect(new Vector2(0f, rect.height / times * i));
            }
        }

        /// <summary>
        ///     Devides a <see cref="Rect"/> <paramref name="rect"/> horizontally into <see cref="int"/> <paramref name="times"/> amount of pieces
        /// </summary>
        /// <param name="rect">the initial <see cref="Rect"/> that is to be devided</param>
        /// <param name="times">the amount of times it should be devided</param>
        /// <returns>An <see cref="IEnumerable{T}"/> with <paramref name="times"/> amount of pieces </returns>
        public static IEnumerable<Rect> DivideHorizontal(this Rect rect, int times)
        {
            for (int i = 0; i < times; i++)
            {
                yield return rect.LeftPartPixels(rect.width / times).MoveRect(new Vector2(rect.width / times * i, 0f));
            }
        }

        /// <summary>
        ///     Makes a text button that executes an <see cref="Action"/>
        /// </summary>
        /// <param name="rect">The <see cref="Rect"/> to draw the button in</param>
        /// <param name="label">The label of the button</param>
        /// <param name="action">The <see cref="Action"/> that is executed</param>
        public static void DrawButtonText(this Rect rect, string label, Action action, bool disable = false)
        {
            if (Widgets.ButtonText(rect, label))
            {
                if (disable)
                {
                    SoundDefOf.ClickReject.PlayOneShotOnCamera();
                }
                else
                {
                    action();
                }
            }
        }

        /// <summary>
        ///     Creates a inner rect for a scroll rect, using the outer rect as base.
        ///     Decreases the inner rects width, if it is high enough for scroll bars to exist, by the width of scroll bars
        /// </summary>
        /// <param name="outerRect">the outer <see cref="Rect"/></param>
        /// <param name="innerHeight">the height of the inner rect</param>
        /// <returns></returns>
        public static Rect GetInnerScrollRect(this Rect outerRect, float innerHeight) => new Rect(outerRect)
        {
            height = innerHeight,
            width = outerRect.width - (innerHeight > outerRect.height ? 17f : 0f)
        };

        /// <summary>
        ///     Draws a highlight into the selected rect, a light highlight if <paramref name="light"/> is true, dark otherwise
        /// </summary>
        /// <param name="rect">The rect the highlight is drawn in</param>
        /// <param name="light">If the highlight is dark or light</param>
        public static void DoRectHighlight(this Rect rect, bool light)
        {
            if (light)
            {
                Widgets.DrawLightHighlight(rect);
            }
            else
            {
                Widgets.DrawHighlight(rect);
            }
        }

        /// <summary>
        ///     Changes the <see cref="Rect"/> <paramref name="rect"/>s x and width to the x and width of the <see cref="Rect"/> <paramref name="other"/>
        /// </summary>
        /// <param name="rect">The <see cref="Rect"/> to be changed</param>
        /// <param name="other">The <see cref="Rect"/> that has the variables to change to</param>
        /// <returns></returns>
        public static Rect AlignXWith(this Rect rect, Rect other) => new Rect(other.x, rect.y, other.width, rect.height);

        /// <summary>
        ///     Flips a <see cref="Rect"/> <paramref name="rect"/> horizontally
        /// </summary>
        /// <param name="rect">the rect to be flipped</param>
        /// <returns>A flipped rect</returns>
        public static Rect FlipHorizontal(this Rect rect) => new Rect(rect.x + rect.width, rect.y, rect.width * -1, rect.height);

        /// <summary>
        ///     Creates a window that displays all <see cref="DisableReason"/>s that determine why a button may be enabled or disabled.
        ///     Doesn't display anything if <paramref name="disableReasons"/> is <see cref="GenList.NullOrEmpty{T}(IList{T})"/>
        /// </summary>
        /// <param name="toolTipArea">The <see cref="Rect"/> in which the window is created, if the mouse is over it</param>
        /// <param name="outerWindowPos">A <see cref="Vector2"/> that modifies the position of the window</param>
        /// <param name="disableReasons">The <see cref="DisableReason"/>s the window displays</param>
        public static void MakeToolTip(this Rect toolTipArea, Vector2 outerWindowPos, List<DisableReason> disableReasons)
        {
            if (disableReasons.NullOrEmpty() || !Mouse.IsOver(toolTipArea)) return;

            string requirementsString = "<color=green>##Requirements:</color>";
            const float RowHeight = 25f;
            const float CommonMargin = 5f;

            Rect rectToolTip = new Rect(Event.current.mousePosition + outerWindowPos + new Vector2(CommonMargin, CommonMargin), new Vector2(RowHeight + 25f, 20f));
            rectToolTip.height += (disableReasons.Count + 1) * (RowHeight + 2f) - 2f;
            rectToolTip.width += Math.Max(Text.CalcSize(requirementsString).x, disableReasons.Max(reason => Text.CalcSize(reason.Reason).x));
            rectToolTip.y = Math.Min(rectToolTip.y, UI.screenHeight - rectToolTip.height);

            Find.WindowStack.ImmediateWindow("help I'm Dying".GetHashCode(), rectToolTip, WindowLayer.Super, () =>
            {
                Rect rectLine = rectToolTip.AtZero().TopPartPixels(RowHeight).MoveRect(new Vector2(10f, 10f));
                rectLine.xMax -= 20f;

                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(rectLine, requirementsString);

                for (int j = 0; j < disableReasons.Count; j++)
                {
                    rectLine = rectLine.MoveRect(new Vector2(0f, rectLine.height + 2f));
                    GUI.DrawTexture(rectLine.LeftPartPixels(rectLine.height).ContractedBy(4f), disableReasons[j].ShouldDisable ? Widgets.CheckboxOffTex : Widgets.CheckboxOnTex);
                    Widgets.Label(rectLine.RightPartPixels(rectLine.width - rectLine.height), disableReasons[j].Reason);
                }

                Text.Anchor = TextAnchor.UpperLeft;
            });
        }
    }
}
