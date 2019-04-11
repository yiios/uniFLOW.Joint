using System;
using System.Collections.Generic;
using System.Text;

namespace Joint.Core.Govern
{
    public class ConfigCatalog
    {
        public class Section
        {
            public string Label { get; set; }
            public Item[] Items { get; set; }
        }
        public class Item
        {
            public string Key { get; set; }
            public string Label { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
            public string DefaultText { get; set; }
            public bool Advanced { get; set; }
        }

        public ModuleInfo ModuleInfo { get; set; }
        public Section[] Sections { get; set; }

        public static void Test()
        {
        }
    }
}
