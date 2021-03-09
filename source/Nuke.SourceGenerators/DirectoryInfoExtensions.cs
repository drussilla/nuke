// Copyright 2021 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.IO;
using System.Linq;

namespace Nuke.SourceGenerators
{
    public static class DirectoryInfoExtensions
    {
        public static DirectoryInfo FindParentDirectory(this DirectoryInfo start, Func<DirectoryInfo, bool> predicate)
        {
            return start
                .DescendantsAndSelf(x => x.Parent)
                .Where(x => x != null)
                .FirstOrDefault(predicate);
        }
    }
}
