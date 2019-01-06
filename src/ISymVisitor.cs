namespace Compiler {
public interface ISymVisitor {
    void Visit(SymVarOrConst symbol);
    void Visit(Symbol symbol);
    void Visit(SymScalar symbol);
    void Visit(SymAlias symbol);
    void Visit(SymTypeAlias symbol);
    void Visit(SymRecord symbol);
    void Visit(SymFunc symbol);
    
    void Visit(WritelnSymFunc symbol);
    void Visit(WriteSymFunc symbol);
}
}