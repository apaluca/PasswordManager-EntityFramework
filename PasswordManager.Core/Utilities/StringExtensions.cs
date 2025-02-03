using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PasswordManager.Core.Utilities
{
        public static class StringExtensions
        {
                public static string SplitCamelCase(this string input)
                {
                        return System.Text.RegularExpressions.Regex.Replace(input,
                            "([A-Z])", " $1",
                            System.Text.RegularExpressions.RegexOptions.Compiled).Trim();
                }
        }
}
