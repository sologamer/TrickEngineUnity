using System;

namespace TrickCore
{
    public static class EnvironmentUtil
    {
        public static bool HasArgument(string argument)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == argument)
                {
                    return true;
                }
            }
            return false;
        }

        public static string GetValue(string argument)
        {
            string pairedValue = string.Empty;
            int index = -1;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == argument)
                {
                    // argument string matches, however the next index will be our value (if only the index exists)
                    index = i + 1;
                }
                else if (index == i)
                {
                    // pair found
                    pairedValue = arg;
                }
            }
            return pairedValue;
        }
    }
}