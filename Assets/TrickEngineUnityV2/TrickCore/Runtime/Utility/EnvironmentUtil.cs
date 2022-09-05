using System;
using System.Linq;

namespace TrickCore
{
    public static class EnvironmentUtil
    {
        /// <summary>
        /// Check if we have a commandline argument (test.exe hello=value)
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        public static bool HasArgument(string argument)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string[] arg = args[i].Split('=');
                if (arg[0].TrimStart('-') == argument.TrimStart('-'))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the value of the argument
        /// </summary>
        /// <param name="argument"></param>
        /// <returns></returns>
        public static string GetValue(string argument)
        {
            string pairedValue = string.Empty;
            int index = -1;
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                string[] arg = args[i].Split('=');
                if (arg[0].TrimStart('-') == argument.TrimStart('-'))
                {
                    // argument string matches, however the next index will be our value (if only the index exists)
                    if (arg.Length >= 2)
                    {
                        pairedValue = string.Join("=", arg.Skip(1));
                    }
                    else
                    {
                        index = i + 1;
                    }
                }
                else if (index == i)
                {
                    // pair found
                    pairedValue = arg[0];
                }
            }
            return pairedValue;
        }
    }
}