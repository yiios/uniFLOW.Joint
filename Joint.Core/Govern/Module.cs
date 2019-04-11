using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Joint.Core.Govern
{
    public class ModuleInfo
    {
        public ModuleIdentifier Identifier { get; set; }
    }
    public struct ModuleIdentifier : IComparable<ModuleIdentifier>
    {
        public string Name { get; set; }
        public Version Version { get; set; }

        public override string ToString() => Name + "-" + Version;
        public override int GetHashCode()
            => Name.ToLowerInvariant().GetHashCode() ^ Version.GetHashCode();
        public override bool Equals(object obj)
        {
            if (!(obj is ModuleIdentifier other)) return false;
            return CompareTo(other) == 0;
        }

        static Regex re = new Regex(@"^[-_.a-zA-Z0-9]*$");
        public static bool TryParse(string text, out ModuleIdentifier identifier)
        {
            identifier = new ModuleIdentifier();

            if (!re.IsMatch(text)) return false;
            var lastdash = text.LastIndexOf('-');
            if (lastdash <= 0 || lastdash == text.Length - 1) return false;
            if (!Version.TryParse(text.Substring(lastdash + 1), out var version)) return false;
            identifier = new ModuleIdentifier
            { Name = text.Substring(0, lastdash), Version = version, };

            return true;
        }

        public int CompareTo(ModuleIdentifier other)
        {
            var res = string.Compare(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            if (res != 0) return res;
            return Version.CompareTo(other.Version);
        }
    }
}
