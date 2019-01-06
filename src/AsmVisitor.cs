using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Compiler {
//return stack usage in qwords
public class AsmVisitor : IAstVisitor<int> {
    private readonly TextWriter _out;
    private readonly AstNode _astRoot;
    private readonly SymStack _symStack;
    
    private Stack<bool> _isLval = new Stack<bool>();
    private bool IsLval => _isLval.Peek();
    
    public AsmVisitor(TextWriter output, AstNode astRoot, SymStack symStack) {
        _out = output;
        _astRoot = astRoot;
        _symStack = symStack;
    }
    
    // save state
    // all recursive callings should be called via this helper in order to save state
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
                        _out.WriteLine($"{writeln.Name}:");
                        _out.WriteLine("enter 0, 0");
                        
                        GenWriteFunctionBody();
                        
                        _out.WriteLine("push 10");
                        _out.WriteLine("mov rcx, rsp");
                        _out.WriteLine("sub rsp, 32");
                        _out.WriteLine("call printf");
                        _out.WriteLine("add rsp, 32");
                        _out.WriteLine("add rsp, 8");
                        
                        _out.WriteLine("leave");        
                        _out.WriteLine("ret");
                        break;
                    
                    case WriteSymFunc write:
                        _out.WriteLine($"{write.Name}:");
                        _out.WriteLine("enter 0, 0");
                        GenWriteFunctionBody();
                        _out.WriteLine("leave");
                        _out.WriteLine("ret");
                        break;
                }
            }
        }
    }

    //without prologue and epilogue
    private void GenWriteFunctionBody() {
        _out.WriteLine("sub rsp, 32");
        _out.WriteLine("lea rcx, [rbp + 16]");
        _out.WriteLine("call printf");
        _out.WriteLine("add rsp, 32");
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
        PushImm64(node.Token.Value);
        return 1;
    }

    public int Visit(FloatNode node) {
        throw new System.NotImplementedException();
    }

    public int Visit(IdentifierNode node) {
        throw new System.NotImplementedException();
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
        throw new System.NotImplementedException();
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
        
        PushImm64(part);
        stackUsage += 1;
        
        var gPointer = asciiBytes.Length - bytesInLastPart - 1;

        while (gPointer >= 0) {
            part = 0;
            for (var i = gPointer; gPointer - i < 8; i--) {
                part <<= 8;
                part += asciiBytes[i];
                
            }

            gPointer -= 8;
            PushImm64(part);
            stackUsage += 1;
        }

        return stackUsage;
    }

    public int Visit(UnaryOperationNode node) {
        throw new System.NotImplementedException();
    }

    public int Visit(AssignNode node) {
        throw new System.NotImplementedException();
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

    private void AllocateStack(int qwords) {
        _out.WriteLine($"sub rsp, {(8*qwords).ToString()}");
    }

    //nasm cannot work with imm64 directly, only via reg
    private void PushImm64(ulong imm64) {
        AllocateStack(2);
        _out.WriteLine($"mov [rsp], rbx");
        _out.WriteLine($"mov rbx, {imm64.ToString()}");        
        _out.WriteLine($"mov [rsp+8], rbx");
        _out.WriteLine("pop rbx");
    }
    
    private void PushImm64(long imm64) {
        AllocateStack(2);
        _out.WriteLine($"mov [rsp], rbx");
        _out.WriteLine($"mov rbx, {imm64.ToString()}");        
        _out.WriteLine($"mov [rsp+8], rbx");
        _out.WriteLine("pop rbx");
    }
    
}
}