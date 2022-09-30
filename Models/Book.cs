using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldMiner.Models
{
    internal struct Book
    {
        public string Title { get; }
        public string Author { get; }
        public string[] Pages { get; }

        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public Book(string title, string author, string[] pages, int x, int y, int z)
        {
            Title = title;
            Author = author;
            Pages = pages;
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendLine($"Title: {Title}");
            builder.AppendLine($"Author: {Author}");
            builder.AppendLine($"Position: {X} {Y} {Z}");
            
            foreach(var it in Pages.Select((page, i) => new {Page = page, Index = i}))
            {
                builder.AppendLine($"Page {it.Index}:");
                builder.AppendLine(it.Page);
            }

            return builder.ToString();
        }
    }
}
