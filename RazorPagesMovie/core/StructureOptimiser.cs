using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model.elements.grid;

namespace RazorPagesMovie.core
{
    public static class StructureOptimiser
    {
        public static void OptimiseColors(List<Row> rows)
        {
            foreach (var row in rows)
            {

            }
        }

        public static void FixColumnsCount(List<Row> sectionRows, bool fluid)
        {
            for (var i = 1; i < sectionRows.Count; i++)
            {
                var previousRow = sectionRows[i - 1];
                var currentRow = sectionRows[i];

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
                            leftPositionsPrevious[j] = previousRow.Padding[3];
                            positionAccumulator += previousRow.Padding[3];
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
                            leftPositions[j] = currentRow.Padding[3] + column.Padding[3];
                            total += currentRow.Padding[3];
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
                            var column = new Column(1);
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
                                    var nextPosition = 0;
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

                        currentRow.Padding[1] = previousRow.Padding[1];
                        currentRow.Padding[3] = previousRow.Padding[3];
                        currentRow.Columns = newColumns;
                    }
                }
            }

            //return sectionRows;
        }

        public static void SplitIntoColumns(List<Row> sectionRows, bool fluid)
        {
            var startSplitIndex = -1;
            var splitRowIndexes = new List<Tuple<int, int>>();
            var sectionRowsCopy = new List<Row>(sectionRows);

            var maxColumnWidth = new List<int>();
            var maxContentWidth = new List<int>();
            var maxLeftPosition = new List<int>();
            // find pair indexes of rows to be splitted e.g. 0-3, 5-10
            for (var i = 0; i < sectionRows.Count; i++)
            {
                var row = sectionRows[i];
                var count = row.Columns.Count;
                //if (count == 1)
                //{
                //    continue;
                //}

                //Debug.WriteLine("row " + i);
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
                        columnWidth[j] += row.Padding[3];
                        contentWidth[j] += row.Padding[3];
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
                                (j != columnWidth.Length - 1 && leftPosition[j] > maxLeftPosition[j + 1]) || (j != 0 && leftPosition[j] < maxLeftPosition[j - 1] + maxContentWidth[j - 1])
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
                var split = new List<Row>();
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

        private static Row SplitRowsIntoColumns(List<Row> rows, bool fluid)
        {
            var result = new Row(1);
            var count = rows[0].Columns.Count;
            var maxColumnWidth = new int[count];
            var maxContentWidth = new int[count];
            var maxLeftPosition = new int[count];

            // find maximum column and content widths
            foreach (var row in rows)
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
                        columnWidth[j] += row.Padding[3];
                        contentWidth[j] += row.Padding[3];
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

            result.Padding[2] = rows.Last().Padding[2];

            // create columns
            var columns = new List<Column>(count);
            var calcMargin = rows[0].Columns[0].MarginCalc[1].Contains("calc");
            for (var i = 0; i < count; i++)
            {
                var column = new Column(1);
                column.Width = maxContentWidth[i];
                
                column.Fluid = fluid;
                column.Margin[1] = maxColumnWidth[i] - (int)column.Width;

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
            foreach (var row in rows)
            {
                var positionAccumulator = 0;
                for (var i = 0; i < row.Columns.Count; i++)
                {
                    var column = row.Columns[i];
                    var leftPosition = positionAccumulator + column.Margin[3];
                    if (i == 0)
                    {
                        leftPosition += row.Padding[3];
                        positionAccumulator += row.Padding[3];
                    }
                    positionAccumulator += (int)column.Width + column.Margin[1] + column.Margin[3];

                    var elementsAccumulator = 0;
                    for (var j = 0; j < column.Elements.Count; j++)
                    {
                        var element = column.Elements[j];
                        elementsAccumulator += (int)element.Width + element.Padding[1] + element.Padding[3] + element.Margin[1] + element.Margin[3];
                        if (j == 0 || element.Width != 0 && elementsAccumulator <= columns[i].Width)
                        {
                            element.Margin[0] += row.Padding[0] + column.Padding[0];
                            element.Padding[2] += column.Padding[2];
                        }

                        //element.Margin[3] += row.Columns[i].Margin[3];
                        if (i == 0)
                        {
                            element.Padding[3] += leftPosition;
                        }
                        else
                        {
                            element.Padding[3] += leftPosition - maxLeftPosition[i];
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
