using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using static Compiler.CodeGenerator;
using static Compiler.AsmArg;
using static Compiler.DoubleArgCmd;
using static Compiler.SingleArgCmd;
using static Compiler.NoArgCmd;

namespace Compiler {
//return stack usage in qwords
public class AsmVisitor : IAstVisitor<int> {
    private readonly TextWriter _out;
    private readonly AstNode _astRoot;
    private readonly SymStack _symStack;
    private ulong _labelCounter;
    private CodeGenerator g;
    
    private Stack<bool> _isLval = new Stack<bool>();
    private bool IsLval => _isLval.Peek();
    
    public AsmVisitor(TextWriter output, AstNode astRoot, SymStack symStack) {
        _out = output;
        _astRoot = astRoot;
        _symStack = symStack;
        g = new CodeGenerator(_out);
    }
    
    // save state
    // all recursive callings should be called via this helper in order to save state
    // Accept can modify any register freely
    private int Accept(AstNode node, bool isLval = false) {
        _isLval.Push(isLval);
        var r = node.Accept(this);
        _isLval.Pop();
        return r;
    }


    public void Generate() {
                
        // 64 bits mode
        _out.WriteLine("bits 64");
        _out.WriteLine("default rel");
        
        //data section
        _out.WriteLine("section .data");
        GenerateGlobals();
        
        // code section
        _out.WriteLine("section .text");
        _out.WriteLine("global main");
        _out.WriteLine("extern printf");
        
        //define all functions
        GenerateFunctions();
        
        //start main
        _out.WriteLine("main:");
        Accept(_astRoot);
    }

    private void GenerateGlobals() {
        
        //todo: add records, arrays, initializers
        foreach (var table in _symStack) {
            foreach (var symbol in table) {
                
                switch (symbol) {
                //main switch start
                                        
                    case SymVar symVar:
                        
                        switch (symVar.Type) {
                            case SymInt symInt:
                                var initIntVal = ((SymIntConst) symVar.InitialValue)?.Value ?? 0;
                                _out.WriteLine($"{symVar.Name}: dq {initIntVal.ToString()}");
                                break;
                            
                            case SymDouble symInt:
                                var initDoubleVal = ((SymDoubleConst) symVar.InitialValue)?.Value ?? 0;
                                _out.WriteLine($"{symVar.Name}: dq {initDoubleVal.ToString(CultureInfo.InvariantCulture)}");
                                break;
                            
                            case SymChar symInt:
                                var initCharVal = ((SymCharConst) symVar.InitialValue)?.Value ?? 0;
                                _out.WriteLine($"{symVar.Name}: db {initCharVal.ToString()}");
                                break;
                        }
                        
                        break;
                }
                //main switch end
            }
        }
    }

    private void GenerateFunctions() {
        foreach (var table in _symStack) {
            foreach (var symbol in table) {
                if (!(symbol is SymFunc symFunc))
                    continue;

                switch (symFunc) {
                    case StringWriteSymFunc strWrite:
                        g.FunctionPrologue(strWrite.Name);
                        g.G(Lea, Rcx(), g.ArgumentValue(0));
                        g.CallPrintf();
                        g.FunctionEpilogue();
                        break;
                    
                    case IntWriteSymFunc intWrite:
                        CallPrintfDecorator("%i", intWrite.Name);
                        break;
                    
                    case DoubleWriteSymFunc doubleWrite:
                        CallPrintfDecorator("% .16E", doubleWrite.Name);
                        break;
                    
                    case CharWriteSymFunc charWrite:
                        CallPrintfDecorator("%c", charWrite.Name);
                        break;
                }
            }
        }
    }

    private void CallPrintfDecorator(string format, string name) {
        g.FunctionPrologue(name);
        var stackUse = g.PushStringInStack(format);
        g.G(Mov, Rdx(), g.ArgumentValue(0));
        g.G(Mov, Rcx(), Rsp());
        
        g.CallPrintf();
        g.FreeStack(stackUse);
        g.FunctionEpilogue();
    }

    
    // left operand goes in stack first
    public int Visit(BlockNode node) {
        var stackUse = 0;
        foreach (var sttmnt in node.Statements) {
            stackUse += Accept(sttmnt);
        }

        return stackUse;
    }

    public int Visit(BinaryExprNode node) {
//        switch (node.Operation) {
//            
//            case OperatorToken op:
//                switch (op.Value) {
//                    case Constants.Operators.Assign:
//                        Accept(node.Left, );
//                }
//                break;
//            
//        }

        return 0;
    }

    public int Visit(IntegerNode node) {
        //aways lval
        g.Comment($"pushing imm64 integer {node.Token.Value.ToString()}");
        g.PushImm64(node.Token.Value);
        return 1;
    }

    public int Visit(DoubleNode node) {
        //nasm requires dot in double number
//        var dot = node.Token.StringValue.Contains('.') ? "" : ".";
//        PushImm64($"__float64__({node.Token.StringValue}{dot})");
        g.PushImm64(node.Token.Value);
        return 1;
    }

    public int Visit(IdentifierNode node) {
        g.Comment($"visiting identifier node {node.Token.Lexeme}");
        
        var stackUse = 0;
        
        var realType = node.Type;

        if (realType is SymTypeAlias symTypeAlias)
            realType = symTypeAlias.Type;
        
        switch (node.Symbol) {
            case SymVar symVar:

                switch (IsLval) {
                    case true:
                        switch (symVar.VarType) {
                            case SymVar.VarTypeEnum.Global:
                                g.G(Push, symVar.Name);
                                stackUse = 1;
                                break;
                            case SymVar.VarTypeEnum.Local:
                            case SymVar.VarTypeEnum.Parameter:
                            case SymVar.VarTypeEnum.VarParameter:
                            case SymVar.VarTypeEnum.ConstParameter:
                            case SymVar.VarTypeEnum.OutParameter:
                                throw new NotImplementedException();
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        
                        break;
                    
                    //case rval
                    case false:
                        switch (symVar.VarType) {
                            case SymVar.VarTypeEnum.Global:
                                
                                switch (realType) {
                                    case SymScalar scalar:
                                        g.G(Push, $"qword [{symVar.Name}]");
//                                        Push($"qword [{symVar.Name}]");
                                        stackUse = 1;
                                        break;
                                }
                                
                                break;
                            case SymVar.VarTypeEnum.Local:
                            case SymVar.VarTypeEnum.Parameter:
                            case SymVar.VarTypeEnum.VarParameter:
                            case SymVar.VarTypeEnum.ConstParameter:
                            case SymVar.VarTypeEnum.OutParameter:
                                throw new NotImplementedException();
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        
                        break;
                }
                
                break;
            
//            case SymConst symConst:
//                Debug.Assert(!IsLval);
//                stackUse = 1;
//                switch (symConst) {
//                    case SymIntConst intConst: 
//                        Push(intConst.Value.ToString());
//                        break;
//                    case SymDoubleConst doubleConst:
//                        Push(doubleConst.Value.ToString(CultureInfo.InvariantCulture));
//                        break;
//                    case SymCharConst charConst:
//                        Push(charConst.Value.ToString());
//                        break;
//                }
//                
//                break;
        }

        return stackUse;
    }

    public int Visit(FunctionCallNode node) {
        throw new NotImplementedException();
        var stackUse = 0;
        for (var i = node.Args.Count - 1; i >= 0; i--) {
            //todo: check l/rval
            stackUse += Accept(node.Args[i]);    
        }
        //crutch since functions are neither symbols nor types
        var funcIdentifier = node.Name as IdentifierNode;
        
        _out.WriteLine($"call {funcIdentifier.Token.Value}");
//        FreeStack(stackUse);
        return 0;
    }

    public int Visit(CastNode node) {
        // only int->double cast is allowed now
        var stackUse = Accept(node.Expr);
        Debug.Assert(stackUse == 1);
        g.G(Finit);
        g.G(Fild, QWord(Der(Rsp())));
        g.G(Fst, QWord(Der(Rsp())));
        
        return stackUse;
    }

    public int Visit(AccessNode node) {
        throw new System.NotImplementedException();
    }

    public int Visit(IndexNode node) { 
        throw new System.NotImplementedException();
    }

    public int Visit(StringNode node) {
        return g.PushStringInStack(node.Token.Value);
    }

    public int Visit(UnaryOperationNode node) {
        throw new System.NotImplementedException();
    }

    public int Visit(AssignNode node) {
        g.Comment("assignment. evaluating lhs");
        var lhsStackUse = Accept(node.Left, true);
        
        g.Comment("assignment. evaluating rhs");
        var rhsStackUse = Accept(node.Right);
        //todo: test with records
        Debug.Assert(lhsStackUse == 1);

        if (rhsStackUse == 1) {
            g.Comment("assign scalar.");
            g.G(Pop, Rbx());
            g.G(Pop, Rax());
            g.G(Mov, Der(Rax()), Rbx());
        }
        
//        Comment($"assign");
//        //keep in r9 dst pointer        
//        Mov("r9", "rsp");
//        Add("r9", (8*rhsStackUse).ToString());
//        Mov("r9", "[r9]");
//        
//        //rcx - counter
//        Xor("rcx", "rcx");
//        // loop
//        var label = WriteGetUniqueLabel();
//        Pop("qword [r9]");
//        Add("r9", "8");
//        
//        Inc("rcx");
//        Cmp("rcx", rhsStackUse.ToString());
//        Jl(label);
//        
//        //lhs
//        g.FreeStack(1);

        return 0;
    }

    public int Visit(IfNode node) {
        throw new System.NotImplementedException();
    }

    public int Visit(WhileNode node) {
        throw new System.NotImplementedException();
    }

    public int Visit(ProcedureCallNode node) {
        return  Accept(node.Function);
    }

    public int Visit(ForNode node) {
        throw new System.NotImplementedException();
    }

    public int Visit(ControlSequence node) {
        throw new System.NotImplementedException();
    }

    public int Visit(EmptyStatementNode node) {
        return 0;
    }

    public int Visit(CharNode node) {
        g.PushImm64((long)node.Value);
        return 1;
    }

    public int Visit(WritelnStatementNode node) {
        
        foreach (var arg in node.Args) {
            var realType = arg.Type;
            
            if (realType is SymTypeAlias symTypeAlias)
                realType = symTypeAlias.Type;
            
            Debug.Assert(realType is SymInt || realType is SymDouble || realType is SymChar || realType is SymString);
            
            switch (realType) {
                case SymInt symInt:
                    var intStackUsage = Accept(arg);
                    g.G(Call, _symStack.IntWrite.Name);
                    g.FreeStack(intStackUsage);
                    break;
                
                case SymDouble symDouble:
                    var doubleStackUsage = Accept(arg);
                    g.G(Call, _symStack.DoubleWrite.Name);
                    g.FreeStack(doubleStackUsage);
                    break;
                
                case SymChar symChar:
                    var charStackUsage = Accept(arg);
                    g.G(Call, _symStack.CharWrite.Name);
                    g.FreeStack(charStackUsage);
                    break;
                
                case SymString symString:
                    var stringStackUsage = Accept(arg);
                    g.G(Call, _symStack.StringWrite.Name);
                    g.FreeStack(stringStackUsage);
                    break;
            }
        }

        var su = g.PushStringInStack("\n");
        g.G(Mov, Rcx(), Rsp());
        g.CallPrintf();
        g.FreeStack(su);
        
        return 0;
    }

//    private int AllocateStack(int qwords) {
//        _out.WriteLine($"sub rsp, {(8*qwords).ToString()}");
//        return qwords;
//    }

    // nasm cannot work with imm64 directly, only via register
//    private void PushImm64(string imm64) {
//        AllocateStack(2);
//        g.G(Mov, Addr(Rsp()), Rbx());
//        g.G(Mov, Rbx(), imm64);
////        Mov("[rsp]", "rbx");
////        Mov("rbx", imm64);
////        Mov("[rsp+8]", "rbx");
////        Pop("rbx");
//    }

    private string WriteGetUniqueLabel() {
        var label = $"meh_{(_labelCounter++).ToString()}";
        _out.Write($"{label}:");
        return label;
    }
    
    // helpers

//    private void Sub(string lhs, string rhs) {
//        _out.WriteLine($"sub {lhs}, {rhs}");        
//    }
//    
//    private void Add(string lhs, string rhs) {
//        _out.WriteLine($"add {lhs}, {rhs}");        
//    }
//
//    private void Xor(string lhs, string rhs) {
//        _out.WriteLine($"xor {lhs}, {rhs}");
//    }
//    
//    private void Inc(string arg) {
//        _out.WriteLine($"inc {arg}");
//    }
//
//    private void Cmp(string lhs, string rhs) {
//        _out.WriteLine($"cmp {lhs}, {rhs}");
//    }
//    
//    private void Jl(string label) {
//        _out.WriteLine($"jl {label}");
//    }
//
//    private void Finit() {
//        _out.WriteLine("finit");
//    }
//
//    private void Fild(string arg) {
//        _out.WriteLine($"fild {arg}");
//    }
//    
//    private void Fst(string arg) {
//        _out.WriteLine($"fst {arg}");
//    }
    
}
}