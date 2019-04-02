using System;
using System.Text.RegularExpressions;

namespace Joint.Core.Govern
{
    public class ModuleIdentifier
    {
        public string Name { get; set; }
        public Version Version { get; set; }
        public override string ToString() => Name + "-" + Version;

        static Regex re = new Regex(@"^[-_.a-zA-Z0-9]*$");
        public static bool TryParse(string text, out ModuleIdentifier identifier)
        {
            identifier = null;

            if (!re.IsMatch(text)) return false;
            var lastdash = text.LastIndexOf('-');
            if (lastdash <= 0 || lastdash == text.Length - 1) return false;
            if (!Version.TryParse(text.Substring(lastdash + 1), out var version)) return false;
            identifier = new ModuleIdentifier
            { Name = text.Substring(0, lastdash), Version = version, };

            return true;
        }

        
    }
}
