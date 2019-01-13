using System.Globalization;
using System.IO;
using System.Text;

namespace Compiler {

using static DoubleArgCmd;
using static SingleArgCmd;
using static NoArgCmd;
using static AsmArg;

public class CodeGenerator {
    private TextWriter _out;

    public CodeGenerator(TextWriter output) {
        _out = output;
    }
    
    //double    
    public void G(DoubleArgCmd cmd, AsmArg lhs, AsmArg rhs) {
        _out.WriteLine($"{cmd.ToString().ToLowerInvariant()} {lhs.Val}, {rhs.Val}");
    }
    
    public void G(DoubleArgCmd cmd, AsmArg lhs, int rhs) {
        _out.WriteLine($"{cmd.ToString().ToLowerInvariant()} {lhs.Val}, {rhs.ToString()}");
    }
    
    public void G(DoubleArgCmd cmd, int lhs, int rhs) {
        _out.WriteLine($"{cmd.ToString().ToLowerInvariant()} {lhs.ToString()}, {rhs.ToString()}");
    }
    
    public void G(DoubleArgCmd cmd, AsmArg lhs, long rhs) {
        _out.WriteLine($"{cmd.ToString().ToLowerInvariant()} {lhs.Val}, {rhs.ToString()}");
    }
    
    public void G(DoubleArgCmd cmd, AsmArg lhs, ulong rhs) {
        _out.WriteLine($"{cmd.ToString().ToLowerInvariant()} {lhs.Val}, {rhs.ToString()}");
    }
    
    public void G(DoubleArgCmd cmd, AsmArg lhs, string rhs) {
        _out.WriteLine($"{cmd.ToString().ToLowerInvariant()} {lhs.Val}, {rhs}");
    }
    
    
    //single
    public void G(SingleArgCmd cmd, AsmArg arg) {
        _out.WriteLine($"{cmd.ToString().ToLowerInvariant()} {arg.Val}");
    }
    
    public void G(SingleArgCmd cmd, string str) {
        _out.WriteLine($"{cmd.ToString().ToLowerInvariant()} {str}");
    }

    public void G(SingleArgCmd cmd, int arg) {
        _out.WriteLine($"{cmd.ToString().ToLowerInvariant()} {arg.ToString()}");
    }

    //no arg

    public void G(NoArgCmd cmd) {
        _out.WriteLine($"{cmd.ToString().ToLowerInvariant()}");
    }

    //helpers


    public void PushImm64(long imm64) {
        //imm64 must be first loaded to register
        AllocateStack(2);
        G(Mov, Der(Rsp()), Rbx());
        G(Mov, Rbx(), imm64);
        G(Mov, Der(Rsp() + 8), Rbx());
        G(Pop, Rbx());
    }
    
    public void PushImm64(ulong imm64) {
            //imm64 must be first loaded to register
            AllocateStack(2);
            G(Mov, Der(Rsp()), Rbx());
            G(Mov, Rbx(), imm64);
            G(Mov, Der(Rsp() + 8), Rbx());
            G(Pop, Rbx());
        }

    public void PushImm64(double imm64) {
        Comment($"pushing double imm64 {imm64.ToString()}");
        
        var strVal = imm64.ToString(CultureInfo.InvariantCulture);
        var dot = strVal.Contains('.') ? "" : ".";
        
        AllocateStack(2);
        G(Mov, Der(Rsp()), Rbx());
        G(Mov, Rbx(), $"__float64__({strVal}{dot})");
        G(Mov, Der(Rsp() + 8), Rbx());
        G(Pop, Rbx());
    }

    public int PushStringInStack(string str) {
        Comment($"pushing string {str}");
        
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

    public void AllocateStack(int qwords) {
        G(Sub, Rsp(), qwords * 8);
    }

    public void FreeStack(int qwords) {
        G(Add, Rsp(), qwords * 8);
    }
    
    
    public void FunctionPrologue(string func) {
        Label(func);
        G(Enter, 0, 0);
    }

    public void FunctionEpilogue() {
        G(Leave);
        G(Ret);
    }

    public void CallPrintf() {
        AllocateStack(4);
        G(Call, "printf");
        FreeStack(4);
    }

    public void Label(string label) {
        _out.WriteLine($"{label}: ");
    } 
    
    //argument counting starts with 0
    public AsmArg ArgumentValue(int number) {
        return Der(Rbp() + (16 + 8 * number));
//        return $"[rbp+{(16 + 8 * number).ToString()}]";
    }


    public void Comment(string comment) {
        _out.WriteLine();
        _out.WriteLine($"; {comment}");
    }
}

//args

public class AsmArg {
    public string Val { get; }
    
    public AsmArg(string val) {
        Val = val;
    }

    public static AsmArg Der(AsmArg arg) {
        return new AsmArg($"[{arg.Val}]");
    }
    
    public static AsmArg Der(string arg) {
        return new AsmArg($"[{arg}]");
    }
    
    public static AsmArg QWord(AsmArg arg) {
        return new AsmArg($"qword {arg.Val}");
    }

    public static AsmArg QWord(string arg) {
        return new AsmArg($"qword {arg}");
    }

    public static AsmArg operator +(AsmArg lhs, int rhs) {
        return new AsmArg($"{lhs.Val} + {rhs.ToString()}"); 
    }
    
    public static AsmArg operator -(AsmArg lhs, int rhs) {
        return new AsmArg($"{lhs.Val} - {rhs.ToString()}"); 
    }
    
    public static AsmArg Rax() {
        return new AsmArg("rax");
    } 
    
    public static AsmArg Rbx() {
        return new AsmArg("rbx");
    }
    
    public static AsmArg Rcx() {
        return new AsmArg("rcx");
    }
    
    public static AsmArg Rdx() {
        return new AsmArg("rdx");
    }
    
    public static AsmArg Rsp() {
        return new AsmArg("rsp");
    }
    
    public static AsmArg Rbp() {
        return new AsmArg("rbp");
    }
    
    public static AsmArg R8() {
        return new AsmArg("r8");
    }
}

//cmds
public enum DoubleArgCmd {
    Mov,
    Sub,
    Add,
    Lea,
    Enter,
    Xor,
    Imul,
}


public enum SingleArgCmd {
    Push,
    Pop,
    Call,
    Fild,
    Fst,
    Neg,
    Not,
    Idiv
}

public enum NoArgCmd {
    Leave,
    Ret,
    Finit,
    Cqo
}

}