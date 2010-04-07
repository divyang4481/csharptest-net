#region Copyright 2008 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Reflection;
using Microsoft.Build.BuildEngine;

namespace CSharpTest.Net.CSBuild.Implementation
{
	[Serializable]
	[System.Diagnostics.DebuggerDisplay("{Assembly}")]
	class ProjectRef
	{
		static char[] DOT = new char[] { '.' };

		public string RefType = null;
		public string Condition = null;
		public Guid? Guid = null;
		public string Output = null;
		public string Project = null;
		public AssemblyName Assembly = null;
		public string RequiresVersion = null;
		public bool CopyLocal = false;
		public bool SpecificVersion = false;

		//state tracking:
		public bool Resolved = false;

		public override int GetHashCode()
		{
			return RefType.GetHashCode() ^
				Guid.GetHashCode() ^
				String.Format("{0}", Condition).GetHashCode() ^
				Assembly.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			ProjectRef other = obj as ProjectRef;
			if( other == null ) return false;

			return (
				GetHashCode() == other.GetHashCode() &&
				RefType == other.RefType &&
				Guid == other.Guid &&
				Condition == other.Condition &&
				Assembly == other.Assembly
				);
		}

		public ProjectRef(BuildItem item, TranslatePath fnTranslate)
		{
			this.RefType = String.Format("{0}", Check.NotNull(item).Name);
			if (item.GetMetadata("Private") == "True")
				CopyLocal = true;
			if (item.GetMetadata("SpecificVersion") == "True")
				SpecificVersion = true;
			if (!String.IsNullOrEmpty(item.Condition))
				this.Condition = item.Condition;
			if (RefType == "ProjectReference")
				this.FromProjectReference(item, Check.NotNull(fnTranslate));
			else if (RefType == "Reference")
				this.FromFileReference(item, Check.NotNull(fnTranslate));
			else
				throw new ApplicationException("Unkown reference type " + RefType);
		}

		private void FromFileReference(BuildItem bi, TranslatePath fnTranslate)
		{
			if (!String.IsNullOrEmpty(bi.Include))
			{
				this.Assembly = new AssemblyName(bi.Include);
				SpecificVersion |= this.Assembly.Version != null;
			}
			if (!String.IsNullOrEmpty(bi.GetMetadata("HintPath")))
			{
				string path = bi.GetMetadata("HintPath");
				fnTranslate(ref path);//< output file doesn't nessessarily exist
				this.Output = path;
			}
			this.RequiresVersion = bi.GetMetadata("RequiredTargetFramework");
		}

		private void FromProjectReference(BuildItem bi, TranslatePath fnTranslate)
		{
			if (!String.IsNullOrEmpty(bi.Include))
			{
				string path = bi.Include;
				if (fnTranslate(ref path))
					this.Project = path;
			}
			if (!String.IsNullOrEmpty(bi.GetMetadata("Project")))
				this.Guid = new Guid(bi.GetMetadata("Project"));
			if (!String.IsNullOrEmpty(bi.GetMetadata("Name")))
				this.Assembly = new AssemblyName(bi.GetMetadata("Name"));
		}

		public delegate bool TranslatePath(ref string path);
	}
}
