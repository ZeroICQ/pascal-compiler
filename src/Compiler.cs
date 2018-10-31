using System;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Tests")]


namespace Compiler {

class Compiler {
    private StreamReader _streamReader;
    
    public Compiler(StreamReader streamReader) {
        // ASK: same objects or copy?
        _streamReader = streamReader;
    }

    public void getNextToken() {
    }
   
}

static class App {
    private static void ShowUsage() {
        Console.WriteLine("[USAGE]");
        Console.WriteLine("dotnet compiler.dll [OPTIONS] source.pas");
        Console.WriteLine("");
        Console.WriteLine("[OPTIONS]");
        Console.WriteLine("-l                perform only lexical analyze");
        Console.WriteLine("-o filename       output file");
    }
    
    public static int  Main(string[] args) {
        if (args.Length == 0) {
            ShowUsage();
            return 0;
        }

        var inputFilePath = args.Last();

        try {
            using (var inputFileStreamReader = File.OpenText(inputFilePath)) {
                var compiler = new Compiler(inputFileStreamReader);
                compiler.getNextToken();
            }
        }
        catch (FileNotFoundException ex) {
            Console.WriteLine($"{ex.FileName} not found.");
        }
        
//        var compiler = new Compiler(new StreamReader(Console.OpenStandardInput()));
        
//            TODO: parse flags
//            var outputFilenameIndex = Array.FindIndex(args, s => s.Equals("-o"));
        return 0;
    }
}
}
