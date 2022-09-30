using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WorldMiner.Models
{
    internal struct Sign
    {
        public string[] Lines { get; }
        public int X { get; }
        public int Y { get; }
        public int Z { get; }

        public Sign(string[] lines, int x, int y, int z)
        {
            Lines = lines;
            X = x;
            Y = y;
            Z = z;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            
            builder.AppendLine($"Position: {X} {Y} {Z}");
            builder.AppendLine("Text:");

            builder.AppendLine(String.Join(Environment.NewLine, Lines));

            return builder.ToString();
        }
    }
}
