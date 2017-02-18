using EnvDTE;
using EnvDTE80;

namespace LessCompiler
{
    public class Settings
    {
        private const string SettingKey = "LessCompiler";
        private static DTE2 _dte = VsHelpers.DTE;

        public static void Enable(Project project, bool isEnabled)
        {
            if (_dte.Solution == null)
                return;

            if (_dte.Solution.Globals.VariableExists[SettingKey])
            {
                string value = _dte.Solution.Globals[SettingKey].ToString();

                if (isEnabled)
                {
                    if (!value.Contains(project.UniqueName))
                        _dte.Solution.Globals[SettingKey] = (value + "," + project.UniqueName).Trim(',', ' ');
                }
                else
                {
                    if (value.Contains(project.UniqueName))
                        _dte.Solution.Globals[SettingKey] = value.Replace(project.UniqueName, "").Replace(",,", ",");
                }
            }
            else if (isEnabled)
            {
                _dte.Solution.Globals[SettingKey] = project.UniqueName;
            }

            _dte.Solution.Globals.VariablePersists[SettingKey] = isEnabled;
        }

        public static bool IsEnabled(Project project)
        {
            if (_dte.Solution == null)
                return false;

            bool isSet = _dte.Solution.Globals.VariableExists[SettingKey];

            if (isSet && _dte.Solution.Globals[SettingKey].ToString().Contains(project.UniqueName))
            {
                return true;
            }

            return false;
        }
    }
}
