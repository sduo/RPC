using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace RPC.Logger.Configuration
{
    public class Mask : Match
    {
        internal string? Regex { get; set; } = null;
        internal string? Replacement { get; set; } = null;
        internal RegexOptions Options { get; set; } = RegexOptions.IgnoreCase | RegexOptions.Singleline;
        internal string GetMask(string input)
        {
            if (string.IsNullOrEmpty(Regex)) { return input; }
            if (string.IsNullOrEmpty(Replacement)) { return "*"; }
            return System.Text.RegularExpressions.Regex.Replace(input, Regex, Replacement, Options);
        }
    }
}
