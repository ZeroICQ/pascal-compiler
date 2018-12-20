namespace Compiler {
public interface ISymVisitor {
    void Visit(SymVarOrConst symbol);
    void Visit(Symbol symbol);
    void Visit(SymScalar symbol);
    void Visit(SymAlias symbols);
}
}