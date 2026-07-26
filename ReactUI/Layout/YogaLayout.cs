using System.Collections.Generic;

namespace ReactUI.Layout;

/// <summary>
/// Pure C# flexbox layout engine. IL2CPP safe (no Reflection.Emit, no dynamic).
/// Handles: flex-direction, justify-content, align-items, align-self, flex-grow,
/// flex-shrink, flex-basis, flex-wrap, gap, padding, margin, min/max constraints,
/// aspect-ratio, position:absolute, display:none, and leaf MeasureFunc.
/// </summary>
public static class YogaLayout
{
    // ------------------------------------------------------------------ helpers
    private static bool IsNaN(float v) => float.IsNaN(v);
    private static float OrZero(float v) => IsNaN(v) ? 0f : v;
    private static float Clamp(float v, float min, float max)
    {
        if (!IsNaN(min) && v < min) v = min;
        if (!IsNaN(max) && v > max) v = max;
        return v;
    }
    private static float Max(float a, float b) => a > b ? a : b;

    // Is the main axis horizontal?
    private static bool IsRow(FlexDirection dir) => dir == FlexDirection.Row || dir == FlexDirection.RowReverse;
    private static bool IsReverse(FlexDirection dir) => dir == FlexDirection.RowReverse || dir == FlexDirection.ColumnReverse;

    // Extract main/cross dimensions from a node's style
    private static float MainSize(LayoutNode n, bool row) => row ? n.Width : n.Height;
    private static float CrossSize(LayoutNode n, bool row) => row ? n.Height : n.Width;
    private static float MainMinSize(LayoutNode n, bool row) => row ? n.MinWidth : n.MinHeight;
    private static float CrossMinSize(LayoutNode n, bool row) => row ? n.MinHeight : n.MinWidth;
    private static float MainMaxSize(LayoutNode n, bool row) => row ? n.MaxWidth : n.MaxHeight;
    private static float CrossMaxSize(LayoutNode n, bool row) => row ? n.MaxHeight : n.MaxWidth;

    private static float MainMarginStart(LayoutNode n, bool row) => row ? n.MarginLeft : n.MarginTop;
    private static float MainMarginEnd(LayoutNode n, bool row) => row ? n.MarginRight : n.MarginBottom;
    private static float CrossMarginStart(LayoutNode n, bool row) => row ? n.MarginTop : n.MarginLeft;
    private static float CrossMarginEnd(LayoutNode n, bool row) => row ? n.MarginBottom : n.MarginRight;

    private static float MainPadStart(LayoutNode n, bool row) => row ? n.PaddingLeft : n.PaddingTop;
    private static float MainPadEnd(LayoutNode n, bool row) => row ? n.PaddingRight : n.PaddingBottom;
    private static float CrossPadStart(LayoutNode n, bool row) => row ? n.PaddingTop : n.PaddingLeft;
    private static float CrossPadEnd(LayoutNode n, bool row) => row ? n.PaddingBottom : n.PaddingRight;

    private static void SetComputedMain(LayoutNode n, bool row, float v) { if (row) n.ComputedWidth = v; else n.ComputedHeight = v; }
    private static void SetComputedCross(LayoutNode n, bool row, float v) { if (row) n.ComputedHeight = v; else n.ComputedWidth = v; }
    private static float GetComputedMain(LayoutNode n, bool row) => row ? n.ComputedWidth : n.ComputedHeight;
    private static float GetComputedCross(LayoutNode n, bool row) => row ? n.ComputedHeight : n.ComputedWidth;

    private static void SetMainPos(LayoutNode n, bool row, float v) { if (row) n.ComputedX = v; else n.ComputedY = v; }
    private static void SetCrossPos(LayoutNode n, bool row, float v) { if (row) n.ComputedY = v; else n.ComputedX = v; }

    // --------------------------------------------------------- public entry point
    public static void Calculate(LayoutNode root, float availableWidth, float availableHeight)
    {
        LayoutInternal(root, availableWidth, availableHeight, MeasureMode.Exactly, MeasureMode.Exactly);
        ComputeAbsolutePositions(root, 0, 0);
        root.IsDirty = false;
    }

    // --------------------------------------------------------- core recursive layout
    private static void LayoutInternal(LayoutNode node, float availableWidth, float availableHeight,
                                       MeasureMode widthMode, MeasureMode heightMode)
    {
        bool row = IsRow(node.FlexDirection);

        // Resolve own definite size (including percent)
        float nodeW = ResolveSize(node.Width, availableWidth, widthMode);
        if (IsNaN(nodeW) && !IsNaN(node.WidthPercent) && !IsNaN(availableWidth))
            nodeW = availableWidth * node.WidthPercent / 100f;
        float nodeH = ResolveSize(node.Height, availableHeight, heightMode);
        if (IsNaN(nodeH) && !IsNaN(node.HeightPercent) && !IsNaN(availableHeight))
            nodeH = availableHeight * node.HeightPercent / 100f;

        // Apply aspect ratio to resolve missing dimension.
        // Aspect ratio takes precedence over the mode-based fallback:
        // if the node has an explicit width + aspect-ratio but no explicit height,
        // derive height from width/ratio rather than using the available height.
        if (!IsNaN(node.AspectRatio))
        {
            bool hasExplicitW = !IsNaN(node.Width) || !IsNaN(node.WidthPercent);
            bool hasExplicitH = !IsNaN(node.Height) || !IsNaN(node.HeightPercent);

            if (!hasExplicitH && !IsNaN(nodeW))
            {
                nodeH = nodeW / node.AspectRatio;
            }
            else if (!hasExplicitW && !IsNaN(nodeH))
            {
                nodeW = nodeH * node.AspectRatio;
            }
        }

        // Apply min/max constraints
        nodeW = Clamp(nodeW, node.MinWidth, node.MaxWidth);
        nodeH = Clamp(nodeH, node.MinHeight, node.MaxHeight);

        // Padding totals
        float padH = node.PaddingLeft + node.PaddingRight;
        float padV = node.PaddingTop + node.PaddingBottom;

        // ---- Leaf node with MeasureFunc ----
        if (node.MeasureFunc != null && node.Children.Count == 0)
        {
            float mw = IsNaN(nodeW) ? availableWidth : nodeW;
            float mh = IsNaN(nodeH) ? availableHeight : nodeH;
            // If available is NaN or 0, give leaf nodes freedom to measure unconstrained
            if (IsNaN(mw) || mw <= 0) mw = 100000f;
            if (IsNaN(mh) || mh <= 0) mh = 100000f;
            MeasureMode mwm = IsNaN(nodeW) ? MeasureMode.AtMost : MeasureMode.Exactly;
            MeasureMode mhm = IsNaN(nodeH) ? MeasureMode.AtMost : MeasureMode.Exactly;
            var measured = node.MeasureFunc(mw, mwm, mh, mhm);
            if (IsNaN(nodeW)) nodeW = measured.w + padH;
            if (IsNaN(nodeH)) nodeH = measured.h + padV;
            nodeW = Clamp(nodeW, node.MinWidth, node.MaxWidth);
            nodeH = Clamp(nodeH, node.MinHeight, node.MaxHeight);
            node.ComputedWidth = Max(nodeW, 0);
            node.ComputedHeight = Max(nodeH, 0);
            return;
        }

        // ---- No children ----
        if (node.Children.Count == 0)
        {
            node.ComputedWidth = IsNaN(nodeW) ? padH : Max(nodeW, 0);
            node.ComputedHeight = IsNaN(nodeH) ? padV : Max(nodeH, 0);
            return;
        }

        // Inner space available for children.
        // Both axes: only use available space when node has explicit size OR Exactly mode (stretch/root).
        // Otherwise NaN → content-sizing (children don't grow/stretch to huge values).
        float innerW, innerH;
        innerW = IsNaN(nodeW)
            ? (widthMode == MeasureMode.Exactly ? availableWidth - padH : float.NaN)
            : nodeW - padH;
        innerH = IsNaN(nodeH)
            ? (heightMode == MeasureMode.Exactly ? availableHeight - padV : float.NaN)
            : nodeH - padV;
        if (!IsNaN(innerW)) innerW = Max(innerW, 0);
        if (!IsNaN(innerH)) innerH = Max(innerH, 0);

        // For overflow:scroll containers, children are not constrained on the main axis.
        // They size to content and the container clips/scrolls the overflow.
        float scrollInnerW = innerW;
        float scrollInnerH = innerH;
        if (node.Overflow == Overflow.Scroll)
        {
            // Column scroll: don't constrain height (children can exceed container)
            if (!IsRow(node.FlexDirection))
                scrollInnerH = float.NaN;
            // Row scroll: don't constrain width
            else
                scrollInnerW = float.NaN;
        }

        bool crossDefinite = row
            ? (!IsNaN(node.Height) || heightMode == MeasureMode.Exactly)
            : (!IsNaN(node.Width) || widthMode == MeasureMode.Exactly);

        float innerMain = row ? scrollInnerW : scrollInnerH;
        float innerCross = row ? scrollInnerH : scrollInnerW;

        // Separate children into relative (in-flow) and absolute
        var relChildren = new List<LayoutNode>();
        var absChildren = new List<LayoutNode>();
        for (int i = 0; i < node.Children.Count; i++)
        {
            var c = node.Children[i];
            if (!c.Display) continue;
            if (c.Position == PositionType.Absolute)
                absChildren.Add(c);
            else
                relChildren.Add(c);
        }

        // ---- Flex layout for relative children ----
        // For wrap containers, use available main-axis space as the wrap constraint
        // even when innerMain is NaN (AtMost mode). This ensures items wrap correctly
        // during intrinsic sizing passes.
        float wrapMain = innerMain;
        if (node.FlexWrap == FlexWrap.Wrap && IsNaN(wrapMain))
        {
            float availMain = row ? availableWidth - padH : availableHeight - padV;
            if (!IsNaN(availMain) && availMain > 0)
                wrapMain = availMain;
        }

        // When auto-sizing under an AtMost constraint, keep the available cross space as
        // a soft limit for measuring children. Without it, a descendant wrap container is
        // measured against unbounded width, reports a single-line flex basis, and that
        // stale basis is force-applied after the final pass wraps to multiple lines —
        // making later siblings overlap it.
        float measureCross = innerCross;
        if (IsNaN(measureCross))
        {
            var crossMode = row ? heightMode : widthMode;
            float availCross = row ? availableHeight - padV : availableWidth - padH;
            if (crossMode != MeasureMode.Undefined && !IsNaN(availCross) && availCross > 0)
                measureCross = availCross;
        }

        // Build flex lines
        var lines = BuildFlexLines(relChildren, node, row, wrapMain, measureCross);

        // Process each line: resolve sizes, grow/shrink, then position
        float totalLineCross = 0;
        for (int li = 0; li < lines.Count; li++)
        {
            var line = lines[li];
            ResolveFlexLine(line, node, row, innerMain, measureCross, crossDefinite, lines.Count > 1);
            totalLineCross += line.CrossSize;
        }
        // Add gap between lines
        if (lines.Count > 1)
            totalLineCross += (lines.Count - 1) * node.Gap;

        // Determine container size if auto
        float containerMain = innerMain;
        if (IsNaN(containerMain))
        {
            // Size to content on main axis
            float maxLineMain = 0;
            for (int li = 0; li < lines.Count; li++)
                if (lines[li].TotalMain > maxLineMain)
                    maxLineMain = lines[li].TotalMain;
            containerMain = maxLineMain;
        }
        float containerCross = innerCross;
        bool hasExplicitCross = row
            ? (!IsNaN(node.Height) || !IsNaN(node.HeightPercent))
            : (!IsNaN(node.Width) || !IsNaN(node.WidthPercent));
        if (IsNaN(containerCross))
            containerCross = totalLineCross;
        else if (node.FlexWrap == FlexWrap.Wrap && lines.Count > 1 && !hasExplicitCross)
            containerCross = totalLineCross;

        // If single line and container has definite cross size, expand line to fill it
        if (lines.Count == 1 && !IsNaN(containerCross) && containerCross > totalLineCross)
        {
            lines[0].CrossSize = containerCross;
            totalLineCross = containerCross;
        }

        // Position children on main axis per JustifyContent, per line
        float crossOffset = 0;
        for (int li = 0; li < lines.Count; li++)
        {
            var line = lines[li];
            PositionMainAxis(line, node, row, containerMain);
            PositionCrossAxis(line, node, row, crossOffset, line.CrossSize);
            crossOffset += line.CrossSize + node.Gap;
        }

        // Compute final node size
        // For wrap containers without an explicit cross dimension, size to content
        // even when nodeH/nodeW was resolved from Exactly mode.
        bool wrapCrossAuto = node.FlexWrap == FlexWrap.Wrap && lines.Count > 1 && !hasExplicitCross;
        float finalW, finalH;
        if (row)
        {
            finalW = IsNaN(nodeW) ? containerMain + padH : nodeW;
            finalH = (IsNaN(nodeH) || wrapCrossAuto) ? containerCross + padV : nodeH;
        }
        else
        {
            finalW = (IsNaN(nodeW) || wrapCrossAuto) ? containerCross + padH : nodeW;
            finalH = IsNaN(nodeH) ? containerMain + padV : nodeH;
        }
        finalW = Clamp(finalW, node.MinWidth, node.MaxWidth);
        finalH = Clamp(finalH, node.MinHeight, node.MaxHeight);
        node.ComputedWidth = Max(finalW, 0);
        node.ComputedHeight = Max(finalH, 0);

        // ---- Absolute children ----
        for (int i = 0; i < absChildren.Count; i++)
            LayoutAbsoluteChild(absChildren[i], node);
    }

    // --------------------------------------------------------- flex line
    private sealed class FlexLine
    {
        public List<LayoutNode> Items = new();
        public List<float> Bases = new();       // resolved flex basis per item
        public List<float> MainSizes = new();   // final main sizes after grow/shrink
        public float TotalMain;                 // sum of main sizes + gaps + margins
        public float CrossSize;                 // max cross size in this line
    }

    // --------------------------------------------------------- build flex lines
    private static List<FlexLine> BuildFlexLines(List<LayoutNode> children, LayoutNode parent,
                                                  bool row, float innerMain, float innerCross)
    {
        var lines = new List<FlexLine>();
        if (children.Count == 0) return lines;

        var line = new FlexLine();
        lines.Add(line);

        float lineMainUsed = 0;
        bool wrap = parent.FlexWrap == FlexWrap.Wrap;

        for (int i = 0; i < children.Count; i++)
        {
            var child = children[i];
            float basis = ResolveFlexBasis(child, row, innerMain, innerCross);
            float marginMain = MainMarginStart(child, row) + MainMarginEnd(child, row);
            float itemMain = basis + marginMain;

            // Wrap check: if wrapping and adding this item would overflow, start new line
            // (unless line is empty — always put at least one item per line)
            if (wrap && line.Items.Count > 0 && !IsNaN(innerMain) && lineMainUsed + itemMain + parent.Gap > innerMain)
            {
                line = new FlexLine();
                lines.Add(line);
                lineMainUsed = 0;
            }

            line.Items.Add(child);
            line.Bases.Add(basis);
            if (line.Items.Count > 1) lineMainUsed += parent.Gap;
            lineMainUsed += itemMain;
        }

        return lines;
    }

    // Main-axis percent size helpers
    private static float MainSizePercent(LayoutNode n, bool row) => row ? n.WidthPercent : n.HeightPercent;

    // --------------------------------------------------------- resolve flex basis for one child
    private static float ResolveFlexBasis(LayoutNode child, bool row, float innerMain, float innerCross)
    {
        float basis = child.FlexBasis;
        if (IsNaN(basis))
        {
            // Use Width/Height if set
            float size = MainSize(child, row);
            if (!IsNaN(size))
                basis = size;
        }

        // Resolve percent on main axis (e.g. width:50% in a row container)
        if (IsNaN(basis))
        {
            float pct = MainSizePercent(child, row);
            if (!IsNaN(pct) && !IsNaN(innerMain))
                basis = innerMain * pct / 100f;
        }

        // If still NaN, we need to measure/layout the child to get intrinsic size
        if (IsNaN(basis))
        {
            // For intrinsic sizing: use large available on BOTH axes for MeasureFunc,
            // but mark cross as AtMost (not Exactly) so stretch doesn't expand to huge values.
            // Use Undefined for main so container doesn't take available as definite.
            float crossAvail = IsNaN(innerCross) ? 100000f : innerCross;

            float caw, cah;
            MeasureMode cwm, chm;
            if (row)
            {
                caw = 100000f; cwm = MeasureMode.AtMost;
                cah = crossAvail; chm = MeasureMode.AtMost;
            }
            else
            {
                caw = crossAvail; cwm = MeasureMode.AtMost;
                cah = 100000f; chm = MeasureMode.AtMost;
            }
            LayoutInternal(child, caw, cah, cwm, chm);
            basis = GetComputedMain(child, row);
        }

        // Apply min/max on main axis
        basis = Clamp(basis, MainMinSize(child, row), MainMaxSize(child, row));
        return Max(basis, 0);
    }

    // --------------------------------------------------------- resolve a flex line (grow/shrink + cross sizes)
    private static void ResolveFlexLine(FlexLine line, LayoutNode parent, bool row,
                                        float innerMain, float innerCross, bool crossDefinite = true,
                                        bool isMultiLine = false)
    {
        if (line.Items.Count == 0) return;

        int count = line.Items.Count;
        float totalGap = (count - 1) * parent.Gap;

        // Sum of bases + margins
        float totalBasis = totalGap;
        for (int i = 0; i < count; i++)
        {
            var child = line.Items[i];
            totalBasis += line.Bases[i] + MainMarginStart(child, row) + MainMarginEnd(child, row);
        }

        // Remaining space
        float remaining = IsNaN(innerMain) ? 0 : innerMain - totalBasis;

        // Initialize main sizes from bases
        line.MainSizes.Clear();
        for (int i = 0; i < count; i++)
            line.MainSizes.Add(line.Bases[i]);

        if (remaining > 0)
        {
            // Distribute via flex-grow
            float totalGrow = 0;
            for (int i = 0; i < count; i++)
                totalGrow += line.Items[i].FlexGrow;

            if (totalGrow > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    if (line.Items[i].FlexGrow > 0)
                    {
                        float grow = remaining * (line.Items[i].FlexGrow / totalGrow);
                        line.MainSizes[i] += grow;
                    }
                }
            }
        }
        else if (remaining < 0)
        {
            // Shrink via flex-shrink
            float totalShrinkScaled = 0;
            for (int i = 0; i < count; i++)
                totalShrinkScaled += line.Items[i].FlexShrink * line.Bases[i];

            if (totalShrinkScaled > 0)
            {
                float overflow = -remaining;
                for (int i = 0; i < count; i++)
                {
                    float shrinkScaled = line.Items[i].FlexShrink * line.Bases[i];
                    float shrinkAmount = overflow * (shrinkScaled / totalShrinkScaled);
                    line.MainSizes[i] -= shrinkAmount;
                    if (line.MainSizes[i] < 0) line.MainSizes[i] = 0;
                }
            }
        }

        // Apply min/max constraints and clamp
        for (int i = 0; i < count; i++)
        {
            line.MainSizes[i] = Clamp(line.MainSizes[i], MainMinSize(line.Items[i], row), MainMaxSize(line.Items[i], row));
            line.MainSizes[i] = Max(line.MainSizes[i], 0);
        }

        // Now layout each child with its resolved main size to determine cross size
        line.CrossSize = 0;
        line.TotalMain = totalGap;
        for (int i = 0; i < count; i++)
        {
            var child = line.Items[i];
            float childMainSize = line.MainSizes[i];
            float childCrossAvail = IsNaN(innerCross) ? float.NaN : innerCross;

            // Determine child available sizes for layout
            float cw, ch;
            MeasureMode cwm, chm;
            if (row)
            {
                cw = childMainSize;
                cwm = MeasureMode.Exactly;
                ch = IsNaN(childCrossAvail) ? 0 : childCrossAvail;
                chm = IsNaN(childCrossAvail) ? MeasureMode.Undefined : MeasureMode.AtMost;

                // If child has definite Height, use it
                if (!IsNaN(child.Height))
                {
                    ch = child.Height;
                    chm = MeasureMode.Exactly;
                }
                else if (!IsNaN(child.HeightPercent) && !IsNaN(innerCross))
                {
                    ch = (innerCross * child.HeightPercent / 100f);
                    chm = MeasureMode.Exactly;
                }
            }
            else
            {
                ch = childMainSize;
                chm = MeasureMode.Exactly;
                cw = IsNaN(childCrossAvail) ? 0 : childCrossAvail;
                cwm = IsNaN(childCrossAvail) ? MeasureMode.Undefined : MeasureMode.AtMost;

                // If child has definite Width, use it
                if (!IsNaN(child.Width))
                {
                    cw = child.Width;
                    cwm = MeasureMode.Exactly;
                }
                else if (!IsNaN(child.WidthPercent) && !IsNaN(innerCross))
                {
                    cw = (innerCross * child.WidthPercent / 100f);
                    cwm = MeasureMode.Exactly;
                }
            }

            // Handle AlignItems.Stretch: if cross size is auto and align is stretch, fill cross
            // Only stretch when parent's cross axis is definite (explicit size or Exactly mode).
            // Don't stretch if child has an explicit cross size (including percent).
            // With multiple wrap lines, stretch is per-line (a line is only as tall as its
            // tallest item), so stretching to the container cross here would blow every line
            // up to the full container size — content-size those children instead.
            AlignItems effectiveAlign = GetEffectiveAlign(child, parent);
            float crossPercent = row ? child.HeightPercent : child.WidthPercent;
            bool hasCrossSize = !IsNaN(CrossSize(child, row)) || !IsNaN(crossPercent);
            if (effectiveAlign == AlignItems.Stretch && !hasCrossSize && !IsNaN(childCrossAvail) && crossDefinite && !isMultiLine)
            {
                float crossMargins = CrossMarginStart(child, row) + CrossMarginEnd(child, row);
                float stretchedCross = childCrossAvail - crossMargins;
                if (stretchedCross < 0) stretchedCross = 0;
                if (row)
                {
                    ch = stretchedCross;
                    chm = MeasureMode.Exactly;
                }
                else
                {
                    cw = stretchedCross;
                    cwm = MeasureMode.Exactly;
                }
            }

            LayoutInternal(child, cw, ch, cwm, chm);

            SetComputedMain(child, row, childMainSize);

            float childCross = GetComputedCross(child, row) + CrossMarginStart(child, row) + CrossMarginEnd(child, row);
            if (childCross > line.CrossSize)
                line.CrossSize = childCross;

            line.TotalMain += childMainSize + MainMarginStart(child, row) + MainMarginEnd(child, row);
        }
    }

    // --------------------------------------------------------- get effective align for a child
    private static AlignItems GetEffectiveAlign(LayoutNode child, LayoutNode parent)
    {
        if (child.AlignSelf != AlignSelf.Auto)
        {
            return child.AlignSelf switch
            {
                AlignSelf.FlexStart => AlignItems.FlexStart,
                AlignSelf.Center => AlignItems.Center,
                AlignSelf.FlexEnd => AlignItems.FlexEnd,
                AlignSelf.Stretch => AlignItems.Stretch,
                _ => parent.AlignItems,
            };
        }
        return parent.AlignItems;
    }

    // --------------------------------------------------------- position children on main axis
    private static void PositionMainAxis(FlexLine line, LayoutNode parent, bool row, float containerMain)
    {
        int count = line.Items.Count;
        if (count == 0) return;

        bool reverse = IsReverse(parent.FlexDirection);

        // Sum of item main sizes + margins (no gaps yet, we handle gap in spacing)
        float usedMain = 0;
        for (int i = 0; i < count; i++)
        {
            usedMain += line.MainSizes[i] + MainMarginStart(line.Items[i], row) + MainMarginEnd(line.Items[i], row);
        }

        float totalGap = (count - 1) * parent.Gap;
        float freeSpace = containerMain - usedMain - totalGap;
        if (IsNaN(freeSpace) || freeSpace < 0) freeSpace = 0;

        float leadingSpace = 0;
        float betweenSpace = parent.Gap;

        // In CSS, justify-content aligns along the main axis direction.
        // For reverse directions, flex-start means the end edge (right for row-reverse,
        // bottom for column-reverse), and flex-end means the start edge.
        JustifyContent jc = parent.JustifyContent;
        if (reverse)
        {
            // Flip start/end for reverse directions
            if (jc == JustifyContent.FlexStart) jc = JustifyContent.FlexEnd;
            else if (jc == JustifyContent.FlexEnd) jc = JustifyContent.FlexStart;
        }

        switch (jc)
        {
            case JustifyContent.FlexStart:
                leadingSpace = 0;
                break;
            case JustifyContent.FlexEnd:
                leadingSpace = freeSpace;
                break;
            case JustifyContent.Center:
                leadingSpace = freeSpace / 2f;
                break;
            case JustifyContent.SpaceBetween:
                leadingSpace = 0;
                if (count > 1)
                    betweenSpace = parent.Gap + freeSpace / (count - 1);
                break;
            case JustifyContent.SpaceAround:
                if (count > 0)
                {
                    float each = freeSpace / count;
                    leadingSpace = each / 2f;
                    betweenSpace = parent.Gap + each;
                }
                break;
            case JustifyContent.SpaceEvenly:
                if (count > 0)
                {
                    float each = freeSpace / (count + 1);
                    leadingSpace = each;
                    betweenSpace = parent.Gap + each;
                }
                break;
        }

        // Padding offset
        float padStart = MainPadStart(parent, row);
        float pos = padStart + leadingSpace;

        for (int idx = 0; idx < count; idx++)
        {
            // For reverse: iterate items in reverse order so first child ends up
            // at the far end (right for row-reverse, bottom for column-reverse)
            int i = reverse ? (count - 1 - idx) : idx;
            var child = line.Items[i];

            pos += MainMarginStart(child, row);
            SetMainPos(child, row, pos);
            pos += line.MainSizes[i];
            pos += MainMarginEnd(child, row);

            if (idx < count - 1)
                pos += betweenSpace;
        }
    }

    // --------------------------------------------------------- position children on cross axis
    private static void PositionCrossAxis(FlexLine line, LayoutNode parent, bool row,
                                          float lineOffset, float lineCross)
    {
        float padCrossStart = CrossPadStart(parent, row);

        for (int i = 0; i < line.Items.Count; i++)
        {
            var child = line.Items[i];
            float childCross = GetComputedCross(child, row);
            float marginStart = CrossMarginStart(child, row);
            float marginEnd = CrossMarginEnd(child, row);
            float childOuterCross = childCross + marginStart + marginEnd;

            AlignItems align = GetEffectiveAlign(child, parent);
            float crossPos;

            switch (align)
            {
                case AlignItems.FlexStart:
                default:
                    crossPos = lineOffset + padCrossStart + marginStart;
                    break;
                case AlignItems.FlexEnd:
                    crossPos = lineOffset + padCrossStart + lineCross - marginEnd - childCross;
                    break;
                case AlignItems.Center:
                    crossPos = lineOffset + padCrossStart + (lineCross - childOuterCross) / 2f + marginStart;
                    break;
                case AlignItems.Stretch:
                    crossPos = lineOffset + padCrossStart + marginStart;
                    // Stretch was already applied during size resolution
                    break;
                case AlignItems.Baseline:
                    // Baseline treated as FlexStart for now
                    crossPos = lineOffset + padCrossStart + marginStart;
                    break;
            }

            SetCrossPos(child, row, crossPos);
        }
    }

    // --------------------------------------------------------- absolute children
    private static void LayoutAbsoluteChild(LayoutNode child, LayoutNode parent)
    {
        float parentW = parent.ComputedWidth;
        float parentH = parent.ComputedHeight;
        float contentW = parentW - parent.PaddingLeft - parent.PaddingRight;
        float contentH = parentH - parent.PaddingTop - parent.PaddingBottom;

        // Determine child width (resolve percent against parent content area)
        float childW = child.Width;
        if (IsNaN(childW) && !IsNaN(child.WidthPercent))
            childW = contentW * child.WidthPercent / 100f;
        if (IsNaN(childW) && !IsNaN(child.PositionLeft) && !IsNaN(child.PositionRight))
            childW = parentW - child.PositionLeft - child.PositionRight - parent.PaddingLeft - parent.PaddingRight;

        // Determine child height (resolve percent against parent content area)
        float childH = child.Height;
        if (IsNaN(childH) && !IsNaN(child.HeightPercent))
            childH = contentH * child.HeightPercent / 100f;
        if (IsNaN(childH) && !IsNaN(child.PositionTop) && !IsNaN(child.PositionBottom))
            childH = parentH - child.PositionTop - child.PositionBottom - parent.PaddingTop - parent.PaddingBottom;

        // Aspect ratio
        if (!IsNaN(child.AspectRatio))
        {
            if (IsNaN(childH) && !IsNaN(childW)) childH = childW / child.AspectRatio;
            else if (IsNaN(childW) && !IsNaN(childH)) childW = childH * child.AspectRatio;
        }

        // Layout child to resolve its size — use AtMost for undefined dims so child sizes to content
        float aw = IsNaN(childW) ? parentW : childW;
        float ah = IsNaN(childH) ? parentH : childH;
        MeasureMode awm = IsNaN(childW) ? MeasureMode.AtMost : MeasureMode.Exactly;
        MeasureMode ahm = IsNaN(childH) ? MeasureMode.AtMost : MeasureMode.Exactly;
        LayoutInternal(child, aw, ah, awm, ahm);

        // Only override computed size if we had a definite value
        if (!IsNaN(childW)) child.ComputedWidth = Max(Clamp(childW, child.MinWidth, child.MaxWidth), 0);
        // Don't override height if it was auto — let LayoutInternal's computed value stand
        if (!IsNaN(childH)) child.ComputedHeight = Max(Clamp(childH, child.MinHeight, child.MaxHeight), 0);

        // Resolve percent margins against parent content area
        float mLeft = child.MarginLeftRaw.Unit == Style.StyleUnit.Percent
            ? contentW * child.MarginLeftRaw.Value / 100f : child.MarginLeft;
        float mRight = child.MarginRightRaw.Unit == Style.StyleUnit.Percent
            ? contentW * child.MarginRightRaw.Value / 100f : child.MarginRight;
        float mTop = child.MarginTopRaw.Unit == Style.StyleUnit.Percent
            ? contentH * child.MarginTopRaw.Value / 100f : child.MarginTop;
        float mBottom = child.MarginBottomRaw.Unit == Style.StyleUnit.Percent
            ? contentH * child.MarginBottomRaw.Value / 100f : child.MarginBottom;

        // Position X
        if (!IsNaN(child.PositionLeft))
            child.ComputedX = parent.PaddingLeft + child.PositionLeft + mLeft;
        else if (!IsNaN(child.PositionRight))
            child.ComputedX = parentW - parent.PaddingRight - child.PositionRight - child.ComputedWidth - mRight;
        else
            child.ComputedX = parent.PaddingLeft + mLeft;

        // Position Y
        if (!IsNaN(child.PositionTop))
            child.ComputedY = parent.PaddingTop + child.PositionTop + mTop;
        else if (!IsNaN(child.PositionBottom))
            child.ComputedY = parentH - parent.PaddingBottom - child.PositionBottom - child.ComputedHeight - mBottom;
        else
            child.ComputedY = parent.PaddingTop + mTop;
    }

    // --------------------------------------------------------- resolve absolute positions (recursive)
    private static void ComputeAbsolutePositions(LayoutNode node, float parentX, float parentY)
    {
        // ComputedX/Y at this point are relative to parent content box.
        // Convert to absolute screen coordinates.
        node.ComputedX += parentX;
        node.ComputedY += parentY;

        float childBaseX = node.ComputedX;
        float childBaseY = node.ComputedY;

        for (int i = 0; i < node.Children.Count; i++)
        {
            var child = node.Children[i];
            if (!child.Display) continue;
            ComputeAbsolutePositions(child, childBaseX, childBaseY);
        }

        node.IsDirty = false;
    }

    // --------------------------------------------------------- resolve a size value
    private static float ResolveSize(float value, float available, MeasureMode mode)
    {
        if (!IsNaN(value))
            return value;

        return mode switch
        {
            MeasureMode.Exactly => available,
            _ => float.NaN,
        };
    }
}
