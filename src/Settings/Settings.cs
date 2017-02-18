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

        public static void Enable(Project project, bool isEnabled)
        {
            if (_dte.Solution == null)
                return;

            string guid = project.UniqueGuid();

            if (_dte.Solution.Globals.VariableExists[SettingKey])
            {
                string value = _dte.Solution.Globals[SettingKey].ToString();

                if (isEnabled)
                {
                    if (!value.Contains(guid))
                        _dte.Solution.Globals[SettingKey] = (value + "," + guid).Trim(',', ' ');
                }
                else
                {
                    if (value.Contains(guid))
                        _dte.Solution.Globals[SettingKey] = value.Replace(guid, "").Replace(",,", ",");
                }
            }
            else if (isEnabled)
            {
                _dte.Solution.Globals[SettingKey] = guid;
            }

            _dte.Solution.Globals.VariablePersists[SettingKey] = isEnabled;

            Changed?.Invoke(project, new SettingsChangedEventArgs(isEnabled));
        }

        public static bool IsEnabled(Project project)
        {
            if (_dte.Solution == null)
                return false;

            bool isSet = _dte.Solution.Globals.VariableExists[SettingKey];

            if (isSet && _dte.Solution.Globals[SettingKey].ToString().Contains(project.UniqueGuid()))
            {
                return true;
            }

            return false;
        }

        public static string UniqueGuid(this Project project)
        {
            if (_solution.GetProjectOfUniqueName(project.UniqueName, out var hierarchy) == VSConstants.S_OK)
                if (_solution.GetGuidOfProject(hierarchy, out Guid projectGuid) == VSConstants.S_OK)
                    return projectGuid.ToString();

            return null;
        }

        public static event EventHandler<SettingsChangedEventArgs> Changed;
    }
}
