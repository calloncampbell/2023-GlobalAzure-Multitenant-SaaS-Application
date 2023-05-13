using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElasticShardSqlUtil.Utils
{
    internal static class ConsoleUtils
    {
        /// <summary>
        /// Writes detailed information to the console.
        /// </summary>
        public static void WriteMessage(string format, params object[] args)
        {
            WriteColor(ConsoleColor.Gray, format, args);
        }

        public static void WriteMessageDarkGray(string format, params object[] args)
        {
            WriteColor(ConsoleColor.DarkGray, format, args);
        }

        /// <summary>
        /// Writes detailed information to the console.
        /// </summary>
        public static void WriteInfo(string format, params object[] args)
        {
            WriteColor(ConsoleColor.Blue, format, args);
        }

        /// <summary>
        /// Writes warning text to the console.
        /// </summary>
        public static void WriteSuccess(string format, params object[] args)
        {
            WriteColor(ConsoleColor.Green, format, args);
        }

        /// <summary>
        /// Writes warning text to the console.
        /// </summary>
        public static void WriteWarning(string format, params object[] args)
        {
            WriteColor(ConsoleColor.Yellow, format, args);
        }

        /// <summary>
        /// Writes warning text to the console.
        /// </summary>
        public static void WriteError(string format, params object[] args)
        {
            WriteColor(ConsoleColor.Red, format, args);
        }

        /// <summary>
        /// Writes colored text to the console.
        /// </summary>
        public static void WriteColor(ConsoleColor color, string format, params object[] args)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(format, args);
            Console.ForegroundColor = oldColor;
        }

        /// <summary>
        /// Reads an integer from the console.
        /// </summary>
        public static int ReadIntegerInput(string prompt)
        {
            return ReadIntegerInput(prompt, allowNull: false).Value;
        }

        /// <summary>
        /// Reads an integer from the console, or returns null if the user enters nothing and allowNull is true.
        /// </summary>
        public static int? ReadIntegerInput(string prompt, bool allowNull)
        {
            while (true)
            {
                Console.Write(prompt);
                string line = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(line) && allowNull)
                {
                    return null;
                }

                int inputValue;
                if (int.TryParse(line, out inputValue))
                {
                    return inputValue;
                }
            }
        }

        /// <summary>
        /// Reads a string from the console.
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        public static string ReadStringInput(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string line = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(line))
                {
                    return null;
                }

                return line;
            }
        }

        /// <summary>
        /// Reads an integer from the console.
        /// </summary>
        public static int ReadIntegerInput(string prompt, int defaultValue, Func<int, bool> validator)
        {
            while (true)
            {
                int? input = ReadIntegerInput(prompt, allowNull: true);

                if (!input.HasValue)
                {
                    // No input, so return default
                    return defaultValue;
                }
                else
                {
                    // Input was provided, so validate it
                    if (validator(input.Value))
                    {
                        // Validation passed, so return
                        return input.Value;
                    }
                }
            }
        }
    }
}
