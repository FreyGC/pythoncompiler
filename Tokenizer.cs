using System.Collections.Generic;

namespace PythonCompiler
{
    public class Tokenizer
    {
        private string _input;
        private int _position;
        private int _line = 1;
        private int _column = 1;
        private Stack<int> _indentationLevels;

        // Se excluyó "print" para que sea tratada como función (Identificador)
        private readonly HashSet<string> _keywords = new HashSet<string>
        {
            "def", "if", "else", "elif", "while", "for", "in", "return", "True", "False", "None"
        };

        public Tokenizer(string input)
        {
            _input = input;
            _position = 0;
            _indentationLevels = new Stack<int>();
            _indentationLevels.Push(0);
        }

        public List<Token> Tokenize()
        {
            List<Token> tokens = new List<Token>();
            HandleIndentation(tokens);

            while (_position < _input.Length)
            {
                char currentChar = _input[_position];

                if (currentChar == '#')
                {
                    while (_position < _input.Length && _input[_position] != '\n') Advance();
                    continue;
                }

                if (currentChar == '\n')
                {
                    tokens.Add(new Token(TokenType.NewLine, "\\n", _line, _column));
                    Advance();
                    _line++;
                    _column = 1;
                    HandleIndentation(tokens);
                    continue;
                }

                if (currentChar == ' ' || currentChar == '\t' || currentChar == '\r')
                {
                    Advance();
                    continue;
                }

                if (char.IsLetter(currentChar) || currentChar == '_')
                {
                    tokens.Add(ReadIdentifierOrKeyword());
                    continue;
                }

                if (char.IsDigit(currentChar))
                {
                    tokens.Add(ReadNumber());
                    continue;
                }

                if (currentChar == '"' || currentChar == '\'')
                {
                    tokens.Add(ReadString(currentChar));
                    continue;
                }

                if ("=+-*/<>!&|".Contains(currentChar))
                {
                    tokens.Add(new Token(TokenType.Operator, currentChar.ToString(), _line, _column));
                    Advance();
                    continue;
                }
                
                if ("():,[]{}.@".Contains(currentChar))
                {
                    tokens.Add(new Token(TokenType.Punctuation, currentChar.ToString(), _line, _column));
                    Advance();
                    continue;
                }

                tokens.Add(new Token(TokenType.Error, currentChar.ToString(), _line, _column));
                Advance();
            }

            while (_indentationLevels.Count > 1)
            {
                _indentationLevels.Pop();
                tokens.Add(new Token(TokenType.Dedent, "DEDENT", _line, _column));
            }

            tokens.Add(new Token(TokenType.EOF, "EOF", _line, _column));
            return tokens;
        }

        private void HandleIndentation(List<Token> tokens)
        {
            int spaces = 0;
            while (_position < _input.Length)
            {
                char c = _input[_position];
                if (c == ' ') { spaces++; Advance(); }
                else if (c == '\t') { spaces += 4; Advance(); }
                else break;
            }

            if (_position >= _input.Length || _input[_position] == '\n' || _input[_position] == '\r' || _input[_position] == '#')
                return; 

            int currentIndent = _indentationLevels.Peek();

            if (spaces > currentIndent)
            {
                _indentationLevels.Push(spaces);
                tokens.Add(new Token(TokenType.Indent, "INDENT", _line, _column));
            }
            else if (spaces < currentIndent)
            {
                while (_indentationLevels.Count > 0 && _indentationLevels.Peek() > spaces)
                {
                    _indentationLevels.Pop();
                    tokens.Add(new Token(TokenType.Dedent, "DEDENT", _line, _column));
                }

                if (_indentationLevels.Peek() != spaces)
                {
                    tokens.Add(new Token(TokenType.Error, "IndentationError", _line, _column));
                }
            }
        }

        private Token ReadIdentifierOrKeyword()
        {
            int startColumn = _column;
            string value = "";
            while (_position < _input.Length && (char.IsLetterOrDigit(_input[_position]) || _input[_position] == '_'))
            {
                value += _input[_position];
                Advance();
            }
            TokenType type = _keywords.Contains(value) ? TokenType.Keyword : TokenType.Identifier;
            return new Token(type, value, _line, startColumn);
        }

        private Token ReadNumber()
        {
            int startColumn = _column;
            string value = "";
            while (_position < _input.Length && char.IsDigit(_input[_position]))
            {
                value += _input[_position];
                Advance();
            }
            return new Token(TokenType.Number, value, _line, startColumn);
        }

        private Token ReadString(char quoteType)
        {
            int startColumn = _column;
            string value = quoteType.ToString();
            Advance(); 
            while (_position < _input.Length && _input[_position] != quoteType)
            {
                value += _input[_position];
                Advance();
            }
            if (_position < _input.Length)
            {
                value += _input[_position]; 
                Advance();
            }
            return new Token(TokenType.String, value, _line, startColumn);
        }

        private void Advance()
        {
            _position++;
            _column++;
        }
    }
}