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
        // @todo metóda na odstraňovanie zbytočných elementov

        public static List<Row> FixColumnsCount(List<Row> sectionRows, bool fluid)
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
                    var positionAccumulator = 0;
                    for (var j = 0; j < length; j++)
                    {
                        var column = previousRow.Columns[j];
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
                    var matched = new int[length];
                    positionAccumulator = 0;
                    for (var j = 0; j < currentRow.Columns.Count; j++)
                    {
                        var column = currentRow.Columns[j];
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
                                (leftPositions[j] > leftPositionsPrevious[h] && leftPositions[j] + column.Width <= leftPositionsPrevious[h] + maxColumnWidth[h])))
                            {
                                match = h;
                                matched[h] = j + 1;
                                break;
                            }
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
                                column.Width = Math.Max(maxColumnWidth[j] - column.Margin[3], currentColumn.Width);
                            }
                            else
                            {
                                column.Width = maxColumnWidth[j];
                            }

                            newColumns.Add(column);
                        }

                        currentRow.Padding[1] = previousRow.Padding[1];
                        currentRow.Padding[3] = previousRow.Padding[3];
                        currentRow.Columns = newColumns;
                    }
                }
            }

            return sectionRows;
        }

        public static List<Row> SplitIntoColumns(List<Row> sectionRows)
        {
            var lastColumnWidths = new List<int>();
            var startSplitIndex = -1;
            var splitRowIndexes = new List<Tuple<int, int>>();
            var sectionRowsCopy = new List<Row>(sectionRows);

            // find pair indexes of rows to be splitted e.g. 0-3, 5-10
            for (var i = 0; i < sectionRows.Count; i++)
            {
                var row = sectionRows[i];

                //Debug.WriteLine("row " + i);

                // detect column widths
                var columnWidths = new int[row.Columns.Count - 1];
                for (var j = 0; j < columnWidths.Length; j++)
                {
                    var column = row.Columns[j];
                    var total = (int)column.Width + column.Margin[1] + column.Margin[3];
                    columnWidths[j] = total;
                    //Debug.WriteLine("column width " + total);
                }

                // check with previous widths
                if (lastColumnWidths.Count != 0)
                {
                    // merge only if we have more than 2 columns
                    bool merge = columnWidths.Length != 0;

                    // check if we have same number of columns
                    if (columnWidths.Length != lastColumnWidths.Count)
                    {
                        merge = false;
                    }
                    else
                    {
                        for (var j = 0; j < columnWidths.Length; j++)
                        {
                            // dont merge if previous width doesn't match with current width
                            if (!Util.AreSame(columnWidths[j], lastColumnWidths[j]))
                            {
                                merge = false;

                                //Debug.WriteLine("not same widths " + i);

                                break;
                            }
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

                lastColumnWidths.Clear();
                lastColumnWidths = columnWidths.ToList();
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
                sectionRows[pair.Item1] = SplitRowsIntoColumns(split);
            }

            return sectionRows;
        }

        private static Row SplitRowsIntoColumns(List<Row> rows)
        {
            var result = new Row(1);
            var count = rows[0].Columns.Count;
            var maxColumnWidths = new int[count];
            var maxContentWidths = new int[count, 2];
            var lowestLeftPadding = int.MaxValue;

            // find maximum column and content widths
            foreach (var row in rows)
            {
                for (var j = 0; j < count; j++)
                {
                    var column = row.Columns[j];
                    var total = (int) column.Width + column.Margin[1];

                    if (total > maxColumnWidths[j])
                    {
                        maxColumnWidths[j] = total;
                    }
                    if ((int)column.Width > maxContentWidths[j, 0])
                    {
                        maxContentWidths[j, 0] = (int)column.Width;
                        maxContentWidths[j, 1] = column.Margin[1];
                    }
                }

                if (row.Padding[3] < lowestLeftPadding)
                {
                    lowestLeftPadding = row.Padding[3];
                }
            }

            result.Padding[2] = rows.Last().Padding[2];

            // create columns
            var columns = new List<Column>(count);
            var fluid = rows[0].Columns[0].Fluid;
            for (var i = 0; i < count; i++)
            {
                var column = new Column(1);
                column.Width = maxContentWidths[i, 0];
                column.Margin[1] = maxContentWidths[i, 1] - (maxColumnWidths[i] - (maxContentWidths[i, 0] + maxContentWidths[i, 1]));
                column.Fluid = fluid;

                if (i == 0)
                {
                    column.Margin[3] = lowestLeftPadding;
                }

                columns.Add(column);
            }

            // fill columns
            foreach (var row in rows)
            {
                for (var i = 0; i < row.Columns.Count; i++)
                {
                    for (var j = 0; j < row.Columns[i].Elements.Count; j++)
                    {
                        var element = row.Columns[i].Elements[j];
                        if (j == 0)
                        {
                            element.Padding[0] += row.Padding[0];
                            element.Margin[3] += row.Columns[i].Margin[3];
                        }
                        element.Padding[3] = row.Padding[3] - lowestLeftPadding;
                        columns[i].Elements.Add(element);

                        // check if padding + width is not greater than max content width
                        if (element.Padding[3] + row.Columns[i].Width > maxContentWidths[i, 0])
                        {
                            maxContentWidths[i, 0] = element.Padding[3] + (int) row.Columns[i].Width;
                            columns[i].Width = maxContentWidths[i, 0];
                            columns[i].Margin[1] = maxContentWidths[i, 1] - (maxColumnWidths[i] - (maxContentWidths[i, 0] - maxContentWidths[i, 1]));
                        }
                    }
                }
            }

            // set columns
            result.Columns = columns;

            return result;
        }
    }
}
