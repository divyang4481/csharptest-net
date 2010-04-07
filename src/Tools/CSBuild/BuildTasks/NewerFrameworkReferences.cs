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
using System.Collections.Generic;
using CSharpTest.Net.CSBuild.Build;

namespace CSharpTest.Net.CSBuild.BuildTasks
{
	[Serializable]
	class NewerFrameworkReferences : BuildTask
	{
		protected override int Run(BuildEngine engine)
		{
			FrameworkVersions currentTools = engine.Framework;
			foreach (ProjectInfo item in engine.Projects)
			{
				FrameworkVersions myFramework = currentTools;

				string projFramework = item.Properties[MSProp.TargetFrameworkVersion];
				if (!String.IsNullOrEmpty(projFramework))
					myFramework = (FrameworkVersions)Enum.Parse(typeof(FrameworkVersions), projFramework.Replace(".", ""));

				foreach (ReferenceInfo r in item.References)
				{
					if (r.RequiresVersion != null)
					{
						if (r.RequiresVersion.Value > myFramework)
							item.References.Remove(r);
					}
				}
			}
			return 0;
		}
	}
}
