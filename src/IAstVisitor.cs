namespace Compiler {
public interface IAstVisitor {
    void Visit(StmntNode node);
    void Visit(BinaryExprNode node);
    void Visit(ConstantNode node);
}
}