using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Tester {
class Tester {
    static void Main(string[] args) {
        var testFiles = Directory.GetFiles(Directory.GetCurrentDirectory());

        foreach (var testFile in testFiles) {
            if (!testFile.EndsWith(".pas"))
                continue;

            var testName = testFile.Substring(0, testFile.IndexOf('.'));
//            var checkFilename = test.Substring(test.IndexOf(".") + 1);
            
            Process pr = new Process();
            pr.StartInfo.FileName = "dotnet";
            pr.StartInfo.Arguments = $"../src/bin/Debug/netcoreapp2.1/Compiler.dll -L {testFile}";
            pr.StartInfo.UseShellExecute = false;
            pr.StartInfo.RedirectStandardOutput = true;
            pr.Start();

            using (var answer = File.OpenText(testName + ".test")) {

                while (!pr.StandardOutput.EndOfStream) {
                    var outLine = pr.StandardOutput.ReadLine();
                    var compiledResult = outLine.Split();

                    var answerLine = answer.ReadLine();
                    var expectedAnswer = answerLine.Split();

                    if (!compiledResult.SequenceEqual(expectedAnswer)) {

                        var diffBuilder = new InlineDiffBuilder(new Differ());
                        var diff = diffBuilder.BuildDiffModel(answerLine, outLine);

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

//                        Console.ForegroundColor = ConsoleColor.Black;
                        break;
                    }


                }
            }

            pr.WaitForExit();// Waits here for the process to exit.
            
        }
    }
}
}