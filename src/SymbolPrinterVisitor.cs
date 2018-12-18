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

    private List<KeyValuePair<string, string>> _entries = new List<KeyValuePair<string, string>>();

    public void Visit(Symbol symbol) {
        const string name = "Type";
        var type = symbol.Name;
        
        UpdateNameColumnLength(name.Length);
        UpdateTypeColumnLength(type.Length);
        
        _entries.Add(new KeyValuePair<string, string>(name, type));
    }

    public void Visit(SymScalar symbol) {
        const string name = "Scalar type";
        var type = symbol.Name;
        
        UpdateNameColumnLength(name.Length);
        UpdateTypeColumnLength(type.Length);
        
        _entries.Add(new KeyValuePair<string, string>(name, type));
    }

    public void Visit(SymArray symbol) {
        // todo: implement
        throw new NotImplementedException();
    }

    public void Visit(SymVar symVar) {
        var name = symVar.Name;
        var type = symVar.Type.Name;
        
        UpdateNameColumnLength(name.Length);
        UpdateTypeColumnLength(type.Length);
        
        _entries.Add(new KeyValuePair<string, string>(name, type));
    }

    public void Print(List<StringBuilder> canvas, string namespaceName = "Global") {
        // head
        canvas.Add(new StringBuilder(""));
        canvas.Add(new StringBuilder($"{namespaceName}:"));
        PrintHorizontalLine(canvas, _lineSeparator);
        InsertLine("Name", "Type", canvas);
        PrintHorizontalLine(canvas, '═');
        
        foreach (var entry in _entries) {
            InsertLine(entry.Key, entry.Value, canvas);
            PrintHorizontalLine(canvas, _lineSeparator);
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