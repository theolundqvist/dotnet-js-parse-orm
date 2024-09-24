using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Project.Util
{
    public static class Debugger
    {

        private static Action<string> print = Console.WriteLine;
        private static bool onlyPrintImportant = false;
        private static List<string> filters = new List<string>();
        private static bool enabled = true;


        public static void Enable() => enabled = true;
        public static void Disable() => enabled = false;
        public static void SetPrintMethod(Action<string> printMethod) => print = printMethod;

        public static void OnlyPrintExceptions() => onlyPrintImportant = true;

        public static void PrintEverything() => onlyPrintImportant = false;

        public static void AddWordFilter(string word) => filters.Add(word.ToLower());

        public static void ClearFilters() => filters.Clear();

        public static void PrintJson(object o,
            bool important = false,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int line = 1) => Print(JsonUtil.Prettify(JsonUtil.ToJson(o)), important, memberName, filePath, line);

        /// <summary>
        /// Print object.ToString() using print method set by "SetPrintMethod",
        /// Default is Console.WriteLine
        /// </summary>
        /// <param name="text"></param>
        public static void Print(object o,
            bool important = false,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int line = 1)
        {
            if (!enabled) return;
            if((onlyPrintImportant && important) || !onlyPrintImportant || o is Exception)
            {
                var str = filePath.Split('/');
                var fileName = str[str.Length - 1];
                var callerInfo = memberName + "() in " + fileName + ":" + line + " says:";
                var message = o == null ? "null" : o.ToString();
                bool filterOK = false;
                foreach (var filter in filters)
                {
                    if (message.ToLower().Contains(filter) || callerInfo.ToLower().Contains(filter))
                    {
                        filterOK = true;
                        break;
                    }
                }
                if (filters.Count == 0 || filterOK)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    print("\n" + callerInfo);
                    Console.ForegroundColor = ConsoleColor.White;
                    print(message);
                }
            }
        }

    }
}