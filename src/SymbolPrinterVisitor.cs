using System;
using System.Collections.Generic;
using System.Text;

namespace Compiler {
public class SymbolPrinterVisitor : ISymVisitor {
    private int _symNameColumnLength = 0;
    private int _symTypeColumnLength = 0;
    private const int _verticalSeparatorWidth = 4;
    private int LineWidth => _symNameColumnLength + _symTypeColumnLength + _verticalSeparatorWidth;

    private const char _lineSeparator = '─';
    private const char _columnSeparator = '│';
    
    private List<KeyValuePair<string, SymTable>> _nestedSymTable = new List<KeyValuePair<string, SymTable>>();

    private List<KeyValuePair<string, string>> _entries = new List<KeyValuePair<string, string>>();

    public void Visit(SymVarOrConst symbol) {
        if (symbol is SymFuncConst symConstFunc) {
            Visit(symConstFunc.FuncType);
            return;
        }
        
        var name = symbol.Name;
        var type = symbol.Type.Name;

        if (symbol.IsConst) {
            type = $"const {type} = {symbol.InitialStringValue}";
        }
        else if (symbol.InitialStringValue != null) {
            type = $"{type} = {symbol.InitialStringValue}";
        }
        
        
        UpdateNameColumnLength(name.Length);
        UpdateTypeColumnLength(type.Length);
        
        _entries.Add(new KeyValuePair<string, string>(name, type));
    }

    public void Visit(Symbol symbol) {
        var name  = symbol.Name;
        const string type = "Type";
        
        UpdateNameColumnLength(name.Length);
        UpdateTypeColumnLength(type.Length);
        
        _entries.Add(new KeyValuePair<string, string>(name, type));
    }

    public void Visit(SymScalar symbol) {
        var name = symbol.Name;
        const string type = "Scalar type";;
        
        UpdateNameColumnLength(name.Length);
        UpdateTypeColumnLength(type.Length);
        
        _entries.Add(new KeyValuePair<string, string>(name, type));
    }

    public void Visit(SymAlias symbol) {
        var name = symbol.Name;
        var type = $"alias to {symbol.Type.Name}";
        
        UpdateNameColumnLength(name.Length);
        UpdateTypeColumnLength(type.Length);
        
        _entries.Add(new KeyValuePair<string, string>(name, type));
    }

    public void Visit(SymTypeAlias symbol) {
        var name = symbol.Name;
        var type = $"type alias to {symbol.Type.Name}";
        
        UpdateNameColumnLength(name.Length);
        UpdateTypeColumnLength(type.Length);
        
        _entries.Add(new KeyValuePair<string, string>(name, type));
    }

    public void Visit(SymRecord symbol) {
        _nestedSymTable.Add(new KeyValuePair<string, SymTable>($"Record \"{symbol.Name}\"", symbol.Fields));
    }

    public void Visit(SymFunc symbol) {
        if (symbol is PredefinedSymFunc)
            return;
        
        var name = symbol.Name;
        var returnType = symbol.ReturnType.Name;

        var paramsList = new StringBuilder();
        var isFirst = true;
        foreach (var p in symbol.Parameters) {
            if (!isFirst)
                paramsList.Append(", ");
            else
                isFirst = false;

            switch (p.LocType) {
                case SymVar.SymLocTypeEnum.OutParameter:
                    paramsList.Append("out ");
                    break;
                case SymVar.SymLocTypeEnum.VarParameter:
                    paramsList.Append("var ");
                    break;
                
                case SymVar.SymLocTypeEnum.ConstParameter:
                    paramsList.Append("const ");
                    break;
            }
            paramsList.Append($"{p.Name}:{p.Type.Name}");
        }
            
        var type = $"function({paramsList}): {returnType}";
        
        UpdateNameColumnLength(name.Length);
        UpdateTypeColumnLength(type.Length);
        
        _entries.Add(new KeyValuePair<string, string>(name, type));
    }
    
    // dont print not to break all tests
    public void Visit(PredefinedSymFunc symbol) {
    }

    public void Print(List<StringBuilder> canvas, string namespaceName = "Global") {
        // head
        canvas.Add(new StringBuilder(""));
        canvas.Add(new StringBuilder($"{namespaceName}:"));
        UpdateNameColumnLength("Name".Length);
        UpdateTypeColumnLength("Type".Length);
        PrintHorizontalLine(canvas, _lineSeparator);
        InsertLine("Name", "Type", canvas);
        
        PrintHorizontalLine(canvas, '═');
        
        foreach (var entry in _entries) {
            InsertLine(entry.Key, entry.Value, canvas);
            PrintHorizontalLine(canvas, _lineSeparator);
        }

        foreach (var (key, value) in _nestedSymTable) {
            var symbolPrinter = new SymbolPrinterVisitor();
            
            foreach (var symbol in value) {
                symbol.Accept(symbolPrinter);
            }
             
            symbolPrinter.Print(canvas, key);
            
        }
    }

    private void InsertLine(string key, string value, List<StringBuilder> canvas) {
        var line = new StringBuilder();
        line.Append(_columnSeparator);
            
        line.Append(key);
        InsertSpace(line, _symNameColumnLength - key.Length + 1);
            
        line.Append(_columnSeparator);
            
        line.Append(value);
        InsertSpace(line, _symTypeColumnLength - value.Length);
            
        line.Append(_columnSeparator);
        canvas.Add(line);
    }

    private void PrintHorizontalLine(List<StringBuilder> canvas, char c) {
        var line = new StringBuilder();
        line.Insert(0, c.ToString(), LineWidth);
        canvas.Add(line);
    }

    private void InsertSpace(StringBuilder line, int amount) {
        line.Append(' ', amount);
    }

    private void UpdateNameColumnLength(int length) {
        _symNameColumnLength = Math.Max(_symNameColumnLength, length);
    }

    private void UpdateTypeColumnLength(int length) {
        _symTypeColumnLength = Math.Max(_symTypeColumnLength, length);
    }
    
}
}