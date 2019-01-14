using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
    
namespace Compiler {

public class SymStack : IEnumerable<SymTable> {
    private Stack<SymTable> _stack = new Stack<SymTable>();
    // standard types
    public readonly SymInt SymInt = new SymInt();
    public readonly SymChar SymChar = new SymChar();
    public readonly SymDouble SymDouble = new SymDouble();
    public readonly SymBool SymBool = new SymBool();
    public readonly SymString SymString = new SymString();
    
    public readonly StringWriteSymFunc StringWrite = new StringWriteSymFunc();
    public readonly IntWriteSymFunc IntWrite = new IntWriteSymFunc();
    public readonly DoubleWriteSymFunc DoubleWrite = new DoubleWriteSymFunc();
    public readonly CharWriteSymFunc CharWrite = new CharWriteSymFunc();
    public readonly BoolWriteSymFunc BoolWrite = new BoolWriteSymFunc();

    public static string InternalPrefix => "_@@@_"; 

    public SymStack() {
        _stack.Push(new SymTable());
        AddType(SymInt);        
        AddType(SymChar);        
        AddType(SymDouble);        
        AddType(SymBool);        
        AddType(SymString);      
        //predefined functions
        _stack.Peek().Add(StringWrite);
        _stack.Peek().Add(IntWrite);
        _stack.Peek().Add(DoubleWrite);
        _stack.Peek().Add(CharWrite);
        _stack.Peek().Add(BoolWrite);
        
    }

    public void Push() {
        _stack.Push(new SymTable());
    }

    public SymTable Pop() {
        return _stack.Pop();
    }

    private void RequireSymbolRewritable(IdentifierToken identifierToken) {
        if (FindInCurrentScope(identifierToken.Value) != null) 
            throw new DuplicateIdentifierException(identifierToken.Lexeme, identifierToken.Line, identifierToken.Line);
    }
    
    // return nullable
    private Symbol Find(string name) {
        foreach (var table in _stack) {
            var symbol = table.Find(name);
            if (symbol != null)
                return symbol;
        }

        return null;
    }

    private Symbol FindInCurrentScope(string name) {
        return _stack.Peek().Find(name);
    }
    
    // return nullable
    public SymType FindType(string name) {
        var symbol = Find(name);

        switch (symbol) {
            case SymAlias alias:
                return alias.Type;
            case SymType symType:
                return symType;
            default:
                return null;
        }
    }
    // return nullable
    public SymVarOrConst FindVarOrConst(string name) {
        if (Find(name) is SymVarOrConst symbol)
            return symbol;
        return null;
    }

    public SymFunc FindFunction(string name) {
        if (Find(name) is SymFunc symFunc)
            return symFunc;
        
        return null;
    }

    public void AddFunction(IdentifierToken nameToken, List<SymVar> paramList, SymTable localVars, StatementNode body, IdentifierToken returnTypeToken) {
        //todo: add checks!!!
        SymType returnType = null;
        RequireSymbolRewritable(nameToken);
        if (returnTypeToken != null) {
            returnType = FindType(returnTypeToken.Value);
            if (returnType == null)
                throw new TypeNotFoundException(returnTypeToken.Lexeme, returnTypeToken.Line, returnTypeToken.Column);
        }
        _stack.Peek().Add(new SymFunc(nameToken.Value, paramList, localVars, body, returnType));
    }
    
    public void AddVariable(IdentifierToken variableToken, IdentifierToken typeToken, SymVar.VarTypeEnum varType, 
        SymConst value = null) 
    {
        var type = FindType(typeToken.Value);
        
        if (type == null)
            throw new TypeNotFoundException(typeToken.Lexeme, typeToken.Line, typeToken.Column);

        RequireSymbolRewritable(variableToken);
        //type check for initial value must be performed earlier. i.e. in parser.       
        var symVar = new SymVar(variableToken.Value, type, value, varType);
        _stack.Peek().Add(symVar);
    }
    
    public SymVar AddVariable(IdentifierToken variableToken, SymType type, SymVar.VarTypeEnum varMod, SymConst value = null) 
    {
        
        RequireSymbolRewritable(variableToken);
        //type check for initial value must be performed earlier. i.e. in parser.       
        var symVar = new SymVar(variableToken.Value, type, value, varMod);
        _stack.Peek().Add(symVar);
        return symVar;
    }

    public void AddArray(IdentifierToken identifierToken, SymArray arrayType, SymVar.VarTypeEnum varType) {
        RequireSymbolRewritable(identifierToken);
        var symVar = new SymVar(identifierToken.Value, arrayType, null, varType);
        _stack.Peek().Add(symVar);
    }

    public void AddConst(IdentifierToken constToken, SymConst symConst) {
        RequireSymbolRewritable(constToken);
        _stack.Peek().Add(symConst);
    }

    private void AddType(SymType symType) {
        //todo: check?
        _stack.Peek().Add(symType);
    }
    
    public void AddType(SymType symType, Token token) {
        //todo: check?
        _stack.Peek().Add(symType);
    }
    
    public void AddAlias(IdentifierToken aliasToken, IdentifierToken typeToken) {
        RequireSymbolRewritable(aliasToken);
            
        var type = FindType(typeToken.Value);
        if (type == null)
            throw new TypeNotFoundException(typeToken.Lexeme, typeToken.Line, typeToken.Column);
        
        _stack.Peek().Add(new SymAlias(aliasToken.Value, type));
    }

    public void AddAlias(IdentifierToken aliasToken, SymType type) {
        RequireSymbolRewritable(aliasToken);
        _stack.Peek().Add(new SymAlias(aliasToken.Value, type));
    }
    
    public void AddAliasType(IdentifierToken aliasToken, IdentifierToken aliasTypeToken) {
        RequireSymbolRewritable(aliasToken);
            
        var type = FindType(aliasTypeToken.Value);
        if (type == null)
            throw new TypeNotFoundException(aliasTypeToken.Lexeme, aliasTypeToken.Line, aliasTypeToken.Column);
        
        // compute underlying type before adding
        var realType = type;
        while (realType is SymTypeAlias typeAlias) {
            if (typeAlias.Type is SymArray arr) {
                realType = arr;
                break;
            }
            realType = FindType(typeAlias.Type.Name);
        }
        
        _stack.Peek().Add(new SymTypeAlias(aliasToken.Value, realType));
    }
    
    public void AddAliasType(IdentifierToken aliasToken, SymType aliasType) {
        RequireSymbolRewritable(aliasToken);
            
        // compute underlying type before adding
        var realType = aliasType;
        while (realType is SymTypeAlias typeAlias) {
            if (typeAlias.Type is SymArray arr) {
                realType = arr;
                break;
            }
            realType = FindType(typeAlias.Type.Name);
        }
        
        _stack.Peek().Add(new SymTypeAlias(aliasToken.Value, realType));
    }

    public IEnumerator<SymTable> GetEnumerator() {
        return _stack.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}

public class SymTable : IEnumerable<Symbol> {
    private Dictionary<string, Symbol> _sym_map = new Dictionary<string, Symbol>();
    private List<Symbol> _sym_list = new List<Symbol>();
    
    public void Add(Symbol symbol) {
        _sym_map.Add(symbol.Name, symbol);
        _sym_list.Add(symbol);
    }

    public Symbol Find(string name) {
        _sym_map.TryGetValue(name, out var symbol);
        return symbol;
    }

    public IEnumerator<Symbol> GetEnumerator() {
        return _sym_list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}

public abstract class Symbol {
    public abstract string Name { get; }
    public abstract void Accept(ISymVisitor visitor);
}


// types
public abstract class SymType : Symbol {
    //in bytes
    public abstract int BSize { get; }
}

// should be only used for parameters
public class ArrayOfConst : SymType {
    public override string Name => "Array of const";
    
    private ArrayOfConst() {}
    private static ArrayOfConst _instance;
    //should not be used
    public override int BSize => throw new NotImplementedException();

    public static ArrayOfConst Instance => _instance ?? (_instance = new ArrayOfConst());

    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

// scalars
public abstract class SymScalar : SymType {
}

public class SymInt : SymScalar {
    public override string Name => "integer";
    public override int BSize => 8;

    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymDouble : SymScalar {
    public override string Name => "double";

    public override int BSize => 8;
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymChar : SymScalar {
    public override string Name => "char";
    public override int BSize => 1;
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymVoid : SymType {
    public override string Name => "void";
    public override int BSize => 0;
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymString : SymType {
    public override string Name => "string";
    public override int BSize => throw new NotImplementedException();
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymBool : SymScalar {
    public override string Name => "boolean";
    public override int BSize => 1;
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public abstract class SymVarOrConst : Symbol {
    public override string Name { get; }
    public SymType Type { get; }
    public bool IsConst { get; }
    // not null only at constants and initialized variables. is used to print symtable.
    public string InitialStringValue { get; protected set; }

    protected SymVarOrConst(string name, SymType type, bool isConst) {
        Name = name;
        Type = type;
        IsConst = isConst;
    }
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

// variables
public class SymVar : SymVarOrConst {
    public enum VarTypeEnum {Global, Local, Parameter, VarParameter, ConstParameter, OutParameter}
    // nullable
    public SymConst InitialValue { get; }
    public VarTypeEnum VarType { get; }
    public SymVar(string name, SymType type, SymConst initialValue, VarTypeEnum varType)
        : base(name, type, false) 
    {
        InitialValue = initialValue;
        VarType = varType;
        InitialStringValue = initialValue?.InitialStringValue;
    }
}
// constants
public abstract class SymConst : SymVarOrConst {
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
    protected SymConst(string name, SymType type) : base(name, type, true) {}
}

public class SymIntConst : SymConst {
    public long Value { get; }

    public SymIntConst(string name, SymType type, long value) : base(name, type) {
        Value = value;
        InitialStringValue = value.ToString();
    }
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymDoubleConst : SymConst {
    public double Value { get; }

    public SymDoubleConst(string name, SymType type, double value) : base(name, type) {
        Value = value;
        InitialStringValue = value.ToString();
    }
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymCharConst : SymConst {
    // or string?
    public char Value { get; }
    
    public SymCharConst(string name, SymType type, char value) : base(name, type) {
        Value = value;
        InitialStringValue = value.ToString();
    }

    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymBoolConst : SymConst {
    public bool Value { get; }

    public SymBoolConst(string name, SymType type, bool value) : base(name, type) {
        Value = value;
        InitialStringValue = value.ToString();
    }
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymStringConst : SymConst {
    public string Value { get; }

    public SymStringConst(string name, SymType type, string value) : base(name, type) {
        Value = value;
        InitialStringValue = value;
    }
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymArray : SymType {
    public SymIntConst MinIndex { get; }
    public SymIntConst MaxIndex { get; }
    public SymType Type { get; }
    
    public long Size => MaxIndex.Value - MinIndex.Value + 1;
    public override int BSize { get; }
    

    public override string Name => $"array[{MinIndex.Value}..{MaxIndex.Value}] of {Type.Name}";

    public SymArray(SymIntConst minIndex, SymIntConst maxIndex, SymType type) {
        MinIndex = minIndex;
        MaxIndex = maxIndex;
        Type = type;
        BSize = (int)Size * Type.BSize;
    }
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
    
}

public class SymRecord : SymType {
    public override string Name { get; }
    public SymTable Fields { get; }
    public override int BSize { get; }
    
    public Dictionary<string, long> OffsetTable = new Dictionary<string, long>();

    public SymRecord(string name, SymTable fields) {
        Name = name;
        Fields = fields;

        foreach (var field in Fields) {
            if (field is SymVar symVar) {
                OffsetTable.Add(field.Name, BSize);
                BSize += symVar.Type.BSize;
            }
            else {
                Debug.Assert(false);
            }
            
            //todo: align?
        }
    }
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymFunc : SymType {
    public override string Name { get; }
    public override int BSize => throw new NotImplementedException();
    
    public List<SymVar> Parameters { get; }
    public SymTable LocalVariables { get; }
    // if null -> procedure
    public SymType ReturnType { get; }
    public StatementNode Body { get; }
    
    public SymFunc(string name, List<SymVar> parameters, SymTable localVariables, StatementNode body, SymType returnType) {
        Name = name;
        Parameters = parameters;
        LocalVariables = localVariables;
        Body = body;
        
        if (returnType == null) {
            ReturnType = new SymVoid();
        }
        else {
            ReturnType = returnType;
        }
    }


    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

//predefined functions
public abstract class PredefinedSymFunc : SymFunc {
    protected PredefinedSymFunc(string name, List<SymVar> parameters, SymTable localVariables, StatementNode body, SymType returnType)
        : base(name, parameters, localVariables, body, returnType) { }
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class StringWriteSymFunc : PredefinedSymFunc {
    public StringWriteSymFunc() : 
        base(
            $"{SymStack.InternalPrefix}swrite", 
            new List<SymVar>() {new SymVar("str", new SymString(), null, SymVar.VarTypeEnum.Parameter)}, 
            null, 
            null, 
            null
        ) { }
}


public class IntWriteSymFunc : PredefinedSymFunc {
    public IntWriteSymFunc()
        : base(
            $"{SymStack.InternalPrefix}iwrite", 
            new List<SymVar>() {new SymVar("intnum", new SymInt(), null, SymVar.VarTypeEnum.Parameter)}, 
            null, 
            null, 
            null
        ) { }
}


public class DoubleWriteSymFunc : PredefinedSymFunc {
    public DoubleWriteSymFunc()
        : base(
            $"{SymStack.InternalPrefix}dwrite", 
            new List<SymVar>() {new SymVar("doublenum", new SymDouble(), null, SymVar.VarTypeEnum.Parameter)}, 
            null, 
            null, 
            null) { }
}

public class CharWriteSymFunc : PredefinedSymFunc {
    public CharWriteSymFunc()
        : base(
            $"{SymStack.InternalPrefix}cwrite", 
            new List<SymVar>() {new SymVar("c", new SymChar(), null, SymVar.VarTypeEnum.Parameter)}, 
            null, 
            null, 
            null) { }
}

public class BoolWriteSymFunc : PredefinedSymFunc {
    public BoolWriteSymFunc()
        : base(
            $"{SymStack.InternalPrefix}bwrite", 
            new List<SymVar>() {new SymVar("b", new SymBool(), null, SymVar.VarTypeEnum.Parameter)}, 
            null, 
            null, 
            null) { }
}
    
public class SymAlias : SymType {
    public override string Name { get; }
    public SymType Type { get; }
    public override int BSize => Type.BSize;
    
    public SymAlias(string name, SymType type) {
        Name = name;
        Type = type;
    }
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymTypeAlias : SymType {
    public override string Name { get; }
    public SymType Type { get; }
    public override int BSize => Type.BSize;
    
    public SymTypeAlias(string name, SymType type) {
        Name = name;
        Type = type;
    }
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

}  