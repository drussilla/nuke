// Copyright 2021 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Linq;

namespace Nuke.SourceGenerators
{
    public static class StringExtensions
    {
        public static string TrimStart(this string str, string trim)
        {
            return str.StartsWith(trim) ? str.Substring(trim.Length) : str;
        }

        public static string Concat(this string str, string concat)
        {
            return str + concat;
        }
    }
}
