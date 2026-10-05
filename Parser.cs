using System;
using System.Collections.Generic;

namespace PythonCompiler
{
    public class Parser
    {
        private readonly List<Token> _tokens;
        private int _current = 0;

        public Parser(List<Token> tokens) { _tokens = tokens; }

        public ProgramNode Parse()
        {
            var program = new ProgramNode();
            while (!IsAtEnd())
            {
                if (Match(TokenType.NewLine)) continue;
                var stmt = ParseStatement();
                if (stmt != null) program.Statements.Add(stmt);
            }
            return program;
        }

        private StatementNode ParseStatement()
        {
            if (Check(TokenType.Keyword))
            {
                switch (Peek().Value)
                {
                    case "def": return ParseFunctionDefinition();
                    case "if": return ParseIfStatement();
                    case "while": return ParseWhileStatement();
                    case "for": return ParseForStatement();
                    case "return": return ParseReturnStatement();
                }
            }

            // Si es un identificador seguido de un '=', es una asignación
            if (Check(TokenType.Identifier) && Lookahead(1)?.Value == "=")
            {
                return ParseAssignment();
            }

            // Si no es nada de lo anterior, lo interpretamos como una expresión (ej: print())
            ExpressionNode expr = ParseExpression();
            ConsumeNewLineOrEOF();
            return new ExpressionStatementNode(expr);
        }

        private FunctionDefinitionNode ParseFunctionDefinition()
        {
            ConsumeKeyword("def", "Se esperaba 'def'.");
            Token nameToken = Consume(TokenType.Identifier, "Se esperaba el nombre de la función.");
            ConsumePunctuation("(", "Se esperaba '(' después del nombre.");

            var funcDef = new FunctionDefinitionNode(nameToken.Value);

            if (!CheckPunctuation(")"))
            {
                do
                {
                    Token paramToken = Consume(TokenType.Identifier, "Se esperaba el nombre del parámetro.");
                    funcDef.Parameters.Add(paramToken.Value);
                } while (MatchPunctuation(","));
            }

            ConsumePunctuation(")", "Se esperaba ')'.");
            ConsumePunctuation(":", "Se esperaba ':' al final.");
            Consume(TokenType.NewLine, "Se esperaba salto de línea tras ':'.");
            Consume(TokenType.Indent, "Se esperaba indentación (INDENT).");

            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                if (Match(TokenType.NewLine)) continue;
                funcDef.Body.Add(ParseStatement());
            }

            Consume(TokenType.Dedent, "Se esperaba fin de bloque (DEDENT).");
            return funcDef;
        }

        private AssignmentNode ParseAssignment()
        {
            Token varToken = Consume(TokenType.Identifier, "Se esperaba el nombre de la variable.");
            Consume(TokenType.Operator, "Se esperaba '='.");
            ExpressionNode value = ParseExpression();
            ConsumeNewLineOrEOF();
            return new AssignmentNode(varToken.Value, value);
        }

        private IfStatementNode ParseIfStatement()
        {
            ConsumeKeyword("if", "Se esperaba 'if'.");
            ExpressionNode condition = ParseExpression();
            ConsumePunctuation(":", "Se esperaba ':'.");
            Consume(TokenType.NewLine, "Se esperaba salto de línea.");
            Consume(TokenType.Indent, "Se esperaba indentación (INDENT).");

            var ifNode = new IfStatementNode(condition);

            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                if (Match(TokenType.NewLine)) continue;
                ifNode.ThenBranch.Add(ParseStatement());
            }

            Consume(TokenType.Dedent, "Se esperaba DEDENT.");

            if (Check(TokenType.Keyword) && Peek().Value == "else")
            {
                Advance(); 
                ConsumePunctuation(":", "Se esperaba ':' tras 'else'.");
                Consume(TokenType.NewLine, "Se esperaba salto de línea.");
                Consume(TokenType.Indent, "Se esperaba indentación para 'else'.");

                while (!Check(TokenType.Dedent) && !IsAtEnd())
                {
                    if (Match(TokenType.NewLine)) continue;
                    ifNode.ElseBranch.Add(ParseStatement());
                }
                Consume(TokenType.Dedent, "Se esperaba DEDENT en 'else'.");
            }
            return ifNode;
        }

        private WhileStatementNode ParseWhileStatement()
        {
            ConsumeKeyword("while", "Se esperaba 'while'.");
            ExpressionNode condition = ParseExpression();
            ConsumePunctuation(":", "Se esperaba ':'.");
            Consume(TokenType.NewLine, "Se esperaba salto de línea.");
            Consume(TokenType.Indent, "Se esperaba INDENT.");

            var whileNode = new WhileStatementNode(condition);
            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                if (Match(TokenType.NewLine)) continue;
                whileNode.Body.Add(ParseStatement());
            }

            Consume(TokenType.Dedent, "Se esperaba DEDENT.");
            return whileNode;
        }

        private ForStatementNode ParseForStatement()
        {
            ConsumeKeyword("for", "Se esperaba 'for'.");
            Token iterator = Consume(TokenType.Identifier, "Se esperaba una variable iteradora.");
            ConsumeKeyword("in", "Se esperaba 'in'.");
            ExpressionNode iterable = ParseExpression();
            ConsumePunctuation(":", "Se esperaba ':'.");
            Consume(TokenType.NewLine, "Se esperaba salto de línea.");
            Consume(TokenType.Indent, "Se esperaba INDENT.");

            var forNode = new ForStatementNode { IteratorVariable = iterator.Value, Iterable = iterable };
            while (!Check(TokenType.Dedent) && !IsAtEnd())
            {
                if (Match(TokenType.NewLine)) continue;
                forNode.Body.Add(ParseStatement());
            }

            Consume(TokenType.Dedent, "Se esperaba DEDENT.");
            return forNode;
        }

        private ReturnStatementNode ParseReturnStatement()
        {
            ConsumeKeyword("return", "Se esperaba 'return'.");
            var returnNode = new ReturnStatementNode();
            
            if (!Check(TokenType.NewLine) && !Check(TokenType.EOF))
            {
                returnNode.Value = ParseExpression();
            }
            
            ConsumeNewLineOrEOF();
            return returnNode;
        }

        private ExpressionNode ParseExpression()
        {
            ExpressionNode left = ParsePrimary();

            if (Check(TokenType.Operator))
            {
                string op = Advance().Value;
                ExpressionNode right = ParsePrimary();
                return new BinaryOpNode(left, op, right);
            }

            return left;
        }

        private ExpressionNode ParsePrimary()
        {
            if (Match(TokenType.Number)) return new NumberLiteralNode(Previous().Value);
            if (Match(TokenType.String)) return new StringLiteralNode(Previous().Value);
            
            if (Match(TokenType.Identifier))
            {
                Token id = Previous();
                // ¿Es una llamada a función? (identificador seguido de paréntesis)
                if (CheckPunctuation("("))
                {
                    Advance(); // Consumir '('
                    var callNode = new FunctionCallNode(id.Value);
                    if (!CheckPunctuation(")"))
                    {
                        do
                        {
                            callNode.Arguments.Add(ParseExpression());
                        } while (MatchPunctuation(","));
                    }
                    ConsumePunctuation(")", "Se esperaba ')'.");
                    return callNode;
                }
                
                // Si no tiene paréntesis, es solo una variable
                return new VariableAccessNode(id.Value);
            }
            
            if (Match(TokenType.Keyword))
            {
                string kw = Previous().Value;
                if (kw == "True") return new BooleanLiteralNode(true);
                if (kw == "False") return new BooleanLiteralNode(false);
                if (kw == "None") return new NoneLiteralNode();
            }

            throw new Exception($"Error sintáctico en Línea {Peek().Line}: Expresión inválida cerca de '{Peek().Value}'");
        }

        private bool Check(TokenType type) => !IsAtEnd() && Peek().Type == type;
        private bool CheckPunctuation(string symbol) => Check(TokenType.Punctuation) && Peek().Value == symbol;
        private bool Match(TokenType type) { if (Check(type)) { Advance(); return true; } return false; }
        private bool MatchPunctuation(string symbol) { if (CheckPunctuation(symbol)) { Advance(); return true; } return false; }
        
        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw new Exception($"Línea {Peek().Line}, Col {Peek().Column}: {message} (Encontrado: '{Peek().Value}')");
        }
        
        private void ConsumeKeyword(string kw, string message)
        {
            if (Check(TokenType.Keyword) && Peek().Value == kw) { Advance(); return; }
            throw new Exception($"Línea {Peek().Line}: {message} (Encontrado: '{Peek().Value}')");
        }
        
        private void ConsumePunctuation(string symbol, string message)
        {
            if (CheckPunctuation(symbol)) { Advance(); return; }
            throw new Exception($"Línea {Peek().Line}: {message} (Encontrado: '{Peek().Value}')");
        }
        
        private void ConsumeNewLineOrEOF()
        {
            if (!IsAtEnd() && !Match(TokenType.NewLine))
            {
                if (!Check(TokenType.EOF)) throw new Exception($"Línea {Peek().Line}: Se esperaba fin de línea. (Encontrado: '{Peek().Value}')");
            }
        }

        private Token Peek() => _tokens[_current];
        private Token Previous() => _tokens[_current - 1];
        private Token Lookahead(int distance) => (_current + distance < _tokens.Count) ? _tokens[_current + distance] : null;
        private bool IsAtEnd() => Peek().Type == TokenType.EOF;
        private Token Advance() { if (!IsAtEnd()) _current++; return Previous(); }
    }
}