namespace Compiler {
public interface IAstVisitor {
    void Visit(MultiChildrenNode node);
    void Visit(IntegerNode node);
}
}