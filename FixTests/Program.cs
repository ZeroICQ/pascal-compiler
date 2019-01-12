using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace FixTests {
class Programm {
        
    static void Main(string[] args) {
        var defaultForegroundColor = Console.ForegroundColor;
        
        var testFiles = Directory.GetFiles(@"C:\Users\Alexey\dev\CSharp\pascal-compiler\tests\semantics").Reverse();

        foreach (var testFile in testFiles) {
            if (!testFile.EndsWith(".pas"))
                continue;

            var testName = testFile.Substring(0, testFile.LastIndexOf('.'));

            var flags = "-s ";
            var pr = RunCompiler(@"C:\Users\Alexey\dev\CSharp\pascal-compiler\src\bin\Debug\netcoreapp2.1\Compiler.dll", $"{flags} -i {testFile}");
            
            using (var answer = new StreamWriter($"{testName}.test", false, Encoding.UTF8)) {
                while (!pr.StandardOutput.EndOfStream) {
                    answer.Write(pr.StandardOutput.Read());
                }
            }
            pr.WaitForExit();// Waits here for the process to exit.
        }
    }

    private static Process RunCompiler(string path, string flags) {
        var pr = new Process();
        
        pr.StartInfo.FileName = "dotnet";
        pr.StartInfo.Arguments = $"{path} {flags}";
        pr.StartInfo.UseShellExecute = false;
        pr.StartInfo.RedirectStandardOutput = true;
        pr.StartInfo.StandardOutputEncoding = Encoding.ASCII;
        pr.Start();
        
        return pr;
    }
}
}