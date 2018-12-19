using System.Collections;
using System.Collections.Generic;
using System.Linq;
    
namespace Compiler {

public class SymStack : IEnumerable<SymTable> {
    private Stack<SymTable> _stack = new Stack<SymTable>();
    // standard types
    public readonly SymInt SymInt = new SymInt();
    public readonly SymChar SymChar = new SymChar();
    public readonly SymFloat SymFloat = new SymFloat();
    public readonly SymBool SymBool = new SymBool();
    public readonly SymString SymString = new SymString();

    public SymStack() {
        _stack.Push(new SymTable());
        AddType(SymInt);        
        AddType(SymChar);        
        AddType(SymFloat);        
        AddType(SymBool);        
        AddType(SymString);        
    }

    public void Push() {
        _stack.Push(new SymTable());
    }

    public void Pop() {
        _stack.Pop();
    }
    
    // return nullable
    public Symbol Find(string name) {
        foreach (var table in _stack) {
            var symbol = table.Find(name);
            if (symbol != null)
                return symbol;
        }

        return null;
    }
    // return nullable
    public SymType FindType(string name) {
        if (Find(name) is SymType symType)
            return symType;
        return null;
    }
    // return nullable
    public SymVarOrConst FindVarOrConst(string name) {
        if (Find(name) is SymVarOrConst symbol)
            return symbol;
        return null;
    }

    public void AddVariable(IdentifierToken variableToken, IdentifierToken typeToken, ExprNode value = null) {
        var type = FindType(typeToken.Value);
        
        if (type == null)
            throw new TypeNotFoundException(typeToken.Lexeme, typeToken.Line, typeToken.Column);
        
        if (_stack.Peek().Find(variableToken.Value) != null)
            throw new DuplicateIdentifierException(variableToken.Lexeme, variableToken.Line, variableToken.Line);
        var symVar = new SymVar(variableToken.Value, type, value);
        
        _stack.Peek().Add(symVar);
    }

    public void AddConst(IdentifierToken constToken, SymConst symConst) {
        if (_stack.Peek().Find(constToken.Value) != null)
            throw new DuplicateIdentifierException(constToken.Value, constToken.Line, constToken.Line);
        
        
        _stack.Peek().Add(symConst);
    }

    public void AddType(SymType symType) {
        //todo: check?
        _stack.Peek().Add(symType);
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
}

// scalars
public abstract class SymScalar : SymType {
}

public class SymInt : SymScalar {
    public override string Name => "integer";

    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymFloat : SymScalar {
    public override string Name => "float";

    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymChar : SymScalar {
    public override string Name => "char";
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymString : SymType {
    public override string Name => "string";
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymBool : SymScalar {
    public override string Name => "boolean";
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public abstract class SymVarOrConst : Symbol {
    public override string Name { get; }
    public SymType Type { get; }
    public bool IsConst { get; }
    // not null only at constants. is used to print symtable.
    public string ConstValue { get; protected set; } = null;

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
    // nullable
    public ExprNode InitialValue { get; }
    
    public SymVar(string name, SymType type, ExprNode initialValue) : base(name, type, false) {
        InitialValue = initialValue;
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
        ConstValue = value.ToString();
    }
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymFloatConst : SymConst {
    public double Value { get; }

    public SymFloatConst(string name, SymType type, double value) : base(name, type) {
        Value = value;
        ConstValue = value.ToString();
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
        ConstValue = value.ToString();
    }

    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymBoolConst : SymConst {
    public bool Value { get; }

    public SymBoolConst(string name, SymType type, bool value) : base(name, type) {
        Value = value;
        ConstValue = value.ToString();
    }
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymStringConst : SymConst {
    public string Value { get; }

    public SymStringConst(string name, SymType type, string value) : base(name, type) {
        Value = value;
        ConstValue = value;
    }
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

public class SymArray : SymType {
    private readonly long _startIndex;
    private readonly long _endIndex;
    private readonly SymType _type;
    
    public override string Name => $"array of {_type.Name}";

    public SymArray(long startIndex, long endIndex, SymType type) {
        _startIndex = startIndex;
        _endIndex = endIndex;
        _type = type;
    }
    
    public override void Accept(ISymVisitor visitor) {
        visitor.Visit(this);
    }
}

}  