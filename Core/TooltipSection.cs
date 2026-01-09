using System.Collections.Generic;
using Terraria;

namespace AbaAbilities.Core
{
    public class TooltipSection
    {
        public string Header { get; set; }

        public IEnumerable<string> Body { get; set; }

        public string Footer { get; set; }

        public TooltipSection(string header = null, IEnumerable<string> body = null, string footer = null)
        {
            Header = header;
            Body = body ?? new string[0];
            Footer = footer;
        }
    }
}
