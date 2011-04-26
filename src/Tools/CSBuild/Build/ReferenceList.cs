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
using Microsoft.Build.BuildEngine;
using System.Reflection;

namespace CSharpTest.Net.CSBuild.Build
{
    class ReferenceList : IEnumerable<ReferenceInfo>
    {
        readonly Project _project;

        public ReferenceList(Project project)
        {
            _project = project;
		}

		public ReferenceInfo Add(AssemblyName asmName)
		{
			BuildItem item = _project.AddNewItem(ReferenceType.Reference.ToString(), asmName.ToString());
			return new ReferenceInfo(_project, item);
		}

		public ReferenceInfo Add(AssemblyName asmName, string fqFilePath)
		{
			ReferenceInfo reference = Add(asmName);
			reference.HintPath = fqFilePath;
			reference.SpecificVersion = false;
			reference.CopyLocal = false;
			return reference;
		}

		public bool Remove(ReferenceInfo reference)
		{
            _project.RemoveItem(reference.BuildItem);
            Log.Verbose("Removed reference to: {0}", reference.Assembly);
            return true;
        }

        public IEnumerator<ReferenceInfo> GetEnumerator()
        {
            //First we enum project-to-project references, I hate these, but alot of ID10Ts still use them ;)
            foreach (BuildItem bi in _project.GetEvaluatedItemsByName("ProjectReference").ToArray())
                yield return new ReferenceInfo(_project, bi);

            //Now we enum all the real Reference items...
            foreach (BuildItem bi in _project.GetEvaluatedItemsByName("Reference").ToArray())
                yield return new ReferenceInfo(_project, bi);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return this.GetEnumerator(); }
    }
}
