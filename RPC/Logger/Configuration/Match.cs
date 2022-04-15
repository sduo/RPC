using System.Diagnostics.CodeAnalysis;

namespace RPC.Logger.Configuration
{
    public class Match
    {
        [NotNull]
        public string? Key { get; set; }

        public bool IgnoreCase { get; set; } = true;

        public bool IsMatch(string x)
        {
            return string.Equals(x, Key, IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }
    }
}
