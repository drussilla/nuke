// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using static Nuke.Common.Constants;

namespace Nuke.Common.Execution
{
    internal class HandleShellCompletionAttribute : BuildExtensionAttributeBase, IOnBuildCreated
    {
        public void OnBuildCreated(NukeBuild build, IReadOnlyCollection<ExecutableTarget> executableTargets)
        {
            SchemaUtility.WriteBuildSchemaFile(build);
            SchemaUtility.WriteDefaultParametersFile();

            if (EnvironmentInfo.GetParameter<bool>(CompletionParameterName))
                Environment.Exit(exitCode: 0);
        }
    }
}
