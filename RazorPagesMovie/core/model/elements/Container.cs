﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RazorPagesMovie.core.model.elements.grid;

namespace RazorPagesMovie.core.model.elements
{
    public class Container : Element
    {
        //public Layout.LayoutType LayoutType { get; set; }
        // @todo určite to nebude duplicitne aj tu aj v section
        public Layout Layout { get; set; }
        public List<Row> Rows { get; set; }
        public Container(int id, Layout layout)
        {
            Id = id;
            Rows = new List<Row>();
            Layout = layout;
        }

        public override string StartTag()
        {
            string type;
            string width;

            if (Layout.Type == Layout.LayoutType.Centered)
            {
                type = "container";
                width = (int) Layout.Width + "px";
            }
            else
            {
                type = "container-fluid";
                width = "100%";
            }

            return $"<div class=\"{type}\" style=\"width:{width};\">";
        }

        public override string Content()
        {
            var output = "";
            foreach (var row in Rows)
            {
                output += row.StartTag();
                output += row.Content();
                output += row.EndTag();
            }
            return output;
        }

        public override string EndTag()
        {
            return "</div>";
        }
    }
}
