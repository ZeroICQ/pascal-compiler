using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Compiler {
//return stack usage in qwords
public class AsmVisitor : IAstVisitor<int> {
    private readonly TextWriter _out;
    private readonly AstNode _astRoot;
    private readonly SymStack _symStack;
    private ulong _labelCounter;
    
    private Stack<bool> _isLval = new Stack<bool>();
    private bool IsLval => _isLval.Peek();
    
    public AsmVisitor(TextWriter output, AstNode astRoot, SymStack symStack) {
        _out = output;
        _astRoot = astRoot;
        _symStack = symStack;
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
                            
                            case SymFloat symInt:
                                var initFloatVal = ((SymFloatConst) symVar.InitialValue)?.Value ?? 0;
                                _out.WriteLine($"{symVar.Name}: dq {initFloatVal.ToString()}");
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
                    case WritelnSymFunc writeln:
                        FunctionPrologue(writeln.Name);
                        WriteFunctionBody();
                        Push("10");
                        Mov("rcx", "rsp");
                        Sub("rsp", "32");
                        Call("printf");
                        Add("rsp", "32");
                        Add("rsp", "8");
                        FunctionEpilogue();
                        break;
                    
                    case WriteSymFunc write:
                        FunctionPrologue(write.Name);
                        WriteFunctionBody();
                        FunctionEpilogue();
                        break;
                    
                    case IntWriteSymFunc intWrite:
                        IntWriteFunctionBody(intWrite.Name, false);
                        break;
                    
                    case IntWritelnSymFunc intWriteln:
                        IntWriteFunctionBody(intWriteln.Name, true);
                        break;
                    
                    case FloatWriteSymFunc floatWrite:
                        FloatWriteFunctionBody(floatWrite.Name, false);
                        break;
                    
                    case FloatWritelnSymFunc floatWriteln:
                        FloatWriteFunctionBody(floatWriteln.Name, true);
                        break;
                }
            }
        }
    }

    private void CallPrintfDecorator(string format, string name, bool isNewline) {
        FunctionPrologue(name);
        var intWritelnStackUse = PushStringInStack(format + (isNewline ? "\n" : ""));
        Mov("rcx", "rsp");
        Mov("rdx", ArgumentValue(0));
        intWritelnStackUse += AllocateStack(4);
        Call("printf");
        FreeStack(intWritelnStackUse);
        FunctionEpilogue();
    }

    private void FloatWriteFunctionBody(string name, bool isNewline) {
        CallPrintfDecorator("%g", name, isNewline);
    }

    private void IntWriteFunctionBody(string name, bool isNewline) {
        CallPrintfDecorator("%i", name, isNewline);        
    }
    
    // number starts with 0 
    private string ArgumentValue(int number) {
        return $"[rbp+{(16 + 8 * number).ToString()}]";
    }

    //without prologue and epilogue
    private void WriteFunctionBody() {
        Sub("rsp", "32");
        Lea("rcx", "[rbp + 16]");
        Call("printf");
        Add("rsp", "32");
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
        PushImm64(node.Token.StringValue);
        return 1;
    }

    public int Visit(FloatNode node) {
        //nasm requires dot in float number
        var dot = node.Token.StringValue.Contains('.') ? "" : ".";
        PushImm64($"__float64__({node.Token.StringValue}{dot})");
        return 1;
    }

    public int Visit(IdentifierNode node) {
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
                                Push($"{symVar.Name}");
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
                                        Push($"qword [{symVar.Name}]");
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
            
            case SymConst symConst:
                Debug.Assert(!IsLval);
                stackUse = 1;
                switch (symConst) {
                    case SymIntConst intConst: 
                        Push(intConst.Value.ToString());
                        break;
                    case SymFloatConst floatConst:
                        Push(floatConst.Value.ToString(CultureInfo.InvariantCulture));
                        break;
                    case SymCharConst charConst:
                        Push(charConst.Value.ToString());
                        break;
                }
                
                break;
        }

        return stackUse;
    }

    public int Visit(FunctionCallNode node) {
        var stackUse = 0;
        for (var i = node.Args.Count - 1; i >= 0; i--) {
            //todo: check l/rval
            stackUse += Accept(node.Args[i]);    
        }
        //crutch since functions are neither symbols nor types
        var funcIdentifier = node.Name as IdentifierNode;
        
        _out.WriteLine($"call {funcIdentifier.Token.Value}");
        FreeStack(stackUse);
        return 0;
    }

    public int Visit(CastNode node) {
        // only int->float cast is allowed now
        var stackUse = Accept(node.Expr);
        Debug.Assert(stackUse == 1);
        Finit();
        //ASK: better conversion?
        Fild("qword [rsp]");
        Fst("qword  [rsp]");
        
        return stackUse;
    }

    public int Visit(AccessNode node) {
        throw new System.NotImplementedException();
    }

    public int Visit(IndexNode node) { 
        throw new System.NotImplementedException();
    }

    public int Visit(StringNode node) {
        return PushStringInStack(node.Token.Value);
    }

    private int PushStringInStack(string str) {
        var asciiBytes = Encoding.ASCII.GetBytes(str);
        var bytesInLastPart = asciiBytes.Length % 8;
        ulong part = 0;

        var stackUsage = 0;
        
        for (var i = asciiBytes.Length - 1; i >= asciiBytes.Length - bytesInLastPart; i--) {
            part <<= 8;
            part += asciiBytes[i];
        }
        
        PushImm64(part.ToString());
        stackUsage += 1;
        
        var gPointer = asciiBytes.Length - bytesInLastPart - 1;

        while (gPointer >= 0) {
            part = 0;
            for (var i = gPointer; gPointer - i < 8; i--) {
                part <<= 8;
                part += asciiBytes[i];
                
            }

            gPointer -= 8;
            PushImm64(part.ToString());
            stackUsage += 1;
        }

        return stackUsage;
    }

    public int Visit(UnaryOperationNode node) {
        throw new System.NotImplementedException();
    }

    public int Visit(AssignNode node) {
        var lhsStackUse = Accept(node.Left, true);
        var rhsStackUse = Accept(node.Right);
        //todo: test with records
        Debug.Assert(lhsStackUse == 1);
        
        //keep in r9 dst pointer        
        Mov("r9", "rsp");
        Add("r9", (8*rhsStackUse).ToString());
        Mov("r9", "[r9]");
        
        //rcx - counter
        Xor("rcx", "rcx");
        // loop
        var label = WriteGetUniqueLabel();
        Pop("qword [r9]");
        Add("r9", "8");
        
        Inc("rcx");
        Cmp("rcx", rhsStackUse.ToString());
        Jl(label);
        
        //lhs
        FreeStack(1);
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
        throw new System.NotImplementedException();
    }

    private void FreeStack(int qwords) {
        _out.WriteLine($"add rsp, {(8*qwords).ToString()}");
    }

    private int AllocateStack(int qwords) {
        _out.WriteLine($"sub rsp, {(8*qwords).ToString()}");
        return qwords;
    }

    // nasm cannot work with imm64 directly, only via register
    private void PushImm64(string imm64) {
        AllocateStack(2);
        Mov("[rsp]", "rbx");
        Mov("rbx", imm64);
        Mov("[rsp+8]", "rbx");
        Pop("rbx");
    }

    private string WriteGetUniqueLabel() {
        var label = $"meh_{(_labelCounter++).ToString()}";
        _out.Write($"{label}:");
        return label;
    }
    
    // helpers
    private void Lea(string lhs, string rhs) {
        _out.WriteLine($"lea {lhs}, {rhs}");
    }
    
    private void Call(string callee) {
        _out.WriteLine($"call {callee}");
    }

    private void Sub(string lhs, string rhs) {
        _out.WriteLine($"sub {lhs}, {rhs}");        
    }
    
    private void Add(string lhs, string rhs) {
        _out.WriteLine($"add {lhs}, {rhs}");        
    }
    
    private void Mov(string to, string from) {
        _out.WriteLine($"mov {to}, {from}");
    }

    private void Xor(string lhs, string rhs) {
        _out.WriteLine($"xor {lhs}, {rhs}");
    }

    private void Push(string arg) {
        _out.WriteLine($"push {arg}");
    }
    
    private void Pop(string to) {
        _out.WriteLine($"pop {to}");
    }
    
    private void Inc(string arg) {
        _out.WriteLine($"inc {arg}");
    }

    private void Cmp(string lhs, string rhs) {
        _out.WriteLine($"cmp {lhs}, {rhs}");
    }
    
    private void Jl(string label) {
        _out.WriteLine($"jl {label}");
    }

    private void Finit() {
        _out.WriteLine("finit");
    }

    private void Fild(string arg) {
        _out.WriteLine($"fild {arg}");
    }
    
    private void Fst(string arg) {
        _out.WriteLine($"fst {arg}");
    }

    private void FunctionPrologue(string func) {
        _out.WriteLine($"{func}:");
        _out.WriteLine("enter 0, 0");
    }

    private void FunctionEpilogue() {
        _out.WriteLine("leave");
        _out.WriteLine("ret");
    }
    
}
}