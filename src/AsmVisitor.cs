using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using static Compiler.AsmArg;
using static Compiler.DoubleArgCmd;
using static Compiler.SingleArgCmd;
using static Compiler.NoArgCmd;
using static Compiler.DataTypes;

namespace Compiler {

class Loop {
    public string Advance { get; }
    public string AfterEnd { get; }

    public Loop(string advance, string afterEnd) {
        Advance = advance;
        AfterEnd = afterEnd;
    }
}

class FuncCall {
    public SymFunc Symbol { get; }
    public string ExitLabel { get; }

    public FuncCall(SymFunc symbol, string exitLabel) {
        Symbol = symbol;
        ExitLabel = exitLabel;
    }
}

//return stack usage in qwords
public class AsmVisitor : IAstVisitor<int> {
    private readonly TextWriter _out;
    private readonly AstNode _astRoot;
    private readonly SymStack _symStack;
    private CodeGenerator g;
    
    private Stack<Loop> _loopStack = new Stack<Loop>();
    private Stack<FuncCall> _funcStack = new Stack<FuncCall>();
    
    private bool IsLval { get; set; }

    private readonly string _txtLabelFalse = $"{SymStack.InternalPrefix}FALSE"; 
    private readonly string _txtLabelTrue = $"{SymStack.InternalPrefix}TRUE"; 
    private readonly string _txtLabelPrintInt = $"{SymStack.InternalPrefix}PRINT_INT"; 
    private readonly string _txtLabelPrintDouble = $"{SymStack.InternalPrefix}PRINT_DOUBLE"; 
    private readonly string _txtLabelPrintChar = $"{SymStack.InternalPrefix}PRINT_CHAR";
    private string _mainExitLabel;
    
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
        IsLval = isLval;
        var r = node.Accept(this);
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
        g.FunctionPrologue("main");
        _mainExitLabel = g.GetUniqueLabel();
        Accept(_astRoot);
        
        //return code 0
        g.Label(_mainExitLabel);
        g.G(Xor, Rax(), Rax());
        g.FunctionEpilogue();
    }

    private void GenerateGlobals() {
        
        //todo: add records, 
        foreach (var table in _symStack) {
            foreach (var symbol in table) {
                
                switch (symbol) {
                //main switch start
                                        
                    case SymVar symVar:
                        g.Comment($"global var {symVar.Name} of type {symVar.Type.Name}");
                        
                        switch (symVar.Type) {
                            case SymInt symInt:
                                var initIntVal = ((SymIntConst) symVar.InitialValue)?.Value ?? 0;
                                g.DeclareVariable(symVar.Name, Dq, initIntVal);
                                break;
                            
                            case SymDouble symDouble:
                                var initDoubleVal = ((SymDoubleConst) symVar.InitialValue)?.Value ?? 0;
                                g.DeclareVariable(symVar.Name, Dq, initDoubleVal);
                                break;
                            
                            case SymChar symChar:
                                var initCharVal = ((SymCharConst) symVar.InitialValue)?.Value ?? 0;
                                g.DeclareVariable(symVar.Name, Db, initCharVal);
                                break;
                            
                            case SymArray symArr:
                                g.DeclareVariable(symVar.Name, symArr);
                                break;
                            case SymRecord symRecord: 
                                g.DeclareVariable(symVar.Name, symRecord);
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
                
                if (!(symbol is SymFuncConst symFuncConst))
                    continue;
                
                switch (symFuncConst.FuncType) {
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
                    
                    case ExitSymFunc _:
                    case HighSymFunc _:
                    case LowSymFunc _:
                        //inline
                        break;
                    
                    // user-defined
                    default:
                        var exitLabel = g.GetUniqueLabel();
                        _funcStack.Push(new FuncCall(symFuncConst.FuncType, exitLabel));
                        
                        g.FunctionPrologue(symFuncConst.Name);
                        g.AllocateStack(symFuncConst.FuncType.LocalVariableBsize / 8);
                        
                        var su = symFuncConst.FuncType.LocalVariableBsize / 8;

                        
                        //initialize local variables
                        foreach (var localVar in symFuncConst.FuncType.LocalVariables) {
                            var lvar = localVar as SymVar;
                            
                            if (lvar.LocType != SymVar.SymLocTypeEnum.Local)
                                continue;
                            
                            var offset = symFuncConst.FuncType.LocalVarOffsetTable[localVar.Name];
                            Debug.Assert(lvar != null);
                            
                            switch (lvar.Type) {
                                case SymArray array:
                                    // trash
                                    //todo: initialize
                                    break;
                                case SymChar c:
                                    var charInitVal = ((SymCharConst) lvar.InitialValue)?.Value ?? 0;
                                    g.G(Xor, Rax(), Rax());
                                    g.G(Mov, Al(), charInitVal);
                                    g.G(Mov, Rbx(), Rbp());
                                    g.G(Sub, Rbx(), offset);
                                    
                                    g.G(Mov, Byte(Der(Rbx())), Al());
                                    break;
                                case SymDouble d:
                                    var doubleInitVal = ((SymDoubleConst) lvar.InitialValue)?.Value ?? 0;
                                    g.PushImm64(doubleInitVal);
                                    g.G(Pop, Rax());
                                    g.G(Mov, Rbx(), Rbp());
                                    g.G(Sub, Rbx(), offset);
                                    
                                    g.G(Mov, Der(Rbx()), Rax());
                                    break;
                                case SymInt i1:
                                    var intInitVal = ((SymIntConst) lvar.InitialValue)?.Value ?? 0;
                                    g.G(Mov, Rax(), intInitVal);
                                    g.G(Mov, Rbx(), Rbp());
                                    g.G(Sub, Rbx(), offset);
                                    
                                    g.G(Mov, Der(Rbx()), Rax());
                                    break;
                                case SymRecord record:
                                    break;
                            }
                        }
                        
                        foreach (var param in symFuncConst.FuncType.Parameters) {
                            if (!(param.Type is OpenArray op && param.LocType == SymVarOrConst.SymLocTypeEnum.Parameter)) 
                                continue;
                            //push after locals
                            var offset = symFuncConst.FuncType.ParamsOffsetTable[param.Name];
                            
                            g.G(Mov, Rax(), Rbp());
                            g.G(Add, Rax(), offset);
                            //rbx - last index
                            g.G(Mov, Rbx(), Rax());
                            g.G(Mov, Rbx(), Der(Rbx()));
                            //rbx -size
                            g.G(Inc, Rbx());
                            
                            //rax - pointer to addr
                            g.G(Add, Rax(), 8);
                            g.G(Mov, Rsi(), Der(Rax()));
                            
                            //rsi - source
                            
                            g.Comment("allocate ");
                            g.G(Mov, Rdx(), Rbx());
                            g.G(Imul, Rdx(), op.InnerType.BSize);
                            g.G(Sub, Rsp(), Rdx());
                            //change pointer
                            g.G(Mov, Der(Rax()), Rsp());

                            g.G(Mov, Rdi(), Rsp());
                            g.G(Mov, Rcx(), Rdx());
                            
                            g.G(Rep);
                            g.G(Movsb);
                        }
                    
                        su += Accept(symFuncConst.FuncType.Body);
                        _funcStack.Pop();  
                        Debug.Assert(su == symFuncConst.FuncType.LocalVariableBsize / 8);
                            
                        g.Label(exitLabel);
                        g.FreeStack(su);
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
        var lhsStackUsage = 0;
        var rhsStackUsage = 0;
        g.Comment($"binary operation for {node.Operation.StringValue} before eval operands");

        //lazy statements evaluate arguments on demand
        switch (node.Operation) {
            case ReservedToken word:
                switch (word.Value) {

                    case Constants.Words.And:
                    //todo: add binary and
                        switch (node.Type) {
                            case SymBool _:
                                var exitLabel = g.GetUniqueLabel();
                                var failLabel = g.GetUniqueLabel();

                                Debug.Assert(Accept(node.Left) == 1);
                                g.G(Pop, Rax());
                                g.G(Cmp, Rax(), 0);
                                g.G(Je, failLabel);

                                Debug.Assert(Accept(node.Right) == 1);
                                g.G(Pop, Rax());
                                g.G(Cmp, Rax(), 0);
                                g.G(Je, failLabel);

                                g.PushImm64(1);
                                g.G(Jmp, exitLabel);

                                g.Label(failLabel);
                                g.PushImm64(0);

                                g.Label(exitLabel);

                                return 1;
                            case SymInt _:
                                lhsStackUsage = Accept(node.Left);
                                rhsStackUsage = Accept(node.Right);
                                Debug.Assert(lhsStackUsage == 1);
                                Debug.Assert(rhsStackUsage == 1);
                                
                                g.G(Pop, Rbx());
                                g.G(Pop, Rax());
                                g.G(And, Rax(), Rbx());
                                g.G(Push, Rax());
                                return 1;
                            default:
                                Debug.Assert(false);
                                break;
                        }
                        break;
                    
                    case Constants.Words.Or:
                        //todo: add binary or
                        switch (node.Type) {
                            case SymBool _:
                                var exitLabel = g.GetUniqueLabel();
                                var successLabel = g.GetUniqueLabel();
                                
                                Debug.Assert(Accept(node.Left) == 1);
                                g.G(Pop, Rax());
                                g.G(Cmp, Rax(), 1);
                                g.G(Je, successLabel);
                                
                                Debug.Assert(Accept(node.Right) == 1);
                                g.G(Pop, Rax());
                                g.G(Cmp, Rax(), 1);
                                g.G(Je, successLabel);
                                
                                g.PushImm64(0);
                                g.G(Jmp, exitLabel);
                                
                                g.Label(successLabel);
                                g.PushImm64(1);
                                
                                g.Label(exitLabel);
                                return 1;
                            case SymInt _:
                                lhsStackUsage = Accept(node.Left);
                                rhsStackUsage = Accept(node.Right);
                                Debug.Assert(lhsStackUsage == 1);
                                Debug.Assert(rhsStackUsage == 1);
                                
                                g.G(Pop, Rbx());
                                g.G(Pop, Rax());
                                g.G(Or, Rax(), Rbx());
                                g.G(Push, Rax());
                                return 1;
                            default:
                                Debug.Assert(false);
                                break;
                        }
                        break;
                }
                break;
        }
        

        lhsStackUsage = Accept(node.Left);
        rhsStackUsage = Accept(node.Right);
        
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
                            
                            case SymDouble _:
                                g.G(Pop, Rax());
                                g.G(Movq, Xmm1(), Rax());
                        
                                g.G(Pop, Rax());
                                g.G(Movq, Xmm0(), Rax());
                                
                                g.G(Addsd, Xmm0(), Xmm1());
                                
                                g.G(Movq, Rax(), Xmm0());
                                g.G(Push, Rax());
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
                            
                            case SymDouble _:
                                g.G(Pop, Rax());
                                g.G(Movq, Xmm1(), Rax());
                        
                                g.G(Pop, Rax());
                                g.G(Movq, Xmm0(), Rax());
                                
                                g.G(Subsd, Xmm0(), Xmm1());
                                
                                g.G(Movq, Rax(), Xmm0());
                                g.G(Push, Rax());
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
                        switch (symVar.LocType) {
                            case SymVar.SymLocTypeEnum.Global:
                                g.G(Push, symVar.Name);
                                stackUse = 1;
                                break;
                            case SymVar.SymLocTypeEnum.Parameter:
                                g.G(Mov, Rax(), Rbp());
                                g.G(Add, Rax(), _funcStack.Peek().Symbol.ParamsOffsetTable[symVar.Name]);
                                
                                g.G(Push, Rax());
                                stackUse = 1;
                                break;
                            case SymVar.SymLocTypeEnum.Local:
                                g.G(Mov, Rax(), Rbp());
                                g.G(Sub, Rax(),  _funcStack.Peek().Symbol.LocalVarOffsetTable[symVar.Name]);
                                g.G(Push, Rax());
                                stackUse = 1;
                                break;
                            case SymVar.SymLocTypeEnum.VarParameter:
                                g.G(Mov, Rax(), Rbp());
                                g.G(Add, Rax(), _funcStack.Peek().Symbol.ParamsOffsetTable[symVar.Name]);
                                
                                if (!(symVar.Type is OpenArray))
                                    g.G(Mov, Rax(), Der(Rax()));
                                
                                g.G(Push, Rax());
                                stackUse = 1;
                                break;
                            case SymVar.SymLocTypeEnum.ConstParameter:
                            case SymVar.SymLocTypeEnum.OutParameter:
                                throw new NotImplementedException();
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                        
                        break;
                    
                    //case rval
                    case false:
                        switch (symVar.LocType) {
                            case SymVar.SymLocTypeEnum.Global:
                                
                                switch (realType) {
                                    case SymChar _:
                                        g.G(Xor, Rbx(), Rbx());
                                        g.G(Mov, Bl(), Der(symVar.Name));
                                        g.G(Push, Rbx());
                                        stackUse = 1;
                                        break;
                                    case SymDouble _:
                                    case SymInt _:
                                        g.G(Push, QWord(Der(symVar.Name)));
                                        stackUse = 1;
                                        break;
                                    
                                    
                                    case SymArray arr:
                                    case SymRecord record:
                                        //get addr
                                        var recStackUsage = Accept(node, true);
                                        Debug.Assert(recStackUsage == 1);
                                        
                                        var wholeQwords = node.Type.BSize / 8;
                                        var reminder = node.Type.BSize % 8;
                                        
                                        var totalInMemoryQSize = wholeQwords + (reminder > 0 ? 1 : 0);
                                        g.G(Pop, Rax());
                                        g.AllocateStack(totalInMemoryQSize);
                                        g.G(Mov, Rbx(), Rsp());
                                        g.PushStructToStack(wholeQwords, reminder);

                                        stackUse = totalInMemoryQSize;
                                        break;
                                }
                                
                                break;
                            case SymVar.SymLocTypeEnum.Parameter:
                                switch (realType) {
                                    case SymScalar _:
                                        g.G(Push, QWord(Der(Rbp() + _funcStack.Peek().Symbol.ParamsOffsetTable[symVar.Name])));
                                        stackUse = 1;
                                        break;
                                    
                                    case SymArray arr:
                                    case SymRecord record:
                                        //get addr
                                        var recStackUsage = Accept(node, true);
                                        Debug.Assert(recStackUsage == 1);
                                        
                                        var wholeQwords = node.Type.BSize / 8;
                                        var reminder = node.Type.BSize % 8;
                                        
                                        var totalInMemoryQSize = wholeQwords + (reminder > 0 ? 1 : 0);
                                        g.G(Pop, Rax());
                                        g.AllocateStack(totalInMemoryQSize);
                                        g.G(Mov, Rbx(), Rsp());
                                        g.PushStructToStack(wholeQwords, reminder);

                                        stackUse = totalInMemoryQSize;
                                        break;
                                    case OpenArray _:
                                        Debug.Assert(false);
                                        break;
                                    
                                }
                                break;
                            
                            case SymVar.SymLocTypeEnum.Local:
                                g.G(Mov, Rax(), Rbp());
                                g.G(Sub, Rax(),  _funcStack.Peek().Symbol.LocalVarOffsetTable[symVar.Name]);
                                //rax - addr to var
                                
                                switch (realType) {
                                    case SymChar _:
                                        
                                        g.G(Xor, Rbx(), Rbx());
                                        g.G(Mov, Bl(), Der(Rax()));
                                        g.G(Push, Rbx());
                                        stackUse = 1;
                                        break;
                                    case SymDouble _:
                                    case SymInt _:
                                        g.G(Push, QWord(Der(Rax())));
                                        stackUse = 1;
                                        break;
                                    
                                    
                                    case SymArray arr:
                                    case SymRecord record:
                                        //get addr
                                        var recStackUsage = Accept(node, true);
                                        Debug.Assert(recStackUsage == 1);
                                        
                                        var wholeQwords = node.Type.BSize / 8;
                                        var reminder = node.Type.BSize % 8;
                                        
                                        var totalInMemoryQSize = wholeQwords + (reminder > 0 ? 1 : 0);
                                        g.G(Pop, Rax());
                                        g.AllocateStack(totalInMemoryQSize);
                                        
                                        g.G(Mov, Rbx(), Rsp());
                                        g.PushStructToStack(wholeQwords, reminder);

                                        stackUse = totalInMemoryQSize;
                                        break;
                                    
                                }    
                                break;
                            case SymVar.SymLocTypeEnum.VarParameter:
                                g.G(Mov, Rax(), Rbp());
                                g.G(Add, Rax(), _funcStack.Peek().Symbol.ParamsOffsetTable[symVar.Name]);
                                g.G(Mov, Rax(), Der(Rax()));
                                //rax - addr to var
                                
                                switch (realType) {
                                    case SymChar _:
                                        g.G(Xor, Rbx(), Rbx());
                                        g.G(Mov, Bl(), Der(Rax()));
                                        g.G(Push, Rbx());
                                        stackUse = 1;
                                        break;
                                    case SymDouble _:
                                    case SymInt _:
                                        g.G(Push, QWord(Der(Rax())));
                                        stackUse = 1;
                                        break;
                                    
                                    
                                    case SymArray arr:
                                    case SymRecord record:
                                        //get addr
                                        var recStackUsage = Accept(node, true);
                                        Debug.Assert(recStackUsage == 1);
                                        
                                        var wholeQwords = node.Type.BSize / 8;
                                        var reminder = node.Type.BSize % 8;
                                        
                                        var totalInMemoryQSize = wholeQwords + (reminder > 0 ? 1 : 0);
                                        g.G(Pop, Rax());
                                        g.AllocateStack(totalInMemoryQSize);
                                        
                                        g.G(Mov, Rbx(), Rsp());
                                        g.PushStructToStack(wholeQwords, reminder);

                                        stackUse = totalInMemoryQSize;
                                        break;
                                    
                                }    
                                break;
                            case SymVar.SymLocTypeEnum.ConstParameter:
                            case SymVar.SymLocTypeEnum.OutParameter:
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
                    case SymFuncConst funcConst:
                        g.G(Push, funcConst.Name);
                        break;
                }
                
                break;
        }
        
        Debug.Assert(stackUse != 0);
        return stackUse;
    }

    private int ExitFunction(ExprNode result) {
        g.Comment($"EXIT!!!!!!!!!!!!!!EJECT!!!!!!!!!!!!!");

        if (_funcStack.Count == 0) {
            g.G(Jmp, _mainExitLabel);
            return 0;
        }
            
        var curFunc = _funcStack.Peek();
            
        if (!(curFunc.Symbol.ReturnType is SymVoid)) {
            var su = Accept(result);
                
            g.G(Mov, Rsi(), Rsp());
            g.G(Mov, Rdi(), Rbp());
            g.G(Add, Rdi(), curFunc.Symbol.ParamsSizeB);
            g.G(Mov, Rcx(), curFunc.Symbol.ReturnType.BSize);
            g.G(Rep);
            g.G(Movsb);
            g.FreeStack(su);
        }
        g.Comment($"EXIT!!!!!!!!!!!!!!EJECT!!!!!!!!!!!!!");
        //push result
        //gotoexit
        g.G(Jmp, _funcStack.Peek().ExitLabel);
        return 0;
    }

    private int HighFunction(ExprNode nodeArg) {
        if (nodeArg.Type is SymArray symArr) {
            g.PushImm64(symArr.MaxIndex.Value);
            return 1;
        }

        if (nodeArg.Type is OpenArray openArray) {
            Accept(nodeArg, true);
            g.G(Pop, Rax());
            g.G(Mov, Rax(), Der(Rax()));
            g.G(Push, Rax());
            return 1;
        }

        Debug.Assert(false);
        return 0;
    }
    
    private int LowFunction(ExprNode nodeArg) {
        if (nodeArg.Type is SymArray symArr) {
            g.PushImm64(symArr.MinIndex.Value);
            return 1;
        }

        if (nodeArg.Type is OpenArray openArray) {
            g.PushImm64(0);
            return 1;
        }

        Debug.Assert(false);
        return 0;
    }
    
    public int Visit(FunctionCallNode node) {
        g.Comment($"function call");
        
        if (node.Symbol is ExitSymFunc) {
            return ExitFunction(node.Args[0]);
        }
        
        if (node.Symbol is HighSymFunc) {
            return HighFunction(node.Args[0]);
        }
        
        if (node.Symbol is LowSymFunc) {
            return LowFunction(node.Args[0]);
        }
            
        //return val
        var returnValStackUse = node.Symbol.ReturnType.BSize / 8 + (node.Symbol.ReturnType.BSize % 8 > 0 ? 1 : 0);
        g.AllocateStack(returnValStackUse);
        var stackUse = 0;

        //todo: add var param 
        for (var i = node.Args.Count - 1; i >= 0; i--) {
            var curParam = node.Symbol.Parameters[i];

            if (curParam.Type is OpenArray) {
                stackUse += Accept(node.Args[i], true);
                var arrayType = node.Args[i].Type as SymArray;
                Debug.Assert(arrayType != null);
                        
                g.PushImm64(arrayType.Size - 1);
                stackUse += 1;
                continue;
            }
            
            switch (curParam.LocType) {
                case SymVarOrConst.SymLocTypeEnum.Parameter:
                    // if open array - push size and addr
//                    if (curParam.Type is OpenArray) {
//                        stackUse += Accept(node.Args[i], true);
//                        var arrayType = node.Args[i].Type as SymArray;
//                        Debug.Assert(arrayType != null);
//                        
//                        var arrTypeSize = arrayType.BSize / 8 + (arrayType.BSize % 8 > 0 ? 1 : 0);
//                        g.PushImm64(arrTypeSize);
//                        stackUse += 8;
//                        break;
//                    }
                    
                    stackUse += Accept(node.Args[i]);
                    break;
                case SymVarOrConst.SymLocTypeEnum.VarParameter:
                    stackUse += Accept(node.Args[i], true);
                    break;
                default:
                    Debug.Assert(false);
                    break;
            }
        }
        
        g.Comment($"function call eval function address");
        var nameStackUsage = Accept(node.Name);
        Debug.Assert(nameStackUsage == 1);
        
        
        g.G(Pop, Rax());
        g.G(Call, Rax());
        g.FreeStack(stackUse);
        return returnValStackUse;
    }

    public int Visit(CastNode node) {
        // only
        // int->double
        // int->boolean
        // cast are allowed now
        var stackUse = Accept(node.Expr);
        Debug.Assert(stackUse == 1);
        g.Comment($"casting {node.Expr.Type.Name} to {node.CastTo.Name}");
        
        switch (node.CastTo) {
            case SymDouble _:
                g.G(Finit);
                g.G(Fild, QWord(Der(Rsp())));
                g.G(Fst, QWord(Der(Rsp())));
                return 1;
            case SymBool _:
                var notZeroLabel = g.GetUniqueLabel();
                var exitLabel = g.GetUniqueLabel();
                
                g.G(Cmp, QWord(Der(Rsp())), 0);
                g.G(Jne, notZeroLabel);
                g.G(Mov, QWord(Der(Rsp())), 0);
                g.G(Jmp, exitLabel);
                
                g.Label(notZeroLabel);
                g.G(Mov, QWord(Der(Rsp())), 1);
                
                g.Label(exitLabel);
                return 1;
        }
        
        Debug.Assert(false);
        return stackUse;
    }

    public int Visit(AccessNode node) {
        var isLval = IsLval;
        g.Comment($"accessing field {node.Field.StringValue} of {node.Name.Type.Name} lval:{IsLval.ToString()}");
        var stackUsage = 0;
    
        var record = node.Name.Type as SymRecord;
        Debug.Assert(record != null);

        stackUsage += Accept(node.Name, true);
        Debug.Assert(stackUsage == 1);
        g.G(Pop, Rax());
        g.G(Add, Rax(), record.OffsetTable[node.Field.Value]);
        g.G(Push, Rax());
        
        if (isLval)
            return 1;
        
        //isrval
        g.G(Pop, Rax());
        
        if (node.Type is SymChar) {
            g.G(Xor, Rdx(), Rdx());
            g.G(Mov, Dl(), Der(Rax()));
            g.G(Push, Rdx());
            return 1;
        }
        
        var wholeQwords = node.Type.BSize / 8;
        var reminder = node.Type.BSize % 8;
        
        var totalInMemoryQSize = wholeQwords + (reminder > 0 ? 1 : 0);
        g.AllocateStack(totalInMemoryQSize);
        g.G(Mov, Rbx(), Rsp());
        g.PushStructToStack(wholeQwords, reminder);

        return wholeQwords;
    }

    public int Visit(IndexNode node) {
        g.Comment($"index array islval: {IsLval.ToString()}");
        var isLval = IsLval;
        var stackUsage = 0;
        
        if (node.Operand.Type is OpenArray) {
            g.Comment("index open array");
            stackUsage += Accept(node.IndexExpr);
            stackUsage += Accept(node.Operand, true);
            // rax - addr
            // rdx - index
            
            //discard index 
            g.G(Pop, Rax());
            g.G(Add, Rax(), 8);
            g.G(Mov, Rax(), Der(Rax()));
//            g.G(Mov, Rax(), Der(Rax()));
            g.G(Pop, Rdx());
            
            g.G(Imul, Rdx(), node.Type.BSize);
            // get addr
            g.G(Add, Rax(), Rdx());
            g.G(Push, Rax());
            stackUsage = 1;
        
            if (isLval)             
                return stackUsage;

            // rvalue - push to stack
            g.G(Pop, Rax());
            stackUsage = 0;
        
            if (node.Type is SymChar) {
                g.G(Xor, Rdx(), Rdx());
                g.G(Mov, Dl(), Der(Rax()));
                g.G(Push, Rdx());
                return 1;
            }
        
            var wholeQwordsOpen = node.Type.BSize / 8;
            var reminderOpen = node.Type.BSize % 8;
        
            //in qwords
            var totalInMemoryQSizeOpen = wholeQwordsOpen + (reminderOpen > 0 ? 1 : 0);
            g.AllocateStack(totalInMemoryQSizeOpen);
            g.G(Mov, Rbx(), Rsp());
            g.PushStructToStack(wholeQwordsOpen, reminderOpen);

            return totalInMemoryQSizeOpen;
        }
        
        var arrType = node.Operand.Type as SymArray;
        Debug.Assert(arrType != null);
        
        g.Comment($"index before eval index expr");
        stackUsage = Accept(node.IndexExpr);
        g.Comment($"index after eval index expr");
        Debug.Assert(stackUsage == 1);
        // calculate address
        stackUsage += Accept(node.Operand, true);
        Debug.Assert(stackUsage == 2);
        g.G(Pop, Rax());
        g.G(Pop, Rdx());
        g.G(Sub, Rdx(), arrType.MinIndex.Value);
        // rax - address
        // rdx - index
        
        // calc offset
        g.G(Imul, Rdx(), node.Type.BSize);
        // get addr
        g.G(Add, Rax(), Rdx());
        g.G(Push, Rax());
        stackUsage = 1;
        
        if (isLval)             
            return stackUsage;

        // rvalue - push to stack
        g.G(Pop, Rax());
        stackUsage = 0;
        
        if (node.Type is SymChar) {
            g.G(Xor, Rdx(), Rdx());
            g.G(Mov, Dl(), Der(Rax()));
            g.G(Push, Rdx());
            return 1;
        }
        
        var wholeQwords = node.Type.BSize / 8;
        var reminder = node.Type.BSize % 8;
        
        //in qwords
        var totalInMemoryQSize = wholeQwords + (reminder > 0 ? 1 : 0);
        g.AllocateStack(totalInMemoryQSize);
        g.G(Mov, Rbx(), Rsp());
        g.PushStructToStack(wholeQwords, reminder);

        return totalInMemoryQSize;
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
                        switch (node.Type) {
                            case SymInt _:
                                g.G(Not, QWord(Der(Rsp())));
                                return 1;
                            case SymBool _:
                                g.G(Xor, QWord(Der(Rsp())), 1);
                                return 1;
                            default:
                                Debug.Assert(false);
                                break;
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
        
        switch (node.Operation.Value) {
            case Constants.Operators.PlusAssign:
                var fakePlus = new BinaryExprNode(node.Left, node.Right, new OperatorToken("+", Constants.Operators.Plus, 0, 0));
                fakePlus.Type = node.Right.Type;
                node.Right = fakePlus;
                break;
            
            case Constants.Operators.MinusAssign:
                var fakeMinus = new BinaryExprNode(node.Left, node.Right, new OperatorToken("-", Constants.Operators.Minus, 0, 0));
                fakeMinus.Type = node.Right.Type;
                node.Right = fakeMinus;
                break;
            
            case Constants.Operators.DivideAssign:
                var fakeDivide = new BinaryExprNode(node.Left, node.Right, new OperatorToken("/", Constants.Operators.Divide, 0, 0));
                fakeDivide.Type = node.Right.Type;
                node.Right = fakeDivide;
                break;
            
            case Constants.Operators.MultiplyAssign:
                var fakeMul= new BinaryExprNode(node.Left, node.Right, new OperatorToken("*", Constants.Operators.Multiply, 0, 0));
                fakeMul.Type = node.Right.Type;
                node.Right = fakeMul;
                break;

        }
        
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

            return 0;
        }
        
        g.Comment("assign struct.");
        var wholeQwords = node.Right.Type.BSize / 8;
        var reminder = node.Right.Type.BSize % 8;
        //in qwords
        
        g.G(Lea, Rdi(), Der(Rsp() + 8*rhsStackUse));
        // rbx - dest
        // rax - source
        g.G(Mov, Rdi(), Der(Rdi()));
        
        g.G(Mov, Rsi(), Rsp());
        g.G(Mov, Rcx(), node.Right.Type.BSize);
        g.G(Rep);
        g.G(Movsb);
//        g.PushStructToStack(wholeQwords, reminder);
        g.FreeStack(rhsStackUse + 1);
        return 0;
    }

    public int Visit(IfNode node) {
        g.Comment($"if node start");
        var stackUsage = 0;
        g.Comment($"if node before condition");
        stackUsage += Accept(node.Condition);
        g.Comment($"if node after condition");
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
//        g.G(Nop);

        if (node.FalseBranch != null) {
            g.Comment($"false branch");
            stackUsage += Accept(node.FalseBranch);
        } 
        
        g.Label(exitLabel);
        
        Debug.Assert(stackUsage == 0);
        g.Comment($"end if");
        return stackUsage;
    }

    public int Visit(WhileNode node) {
        g.Comment("while node");
        
        var conditionLabel = g.GetUniqueLabel();
        var exitLabel = g.GetUniqueLabel();
        
        _loopStack.Push(new Loop(conditionLabel, exitLabel));
        
        g.Label(conditionLabel);
        var stackUse = Accept(node.Condition);
        Debug.Assert(stackUse == 1);
        g.G(Pop, Rax());
        stackUse = 0;
        g.G(Cmp, Rax(), 0);
        g.G(Je, exitLabel);
        
        stackUse += Accept(node.Block);
        
        Debug.Assert(stackUse == 0);
        g.G(Jmp, conditionLabel);
        
        g.Label(exitLabel);
        return stackUse;
    }

    public int Visit(ProcedureCallNode node) {
        var su  = Accept(node.Function);
        g.FreeStack(su);
        return 0;
    }

    public int Visit(ForNode node) {
        g.Comment("for node");
        // stack invariant layout
        // [final value]
        // [var address]

        g.Comment("for node before final value eval");
        var stackUsage = Accept(node.Final);
        Debug.Assert(stackUsage == 1);
        
        g.Comment("for node before var eval");
        stackUsage += Accept(node.Initial.Left, true);
        Debug.Assert(stackUsage == 2);

        g.Comment("for node before initial value eval");
        stackUsage += Accept(node.Initial.Right);
        Debug.Assert(stackUsage == 3);
        // stack state
        //[final value]
        //[variable address]
        //[initial value]
        g.G(Pop, Rbx());
        g.G(Mov, Rax(), Der(Rsp()));
        g.G(Mov, Der(Rax()), Rbx());
         
        // stack state
        //[final value]
        //[variable address]
        stackUsage = 2;
        
        var conditionLabel = g.GetUniqueLabel();
        var advanceLabel = g.GetUniqueLabel();
        var exitLabel = g.GetUniqueLabel();
        
        _loopStack.Push(new Loop(advanceLabel, exitLabel));
        
        //compare
        g.Label(conditionLabel);
        //load addr
        g.G(Mov, Rcx(), Der(Rsp()));
        // load value
        g.G(Mov, Rdx(), Der(Rsp()+8));
        
        g.G(Cmp, Der(Rcx()), Rdx());
        
        g.G(node.Direction == ForNode.DirectionType.To ? Jg : Jl, exitLabel);

        stackUsage += Accept(node.Body);
        Debug.Assert(stackUsage == 2);
        
        g.Label(advanceLabel);
        //advance counter and save
        g.G(Mov, Rax(), Der(Rsp()));
        g.G(Mov, Rcx(), Der(Rax()));
        
        g.G(node.Direction == ForNode.DirectionType.To ? Inc : Dec, Rcx());
        g.G(Mov, Der(Rax()), Rcx());
        g.G(Jmp, conditionLabel);

        g.Label(exitLabel);
        g.FreeStack(2);
        
        _loopStack.Pop();
        return 0;
    }

    public int Visit(ControlSequence node) {
        Debug.Assert(_loopStack.Count != 0);
        switch (node.ControlWord.Value) {
            case Constants.Words.Break:
                g.G(Jmp, _loopStack.Peek().AfterEnd);
                return 0;
            case Constants.Words.Continue:
                g.G(Jmp, _loopStack.Peek().Advance);
                return 0;
        }
        
        Debug.Assert(false);
        return 0;
    }

    public int Visit(EmptyStatementNode node) {
        g.G(Nop);
        return 0;
    }

    public int Visit(CharNode node) {
        g.PushImm64((long)node.Value);
        return 1;
    }

    public int Visit(WriteStatementNode node) {
        
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

        if (!node.IsLn)
            return 0;

        var su = g.PushStringInStack("\n");
        g.G(Mov, Rcx(), Rsp());
        g.CallPrintf();
        g.FreeStack(su);
        
        return 0;
    }
    
}
}