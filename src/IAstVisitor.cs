namespace Compiler {
public interface IAstVisitor<out T> {
    T Visit(BlockNode node);
    T Visit(BinaryExprNode node);
    T Visit(IntegerNode node);
    T Visit(FloatNode node);
    T Visit(IdentifierNode node);
}
}