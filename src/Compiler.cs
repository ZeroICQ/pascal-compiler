using System;
using System.Linq;

namespace Compiler
{
    public static class Compiler {
        public static int  Main(string[] args) {
            if (args.Length == 0) {
                ShowUsage();
                return 0;
            }

            var outputFilenameIndex = Array.FindIndex(args, s => s.Equals("-o"));
            
            return 0;
        }

        private static void ShowUsage() {
            Console.WriteLine("[USAGE]");
            Console.WriteLine("dotnet compiler.dll [OPTIONS] source.pas");
            Console.WriteLine("");
            Console.WriteLine("[OPTIONS]");
            Console.WriteLine("-l                perform only lexical analyze");
            Console.WriteLine("-o filename       output file");
        }
    }
}
