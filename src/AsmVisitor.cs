using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
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
    private CodeGenerator g;
    
    private Stack<bool> _isLval = new Stack<bool>();
    private bool IsLval => _isLval.Peek();

    private readonly string _txtLabelFalse = $"{SymStack.InternalPrefix}FALSE"; 
    private readonly string _txtLabelTrue = $"{SymStack.InternalPrefix}TRUE"; 
    private readonly string _txtLabelPrintInt = $"{SymStack.InternalPrefix}PRINT_INT"; 
    private readonly string _txtLabelPrintDouble = $"{SymStack.InternalPrefix}PRINT_DOUBLE"; 
    private readonly string _txtLabelPrintChar = $"{SymStack.InternalPrefix}PRINT_CHAR"; 
    
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
        //predefined globals
        g.DeclareVariable(_txtLabelFalse, "FALSE");
        g.DeclareVariable(_txtLabelTrue, "TRUE");
        g.DeclareVariable(_txtLabelPrintInt, "%lli");
        g.DeclareVariable(_txtLabelPrintDouble, "% .16LE");
        g.DeclareVariable(_txtLabelPrintChar, "%c");
        
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
        
        //return code 0
        g.G(Xor, Rax(), Rax());
        g.G(Ret);
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
                                var initDoubleVal = (((SymDoubleConst) symVar.InitialValue)?.Value ?? 0).ToString(CultureInfo.InvariantCulture);
                                var dot = initDoubleVal.Contains('.') ? "" : ".";
                                _out.WriteLine($"{symVar.Name}: dq {initDoubleVal}{dot}");
                                break;
                            
                            case SymChar symChar:
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
                        CallPrintfDecorator(_txtLabelPrintInt, intWrite.Name);
                        break;
                    
                    case DoubleWriteSymFunc doubleWrite:
                        CallPrintfDecorator(_txtLabelPrintDouble, doubleWrite.Name);
                        break;
                    
                    case CharWriteSymFunc charWrite:
                        CallPrintfDecorator(_txtLabelPrintChar, charWrite.Name);
                        break;
                    
                    case BoolWriteSymFunc boolWrite:
                        g.FunctionPrologue(boolWrite.Name);

                        var endLabel = g.GetUniqueLabel();
                        var printFalseLabel = g.GetUniqueLabel();
                        
                        g.G(Cmp, QWord(g.ArgumentValue(0)), 1);
                        g.G(Jne, printFalseLabel);
                        
                        g.G(Mov, Rcx(), _txtLabelTrue);
                        g.G(Jmp, endLabel);
                        
                        g.Label(printFalseLabel);
                        g.G(Mov, Rcx(), _txtLabelFalse);
                        
                        g.Label(endLabel);
                        g.CallPrintf();
                        g.FunctionEpilogue();
                        break;
                }
            }
        }
    }

    private void CallPrintfDecorator(string formatLabel, string name) {
        g.FunctionPrologue(name);
        
        g.G(Mov, Rdx(), g.ArgumentValue(0));
        g.G(Mov, Rcx(), formatLabel);
        g.CallPrintf();
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
        g.Comment($"binary operation for {node.Operation.StringValue} before eval operands");

        var lhsStackUsage = Accept(node.Left);
        var rhsStackUsage = Accept(node.Right);
        
        g.Comment($"binary operation for {node.Operation.StringValue} after eval operands");
        var stackUsage = 0;
        
        switch (node.Operation) {
            
            case OperatorToken op:
                
                switch (op.Value) {
                    case Constants.Operators.Plus:

                        switch (node.Type) {
                            case SymInt _:
                                g.G(Pop, Rbx());
                                g.G(Add, Der(Rsp()), Rbx());
                                stackUsage = 1;
                                break;
                        }
                        
                        break;
                    case Constants.Operators.Minus:
                        
                        switch (node.Type) {
                            case SymInt _:
                                g.G(Pop, Rbx());
                                g.G(Sub, Der(Rsp()), Rbx());
                                stackUsage = 1;
                                break;
                        }
                        
                        break;
                    
                    case Constants.Operators.Multiply:
                        
                        switch (node.Type) {
                            case SymInt _:
                                g.G(Pop, Rbx());
                                g.G(Pop, Rax());
                                g.G(Cqo);
                                g.G(Imul, Rax(), Rbx());
                                g.G(Push, Rax());
                                stackUsage = 1;
                                break;
                            
                            case SymDouble _:
                                g.G(Pop, Rax());
                                g.G(Movq, Xmm1(), Rax());
                                
                                g.G(Pop, Rax());
                                g.G(Movq, Xmm0(), Rax());
                                
                                g.G(Mulsd, Xmm0(), Xmm1());
                                
                                g.G(Movq, Rax(), Xmm0());
                                g.G(Push, Rax());
                                stackUsage = 1;
                                break;
                        }
                        break;
                    
                    case Constants.Operators.Divide:
                        Debug.Assert(node.Type is SymDouble);
                        g.G(Pop, Rax());
                        g.G(Movq, Xmm1(), Rax());
                        
                        g.G(Pop, Rax());
                        g.G(Movq, Xmm0(), Rax());
                                
                        g.G(Divsd, Xmm0(), Xmm1());
                                
                        g.G(Movq, Rax(), Xmm0());
                        g.G(Push, Rax());
                        stackUsage = 1;
                        break;
                    
                    // compare operators
                    
                    case Constants.Operators.Less:
                        //node.left.type should be == node.right.type
                        switch (node.Left.Type) {
                            case SymInt _:
                                g.CmpIntegers(Jl);
                                stackUsage = 1;
                                break;
                            
                            case SymDouble _:
                                g.CmpDoubles(Cmpltsd);
                                stackUsage = 1;
                                break;
                        }
                        break;
                    
                    case Constants.Operators.More:
                        switch (node.Left.Type) {
                            case SymInt _:
                                g.CmpIntegers(Jg);
                                stackUsage = 1;
                                break;
                            
                            case SymDouble _:
                                g.CmpDoubles(Cmpltsd, true);
                                stackUsage = 1;
                                break;
                        }
                        break;
                    
                    case Constants.Operators.LessOrEqual:
                        switch (node.Left.Type) {
                            case SymInt _:
                                g.CmpIntegers(Jle);
                                stackUsage = 1;
                                break;
                            
                            case SymDouble _:
                                g.CmpDoubles(Cmplesd);
                                stackUsage = 1;
                                break;
                        }
                        break;
                    
                    case Constants.Operators.MoreOreEqual:
                        switch (node.Left.Type) {
                            case SymInt _:
                                g.CmpIntegers(Jge);
                                stackUsage = 1;
                                break;
                            
                            case SymDouble _:
                                g.CmpDoubles(Cmplesd, true);
                                stackUsage = 1;
                                break;
                        }
                        break;
                    
                    case Constants.Operators.NotEqual:
                        switch (node.Left.Type) {
                            case SymInt _:
                                g.CmpIntegers(Jne);
                                stackUsage = 1;
                                break;
                            
                            case SymDouble _:
                                g.CmpDoubles(Cmpneqsd);
                                stackUsage = 1;
                                break;
                        }
                        break;
                    
                    case Constants.Operators.Equal:
                        switch (node.Left.Type) {
                            case SymInt _:
                                g.CmpIntegers(Je);
                                stackUsage = 1;
                                break;
                            
                            case SymDouble _:
                                g.CmpDoubles(Cmpeqsd);
                                stackUsage = 1;
                                break;
                        }
                        break;
                    
                }
                break;
            
            case ReservedToken word:

                switch (word.Value) {
                    case Constants.Words.Div:
                        Debug.Assert(node.Left.Type is SymInt);
                        Debug.Assert(node.Right.Type is SymInt);
                        
                        g.G(Xor, Rdx(), Rdx());
                        
                        g.G(Pop, Rbx());
                        g.G(Pop, Rax());
                        g.G(Cqo);
                        g.G(Idiv, Rbx());
                        g.G(Push, Rax());
                        stackUsage = 1;
                        break;
                    
                    case Constants.Words.Mod:
                        Debug.Assert(node.Left.Type is SymInt);
                        Debug.Assert(node.Right.Type is SymInt);
                        
                        g.G(Xor, Rdx(), Rdx());
                        
                        g.G(Pop, Rbx());
                        g.G(Pop, Rax());
                        g.G(Cqo);
                        g.G(Idiv, Rbx());
                        g.G(Push, Rdx());
                        stackUsage = 1;
                        break;
                }
                
                break;    
        }

        return stackUsage;
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
                                        g.G(Push, QWord(Der(symVar.Name)));
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
            
            case SymConst symConst:
                Debug.Assert(!IsLval);
                stackUse = 1;
                switch (symConst) {
                    case SymIntConst intConst:
                        g.PushImm64(intConst.Value);
                        break;
                    case SymDoubleConst doubleConst:
                        g.PushImm64(doubleConst.Value);
                        break;
                    case SymCharConst charConst:
                        g.PushImm64(charConst.Value);
                        break;
                }
                
                break;
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
        g.Comment($"unary operation before arg process {node.Operation.StringValue}");
        Accept(node.Expr);
        g.Comment($"unary operation after arg process {node.Operation.StringValue}");            
        switch (node.Operation) {
            
            case OperatorToken opT:
                switch (opT.Value) {
                    
                    case Constants.Operators.Minus:

                        switch (node.Type) {
                            case SymInt _:
                                g.G(Neg, QWord(Der(Rsp())));
                                return 1;
                            case SymDouble _:
                                g.G(Mov, Rcx(), 0x8000000000000000);
                                g.G(Xor, Der(Rsp()), Rcx());
                                return 1;
                        }
                        break;
                }
                break;
            
            case ReservedToken wrd:
                switch (wrd.Value) {
                    
                    case Constants.Words.Not:
                        Debug.Assert(node.Type is SymInt);
                        
                        switch (node.Type) {
                            case SymInt _:
                                g.G(Not, QWord(Der(Rsp())));
                                return 1;
                        }
                        break;
                }
                break;
        }
        
        Debug.Assert(false);
        return 0;
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
            
            if (node.Left.Type is SymChar)
                g.G(Mov, Der(Rax()), Bl());
            else
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
        g.Comment($"if node start");
        var stackUsage = 0;
        g.Comment($"if node condition");
        stackUsage += Accept(node.Condition);
        Debug.Assert(stackUsage == 1);
        
        var exitLabel = g.GetUniqueLabel();
        var falseBranchLabel = g.GetUniqueLabel();
        g.G(Pop, Rax());
        stackUsage -= 1;
        g.G(Cmp, Rax(), 1);
        
        g.G(Jne, falseBranchLabel);
        
        //true
        if (node.TrueBranch != null) {
            g.Comment($"true branch");
            stackUsage += Accept(node.TrueBranch);
        }
        g.G(Jmp, exitLabel);
        
        //false
        g.Label(falseBranchLabel);
        g.G(Nop);

        if (node.FalseBranch != null) {
            g.Comment($"false branch");
            stackUsage += Accept(node.FalseBranch);
        } 
        
        g.Label(exitLabel);
        
        Debug.Assert(stackUsage == 0);
        return stackUsage;
    }

    public int Visit(WhileNode node) {
        throw new System.NotImplementedException();
    }

    public int Visit(ProcedureCallNode node) {
        return  Accept(node.Function);
    }

    public int Visit(ForNode node) {
        throw new NotImplementedException();
        var initSu = Accept(node.Initial);
        Debug.Assert(initSu == 0);
        //compare
        
        //body
        
        //label_to:continue
        //increase counter
        //jmp compare
        
        //label_to:end
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
            
            Debug.Assert(realType is SymInt || realType is SymDouble || realType is SymChar || realType is SymString || realType is SymBool);
            
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
                
                case SymBool symBool:
                    var boolStackUsage = Accept(arg);
                    g.G(Call, _symStack.BoolWrite.Name);
                    g.FreeStack(boolStackUsage);
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
    
}
}