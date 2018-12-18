namespace Compiler {
public interface ISymVisitor {
    void Visit(SymVar symbol);
    void Visit(Symbol symbol);
    void Visit(SymScalar symbol);
}
}