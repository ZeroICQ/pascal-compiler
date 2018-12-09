using System.Collections.Generic;

namespace Compiler {
public class SymStack {
    private Stack<BlockNode> _stack;

    public void Push(BlockNode stmnt) {
        _stack.Push(stmnt);
    }

    public StatementNode Pop() {
        return _stack.Pop();
    }

    public void AddControlSequence(ControlSequence ctrl) {
        _stack.Peek().AddStatement(ctrl);
    }
}
}