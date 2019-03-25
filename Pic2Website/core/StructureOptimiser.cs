using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using OpenCvSharp;
using Pic2Website.core.model.elements;
using Pic2Website.core.model.elements.basic;
using Pic2Website.core.model.elements.grid;

namespace Pic2Website.core
{
    public static class StructureOptimiser
    {
        public static void CheckForUnmergedTexts(Block block)
        {
            var elements = block.Elements;
            var onlyTexts = true;

            if (elements.Count <= 1)
            {
                return;
            }

            foreach (var element in elements)
            {
                if (element.GetType() != typeof(Text) || (element.GetType() == typeof(Text) && element.Display != "inline-block"))
                {
                    onlyTexts = false;
                }
            }

            if (onlyTexts)
            {
                var texts = new Text[elements.Count];
                for (var i = 0; i < texts.Length; i++)
                {
                    var text = (Text)elements[i];
                    texts[i] = text;
                }

                var unify = MergeTexts(texts, false);

                block.Elements.Clear();
                block.Elements.Add(unify);
            }
        }

        /// <summary>
        ///  Merge text elements, rows and unify font attributes
        /// </summary>
        /// <param name="rows"></param>
        public static void OptimiseText(List<Element> rows)
        {
            for (var i = 0; i < rows.Count; i++)
            {
                var row = (Row)rows.ElementAt(i);
                var columnsCount = row.Columns.Count();

                // check if there's not just one column and one element inside row
                if (columnsCount == 1)
                {
                    var firstColumn = row.Columns.First();
                    var elementsCount = firstColumn.Elements.Count();
                    if (elementsCount == 1 && firstColumn.MarginCalc[1] == "" /*&& !(firstColumn.Elements.First().GetType() == typeof(Text) && firstColumn.BackgroundColor != null)*/)
                    {
                        // we can remove useless row and column
                        var element = firstColumn.Elements.First();

                        if (firstColumn.BackgroundColor != null)
                        {
                            element.Padding[1] += element.Margin[1];
                            element.Padding[3] += element.Margin[3];
                            element.Padding[2] += element.Margin[2];
                            element.Padding[0] += element.Margin[0];
                            element.Margin[1] = 0;
                            element.Margin[3] = 0;
                            element.Margin[2] = 0;
                            element.Margin[0] = 0;
                        }

                        element.Margin = element.Margin.Zip(firstColumn.Margin, (a, b) => a + b).ToArray();


                        //if (element.GetType() == typeof(Block) || element.GetType() == typeof(Row))
                        //{
                            //element.Margin = element.Margin.Zip(row.Padding, (a, b) => a + b).ToArray();
                            //element.Margin = element.Margin.Zip(row.Margin, (a, b) => a + b).ToArray();
                        //}
                        //else
                        //{
                            element.Padding = element.Padding.Zip(row.Padding, (a, b) => a + b).ToArray();
                            element.Margin = element.Margin.Zip(row.Margin, (a, b) => a + b).ToArray();

                          
                        //}

                     

                        //Debug.WriteLine("sub = " + sub);
                        /*if (row.ActAsColumn)
                        {
                            element.Padding[1] += row.Margin[1];
                            element.Padding[3] += row.Margin[3];

                            element.Margin[0] += row.Margin[0];
                            element.Margin[2] += row.Margin[2];

                            element.Padding[0] += row.Padding[0];
                            element.Padding[2] += row.Padding[2];
                        }
                        else
                        {
                            element.Padding[1] = element.Margin[1];
                            element.Margin[1] = 0;
                            element.Padding[3] = element.Margin[3];
                            element.Margin[3] = 0;
                            element.Margin = element.Margin.Zip(row.Margin, (a, b) => a + b).ToArray();
                            element.Margin = element.Margin.Zip(row.Padding, (a, b) => a + b).ToArray();
                        }*/


                        if (Util.AreSame(row.Margin[1], row.Margin[3], 15) && element.GetType() == typeof(Text) && firstColumn.BackgroundColor == null)
                        {
                            //element.Padding[0] += row.Padding[0];
                            //element.Padding[2] += row.Padding[2];
                            //element.Padding[1] += row.Padding[1];
                            //element.Padding[3] += row.Padding[3];

                            //element.Margin = element.Margin.Zip(row.Margin, (a, b) => a + b).ToArray();

                            //element.Padding[1] += row.Margin[1];
                            //element.Padding[3] += row.Margin[3];

                            //element.Margin[0] += row.Margin[0];
                            //element.Margin[2] += row.Margin[2];

                            //element.Padding[1] = element.Padding[3] = 0;
                            element.TextAlign = "center";
                        }
                        else
                        {
                            if (firstColumn.BackgroundColor != null)
                            {
                                // @todo not sure, môže byť situácia button ale vo vnútri zoberie text ako obrázok takže chceme mať aj bg color
                                //if (element.GetType() != typeof(Image))
                                //{
                                    element.BackgroundColor = firstColumn.BackgroundColor;

                                //}
                                
                                if (element.GetType() != typeof(Text))
                                {
                                    element.Width = firstColumn.Width;
                                }

                                //element.Padding[1] += row.Margin[1];
                                //element.Padding[3] += row.Margin[3];

                                //element.Margin[0] += row.Margin[0];
                                //element.Margin[2] += row.Margin[2];
                            }
                            else
                            {
                                //element.Margin = element.Margin.Zip(row.Margin, (a, b) => a + b).ToArray();
                            }
                        }

                        element.Fluid = row.Fluid;

                        rows[i] = element;
                    }
                }
                // check if there aren't text rows inside columns
                else if (columnsCount > 1)
                {
                    for (var j = 0; j < row.Columns.Count; j++)
                    {
                        var column = row.Columns[j];
                        // check for text lines
                        if (column.Elements.Count > 1)
                        {
                            MergeTextRows(column.Elements);
                        }

                        if (column.Elements.Count == 1 && column.Padding.Sum() == 0 && column.BackgroundColor != null)
                        {
                            var firstElement = column.Elements.First();
                            // column has background color with no effect - we should remove it
                            if (firstElement.Padding.Sum() + firstElement.Margin.Sum() == 0 || Util.SameColors(firstElement.Color, column.BackgroundColor))
                            {
                                column.BackgroundColor = null;
                            }
                        }
                    }

                    // rows were merged into columns inside row
                    if (row.MergedColumns)
                    {
                        // check for title text styles
                        var titles = new List<Text>();
                        foreach (var column in row.Columns)
                        {
                            if (column.Elements.Count > 0)
                            {
                                var element = column.Elements.First();
                                if (element.GetType() == typeof(Text))
                                {
                                    titles.Add((Text)element);
                                }
                            }
                        }

                        if (titles.Count > 0)
                        {
                            var unify = MergeTexts(titles.ToArray(), false);
                            foreach (var column in row.Columns)
                            {
                                var element = column.Elements.First();
                                if (element.GetType() == typeof(Text))
                                {
                                    CopyTextStyle(unify, (Text)element);
                                }
                            }
                        }
                    }
                }

                // check if there aren't an unmerged text elements
                if (row.Columns.Count > 1)
                {
                    var oneTextElement = true;

                    foreach (var column in row.Columns)
                    {
                        if (column.Elements.Count != 1 || column.Margin[1] > 20 || column.Elements[0].GetType() != typeof(Text) || column.BackgroundColor != null)
                        {
                            oneTextElement = false;
                            break;
                        }
                    }

                    if (oneTextElement)
                    {
                        var texts = new Text[row.Columns.Count];
                        for (var j = 0; j < texts.Length; j++)
                        {
                            texts[j] = (Text)row.Columns[j].Elements[0];
                        }

                        var textElement = MergeTexts(texts, false);

                        if (Util.AreSame(row.Margin[1], row.Margin[3], 15))
                        {
                            textElement.Padding[0] = row.Margin[0];
                            textElement.Padding[2] = row.Margin[2];
                            textElement.TextAlign = "center";
                        }
                        else
                        {
                            textElement.Padding = row.Padding;
                            textElement.Margin = row.Margin;
                        }

                        rows[i] = textElement;
                    }
                }
            }

            if (rows.Count > 1)
            {
                MergeTextRows(rows);
            }
        }

        private static void MergeTextRows(List<Element> rows)
        {
            var mergePairs = new List<Tuple<int, int>>();
            var startMergeIndex = -1;
            var previousGap = 0;
            for (var i = 0; i < rows.Count - 1; i++)
            {
                var merge = false;
                var firstRow = rows[i];
                var secondRow = rows[i + 1];

                // check if items are really rows and not just words next to each other
                if (secondRow.Rect.Top < firstRow.Rect.Bottom)
                {
                    return;
                }

                var firstGap = secondRow.Margin[0] + secondRow.Padding[0] + firstRow.Padding[2] + firstRow.Margin[2];

                if (i < rows.Count - 2)
                {
                    var thirdRow = rows[i + 2];
                    var secondGap = thirdRow.Margin[0] + thirdRow.Padding[0] + secondRow.Padding[2] + secondRow.Margin[2];

                    if (firstRow.GetType() == typeof(Text) && secondRow.GetType() == typeof(Text) && firstGap <= 15 &&
                        (
                            (Util.AreSame(firstGap, secondGap, 3) && Util.AreSame(firstRow.FontSize, secondRow.FontSize, 3)) ||
                            (Util.AreSame(firstGap, previousGap, 3) && startMergeIndex != -1) ||
                            (secondGap > firstGap && firstGap <= 15 && Util.AreSame(firstRow.FontSize, secondRow.FontSize, 3))
                        )
                    )
                    {
                        merge = true;
                    }

                    previousGap = firstGap;
                }
                else
                {
                    if (firstRow.GetType() == typeof(Text) && secondRow.GetType() == typeof(Text) && firstGap <= 15 && 
                        (
                            (Util.AreSame(firstGap, previousGap, 3) && startMergeIndex != -1) ||
                            (previousGap > firstGap && Util.AreSame(firstRow.FontSize, secondRow.FontSize, 3) && startMergeIndex != -1) ||
                            (previousGap == 0 && Util.AreSame(firstRow.FontSize, secondRow.FontSize, 3))
                        )
                    )
                    {
                        merge = true;
                    }
                }

                if (startMergeIndex == -1 && merge)
                {
                    startMergeIndex = i;
                }
                else if (startMergeIndex != -1 && !merge)
                {
                    mergePairs.Add(new Tuple<int, int>(startMergeIndex, i));
                    startMergeIndex = -1;
                }
            }

            // finish last row
            if (startMergeIndex != -1)
            {
                mergePairs.Add(new Tuple<int, int>(startMergeIndex, rows.Count - 1));
            }

            // check if there are some rows thats needs to be merged
            if (mergePairs.Count > 0)
            {
                // start from the end
                mergePairs.Reverse();

                foreach (var pair in mergePairs)
                {
                    // fill texts array
                    var texts = new Text[pair.Item2 - pair.Item1 + 1];
                    var j = 0;
                    for (var i = pair.Item1; i <= pair.Item2; i++)
                    {
                        texts[j] = (Text)rows[i];
                        j++;
                    }

                    // crate text element
                    var textElement = MergeTexts(texts);

                    // apply margins
                    var firstRow = rows[pair.Item1];
                    var lastRow = rows[pair.Item2];
                    textElement.Margin[0] = firstRow.Margin[0] + firstRow.Padding[0];
                    if (textElement.TextAlign != "center")
                    {
                        textElement.Margin[3] = firstRow.Margin[3] + firstRow.Padding[3];
                    }
                    textElement.Margin[2] = lastRow.Margin[2] + lastRow.Padding[2];

                    // remove rows that will be merged
                    rows.RemoveRange(pair.Item1 + 1, pair.Item2 - pair.Item1);

                    // replace with merged content
                    rows[pair.Item1] = textElement;
                }
            }
        }

        private static Text MergeTexts(Text[] textElements, bool lineBreaks = true)
        {
            var length = textElements.Length;
            var text = new string[lineBreaks ? length : 1];
            var fontColors = new List<int[]>(length);
            var fontSizes = new int[length];
            var fontWeights = new int[length];
            var fontFamilies = new string[length];
            var fontStyles = new string[length];
            var fontTransforms = new string[length];
            var margins = new int[length - 1];
            var merge = "";
            for (var i = 0; i < length; i++)
            {
                var textElem = textElements[i];

                // text
                if (lineBreaks)
                {
                    // set i text
                    text[i] = textElem.GetText()[0];

                    if (i != 0)
                    {
                        margins[i - 1] = textElem.Margin[0] + textElem.Padding[0];
                    }
                }
                else
                {
                    // add space
                    if (i != 0)
                    {
                        merge += " ";
                    }

                    // append text
                    merge += textElem.GetText()[0];

                    // set text in last element
                    if (i == length - 1)
                    {
                        text[0] = merge;
                    }
                }

                // text attributes
                fontColors.Add(textElem.Color);
                fontSizes[i] = textElem.FontSize;
                fontWeights[i] = textElem.FontWeight != 0 ? textElem.FontWeight : 400;
                fontFamilies[i] = textElem.FontFamily;
                fontStyles[i] = textElem.FontStyle;
                fontTransforms[i] = textElem.FontTransform;
            }

            var fontColor = fontColors.MostCommon();
            var fontSizeMostCommon = fontSizes.MostCommon();
            var fontSize = fontSizes.Where(s => s == fontSizeMostCommon).Count() > 1 ? fontSizeMostCommon : (int)fontSizes.Average();
            var fontFamily = fontFamilies.MostCommon();
            var fontWeight = fontWeights.MostCommon();
            var fontStyle = fontStyles.MostCommon();
            var fontTransform = fontTransforms.MostCommon();

            // make sure all chars in text are lowercase when font transform is uppercase
            if (fontTransform == "uppercase")
            {
                text = text.Select(s => s.ToLowerInvariant()).ToArray();
            }

            var textElement = new Text(text, fontFamily, fontColor, fontSize, fontWeight == 700, fontStyle == "italic", fontTransform);
            if (textElements[0].TextAlign == "center" && textElements[length - 1].TextAlign == "center")
            {
                textElement.TextAlign = "center";
            }
            
            if (lineBreaks)
            {
                textElement.LineHeight = fontSize + margins.Average();
            }

            return textElement;
        }

        /// <summary>
        /// Merge columns that have same spacing in between
        /// </summary>
        /// <param name="sectionRows"></param>
        /// <param name="fluid"></param>
        public static void MergeIntoLogicalColumns(List<Element> sectionRows, bool fluid)
        {
            foreach (Row row in sectionRows)
            {
                var mergedColumns = new List<Column>();
                var columns = row.Columns;

                if (columns.Count > 2 && !fluid)
                {
                    var mergePairs = new List<Tuple<int, int>>();
                    var index = 0;
                    var previousGap = 0;
                    while (index + 2 < columns.Count)
                    {
                        var currentColumn = columns[index];
                        var firstColumn = columns[index + 1];
                        var secondColumn = columns[index + 2];

                        if (currentColumn.Elements.Count != 1 || firstColumn.Elements.Count != 1 || currentColumn.BackgroundColor != null || firstColumn.BackgroundColor != null)
                        {
                            index++;
                            continue;
                        }

                        var firstGap = firstColumn.Rect.Left - currentColumn.Rect.Right;
                        var secondGap = secondColumn.Rect.Left - firstColumn.Rect.Right;

                        // Compare first and second gap between columns
                        if (Util.AreSame(firstGap, secondGap, 4) || Util.AreSame(firstGap, previousGap, 4))
                        {
                            //Debug.WriteLine("merge A " + index + "-" + (index + 1));

                            mergePairs.Add(new Tuple<int, int>(index, index + 1));

                            index++;
                            previousGap = firstGap;

                            if (index + 2 == columns.Count && Util.AreSame(firstGap, secondGap, 4))
                            {
                                mergePairs.Add(new Tuple<int, int>(index, index + 1));
                            }

                            continue;
                        }

                        previousGap = firstGap;

                        // Compare first and third gap between columns
                        if (index + 3 < columns.Count)
                        {
                            var thirdColumn = columns[index + 3];
                            var thirdGap = thirdColumn.Rect.Left - secondColumn.Rect.Right;

                            if (Util.AreSame(firstGap, thirdGap, 4))
                            {
                                //Debug.WriteLine("merge B " + index + "-" + (index + 1));
                                mergePairs.Add(new Tuple<int, int>(index, index + 1));
                                previousGap = secondGap;

                                index++;
                            }
                            // Might not be connected but other columns are further
                            else if (thirdGap > firstGap * 2.5 || secondGap > firstGap * 2.5)
                            {
                                //Debug.WriteLine("merge C " + index + "-" + (index + 1));
                                mergePairs.Add(new Tuple<int, int>(index, index + 1));
                                previousGap = secondGap;

                                index++;
                            }
                        }

                        index++;
                    }

                    // we don't want to merge column that are already merged
                    if (mergePairs.Count > 1)
                    {
                        // connect pairs
                        var pairs = new List<Tuple<int, int>>();
                        for (var i = 0; i < columns.Count; i++)
                        {
                            if (mergePairs.Contains(new Tuple<int, int>(i, i + 1)))
                            {
                                var start = i;
                                var end = i + 1;
                                i++;
                                while (mergePairs.Contains(new Tuple<int, int>(i, i + 1)) && i < columns.Count - 1)
                                {
                                    i++;
                                    end = i;
                                }

                                pairs.Add(new Tuple<int, int>(start, end));
                            }
                            else
                            {
                                pairs.Add(new Tuple<int, int>(i, i));
                            }
                        }

                        if (pairs.Count >= 1)
                        {
                            foreach (var pair in pairs)
                            {
                                if (pair.Item1 == pair.Item2)
                                {
                                    mergedColumns.Add(columns.ElementAt(pair.Item1));
                                }
                                else
                                {
                                    var columnWidth = 0;
                                    var columnElements = new List<Element>();
                                    var isList = pair.Item2 - pair.Item1 + 1 >= 4 && (row.Rect.Height - row.Margin[0] - row.Margin[2]) <= 100;
                                    var newColumn = new Column();

                                    for (var i = pair.Item1; i <= pair.Item2; i++)
                                    {
                                        var column = columns.ElementAt(i);
                                        columnWidth += (int)column.Width;

                                        if (i != pair.Item2)
                                        {
                                            columnWidth += column.Margin[1];
                                        }
                                        else
                                        {
                                            newColumn.Margin[1] = column.Margin[1];
                                        }

                                        if (column.Elements.Count > 1)
                                        {
                                            throw new Exception("column elements should be 1!");
                                        }

                                        var element = column.Elements.First();
                                        element.Width = column.Width;
                                        element.Padding = element.Padding.Zip(column.Padding, (a, b) => a + b).ToArray();
                                        element.Display = "inline-block";
                                        if (i != pair.Item2)
                                        {
                                            element.Margin[1] += column.Margin[1];
                                        }

                                        columnElements.Add(element);
                                    }

                                    newColumn.Width = columnWidth;

                                    if (isList)
                                    {
                                        var elements = new List<Element>(1);
                                        var list = new List();

                                        // check if element are not texts
                                        var texts = new List<Text>();
                                        Text unify = null;
                                        foreach (var element in columnElements)
                                        {
                                            if (element.GetType() == typeof(Text))
                                            {
                                                texts.Add((Text)element);
                                            }
                                        }
                                        // merge texts to find proper font attributes
                                        if (texts.Count > 0)
                                        {
                                            unify = MergeTexts(texts.ToArray(), false);
                                        }

                                        // recalculate right margin
                                        var total = 0.0;
                                        foreach (var element in columnElements)
                                        {
                                            total += element.Margin[1];
                                        }
                                        var rightMargin = (int) Math.Floor(total / (columnElements.Count() - 1));

                                        foreach (var element in columnElements)
                                        {
                                            // replace font attributes if element is text
                                            if (unify != null && element.GetType() == typeof(Text))
                                            {
                                                CopyTextStyle(unify, element);
                                            }

                                            var item = new ListItem(element, "https://www.google.com", "blank");

                                            // move styles from element to link
                                            item.Link.Width = element.Width;
                                            item.Link.Padding = element.Padding;
                                            item.Link.Margin = element.Margin;

                                            // items should have same space in between
                                            item.Link.Margin[1] = element == columnElements.Last() ? 0 : rightMargin;
                                            // items have vertical align middle
                                            item.Link.Margin[0] = item.Link.Margin[2] = 0;
                                            element.Width = 0;
                                            element.Padding = new[] { 0, 0, 0, 0 };
                                            element.Margin = new[] { 0, 0, 0, 0 };
                                            element.Display = null;
                                            if (element.GetType() == typeof(Text))
                                            {
                                                CopyTextStyle(element, item.Link);

                                                // output text without element
                                                element.Tag = "";
                                                element.PairTag = false;
                                            }

                                            list.Items.Add(item);
                                        }

                                        elements.Add(list);

                                        newColumn.Elements = elements;
                                    }
                                    else
                                    {
                                        newColumn.Elements = columnElements;
                                    }
                                    

                                    mergedColumns.Add(newColumn);
                                }
                            }

                            row.Columns = mergedColumns;
                        }
                    }
                }
            }
        }

        public static void CopyTextStyle(Element from, Element to)
        {
            to.FontFamily = from.FontFamily;
            to.FontSize = from.FontSize;
            to.Color = from.Color;
            to.FontWeight = from.FontWeight;
            to.FontStyle = from.FontStyle;
            to.FontTransform = from.FontTransform;
        }

        /// <summary>
        ///  Fix last row in sequence which has different number of columns than rows before e.g. 3 text columns, 1. column has 5 rows, 2. column has 4 rows, 3. column has 5 rows => each column has 5 rows
        /// </summary>
        /// <param name="sectionRows"></param>
        /// <param name="fluid"></param>
        public static void FixColumnsCount(List<Element> sectionRows, bool fluid)
        {
            for (var i = 1; i < sectionRows.Count; i++)
            {
                var previousRow = (Row)sectionRows[i - 1];
                var currentRow = (Row)sectionRows[i];

                if (currentRow.Columns.Count < previousRow.Columns.Count && currentRow.Columns.Count > 0)
                {
                    // check previous columns dimensions
                    var length = previousRow.Columns.Count;
                    var maxColumnWidth = new int[length];
                    //var maxContentWidth = new int[length];
                    var leftPositionsPrevious = new int[length];
                    var bgColorPrevious = new List<int[]>(length);
                    var positionAccumulator = 0;
                    for (var j = 0; j < length; j++)
                    {
                        var column = previousRow.Columns[j];
                        bgColorPrevious.Add(column.BackgroundColor);

                        var total = (int)column.Width + column.Margin[1] + column.Margin[3] + column.Padding[3];

                        if (total > maxColumnWidth[j])
                        {
                            maxColumnWidth[j] = total;
                        }
                        //if ((int)column.Width > maxContentWidth[j])
                        //{
                        //    maxContentWidth[j] = (int)column.Width;
                        //}

                        if (j == 0)
                        {
                            leftPositionsPrevious[j] = previousRow.Margin[3];
                            positionAccumulator += previousRow.Margin[3];
                        }
                        else
                        {
                            leftPositionsPrevious[j] = positionAccumulator;
                        }

                        if (fluid)
                        {
                            leftPositionsPrevious[j] = positionAccumulator;
                        }
                        //Debug.WriteLine("left " + leftPositionsPrevious[j]);

                        positionAccumulator += total;
                    }

                    // check if columns are aligned the same way
                    var error = false;
                    var leftPositions = new int[currentRow.Columns.Count];
                    var bgColor = new List<int[]>(currentRow.Columns.Count);
                    var matched = new int[length];
                    positionAccumulator = 0;
                    for (var j = 0; j < currentRow.Columns.Count; j++)
                    {
                        var column = currentRow.Columns[j];
                        bgColor.Add(column.BackgroundColor);

                        var total = (int)column.Width + column.Margin[1] + column.Margin[3];
                        if (j == 0)
                        {
                            leftPositions[j] = currentRow.Margin[3] + column.Padding[3];
                            total += currentRow.Margin[3];
                        }
                        else
                        {
                            leftPositions[j] = column.Padding[3] + positionAccumulator;
                        }

                        if (fluid)
                        {
                            leftPositions[j] = positionAccumulator;
                            total -= currentRow.Padding[3];
                        }
                        positionAccumulator += total;

                        var match = -1;
                        for (var h = 0; h < length; h++)
                        {
                            if (matched[h] == 0 && (
                                Util.AreSame(leftPositionsPrevious[h], leftPositions[j]) ||
                                (leftPositions[j] >= leftPositionsPrevious[h] && leftPositions[j] + column.Width <= leftPositionsPrevious[h] + maxColumnWidth[h])))
                            {
                                match = h;
                                matched[h] = j + 1;
                                break;
                            }
                        }

                        // check background color of column
                        // @todo not sure, nie všetky majú nastavenú farbu a niektoré columny majú takú ako je pozadie..
                        /*if (match != -1 && bgColor[j] != bgColorPrevious[match])
                        {
                            match = -1;
                        }*/

                        // it has other background
                        if (match != -1 && fluid && column.MarginCalc[1].Contains("calc"))
                        {
                            match = -1;
                        }

                        // @todo možno tu bude treba podmienku na width pre fluid layout
                        if (match == -1 || ((int)column.Width > maxColumnWidth[match] && match != length - 1 && !column.Fluid))
                        {
                            error = true;
                        }
                    }

                    // it's okay we can split it into more columns
                    if (!error)
                    {
                        var newColumns = new List<Column>(length);

                        for (var j = 0; j < length; j++)
                        {
                            var column = new Column();
                            var previousColumn = previousRow.Columns[j];
                            column.Fluid = previousColumn.Fluid;
                            var match = matched[j] - 1;

                            // paste old content
                            if (match != -1)
                            {
                                var currentColumn = currentRow.Columns[match];
                                column.Elements = currentColumn.Elements;

                                column.Margin[3] = Math.Abs(leftPositions[match] - leftPositionsPrevious[j]);
                                column.Width = currentColumn.Width;
                                if (j != length - 1)
                                {
                                    column.Margin[1] = maxColumnWidth[j] - (int)column.Width - column.Margin[3];
                                }
                            }
                            else
                            {
                                column.Width = 0;
                               
                                if (j + 1 <= length - 1)
                                {
                                    // the next column's left position might be smaller than maxColumnWidth
                                    var nextPosition = leftPositionsPrevious[j + 1];
                                    foreach (var position in leftPositions)
                                    {
                                        if (position >= leftPositionsPrevious[j])
                                        {
                                            nextPosition = position;
                                        }
                                    }

                                    var nextPreviousPosition = leftPositionsPrevious[j + 1];

                                    if (nextPosition < nextPreviousPosition)
                                    {
                                        column.Margin[1] = maxColumnWidth[j] - (nextPreviousPosition - nextPosition);
                                    }
                                    else
                                    {
                                        column.Margin[1] = maxColumnWidth[j];
                                    }
                                    
                                }
                                else
                                {
                                    column.Margin[1] = maxColumnWidth[j];
                                }
                                
                            }

                            newColumns.Add(column);
                        }

                        currentRow.Margin[1] = previousRow.Margin[1];
                        currentRow.Margin[3] = previousRow.Margin[3];
                        currentRow.Columns = newColumns;
                    }
                }
            }

            //return sectionRows;
        }

        /// <summary>
        /// Find identical columns inside rows into one parent column e.g. Row 1 - column 6, column 6, Row 2 - column 6, column 6 => Row 1 - column 1 (which has 2 rows), column 2 (which has 2 rows)
        /// </summary>
        /// <param name="sectionRows"></param>
        /// <param name="fluid"></param>
        public static void SplitIntoColumns(List<Element> sectionRows, bool fluid)
        {
            var startSplitIndex = -1;
            var splitRowIndexes = new List<Tuple<int, int>>();
            var sectionRowsCopy = new List<Element>(sectionRows);

            var maxColumnWidth = new List<int>();
            var maxContentWidth = new List<int>();
            var maxLeftPosition = new List<int>();
            // find pair indexes of rows to be splitted e.g. 0-3, 5-10
            for (var i = 0; i < sectionRows.Count; i++)
            {
                var row = (Row)sectionRows[i];
                var count = row.Columns.Count;

                if (maxColumnWidth.Count == 0)
                {
                    maxColumnWidth = new List<int>(new int[count]);
                    maxContentWidth = new List<int>(new int[count]);
                    maxLeftPosition = new List<int>(new int[count]);
                }

                // detect column widths
                var columnWidth = new int[count];
                var contentWidth = new int[count];
                var leftPosition = new int[count];
                var positionAccumulator = 0;
                for (var j = 0; j < count; j++)
                {
                    var column = row.Columns[j];
                    var total = (int)column.Width + column.Margin[1] + column.Margin[3];
                    columnWidth[j] = total;
                    contentWidth[j] = (int)column.Width + column.Margin[3];

                    if (j == 0)
                    {
                        columnWidth[j] += row.Margin[3];
                        contentWidth[j] += row.Margin[3];
                        leftPosition[j] = 0;
                    }
                    else
                    {
                        leftPosition[j] = positionAccumulator;
                    }

                    positionAccumulator += columnWidth[j];

                    if (maxLeftPosition.Count == count)
                    {
                        // fill first values
                        if (maxColumnWidth[j] == 0)
                        {
                            maxColumnWidth[j] = columnWidth[j];
                            maxContentWidth[j] = contentWidth[j];
                            maxLeftPosition[j] = leftPosition[j];
                        }

                        // check if column fits into column
                        bool over = false;
                        if (
                            (j != count - 1 && maxLeftPosition[j + 1] != 0 && leftPosition[j] + contentWidth[j] >= maxLeftPosition[j + 1]) ||
                            /*leftPosition[j] + columnWidth[j] <= maxLeftPosition[j] + maxContentWidth[j] ||*/
                            (j != 0 && leftPosition[j] < maxLeftPosition[j - 1] + maxContentWidth[j - 1])
                        )
                        {
                            over = true;
                        }
                        if (!over)
                        {
                            // check if item is fitting into column exactly
                            if (leftPosition[j] == maxLeftPosition[j])
                            {
                                // update max content width
                                if (contentWidth[j] > maxContentWidth[j])
                                {
                                    maxContentWidth[j] = contentWidth[j];
                                    // last column
                                    if (j == count - 1 && columnWidth[j] > maxColumnWidth[j])
                                    {
                                        maxColumnWidth[j] = columnWidth[j];
                                    }
                                }

                                //continue;
                            }

                            // item is not fitting on left side
                            else if (leftPosition[j] > maxLeftPosition[j])
                            {
                                // check if column is not bigger than max column
                                var margin = leftPosition[j] - maxLeftPosition[j];
                                if (contentWidth[j] + margin > maxContentWidth[j])
                                {
                                    maxContentWidth[j] = contentWidth[j] + margin;
                                }

                            }
                            // item is not fitting on right side
                            else if (leftPosition[j] < maxLeftPosition[j])
                            {
                                if (contentWidth[j] > maxContentWidth[j])
                                {
                                    maxContentWidth[j] = contentWidth[j];
                                }
                            }


                            // check if next column is not before next max column
                            if (j != count - 1 && leftPosition[j] + columnWidth[j] < maxLeftPosition[j + 1])
                            {
                                maxColumnWidth[j] -= maxLeftPosition[j + 1] - (leftPosition[j] + columnWidth[j]);

                                var orig = maxLeftPosition[j + 1];
                                maxLeftPosition[j + 1] = maxLeftPosition[j] + maxColumnWidth[j];
                                maxColumnWidth[j + 1] += orig - maxLeftPosition[j + 1];
                                maxContentWidth[j + 1] += orig - maxLeftPosition[j + 1];
                            }
                        }
                    }
                }

                // check with previous widths
                if (i > 0)
                {
                    // merge only if we have more than 2 columns && same number of columns
                    bool merge = columnWidth.Length > 1 && columnWidth.Length == maxColumnWidth.Count;

                    // check if columns fits with previous row(s)
                    if (merge)
                    {
                        for (var j = 0; j < columnWidth.Length; j++)
                        {
                            // dont merge if previous width doesn't match with current width
                            if (contentWidth[j] > maxContentWidth[j] ||
                                leftPosition[j] < maxLeftPosition[j] ||
                                (j != columnWidth.Length - 1 && leftPosition[j] > maxLeftPosition[j + 1]) || (j != 0 && leftPosition[j] < maxLeftPosition[j - 1] + maxContentWidth[j - 1]) /*||
                                (row.Columns[j].BackgroundColor != null && row.Columns[j].Elements.Count() == 1)*/
                            )
                            {
                                merge = false;

                                break;
                            }
                        }
                    }

                    if (!merge)
                    {
                        maxColumnWidth = new List<int>(new int[count]);
                        maxContentWidth = new List<int>(new int[count]);
                        maxLeftPosition = new List<int>(new int[count]);

                        for (var j = 0; j < count; j++)
                        {
                            maxColumnWidth[j] = columnWidth[j];
                            maxContentWidth[j] = contentWidth[j];
                            maxLeftPosition[j] = leftPosition[j];
                        }
                    }

                    // we have got start of new split from index i - 1
                    if (merge && startSplitIndex == -1)
                    {
                        startSplitIndex = i - 1;
                    }
                    // we have got end of split to index i - 1
                    else if (!merge && startSplitIndex != -1)
                    {
                        splitRowIndexes.Add(new Tuple<int, int>(startSplitIndex, i - 1));
                        startSplitIndex = -1;
                    }
                }
            }

            // finish last row
            if (startSplitIndex != -1)
            {
                splitRowIndexes.Add(new Tuple<int, int>(startSplitIndex, sectionRows.Count - 1));
            }

            // split index pairs
            splitRowIndexes.Reverse();
            foreach (var pair in splitRowIndexes)
            {
                // create list with rows
                var split = new List<Element>();
                for (var i = pair.Item1; i <= pair.Item2; i++)
                {
                    split.Add(sectionRowsCopy[i]);
                }

                // remove rows that will be splitted
                sectionRows.RemoveRange(pair.Item1 + 1, pair.Item2 - pair.Item1);

                // replace with splitted content
                sectionRows[pair.Item1] = SplitRowsIntoColumns(split, fluid);
            }

            //return sectionRows;
        }

        /// <summary>
        /// Split found identical columns
        /// </summary>
        /// <param name="rows"></param>
        /// <param name="fluid"></param>
        /// <returns></returns>
        private static Row SplitRowsIntoColumns(List<Element> rows, bool fluid)
        {
            var result = new Row();
            result.MergedColumns = true;

            var count = ((Row)rows[0]).Columns.Count;
            var maxColumnWidth = new int[count];
            var maxContentWidth = new int[count];
            var maxLeftPosition = new int[count];

            // find maximum column and content widths
            foreach (Row row in rows)
            {
                var columnWidth = new int[count];
                var contentWidth = new int[count];
                var leftPosition = new int[count];
                var positionAccumulator = 0;
                for (var j = 0; j < count; j++)
                {
                    var column = row.Columns[j];
                    var total = (int)column.Width + column.Margin[1] + column.Margin[3];
                    columnWidth[j] = total;
                    contentWidth[j] = (int)column.Width + column.Margin[3];

                    if (j == 0)
                    {
                        if (!fluid)
                        {
                            columnWidth[j] += row.Margin[3];
                            contentWidth[j] += row.Margin[3];
                        }
                    
                        leftPosition[j] = 0;
                    }
                    else
                    {
                        leftPosition[j] = positionAccumulator;
                    }

                    positionAccumulator += columnWidth[j];

                    if (maxColumnWidth[j] == 0)
                    {
                        maxColumnWidth[j] = columnWidth[j];
                        maxContentWidth[j] = contentWidth[j];
                        maxLeftPosition[j] = leftPosition[j];
                    }


                    // check if item is fitting into column exactly
                    if (leftPosition[j] == maxLeftPosition[j])
                    {
                        // update max content width
                        if (contentWidth[j] > maxContentWidth[j])
                        {
                            maxContentWidth[j] = contentWidth[j];
                            // last column
                            if (j == count - 1 && columnWidth[j] > maxColumnWidth[j])
                            {
                                maxColumnWidth[j] = columnWidth[j];
                            }
                        }

                        //continue;
                    }

                    // item is not fitting on left side
                    else if (leftPosition[j] > maxLeftPosition[j])
                    {
                        // check if column is not bigger than max column
                        var margin = leftPosition[j] - maxLeftPosition[j];
                        if (contentWidth[j] + margin > maxContentWidth[j])
                        {
                            maxContentWidth[j] = contentWidth[j] + margin;
                        }

                    }
                    // item is not fitting on right side
                    else if (leftPosition[j] < maxLeftPosition[j])
                    {
                        if (contentWidth[j] > maxContentWidth[j])
                        {
                            maxContentWidth[j] = contentWidth[j];
                        }
                    }


                    // check if next column is not before next max column
                    if (j != count - 1 && leftPosition[j] + columnWidth[j] < maxLeftPosition[j + 1])
                    {
                        maxColumnWidth[j] -= maxLeftPosition[j + 1] - (leftPosition[j] + columnWidth[j]);

                        var orig = maxLeftPosition[j + 1];
                        maxLeftPosition[j + 1] = maxLeftPosition[j] + maxColumnWidth[j];
                        maxColumnWidth[j + 1] += orig - maxLeftPosition[j + 1];
                        maxContentWidth[j + 1] += orig - maxLeftPosition[j + 1];
                    }
                }
            }

            result.Margin[0] = rows.First().Margin[0];
            result.Margin[2] = rows.Last().Margin[2];

            // create columns
            var columns = new List<Column>(count);
            var calcMargin = ((Row)rows[0]).Columns[0].MarginCalc[1].Contains("calc");
            for (var i = 0; i < count; i++)
            {
                var column = new Column();
                column.Width = maxContentWidth[i];
                
                column.Fluid = fluid;
                column.Margin[1] = maxColumnWidth[i] - (int)column.Width;

                if (fluid && i == 0)
                {
                    column.Margin[3] = rows.First().Margin[3];
                    column.Margin[1] -= rows.First().Margin[3];
                }

                columns.Add(column);
            }

            // fluid layout with calc margin needs to adjusted
            if (calcMargin && fluid)
            {
                var total = 0.0;
                foreach (var column in columns)
                {
                    total += (int)column.Width;
                }

                foreach (var column in columns)
                {
                    var percents = Math.Round(column.Width / total * 100);
                    column.MarginCalc[1] = column.MarginCalc[3] = $"calc(({percents}% - {column.Width}px) / 2)";
                    column.Fluid = false;
                }
                
            }

            // fill columns
            for (var k = 0; k < rows.Count; k++)
            {
                var row = (Row)rows[k];
                var positionAccumulator = 0;
                for (var i = 0; i < row.Columns.Count; i++)
                {
                    var column = row.Columns[i];
                    var leftPosition = positionAccumulator + column.Margin[3];
                    if (i == 0)
                    {
                        leftPosition += row.Margin[3];
                        positionAccumulator += row.Margin[3];
                    }
                    positionAccumulator += (int)column.Width + column.Margin[1] + column.Margin[3];

                    var elementsAccumulator = 0;
                    for (var j = 0; j < column.Elements.Count; j++)
                    {
                        var element = column.Elements[j];
                        elementsAccumulator += (int)element.Width + element.Padding[1] + element.Padding[3] + element.Margin[1] + element.Margin[3];
                        if (j == 0 || element.Width != 0 && elementsAccumulator <= columns[i].Width)
                        {
                            // texts need only padding (because of margin collapsing)
                            if (element.GetType() == typeof(Text))
                            {
                                element.Padding = element.Padding.Zip(element.Margin, (a, b) => a + b).ToArray();
                                element.Margin = new int[] { 0, 0, 0, 0 };

                                element.Padding[2] += column.Padding[2];

                                // first row has already row margin
                                if (k != 0)
                                {
                                    element.Padding[0] += column.Padding[0];
                                    element.Padding[0] += row.Margin[0];
                                }
                                else
                                {
                                    element.Padding[0] += column.Padding[0];
                                }
                            }
                            else
                            {
                                element.Margin[2] += column.Padding[2];

                                // first row has already row margin
                                if (k != 0)
                                {
                                    element.Padding[0] += column.Padding[0];
                                    element.Margin[0] += row.Margin[0];
                                }
                                else
                                {
                                    element.Padding[0] += column.Padding[0];
                                }
                            }
                        }

                        if (column.BackgroundColor != null)
                        {
                            element.BackgroundColor = column.BackgroundColor;
                        }

                        //element.Margin[3] += row.Columns[i].Margin[3];

                        if (!fluid)
                        {
                            if (i == 0)
                            {
                                element.Margin[3] += leftPosition;
                            }
                            else
                            {
                                element.Margin[3] += leftPosition - maxLeftPosition[i];
                            }
                        }

                        columns[i].Elements.Add(element);
                    }
                }
              }

              // set columns
              result.Columns = columns;

              return result;
         }
    }
}
