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
using CSharpTest.Net.CSBuild.Configuration;
using CSharpTest.Net.CSBuild.Build;
using System.IO;
using System.Reflection;

namespace CSharpTest.Net.CSBuild.BuildTasks
{
    [Serializable]
    class TargetBuilder : BuildTask
    {
		readonly CSBuildConfig _config;
        readonly BuildTarget _target;
        readonly string[] _properties;
        readonly BuildAll _buildTask;

        public TargetBuilder(CSBuildConfig config, BuildTarget target, string[] properties, string[] targets)
        {
            _config = config;
            _target = target;
            _properties = properties;
            _buildTask = new BuildAll(targets);
        }

        protected override int Run(BuildEngine engine)
        {
			string targetName = String.Join(",", _buildTask.Targets);
			if (String.IsNullOrEmpty(targetName))
				targetName = "(default)";
			int errors = 0;

			if (_target.TextLog != null)
				errors += new LogFileOutput(_target.TextLog.AbsolutePath, _target.TextLog.Level).Perform(engine);
            if (_target.XmlLog != null)
				errors += new XmlFileOutput(_target.XmlLog.AbsolutePath, _target.XmlLog.Level).Perform(engine);

			//Globals must preceed project loading
			errors += new SetGlobal(MSProp.Configuration, _target.Configuration).Perform(engine);
			errors += new SetGlobal(MSProp.Platform, _target.Platform.ToString()).Perform(engine);
            foreach (string property in _properties)
            {
                string[] values = property.Split(new char[] { '=', ':' }, 2);
                if (values.Length == 2)
					engine.Properties.SetValue(values[0], values[1]);
            }

            Log.Info("CSBuild {0} {1} - Runtime={2}, Configuration={3}, Platform={4}",
                targetName.ToLower(), _target.GroupName.ToLower(), engine.Framework.ToString().Insert(2,"."),
                engine.Properties[MSProp.Configuration], engine.Properties[MSProp.Platform]);

            //Add projects
            ProjectFinder projects = new ProjectFinder();
            projects.Add(_config.Projects.AddProjects);
            projects.Add(_target.AddProjects);
            projects.Remove(_config.Projects.RemoveProjects);
            projects.Remove(_target.RemoveProjects);
			errors += projects.Perform(engine);

			if (errors > 0 && !_config.Options.ContinueOnError)
				return errors;

			//Add #defines
			List<string> defines = new List<string>();
			foreach (BuildDefineConst define in _target.DefineConstants)
				defines.Add(define.Value);
			if (defines.Count > 0)
				errors += new DefineConstants(defines.ToArray()).Perform(engine);

			//Project configuration
			if(_target.TargetFramework != null)
				errors += new SetProjectProperty(MSProp.TargetFrameworkVersion, _target.TargetFramework.Version.ToString().Insert(2, ".")).Perform(engine);
			if(_target.OutputPath != null)
				errors += new SetProjectPathProperty(MSProp.OutputPath, _target.OutputPath.AbsolutePath).Perform(engine);
			if(_target.IntermediateFiles != null)
				errors += new SetProjectPathProperty(MSProp.IntermediateOutputPath, _target.IntermediateFiles.AbsolutePath).Perform(engine);

			errors += new SetSolutionDir().Perform(engine);
			errors += new NewerFrameworkReferences().Perform(engine);

			EnforceReferences folders = new EnforceReferences(_config.Options.StrictReferences, _config.Options.NoStdReferences, _config.Options.ForceReferencesToFile);
			folders.Add(_config.Projects.ReferenceFolders);
			folders.Add(_target.ReferenceFolders);
			errors += folders.Perform(engine);

			if (errors > 0 && !_config.Options.ContinueOnError)
				return errors;

			System.Diagnostics.TraceLevel warningLevel;
			bool saveChanges = _config.Options.SaveProjectChanges(out warningLevel);
			if (_target.SaveProjectChanges != null)
			{
				saveChanges = _target.SaveProjectChanges.Enabled;
				warningLevel = _target.SaveProjectChanges.LogLevel;
			}
			if(saveChanges)
				errors += new SaveModifiedProjects(warningLevel).Perform(engine);

			//Build it
			errors += _buildTask.Perform(engine);
			return errors;
        }

    }
}
