using System.Collections.Generic;

namespace PythonCompiler
{
    public abstract class AstNode { }

    public class ProgramNode : AstNode
    {
        public List<StatementNode> Statements { get; set; } = new List<StatementNode>();
    }

    public abstract class StatementNode : AstNode { }
    public abstract class ExpressionNode : AstNode { }

    // NUEVO: Permite ejecutar cualquier expresión como si fuera una línea de código
    public class ExpressionStatementNode : StatementNode
    {
        public ExpressionNode Expression { get; set; }
        public ExpressionStatementNode(ExpressionNode expr) { Expression = expr; }
    }

    public class AssignmentNode : StatementNode
    {
        public string VariableName { get; set; }
        public ExpressionNode Value { get; set; }
        public AssignmentNode(string variableName, ExpressionNode value)
        {
            VariableName = variableName;
            Value = value;
        }
    }

    public class IfStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; set; }
        public List<StatementNode> ThenBranch { get; set; } = new List<StatementNode>();
        public List<StatementNode> ElseBranch { get; set; } = new List<StatementNode>();
        public IfStatementNode(ExpressionNode condition) { Condition = condition; }
    }

    public class FunctionDefinitionNode : StatementNode
    {
        public string Name { get; set; }
        public List<string> Parameters { get; set; } = new List<string>();
        public List<StatementNode> Body { get; set; } = new List<StatementNode>();
        public FunctionDefinitionNode(string name) { Name = name; }
    }

    public class WhileStatementNode : StatementNode
    {
        public ExpressionNode Condition { get; set; }
        public List<StatementNode> Body { get; set; } = new List<StatementNode>();
        public WhileStatementNode(ExpressionNode condition) { Condition = condition; }
    }

    public class ForStatementNode : StatementNode
    {
        public string IteratorVariable { get; set; }
        public ExpressionNode Iterable { get; set; }
        public List<StatementNode> Body { get; set; } = new List<StatementNode>();
    }

    public class ReturnStatementNode : StatementNode
    {
        public ExpressionNode Value { get; set; }
    }

    // MODIFICADO: Ahora hereda de ExpressionNode para poder anidarse
    public class FunctionCallNode : ExpressionNode
    {
        public string FunctionName { get; set; }
        public List<ExpressionNode> Arguments { get; set; } = new List<ExpressionNode>();
        public FunctionCallNode(string functionName) { FunctionName = functionName; }
    }

    public class BinaryOpNode : ExpressionNode
    {
        public ExpressionNode Left { get; set; }
        public string Operator { get; set; }
        public ExpressionNode Right { get; set; }
        public BinaryOpNode(ExpressionNode left, string op, ExpressionNode right)
        {
            Left = left; Operator = op; Right = right;
        }
    }

    public class NumberLiteralNode : ExpressionNode
    {
        public string Value { get; set; }
        public NumberLiteralNode(string value) { Value = value; }
    }

    public class StringLiteralNode : ExpressionNode
    {
        public string Value { get; set; }
        public StringLiteralNode(string value) { Value = value; }
    }

    public class VariableAccessNode : ExpressionNode
    {
        public string Name { get; set; }
        public VariableAccessNode(string name) { Name = name; }
    }

    public class BooleanLiteralNode : ExpressionNode
    {
        public bool Value { get; set; }
        public BooleanLiteralNode(bool value) { Value = value; }
    }

    public class NoneLiteralNode : ExpressionNode { }
}