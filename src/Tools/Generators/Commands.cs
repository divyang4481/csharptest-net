#region Copyright 2010 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;
using CSharpTest.Net.Commands;
using CSharpTest.Net.Generators.ResX;

namespace CSharpTest.Net.Generators
{
    public static class Commands
    {
        [Command("Config", Description = "Writes a default CmdTool.config for this toolset to the console.")]
        public static void Config()
        {
            Console.WriteLine(Properties.Resources.CmdTool_csproj);
        }

        [Command("ResX", Description = "Generates strongly typed resources from .resx files with formatting and excpetions.")]
        public static void ResX(
            [Argument("input", "in", Description = "The input resx file to generate resources from.")]
            string inputFile,
            [Argument("namespace", "ns", Description = "The resulting namespace to use when generating resource classes.")]
            string nameSpace,
            [Argument("class", "c", Description = "The name of the containing class to use for the generated resources.")]
            string className,
            [Argument("resxNamespace", "rxns", Description = "The namespace that the resource file will be embeded with.", DefaultValue = null)]
            string resxNamespace,
            [Argument("public", Description = "Determines if the output resource class should be public or internal.", DefaultValue = false)]
            bool makePublic,
            [Argument("partial", Description = "Markes generated resource classes partial.", DefaultValue = true)]
            bool makePartial,
            [Argument("test", Description = "Attempts to run String.Format over all formatting strings.", DefaultValue = true)]
            bool testFormat
            )
        {
            ResxGenWriter writer = new ResxGenWriter(inputFile, nameSpace, resxNamespace ?? nameSpace, makePublic, makePartial, className);
            writer.Write(Console.Out);

            if (testFormat && !writer.Test(Console.Error))
                throw new ApplicationException("One or more String.Format operations failed.");
        }
    }
}
