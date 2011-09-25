#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Collections.Generic;
using System.Xml.Serialization;

namespace CSharpTest.Net.CSBuild.Build
{
    enum ProjectType 
    { 
        WinExe = 1, 
        Exe = 2, 
        Library = 3 
    }

	enum MSProp
	{
		ProjectGuid,
		AssemblyName,
		ProjectDir,
		OutputType,
		OutDir,
		OutputPath,
		TargetFileName,
		DefineConstants,
		SolutionDir,
		TargetFrameworkVersion,
		Configuration,
		Platform,
		NoStdLib,
		AssemblySearchPaths,
		IntermediateOutputPath,
	}

    public enum ReferenceType
    {
        Undefined = 0,
        ProjectReference = 1,
        Reference = 2,
    }

    public enum FrameworkVersions
    {
        [XmlEnum("v2.0")]
        v20 = 20,
        [XmlEnum("v3.0")]
        v30 = 30,
        [XmlEnum("v3.5")]
        v35 = 35,
        [XmlEnum("v4.0")]
        v40 = 40,
    }

    public enum BuildPlatforms
    {
        [XmlEnum("AnyCPU")]
        AnyCPU = 0,
        [XmlEnum("x86")]
        x86 = 1,
        [XmlEnum("Itanium")]
        Itanium = 2,
        [XmlEnum("x64")]
        x64 = 3,
    }

}
