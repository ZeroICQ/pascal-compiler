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
        
        const string fpcPath = @"C:\Users\Alexey\dev\toolchains\lazarus\fpc\3.0.4\bin\x86_64-win64";
        const string compatibilityUnitName = "compatibility";
        
        //clean tmp dir
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

        foreach (var testSourcePath in testFiles) {
            if (!testSourcePath.EndsWith(".pas") || testSourcePath.EndsWith(compatibilityUnitName+".pas"))
                continue;

            var testName = Path.GetFileNameWithoutExtension(testSourcePath);
            var testAnswerPath = $"{testDir}/{path}/{testName}.test";
            var compilerPr = RunCompiler(compilerPath, $"-a -i {testSourcePath}");

//            var testOutFilePath = $"{testDir}/{path}/{testName}.out";
//            
//            var needCompile = File.Exists(testOutFilePath);
            
//            if (!needCompile)
//                continue;

            var tmpAsmFilePath = $"{tmpDirPath}/{Path.GetFileName(testName)}.s";

            //nasm doesnt know about utf
            compilerPr.Start();
            using (var tmpAsmFile = new StreamWriter(tmpAsmFilePath, false, Encoding.ASCII)) {
                while (!compilerPr.StandardOutput.EndOfStream) {
                    tmpAsmFile.WriteLine(compilerPr.StandardOutput.ReadLine());
                }
            }

            var nasmPr = new Process();
            nasmPr.StartInfo.FileName = nasmPath;
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                nasmPr.StartInfo.Arguments = $"-f win64 -o {tmpAsmFilePath}.obj {tmpAsmFilePath}";
            }
            else {
                //todo: check
                nasmPr.StartInfo.Arguments = $"-f elf64 -o {tmpAsmFilePath}.obj {tmpAsmFilePath}";
            } 
                
            nasmPr.StartInfo.UseShellExecute = false;
            nasmPr.StartInfo.RedirectStandardOutput = true;
            nasmPr.StartInfo.RedirectStandardError = true;
            nasmPr.StartInfo.StandardOutputEncoding = Encoding.ASCII;
            nasmPr.StartInfo.StandardErrorEncoding = Encoding.ASCII;
            nasmPr.Start();
            nasmPr.WaitForExit();

            var nasmOutput = nasmPr.StandardOutput.ReadToEnd();
            if (nasmOutput.Length != 0) {
                Console.WriteLine("NASM output:");
                Console.Write(nasmOutput);
            }

            if (nasmPr.ExitCode != 0) {
                Console.WriteLine("NASM error:");
                Console.Write(nasmPr.StandardError.ReadToEnd());
                return;
            }
            
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
            gccPr.StartInfo.StandardErrorEncoding= Encoding.UTF8;
            gccPr.Start();
            gccPr.WaitForExit();
            

            var gccOutput = gccPr.StandardOutput.ReadToEnd();
            if (gccOutput.Length != 0) {
                Console.WriteLine("gcc output:");
                Console.Write(gccOutput);
            }

            if (gccPr.ExitCode != 0) {
                Console.WriteLine("gcc error:");
                Console.Write(gccPr.StandardError.ReadToEnd());
                return;
            }

            
            //now compile with fpc
            
            var fpcPr = new Process();
            fpcPr.StartInfo.FileName = $"{fpcPath}\\fpc.exe";
            
            //facompatibility
            var fpcCompiledExePath = $"{tmpDirPath}/{Path.GetFileName(testName)}FPC.exe";
            fpcPr.StartInfo.Arguments = $"-CF64 -Fa{compatibilityUnitName} -Fu{testDir}/{path} -FE{tmpDirPath} -o{Path.GetFileName(testName)}FPC.exe {testSourcePath}";
            
            fpcPr.StartInfo.UseShellExecute = false;
            fpcPr.StartInfo.RedirectStandardOutput = true;
            fpcPr.StartInfo.RedirectStandardError = true;
            fpcPr.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            fpcPr.StartInfo.StandardErrorEncoding= Encoding.UTF8;
            fpcPr.Start();
            fpcPr.WaitForExit();
            
            if (fpcPr.ExitCode != 0) {
                Console.WriteLine("fpc output:");
                Console.Write(fpcPr.StandardOutput.ReadToEnd());
                
                Console.WriteLine("fpc error:");
                Console.Write(fpcPr.StandardError.ReadToEnd());
                return;
            }
            
            
            var compiledMyPr = new Process();
            compiledMyPr.StartInfo.FileName = $"{tmpAsmFilePath}{exePostfix}";
            compiledMyPr.StartInfo.UseShellExecute = false;
            compiledMyPr.StartInfo.RedirectStandardOutput = true;
            compiledMyPr.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            compiledMyPr.Start();
            compiledMyPr.WaitForExit();
            
            var compiledFpcPr = new Process();
            compiledFpcPr.StartInfo.FileName = $"{fpcCompiledExePath}";
            compiledFpcPr.StartInfo.UseShellExecute = false;
            compiledFpcPr.StartInfo.RedirectStandardOutput = true;
            compiledFpcPr.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            compiledFpcPr.Start();
            compiledFpcPr.WaitForExit();
            
            var errFound = false;
            while (!compiledMyPr.StandardOutput.EndOfStream) {
                if (compiledFpcPr.StandardOutput.EndOfStream)
                    break;
                
                var outLine = compiledMyPr.StandardOutput.ReadLine();
                var answerLine = compiledFpcPr.StandardOutput.ReadLine();

                if (outLine.Equals(answerLine))
                    continue;
                
                PrintDiff(answerLine, outLine, testName);
                compilerPr.WaitForExit();
                errFound = true;
                break;
            }
            
            if (errFound)
                break;
            
            if (!compiledFpcPr.StandardOutput.EndOfStream || !compiledMyPr.StandardOutput.EndOfStream) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[-] line count mismatch at compilation phase in {testName}");
            }
            else {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[+] passed compile {testName}");
            }
            
            Console.ForegroundColor = defaultForegroundColor;
            
            compiledMyPr.WaitForExit();
        }
        
        //cleanup
//        if (Directory.Exists(tmpDirPath)) {
//            Directory.Delete(tmpDirPath, true);
//        }
    }
}
}