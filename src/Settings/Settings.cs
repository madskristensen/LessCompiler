using System;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace LessCompiler
{
    public static class Settings
    {
        private const string SettingKey = "LessCompiler";
        private static DTE2 _dte = VsHelpers.DTE;
        private static IVsSolution2 _solution = (IVsSolution2)ServiceProvider.GlobalProvider.GetService(typeof(SVsSolution));

        public static void EnableLessCompilation(this Project project, bool isEnabled)
        {
            if (_dte.Solution == null)
                return;

            string guid = project.UniqueGuid();

            if (string.IsNullOrEmpty(guid))
                return;

            string value = guid;

            if (_dte.Solution.Globals.VariableExists[SettingKey])
            {
                value = _dte.Solution.Globals[SettingKey].ToString();

                if (isEnabled)
                {
                    if (!value.Contains(guid))
                        value += "," + guid;
                }
                else
                {
                    if (value.Contains(guid))
                        value = value.Replace(guid, "").Replace(",,", ",");
                }
            }

            _dte.Solution.Globals[SettingKey] = value.Trim(',', ' ');
            _dte.Solution.Globals.VariablePersists[SettingKey] = !string.IsNullOrEmpty(value);

            Changed?.Invoke(project, new SettingsChangedEventArgs(isEnabled));
        }

        public static bool IsLessCompilationEnabled(this Project project)
        {
            if (project == null || _dte.Solution == null)
                return false;

            bool isSet = _dte.Solution.Globals.VariableExists[SettingKey];
            string guid = project.UniqueGuid();

            if (string.IsNullOrEmpty(guid))
                return false;

            if (isSet && _dte.Solution.Globals[SettingKey].ToString().Contains(guid))
            {
                return true;
            }

            return false;
        }

        private static string UniqueGuid(this Project project)
        {
            if (project == null)
                return null;

            if (_solution.GetProjectOfUniqueName(project.UniqueName, out var hierarchy) == VSConstants.S_OK)
                if (_solution.GetGuidOfProject(hierarchy, out Guid projectGuid) == VSConstants.S_OK)
                    return projectGuid.ToString();

            return null;
        }

        public static event EventHandler<SettingsChangedEventArgs> Changed;
    }
}
