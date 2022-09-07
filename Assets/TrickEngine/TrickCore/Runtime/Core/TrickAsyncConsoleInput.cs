using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TrickCore
{
    /// <summary>
    /// Support console command listening for non-unity apps (console window)
    /// Call TrickAsyncConsoleInput.Initialize(.., ..) to activate it
    /// Subscribe commands: TrickAsyncConsoleInput.Subscribe("test", action);
    /// </summary>
    public static class TrickAsyncConsoleInput
    {
        public delegate Task InvokeCommandTask(string[] args);
        public delegate void InvokeCommand(string[] args);
        private static readonly List<KeyValuePair<string, CommandData>> InvokeDict = new List<KeyValuePair<string, CommandData>>();
        
        private static string[] _invalidArgs;
        private static bool _stop;
        private static bool _initialized;
        private static bool _useUnityTask;

        /// <summary>
        /// Initialize the console input class
        /// </summary>
        /// <param name="useUnityTask">If true, we use the <see cref="UnityTrickTask"/> class otherwise the <see cref="TrickTask"/> as our task factory</param>
        /// <param name="invokeToMainThread">If true, we invoke to our main thread. The thread that calls the TrickEngine.Update() calls</param>
        public static void Initialize(bool useUnityTask, bool invokeToMainThread)
        {
            if (_initialized) return;
            _initialized = true;
            _useUnityTask = useUnityTask;
            
            SubscribeInternalCommands();

            async Task UpdateLoop()
            {
                while (!_stop)
                {
                    if (Console.KeyAvailable)
                    {
                        string input = Console.ReadLine();
                        if (invokeToMainThread)
                        {
                            await TrickTask.ExecuteSynchronously(() => { InvokeInput(input); });
                        }
                        else
                        {
                            await InvokeInputTask(input);
                        }
                    }

                    await Task.Delay(100);
                }
            }

            if (useUnityTask)
                UnityTrickTask.StartNewTask(async () => { await UpdateLoop(); });
            else
                TrickTask.StartNewTask(async () => { await UpdateLoop(); });
        }

        /// <summary>
        /// Clear all subscribed calls.
        /// </summary>
        public static void ClearSubscribeList()
        {
            InvokeDict.Clear();
            SubscribeInternalCommands();
        }

        private struct CommandData
        {
            public List<InvokeCommand> Commands;
            public List<InvokeCommandTask> CommandTasks;
            public string Description;
            public string Usage;
            public bool Silent;
            public List<string> Aliases;
        }

        private static void SubscribeInternalCommands()
        {
            Subscribe(new List<string>() {"command", "commands"}, args =>
            {
                List<KeyValuePair<string, CommandData>> filteredInvokeMap =
                    new List<KeyValuePair<string, CommandData>>();

                foreach (var p in InvokeDict)
                {
                    bool skip = false;
                    foreach (var t in p.Value.Aliases)
                    {
                        if (t.Contains("command") || t.Contains("?") || p.Value.Silent)
                        {
                            skip = true;
                        }
                    }
                    if (skip) continue;
                    filteredInvokeMap.Add(p);
                }
                
                Logger.Core.Log($"listing {filteredInvokeMap.Count} available command(s):\n");

                // Calculate howmany spaces we need
                int maxIndentSpaces = 0;
                foreach (var p in filteredInvokeMap)
                {
                    List<string> list = new List<string>(p.Value.Aliases);
                    for (int i = 0; i < list.Count; ++i)
                        list[i] = '/' + list[i].TrimStart('/', ' '); // trim whitespaces and add a '/'
                    var commands = string.Join(" or ", list.ToArray()).ToLower();
                    if (maxIndentSpaces < commands.Length)
                        maxIndentSpaces = commands.Length;
                }

                // Print our commands
                foreach (var p in filteredInvokeMap)
                {
                    List<string> list = new List<string>(p.Value.Aliases);
                    for (int i = 0; i < list.Count; ++i)
                        list[i] = '/' + list[i].TrimStart('/', ' '); // trim whitespaces and add a '/'
                    var commands = list.First().ToLower();

                    string text = commands;
                    for (int i = 0; i < (maxIndentSpaces - commands.Length); i++)
                        text += " ";

                    text += $"\t\t{p.Value.Description}";

                    Logger.Core.Log(text);
                }
            });

            Subscribe(new List<string>() {"?"}, args =>
            {
                string command = string.Join(" ", args.ToArray()).Trim(); // remove spaces on both sides
                if (command.Length > 0 && command[0] == '/') command = command.Remove(0, 1); // remove one '/'

                var splitArgs = command.Split(' ').ToList();
                if (splitArgs.Count <= 0) return;

                // The command
                string cmd = splitArgs.First().ToLower();

                // remove the command from the args
                splitArgs.RemoveAt(0);


                bool found = false;

                foreach (var p in InvokeDict)
                {
                    if (!p.Value.Aliases.Any(s1 => s1.Equals(cmd, StringComparison.CurrentCultureIgnoreCase))) continue;

                    try
                    {
                        Logger.Core.Log(!string.IsNullOrEmpty(p.Value.Description) ? $"{p.Value.Description}" : $"No description found for command '{cmd}'");

                        if (!string.IsNullOrEmpty(p.Value.Usage))
                        {
                            Logger.Core.Log($"USAGE: /{cmd} {p.Value.Usage}");
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Core.LogException(e);
                    }

                    found = true;
                }

                if (!found) Logger.Core.Log($"Unable to get help for command '{cmd}'. The command doesn't exists!");

                return;
            });
        }

        public static void InvalidArguments(string[] args)
        {
            _invalidArgs = args;
        }

        public static void Stop()
        {
            _stop = true;
        }
        
        public static void InvokeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return;
            }

            string[] semicolonSplit = input.Split(';');

            foreach (string s in semicolonSplit)
            {
                var ss = s.TrimStart('/').TrimStart(' ');
                string[] split = ss.Split(' ');
                string command = split[0].TrimStart('/').TrimStart(' ');
                string[] args = split.Length > 1 ? ss.Substring(command.Length + 1, ss.Length - command.Length - 1).Split(' ') : new string[0];
                
                List<InvokeCommand> invokeList = new List<InvokeCommand>();
                List<InvokeCommandTask> invokeListTask = new List<InvokeCommandTask>();

                // exec action
                foreach (var pair in InvokeDict)
                {
                    if (pair.Value.Aliases.Any(s1 => s1.Equals(command, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        pair.Value.Commands?.ForEach(invokeList.Add);
                        pair.Value.CommandTasks?.ForEach(invokeListTask.Add);
                    }
                }

                if (invokeList.Count > 0 || invokeListTask.Count > 0)
                {
                    foreach (InvokeCommand inv in invokeList)
                    {
                        inv?.Invoke(args);
                        if (_invalidArgs == null) continue;
                        _invalidArgs = null;
                        InvokeInput($"/? {command}");
                    }

                    foreach (InvokeCommandTask inv in invokeListTask)
                    {
                        if (inv != null)
                        {
                            if (_useUnityTask)
                                UnityTrickTask.StartNewTask(async () => { await inv(args); });
                            else
                                TrickTask.StartNewTask(async () => { await inv(args); });
                        }
                        if (_invalidArgs == null) continue;
                        _invalidArgs = null;
                        InvokeInput($"/? {command}");
                    }
                }
                else
                {
                    Logger.Core.Log($"Unknown command '{command}'");
                }
            }
        }

        public static async Task InvokeInputTask(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                await Task.CompletedTask;
                return;
            }

            string[] semicolonSplit = input.Split(';');

            foreach (string s in semicolonSplit)
            {
                var ss = s.TrimStart('/').TrimStart(' ');
                string[] split = ss.Split(' ');
                string command = split[0].TrimStart('/').TrimStart(' ');
                string[] args = split.Length > 1 ? ss.Substring(command.Length + 1, ss.Length - command.Length - 1).Split(' ') : new string[0];

                List<InvokeCommand> invokeList = new List<InvokeCommand>();
                List<InvokeCommandTask> invokeListTask = new List<InvokeCommandTask>();

                // exec action
                foreach (var pair in InvokeDict)
                {
                    if (pair.Value.Aliases.Any(s1 => s1.Equals(command, StringComparison.CurrentCultureIgnoreCase)))
                    {
                        pair.Value.Commands?.ForEach(invokeList.Add);
                        pair.Value.CommandTasks?.ForEach(invokeListTask.Add);
                    }
                }

                if (invokeList.Count > 0 || invokeListTask.Count > 0)
                {
                    foreach (InvokeCommand inv in invokeList)
                    {
                        inv?.Invoke(args);
                        if (_invalidArgs == null) continue;
                        _invalidArgs = null;
                        InvokeInput($"/? {command}");
                    }

                    foreach (InvokeCommandTask inv in invokeListTask)
                    {
                        if (inv != null) await inv(args);
                        if (_invalidArgs == null) continue;
                        _invalidArgs = null;
                        await InvokeInputTask($"/? {command}");
                    }
                }
                else
                {
                    Logger.Core.Log($"Unknown command '{command}'");
                }
            }
        }

        public static void Subscribe(List<string> commands, InvokeCommand action)
        {
            Subscribe(commands, action, string.Empty, string.Empty);
        }

        public static void Subscribe(List<string> commands, InvokeCommand action, string description)
        {
            Subscribe(commands, action, description, string.Empty);
        }
        
        public static void Subscribe(List<string> commands, InvokeCommand action, string description, string usage)
        {
            int index = InvokeDict.FindIndex(pair => pair.Key == commands.FirstOrDefault());
            if (index == -1)
            {
                InvokeDict.Add(new KeyValuePair<string, CommandData>(commands.FirstOrDefault(), new CommandData()
                {
                    Commands = new List<InvokeCommand>() {action},
                    Description = description,
                    Usage = usage,
                    Silent = false,
                    Aliases = commands
                }));
            }
            else
            {
                // Add missing aliases
                foreach (var alias in commands.Where(s => InvokeDict[index].Value.Aliases.All(command => command != s)))
                {
                    InvokeDict[index].Value.Aliases.Add(alias);
                }
                InvokeDict[index].Value.Commands.Add(action);
            }
        }

        public static void Subscribe(string command, InvokeCommand action, string description, string usage)
        {
            Subscribe(new List<string>() {command}, action, description, usage);
        }

        public static void Subscribe(string command, InvokeCommand action, string description)
        {
            Subscribe(new List<string>() { command }, action, description, string.Empty);
        }

        public static void Subscribe(string command, InvokeCommand action)
        {
            Subscribe(new List<string>() { command }, action, string.Empty, string.Empty);
        }

        public static void SilentSubscribe(List<string> commands, InvokeCommand action)
        {
            SilentSubscribe(commands, action, string.Empty, string.Empty);
        }

        public static void SilentSubscribe(List<string> commands, InvokeCommand action, string description)
        {
            SilentSubscribe(commands, action, description, string.Empty);
        }
        
        public static void SilentSubscribe(List<string> commands, InvokeCommand action, string description, string usage)
        {
            int index = InvokeDict.FindIndex(pair => pair.Key == commands.FirstOrDefault());
            if (index == -1)
            {
                InvokeDict.Add(new KeyValuePair<string, CommandData>(commands.FirstOrDefault(), new CommandData()
                {
                    Commands = new List<InvokeCommand>() { action },
                    Description = description,
                    Usage = usage,
                    Silent = true,
                    Aliases = commands
                }));
            }
            else
            {
                // Add missing aliases
                foreach (var alias in commands.Where(s => InvokeDict[index].Value.Aliases.All(command => command != s)))
                {
                    InvokeDict[index].Value.Aliases.Add(alias);
                }
                InvokeDict[index].Value.Commands.Add(action);
            }
        }

        public static void SilentSubscribe(string command, InvokeCommand action, string description, string usage)
        {
            SilentSubscribe(new List<string>() {command}, action, description, usage);
        }

        public static void SilentSubscribe(string command, InvokeCommand action, string description)
        {
            SilentSubscribe(new List<string>() { command }, action, description, string.Empty);
        }

        public static void SilentSubscribe(string command, InvokeCommand action)
        {
            SilentSubscribe(new List<string>() { command }, action, string.Empty, string.Empty);
        }

        // ASYNC

        public static void SubscribeTask(List<string> commands, InvokeCommandTask task)
        {
            SubscribeTask(commands, task, string.Empty, string.Empty);
        }

        public static void SubscribeTask(List<string> commands, InvokeCommandTask task, string description)
        {
            SubscribeTask(commands, task, description, string.Empty);
        }

        public static void SubscribeTask(List<string> commands, InvokeCommandTask task, string description, string usage)
        {
            int index = InvokeDict.FindIndex(pair => pair.Key == commands.FirstOrDefault());
            if (index == -1)
            {
                InvokeDict.Add(new KeyValuePair<string, CommandData>(commands.FirstOrDefault(), new CommandData()
                {
                    CommandTasks = new List<InvokeCommandTask>() { task },
                    Description = description,
                    Usage = usage,
                    Silent = false,
                    Aliases = commands
                }));
            }
            else
            {
                // Add missing aliases
                foreach (var alias in commands.Where(s => InvokeDict[index].Value.Aliases.All(command => command != s)))
                {
                    InvokeDict[index].Value.Aliases.Add(alias);
                }
                InvokeDict[index].Value.CommandTasks.Add(task);
            }
        }

        public static void SubscribeTask(string command, InvokeCommandTask task, string description, string usage)
        {
            SubscribeTask(new List<string>() { command }, task, description, usage);
        }

        public static void SubscribeTask(string command, InvokeCommandTask task, string description)
        {
            SubscribeTask(new List<string>() { command }, task, description, string.Empty);
        }

        public static void SubscribeTask(string command, InvokeCommandTask task)
        {
            SubscribeTask(new List<string>() { command }, task, string.Empty, string.Empty);
        }

        public static void SilentSubscribeTask(List<string> commands, InvokeCommandTask task)
        {
            SilentSubscribeTask(commands, task, string.Empty, string.Empty);
        }

        public static void SilentSubscribeTask(List<string> commands, InvokeCommandTask task, string description)
        {
            SilentSubscribeTask(commands, task, description, string.Empty);
        }

        public static void SilentSubscribeTask(List<string> commands, InvokeCommandTask task, string description, string usage)
        {
            int index = InvokeDict.FindIndex(pair => pair.Key == commands.FirstOrDefault());
            if (index == -1)
            {
                InvokeDict.Add(new KeyValuePair<string, CommandData>(commands.FirstOrDefault(), new CommandData()
                {
                    CommandTasks = new List<InvokeCommandTask>() { task },
                    Description = description,
                    Usage = usage,
                    Silent = true,
                    Aliases = commands
                }));
            }
            else
            {
                // Add missing aliases
                foreach (var alias in commands.Where(s => InvokeDict[index].Value.Aliases.All(command => command != s)))
                {
                    InvokeDict[index].Value.Aliases.Add(alias);
                }
                InvokeDict[index].Value.CommandTasks.Add(task);
            }
        }

        public static void SilentSubscribeTask(string command, InvokeCommandTask task, string description, string usage)
        {
            SilentSubscribeTask(new List<string>() { command }, task, description, usage);
        }

        public static void SilentSubscribeTask(string command, InvokeCommandTask task, string description)
        {
            SilentSubscribeTask(new List<string>() { command }, task, description, string.Empty);
        }

        public static void SilentSubscribeTask(string command, InvokeCommandTask task)
        {
            SilentSubscribeTask(new List<string>() { command }, task, string.Empty, string.Empty);
        }
    }
}
