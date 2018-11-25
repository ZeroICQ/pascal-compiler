namespace Compiler {
public interface IAstVisitor<out T> {
    T Visit(BlockNode node);
    
    T Visit(BinaryExprNode node);
    T Visit(IntegerNode node);
    T Visit(FloatNode node);
    T Visit(IdentifierNode node);
    
    T Visit(FunctionCallNode node);
    T Visit(CastNode node);
    T Visit(AccessNode node);
    T Visit(IndexNode node);
    T Visit(CharNode node);
    T Visit(UnaryOperationNode node);
    
    T Visit(AssignNode node);
    T Visit (IfNode node);
    T Visit(WhileNode node);
}
}