using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Tester {
class Tester {
    private static void ShowUsage() {
        Console.WriteLine("USAGE");
        Console.WriteLine("dotnet Tester.dll [pathToCompiler] [pathToTestsRoot]");
    }
        
    static void Main(string[] args) {
        if (args.Length < 2) {
            ShowUsage();
            return;
        }
        
        Test(args[0], $"{args[1]}\\tests", "-l");
        
    }

    static void Test(string compilerPath, string testDir, string flags) {
        var defaultForegroundColor = Console.ForegroundColor;
        
        var testFiles = Directory.GetFiles(testDir);

        foreach (var testFile in testFiles) {
            if (!testFile.EndsWith(".pas"))
                continue;

            var testName = testFile.Substring(0, testFile.LastIndexOf('.'));
            
            var pr = new Process();
            pr.StartInfo.FileName = "dotnet";
            pr.StartInfo.Arguments = $"{compilerPath} {flags} {testFile}";
            pr.StartInfo.UseShellExecute = false;
            pr.StartInfo.RedirectStandardOutput = true;
            pr.Start();

            var foundError = false;
            using (var answer = File.OpenText(testName + ".test")) {

                while (!pr.StandardOutput.EndOfStream) {
                    if (answer.EndOfStream)
                        break;
                        
                    var compiledResult = pr.StandardOutput.ReadLine().Split().Where(i => i != "" && i != "\t");
                    var outLine = string.Join(" ", compiledResult);

                    var expectedAnswer = answer.ReadLine().Split().Where(i => i != "" && i != "\t");
                    var answerLine = string.Join(" ", expectedAnswer);

                    if (!compiledResult.SequenceEqual(expectedAnswer)) {
                        foundError = true;
                        var diffBuilder = new InlineDiffBuilder(new Differ());
                        var diff = diffBuilder.BuildDiffModel(answerLine, outLine);

                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"[!] Error in {testName}");
                        Console.ForegroundColor = defaultForegroundColor;
                        
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
                            Console.ForegroundColor = defaultForegroundColor;
                        }

//                        Console.ForegroundColor = ConsoleColor.Black;
                        break;
                    }
                }
                
                if(foundError)
                    break;
                
                if (!answer.EndOfStream || !pr.StandardOutput.EndOfStream) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("[-] ");
                    Console.WriteLine($"line count mismatch in {testName}");
                    Console.ForegroundColor = defaultForegroundColor;
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("[+] ");
                    Console.WriteLine($"passed {testName}");
                    Console.ForegroundColor = defaultForegroundColor;
                }
                
            }
            pr.WaitForExit();// Waits here for the process to exit.
            
            
        }
    }
}
}