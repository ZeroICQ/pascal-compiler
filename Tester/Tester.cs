using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        var testSemantics = new SwitchArgument('s', "semantics", "Test parser with semantics", false);
        var testCodeGen = new SwitchArgument('g', "codegen", "Test code generation", false);
        var testAll = new SwitchArgument('a', "all", "Launch all tests", false);
        
        commandLineParser.Arguments.Add(compilerPath);
        commandLineParser.Arguments.Add(testsDirectory);
        
        commandLineParser.Arguments.Add(testLexer);
        commandLineParser.Arguments.Add(testParser);
        commandLineParser.Arguments.Add(testSemantics);
        commandLineParser.Arguments.Add(testCodeGen);
        commandLineParser.Arguments.Add(testAll);

        var testGroupCertification = new ArgumentGroupCertification("a,p,l,s,g", EArgumentGroupCondition.AtLeastOneUsed);
        commandLineParser.Certifications.Add(testGroupCertification);

        try {
            commandLineParser.ParseCommandLine(args);
        }
        catch (CommandLineException) {
            commandLineParser.ShowUsageHeader = "dotnet Tester.dll [pathToCompiler] [pathToTestsRoot]";
            commandLineParser.ShowUsage();
        }


        if (testLexer.Value || testAll.Value) 
            TestLexer(compilerPath.Value.FullName, testsDirectory.Value.FullName);

        if (testParser.Value || testAll.Value)
            TestParser(compilerPath.Value.FullName, testsDirectory.Value.FullName, "parser");
        
        if (testSemantics.Value || testAll.Value)
            TestParser(compilerPath.Value.FullName, testsDirectory.Value.FullName, "semantics", true);

        if (testCodeGen.Value || testAll.Value) {
            string nasmPath;
            string gccPath;
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                //todo: remove hardcode?
                nasmPath = @"C:\Program Files\NASM\nasm.exe";
                gccPath = @"C:\Users\Alexey\dev\toolchains\mingw-w64\x86_64-8.1.0-win32-seh-rt_v6-rev0\mingw64\bin\gcc.exe";
            }
            else {
                nasmPath = "nasm";
                gccPath = "gcc";
            }
            TestCodeGen(compilerPath.Value.FullName, nasmPath, gccPath, testsDirectory.Value.FullName, "codegen");
        }
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

    static void TestParser(string compilerPath, string testDir, string path, bool semantics = false) {
        var defaultForegroundColor = Console.ForegroundColor;
        
        var testFiles = Directory.GetFiles($"{testDir}/{path}/").Reverse();

        foreach (var testFile in testFiles) {
            if (!testFile.EndsWith(".pas"))
                continue;

            var testName = testFile.Substring(0, testFile.LastIndexOf('.'));

            var flags = "-s ";
            if (!semantics)
                flags += "-c";

            var pr = RunCompiler(compilerPath, $"{flags} -i {testFile}");
            
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

    private static void TestCodeGen(string compilerPath, string nasmPath, string gccPath, string testDir, string path) {
        var defaultForegroundColor = Console.ForegroundColor;
        
        var testFiles = Directory.GetFiles($"{testDir}/{path}/").Reverse();
        var tmpDirPath = $"{testDir}/{path}/tmp";

        foreach (var testFile in testFiles) {
            if (!testFile.EndsWith(".pas"))
                continue;

            var testName = testFile.Substring(0, testFile.LastIndexOf('.'));
            var pr = RunCompiler(compilerPath, $"-a -i {testFile}");
            var foundError = false;

            var testOutFilePath = $"{testName}.out";
            var compile = File.Exists(testOutFilePath);

            if (compile) {
                if (Directory.Exists(tmpDirPath)) {
                    var di = new DirectoryInfo(tmpDirPath);
                    
                    foreach (var file in di.EnumerateFiles()) {
                        file.Delete(); 
                    }
                    
                    foreach (var dir in di.EnumerateDirectories()) {
                        dir.Delete(true); 
                    }
                }
                else {
                    Directory.CreateDirectory(tmpDirPath);
                }
            }


            StreamWriter tmpAsmFile = null;
            
            var tmpAsmFilePath = $"{tmpDirPath}/{Path.GetFileName(testName)}.s";
            if (compile) {
                // nasm doesnt know about utf =(
                tmpAsmFile = new StreamWriter(tmpAsmFilePath, true, Encoding.ASCII);
            }

            using (var answer = File.OpenText($"{testName}.test")) {
                
                while (!pr.StandardOutput.EndOfStream) {
                    if (answer.EndOfStream) {
                        tmpAsmFile?.Close();
                        pr.WaitForExit();
                        break;
                    }
                    
                    var outLine = pr.StandardOutput.ReadLine();
                    var answerLine = answer.ReadLine();

                    if (outLine.Equals(answerLine)) {
                        tmpAsmFile?.WriteLine(outLine);
                        continue;
                    } 
                    
                    foundError = true;
                    PrintDiff(answerLine, outLine, testName);
                    tmpAsmFile?.Close();
                    pr.WaitForExit();
                    break;
                }

                if (foundError) {
                    tmpAsmFile?.Close();
                    pr.WaitForExit();
                    break;
                }
                
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
            
            tmpAsmFile?.Close();
            
            pr.WaitForExit();// Waits here for the process to exit.

            if (!compile) 
                continue;
            
            var nasmPr = new Process();
            
            nasmPr.StartInfo.FileName = nasmPath;
                
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                nasmPr.StartInfo.Arguments = $"-f win64 -o {tmpAsmFilePath}.obj {tmpAsmFilePath}";
            }
            else {
                //todo: check
                nasmPr.StartInfo.Arguments = $"???";
            } 
                
            nasmPr.StartInfo.UseShellExecute = false;
            nasmPr.StartInfo.RedirectStandardOutput = true;
            nasmPr.StartInfo.RedirectStandardError = true;
            nasmPr.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            nasmPr.Start();
            nasmPr.WaitForExit();
            Console.Write(nasmPr.StandardError.ReadToEnd());
            var gccPr = new Process();
            gccPr.StartInfo.FileName = gccPath;
            
            var exePostfix = "";
                
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                exePostfix = ".exe";
            }
            
            gccPr.StartInfo.Arguments = $"-o {tmpAsmFilePath}{exePostfix} {tmpAsmFilePath}.obj";
            
            gccPr.StartInfo.UseShellExecute = false;
            gccPr.StartInfo.RedirectStandardOutput = true;
            gccPr.StartInfo.RedirectStandardError = true;
            gccPr.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            gccPr.Start();
            gccPr.WaitForExit();
            
            
            var compiledPr = new Process();
            
            compiledPr.StartInfo.FileName = $"{tmpAsmFilePath}{exePostfix}";
            compiledPr.StartInfo.UseShellExecute = false;
            compiledPr.StartInfo.RedirectStandardOutput = true;
            compiledPr.StartInfo.RedirectStandardError = true;
            compiledPr.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            compiledPr.Start();

            
            using (var answer = File.OpenText($"{testName}.out")) {
                
                while (!compiledPr.StandardOutput.EndOfStream) {
                    if (answer.EndOfStream) {
                        compiledPr.WaitForExit();
                        break;
                    }
                    
                    var outLine = compiledPr.StandardOutput.ReadLine();
                    var answerLine = answer.ReadLine();

                    if (outLine.Equals(answerLine)) {
                        continue;
                    } 
                    
                    foundError = true;
                    PrintDiff(answerLine, outLine, testName);
                    break;
                }

                if (foundError) {
                    compiledPr.WaitForExit();
                    break;
                }
                
                if (!answer.EndOfStream || !compiledPr.StandardOutput.EndOfStream) {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[-] line count mismatch in {testName}");
                }
                else {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[+] passed {testName}");
                }
                
                Console.ForegroundColor = defaultForegroundColor;
            }
            
            compiledPr.WaitForExit();
        }
        
        
        
        //cleanup
        if (Directory.Exists(tmpDirPath)) {
            Directory.Delete(tmpDirPath, true);
        }
    }
}
}