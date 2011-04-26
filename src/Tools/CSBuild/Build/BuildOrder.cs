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

namespace CSharpTest.Net.CSBuild.Build
{
	[System.Diagnostics.DebuggerDisplay("{Enumerate()}")]
    class BuildOrder
    {
        readonly ProjectList _projects;
        readonly List<ProjectInfo> _buildList;

        private BuildOrder(ProjectList projects)
        {
            _projects = projects;
            _buildList = new List<ProjectInfo>();
        }
        public BuildOrder(ProjectList projects, IEnumerable<string> buildList)
            : this(projects)
        {
            PrepareBuild(FileToProject(buildList));
        }

        private IEnumerable<ProjectInfo> FileToProject(IEnumerable<string> projectFiles)
        {
            foreach (string file in projectFiles)
            {
                ProjectInfo pi;
                if (_projects.TryGetProject(file, out pi))
                    yield return pi;
            }
        }

        class WorkItem
        {
            public WorkItem(ProjectInfo proj) 
            {
                Project = proj;
                FullName = proj.ProjectFile;
                Depends = new List<string>();
            }

            public readonly string FullName;
            public readonly ProjectInfo Project;
            public readonly List<String> Depends;
            public int Counter;
        }

		int SortWork(KeyValuePair<string, WorkItem> x, KeyValuePair<string, WorkItem> y)
		{ return StringComparer.OrdinalIgnoreCase.Compare(x.Key, y.Key); }

        int PrepareBuild(IEnumerable<ProjectInfo> projectFiles)
        {
            Dictionary<string, WorkItem> working = new Dictionary<string, WorkItem>(StringComparer.OrdinalIgnoreCase);

            ProjectInfo refProj;
            foreach (ProjectInfo pi in projectFiles)
            {
                ReferenceInfo lastRef = null;
                try
                {
                    WorkItem item = new WorkItem(pi);

                    item.Depends.AddRange(pi.Dependencies);
                    foreach (ReferenceInfo r in pi.References)
                    {
                        lastRef = r;
                        if (_projects.TryGetProject(r, out refProj))
                            item.Depends.Add(refProj.ProjectFile);
                    }
                    working.Add(pi.ProjectFile, item);
                }
                catch (Exception e)
                {
                    throw new ApplicationException(
                        String.Format("Unable to parse references from {0}, reference={1}, error={2}",
                            pi.ProjectFile, lastRef, e.Message), e);
                }
            }

            using (Log.Start("Sorting {0} projects", _projects.Count))
            {
                int lastCount = 0;
                while (working.Count > 0 && lastCount != working.Count)
                {
                    lastCount = working.Count;//every loop should peel at least one project out...

					List<KeyValuePair<string, WorkItem>> loop = new List<KeyValuePair<string, WorkItem>>(working);
					loop.Sort(SortWork);

					foreach (KeyValuePair<string, WorkItem> item in loop)
                    {
                        bool canBuild = true;
                        while(item.Value.Depends.Count > 0)
                        {
                            string fileref = item.Value.Depends[0];
                            if (!working.ContainsKey(fileref))
                                item.Value.Depends.RemoveAt(0);
                            else
                            {
                                canBuild = false;
                                break;
                            }
                        }

                        if (canBuild)
                        {
                            _buildList.Add(item.Value.Project);
                            working.Remove(item.Key);
                        }
                    }
                }

                if (working.Count > 0)
                {
                    try { ReportCycles(working); } catch { }
                    throw new ApplicationException(String.Format("Aborted {0} Projects: Circular reference(s) in projects.", working.Count));
                }

                return 0;
            }
        }

        #region void ReportCycles(...)
        DateTime cyclicReportStart;
        void ReportCycles(Dictionary<string, WorkItem> working)
        {
            cyclicReportStart = DateTime.Now;
            Console.Error.WriteLine();
            Console.Error.WriteLine("Circular reference in:");

            foreach (KeyValuePair<string, WorkItem> item in working)
            {
                Stack<WorkItem> parents = new Stack<WorkItem>();
                DoesContain(parents, item.Value, working);
            }
            List<WorkItem> sorted = new List<WorkItem>(working.Values);
            sorted.Sort(delegate(WorkItem a, WorkItem b) { return b.Counter.CompareTo(a.Counter); });

			int lastCounter = sorted[0].Counter;
			foreach (WorkItem wi in sorted)
            {
				if (wi.Counter != lastCounter)
					break;
                Console.Error.WriteLine("  {0}", wi.FullName);
            }
        }

        bool DoesContain(Stack<WorkItem> parents, WorkItem node, Dictionary<string, WorkItem> working)
        {
            if (parents.Count >= 100 || (DateTime.Now - cyclicReportStart).TotalMinutes > 1)
                throw new OperationCanceledException();

            parents.Push(node);
            try
            {
                foreach (string item in node.Depends)
                {
                    WorkItem child = working[item];

                    if (parents.Contains(child))
                    { node.Counter++; return true; }

                    if (DoesContain(parents, child, working))
                    { node.Counter++; return true; }
                }
                return false;
            }
            finally { parents.Pop(); }
        }
        #endregion

        public int Count { get { return _buildList.Count; } }

        internal IEnumerable<ProjectInfo> Enumerate()
        { return _buildList; }
    }
}
