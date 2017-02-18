using System;

namespace LessCompiler
{
    public class SettingsChangedEventArgs : EventArgs
    {
        public SettingsChangedEventArgs(bool enabled)
        {
            Enabled = enabled;
        }

        public bool Enabled { get; }
    }
}
