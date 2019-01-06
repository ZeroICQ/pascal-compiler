using System;
using System.IO;
using System.Text;
using CommandLineParser.Arguments;
using CommandLineParser.Exceptions;
using CommandLineParser.Validation;

namespace Compiler {

internal static class App {
    public static int  Main(string[] args) {
        Console.OutputEncoding = Encoding.UTF8;
        var commandLineParser = new CommandLineParser.CommandLineParser();
        
        var sourcePath = new FileArgument('i', "input", "Source file path") { Optional = false };
        var lexicalAnalysis = new SwitchArgument('l', "lexical", false);
        var syntaxAnalysis = new SwitchArgument('s', "syntax", false);
        var semanticsCheck = new SwitchArgument('c', "semantics", "turn off semantics check", true);
        var codeGeneration = new SwitchArgument('a', "assembler", "generate assembler", false);
        var optimization = new SwitchArgument('o', "optimization", "optimization", false);
        
        commandLineParser.Arguments.Add(sourcePath);
        commandLineParser.Arguments.Add(lexicalAnalysis);
        commandLineParser.Arguments.Add(syntaxAnalysis);
        commandLineParser.Arguments.Add(semanticsCheck);
        commandLineParser.Arguments.Add(codeGeneration);
        commandLineParser.Arguments.Add(optimization);

        var compileStageGroupCertification = new ArgumentGroupCertification("l,s,a", EArgumentGroupCondition.ExactlyOneUsed);
        commandLineParser.Certifications.Add(compileStageGroupCertification);

        try {
            commandLineParser.ParseCommandLine(args);
        }
        catch (CommandLineException) {
            commandLineParser.ShowUsage();
            return 1;
        }

        using (var input = new StreamReader(sourcePath.OpenFileRead())) {
            if (lexicalAnalysis.Value)
                PerformLexicalAnalysis(input);
            
            if (syntaxAnalysis.Value)
                PerformSyntaxAnalysis(input, semanticsCheck.Value);
            
            if (codeGeneration.Value)
                PerformCodeGeneration(input, optimization.Value);
        }
            
        return 0;
    }

    private static void PerformCodeGeneration(StreamReader streamReader, bool useOptimization) {
        var compiler = new Compiler(streamReader, true);
        compiler.printAsm();
    }

    private static void PerformSyntaxAnalysis(StreamReader streamReader, bool checkSemantics) {
        var compiler = new Compiler(streamReader, checkSemantics);
        compiler.PrintAst();
    }

    private static void PerformLexicalAnalysis(StreamReader streamReader) {
        var compiler = new Compiler(streamReader, false);
            
        Token lt = null;
            
        do {
            try {
                lt = compiler.GetNextToken();
                Console.WriteLine("{0},{1}\t{2}\t{3}\t{4}", lt.Line.ToString(), lt.Column.ToString(),
                    lt.Type.ToString(), lt.StringValue, lt.Lexeme);
            }
            catch (LexerException ex) {
                Console.WriteLine(ex.Message);
            }
        } while (!(lt is EofToken));
    }
}
}
