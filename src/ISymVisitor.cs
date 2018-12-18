namespace Compiler {
public interface ISymVisitor {
    void Visit(SymVar symbol);
    void Visit(SymScalar symbol);
    void Visit(SymArray symbol);
}
}