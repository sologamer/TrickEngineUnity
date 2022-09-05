using System;
using System.Collections.Generic;
using System.Linq;

namespace TrickCore
{
    public static class TrickConsole
    {
        public static event Action<string> OnWriteLog;
        
        private static int FindCommand(string format, int startIndex, out int prevIndex, out string command)
        {
            int openIndex = format.IndexOf('<', startIndex);
            if (openIndex != -1)
            {
                // Look for ]
                int closedIndex = format.IndexOf('>', openIndex);
                if (closedIndex != -1)
                {
                    command = format.Substring(openIndex + 1, closedIndex - 1 - openIndex);
                    prevIndex = openIndex;
                    return closedIndex + 1;
                }
            }
            command = null;
            prevIndex = -1;
            return -1;
        }

        public static string Pad(this object obj, int length)
        {
            string objStr = obj?.ToString() ?? string.Empty;
            string newStr = GetStringWithoutColor(obj);
            int diff = newStr.Length - objStr.Length;
            return length < 0 ? objStr.PadRight(-(length + diff)) : objStr.PadLeft(length - diff);
        }

        public static void WriteColorLine()
        {
            Console.WriteLine();
        }

        public static string GetStringWithoutColor(this object obj)
        {
            string objStr = obj?.ToString() ?? string.Empty;

            int index = 0;
            int storedIndex = -1;
            string newStr = string.Empty;
            List<ConsoleColor> colorDict = new List<ConsoleColor>();
            while ((index = FindCommand(objStr, index, out var prevIndex, out var command)) != -1)
            {
                if (storedIndex == -1 && prevIndex > 0)
                {
                    newStr += objStr.Substring(0, prevIndex);
                }

                ConsoleColor color;
                bool isValid = false;
                if (command != null)
                {
                    if (!command.StartsWith("/"))
                    {
                        if (Enum.TryParse(command, true, out color) && !int.TryParse(command, out var num))
                        {
                            colorDict.Add(color);
                            isValid = true;
                        }
                    }
                }

                if (isValid)
                {
                    if (storedIndex != -1)
                    {
                        newStr += objStr.Substring(storedIndex, prevIndex - storedIndex);
                    }
                }
                else if (command != null)
                {
                    if (storedIndex != -1)
                    {
                        newStr += objStr.Substring(storedIndex, prevIndex - storedIndex);
                    }

                    if (command.StartsWith("/"))
                    {
                        // Closing command
                        command = command.Remove(0, 1);
                        if (Enum.TryParse(command, true, out color) && !int.TryParse(command, out var num))
                        {
                            if (colorDict.Count > 0 && colorDict.Last() == color)
                            {
                                colorDict.RemoveAt(colorDict.Count - 1);
                                isValid = true;
                            }
                        }
                        else
                        {
                            newStr += ($"</{command}>");
                        }
                    }
                    else
                    {
                        newStr += ($"<{command}>");
                    }
                }

                storedIndex = index;
            }

            if (storedIndex == -1)
            {
                newStr += objStr;
            }
            else if (objStr.Length - storedIndex > 0)
            {
                newStr += objStr.Substring(storedIndex, objStr.Length - storedIndex);
            }

            return newStr;
        }

        public static void WriteColorLine(string format, params object[] arguments)
        {
            var backColor = Console.ForegroundColor;

            int index = 0;
            int storedIndex = -1;
            List<ConsoleColor> colorDict = new List<ConsoleColor>();
            while ((index = FindCommand(format, index, out var prevIndex, out var command)) != -1)
            {
                if (storedIndex == -1 && prevIndex > 0)
                {
                    if (arguments.Length == 0)
                        Write(format.Substring(0, prevIndex));
                    else
                        Write(format.Substring(0, prevIndex), arguments);
                }

                ConsoleColor color;
                bool isValid = false;
                if (command != null)
                {
                    if (!command.StartsWith("/"))
                    {
                        if (Enum.TryParse(command, true, out color) && !int.TryParse(command, out var num))
                        {
                            Console.ForegroundColor = color;
                            colorDict.Add(color);
                            isValid = true;
                        }
                    }
                }

                if (isValid)
                {
                    if (storedIndex != -1)
                    {
                        if (arguments.Length == 0)
                            Write(format.Substring(storedIndex, prevIndex - storedIndex));
                        else
                            Write(format.Substring(storedIndex, prevIndex - storedIndex), arguments);
                    }
                }
                else if (command != null)
                {
                    if (storedIndex != -1)
                    {
                        if (arguments.Length == 0)
                            Write(format.Substring(storedIndex, prevIndex - storedIndex));
                        else
                            Write(format.Substring(storedIndex, prevIndex - storedIndex), arguments);
                    }

                    if (command.StartsWith("/"))
                    {
                        // Closing command
                        command = command.Remove(0, 1);
                        if (Enum.TryParse(command, true, out color) && !int.TryParse(command, out var num))
                        {
                            if (colorDict.Count > 0 && colorDict.Last() == color)
                            {
                                colorDict.RemoveAt(colorDict.Count - 1);
                                Console.ForegroundColor = colorDict.Count == 0 ? backColor : colorDict.Last();
                                isValid = true;
                            }
                        }
                        else
                        {
                            Write($"</{command}>");
                        }
                    }
                    else
                    {
                        Write($"<{command}>");
                    }
                }

                storedIndex = index;
            }

            Console.ForegroundColor = backColor;
            if (storedIndex == -1)
            {
                if (arguments.Length == 0)
                    Write(format);
                else
                    Write(format, arguments);
            }
            else if (format.Length - storedIndex > 0)
            {
                if (arguments.Length == 0)
                    Write(format.Substring(storedIndex, format.Length - storedIndex));
                else
                    Write(format.Substring(storedIndex, format.Length - storedIndex), arguments);
            }

            WriteLine();
        }

        private static void Write(string obj)
        {
            Console.Write(obj);
            OnWriteLog?.Invoke(obj != null ? obj : "null");
        }

        private static void Write(object obj)
        {
            Console.Write(obj);
            OnWriteLog?.Invoke(obj != null ? obj.ToString() : "null");
        }

        private static void Write(string format, params object[] args)
        {
            Console.Write(format, args);
            OnWriteLog?.Invoke(string.Format(format, args));
        }

        private static void WriteLine()
        {
            Console.WriteLine();
            OnWriteLog?.Invoke("\n");
        }
    }
}