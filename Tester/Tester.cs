using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;
using CommandLineParser.Validation;
using DiffPlex;
using DiffPlex.DiffBuilder.Model;

namespace Tester {
class Tester {
        
    static void Main(string[] args) {
        var commandLineParser = new CommandLineParser.CommandLineParser();
        
        var compilerPath = new FileArgument('c', "compiler", "Path to compiler") {
            Optional = false
        };
        
        var testsDirectory = new DirectoryArgument('t', "test-dir", "Path to tests directory") {
            Optional = false
        };
        
        var testLexer = new SwitchArgument('l', "lexer", "Test lexer", false);
        var testParser = new SwitchArgument('p', "parser", "Test parser", false);
        var testAll = new SwitchArgument('a', "all", "Launch all tests", false);
        
        commandLineParser.Arguments.Add(compilerPath);
        commandLineParser.Arguments.Add(testsDirectory);
        
        commandLineParser.Arguments.Add(testAll);
        commandLineParser.Arguments.Add(testLexer);
        commandLineParser.Arguments.Add(testParser);

        var testGroupCertification = new ArgumentGroupCertification("a,p,l", EArgumentGroupCondition.AtLeastOneUsed);
        commandLineParser.Certifications.Add(testGroupCertification);

        try {
            commandLineParser.ParseCommandLine(args);
        }
        catch (CommandLineException) {
            commandLineParser.ShowUsageHeader = "dotnet Tester.dll [pathToCompiler] [pathToTestsRoot]";
            commandLineParser.ShowUsage();
        }

        if (testAll.Value) {
            TestLexer(compilerPath.Value.FullName, testsDirectory.Value.FullName);
            TestParser(compilerPath.Value.FullName, testsDirectory.Value.FullName);
            return;
        }

        if (testLexer.Value) 
            TestLexer(compilerPath.Value.FullName, testsDirectory.Value.FullName);

        if (testParser.Value)
            TestParser(compilerPath.Value.FullName, testsDirectory.Value.FullName);
    }

    private static Process RunCompiler(string path, string flags) {
        var pr = new Process();
        
        pr.StartInfo.FileName = "dotnet";
        pr.StartInfo.Arguments = $"{path} {flags}";
        pr.StartInfo.UseShellExecute = false;
        pr.StartInfo.RedirectStandardOutput = true;
        pr.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        pr.Start();
        
        return pr;
    }

    private static void PrintDiff(string expectedLine, string gotLine, string testName) {
        var diffBuilder = new InlineDiffBuilder(new Differ());
        
        var diff = diffBuilder.BuildDiffModel(expectedLine, gotLine);
        

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[!] Error in {testName}");
                        
        foreach (var line in diff.Lines) {
            switch (line.Type) {
                case ChangeType.Inserted:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("+ ");
                    break;
                case ChangeType.Deleted:
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("- ");
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("  ");
                    break;
            }

            Console.WriteLine(line.Text);
                            
        }
    }

    private static void TestLexer(string compilerPath, string testDir) {
        var defaultForegroundColor = Console.ForegroundColor;
        
        var testFiles = Directory.GetFiles($"{testDir}/lexer/");

        foreach (var testFile in testFiles) {
            if (!testFile.EndsWith(".pas"))
                continue;

            var testName = testFile.Substring(0, testFile.LastIndexOf('.'));
            var pr = RunCompiler(compilerPath, $"-l -i {testFile}");
            var foundError = false;
            
            using (var answer = File.OpenText($"{testName}.test")) {

                while (!pr.StandardOutput.EndOfStream) {
                    if (answer.EndOfStream)
                        break;
                        
                    var gotResult = pr.StandardOutput.ReadLine().Split().Where(i => i != "" && i != "\t");
                    var gotLine = string.Join(" ", gotResult);

                    var expectedResult = answer.ReadLine().Split().Where(i => i != "" && i != "\t");
                    var expectedLine = string.Join(" ", expectedResult);

                    if (gotLine.SequenceEqual(expectedLine)) 
                        continue;
                    
                    foundError = true;
                    PrintDiff(expectedLine, gotLine, testName);
                    break;
                }
                
                if(foundError)
                    break;
                
                if (!answer.EndOfStream || !pr.StandardOutput.EndOfStream) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[-] line count mismatch in {testName}");
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[+] passed {testName}");
                }
                
                Console.ForegroundColor = defaultForegroundColor;
            }
            pr.WaitForExit();// Waits here for the process to exit.
            
        }
    }

    static void TestParser(string compilerPath, string testDir) {
        var defaultForegroundColor = Console.ForegroundColor;
        
        var testFiles = Directory.GetFiles($"{testDir}/parser").Reverse();

        foreach (var testFile in testFiles) {
            if (!testFile.EndsWith(".pas"))
                continue;

            var testName = testFile.Substring(0, testFile.LastIndexOf('.'));

            var pr = RunCompiler(compilerPath, $"-s -i  {testFile}");
            
            var foundError = false;
            using (var answer = File.OpenText($"{testName}.test")) {
                
                while (!pr.StandardOutput.EndOfStream) {
                    if (answer.EndOfStream)
                        break;
                    
                    var outLine = pr.StandardOutput.ReadLine();
                    var answerLine = answer.ReadLine();

                    if (outLine.Equals(answerLine)) 
                        continue;
                    
                    foundError = true;
                    PrintDiff(answerLine, outLine, testName);
                    break;
                }
                
                if(foundError)
                    break;
                
                if (!answer.EndOfStream || !pr.StandardOutput.EndOfStream) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[-] line count mismatch in {testName}");
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[+] passed {testName}");
                }
                
                Console.ForegroundColor = defaultForegroundColor;
            }
            pr.WaitForExit();// Waits here for the process to exit.
        }
    }
}
}