using System;
using System.Collections.Generic;

namespace Compiler {

public class SymStack {
    private Stack<SymTable> _stack;

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

    public void AddVariable(IdentifierToken variableToken, IdentifierToken typeToken, ConstantToken value = null) {
        var type = FindType(typeToken.Value);
        
        if (type == null)
            throw new TypeNotFoundException(typeToken.Lexeme, typeToken.Line, typeToken.Column);
        
        if (_stack.Peek().Find(variableToken.Value) != null)
            throw new DuplicateIdentifierException(variableToken.Lexeme, variableToken.Line, variableToken.Line);

        var symVar = new SymVar(variableToken.Value, type);
    }
}

internal class SymTable {
    private Dictionary<string, Symbol> _sym_map;
    private List<Symbol> _sym_list;
    
    public void Add(Symbol symbol) {
        _sym_map.Add(symbol.Name, symbol);
        _sym_list.Add(symbol);
    }

    public Symbol Find(string name) {
        _sym_map.TryGetValue(name, out var symbol);
        return symbol;
    }
}

public abstract class Symbol {
    public abstract string Name { get; }
}


// types
public abstract class SymType : Symbol {
}

public abstract class SymScalar : SymType {
}

public class SymInt : SymScalar {
    public override string Name => "integer";
}

public class SymFloat : SymScalar {
    public override string Name => "float";
}

public class SymChar : SymScalar {
    public override string Name => "char";
}

// variables
public class SymVar : Symbol {
    public override string Name { get; }
    public SymType Type;
    public ExprNode InitialValue { get; }
    
    public SymVar(string name, SymType type, ExprNode) {
        Name = name;
        Type = type;
    }
}

}  