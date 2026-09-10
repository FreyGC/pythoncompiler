using System.Collections.Generic;

namespace PythonCompiler
{
    public class Tokenizer
    {
        private string _input;
        private int _position;
        private int _line = 1;
        private int _column = 1;
        
        // Pila para llevar el control de los niveles de indentación
        private Stack<int> _indentationLevels;

        private readonly HashSet<string> _keywords = new HashSet<string>
        {
            "def", "if", "else", "elif", "while", "for", "in", "return", "print", "True", "False", "None"
        };

        public Tokenizer(string input)
        {
            _input = input;
            _position = 0;
            _indentationLevels = new Stack<int>();
            _indentationLevels.Push(0); // Nivel base siempre es 0
        }

        public List<Token> Tokenize()
        {
            List<Token> tokens = new List<Token>();

            // Evaluar la indentación de la primera línea del archivo
            HandleIndentation(tokens);

            while (_position < _input.Length)
            {
                char currentChar = _input[_position];

                // 1. Manejar Comentarios (ignorarlos hasta el salto de línea)
                if (currentChar == '#')
                {
                    while (_position < _input.Length && _input[_position] != '\n')
                    {
                        Advance();
                    }
                    continue;
                }

                // 2. Manejar saltos de línea y la indentación de la línea siguiente
                if (currentChar == '\n')
                {
                    tokens.Add(new Token(TokenType.NewLine, "\\n", _line, _column));
                    Advance();
                    _line++;
                    _column = 1;
                    
                    // Al iniciar una nueva línea, evaluamos su indentación
                    HandleIndentation(tokens);
                    continue;
                }

                // 3. Ignorar espacios en blanco ENTRE palabras (ya no al inicio de línea)
                if (currentChar == ' ' || currentChar == '\t' || currentChar == '\r')
                {
                    Advance();
                    continue;
                }

                // 4. Manejar Identificadores y Palabras Clave
                if (char.IsLetter(currentChar) || currentChar == '_')
                {
                    tokens.Add(ReadIdentifierOrKeyword());
                    continue;
                }

                // 5. Manejar Números
                if (char.IsDigit(currentChar))
                {
                    tokens.Add(ReadNumber());
                    continue;
                }

                // 6. Manejar Strings (Cadenas de texto)
                if (currentChar == '"' || currentChar == '\'')
                {
                    tokens.Add(ReadString(currentChar));
                    continue;
                }

                // 7. Manejar Operadores y Puntuación
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

            // AL FINAL DEL ARCHIVO: Generar DEDENTs para cualquier bloque que haya quedado abierto
            while (_indentationLevels.Count > 1)
            {
                _indentationLevels.Pop();
                tokens.Add(new Token(TokenType.Dedent, "DEDENT", _line, _column));
            }

            tokens.Add(new Token(TokenType.EOF, "EOF", _line, _column));
            return tokens;
        }

        // Método clave para calcular INDENT y DEDENT
        private void HandleIndentation(List<Token> tokens)
        {
            int spaces = 0;

            // Contar espacios al inicio de la línea
            while (_position < _input.Length)
            {
                char c = _input[_position];
                if (c == ' ') 
                { 
                    spaces++; 
                    Advance(); 
                }
                else if (c == '\t') 
                { 
                    spaces += 4; // Asumimos que un tab equivale a 4 espacios
                    Advance(); 
                }
                else 
                { 
                    break; // Llegamos al primer carácter visible
                }
            }

            // Si la línea está en blanco (solo tiene un salto de línea, un comentario o EOF), la ignoramos
            if (_position >= _input.Length || _input[_position] == '\n' || _input[_position] == '\r' || _input[_position] == '#')
            {
                return; 
            }

            int currentIndent = _indentationLevels.Peek();

            // Si hay más espacios que el tope de la pila -> Entramos a un bloque
            if (spaces > currentIndent)
            {
                _indentationLevels.Push(spaces);
                tokens.Add(new Token(TokenType.Indent, "INDENT", _line, _column));
            }
            // Si hay menos espacios que el tope -> Salimos de uno o más bloques
            else if (spaces < currentIndent)
            {
                while (_indentationLevels.Count > 0 && _indentationLevels.Peek() > spaces)
                {
                    _indentationLevels.Pop();
                    tokens.Add(new Token(TokenType.Dedent, "DEDENT", _line, _column));
                }

                // En Python, si la indentación al retroceder no coincide con una previa, es un error fatal de sintaxis.
                if (_indentationLevels.Peek() != spaces)
                {
                    tokens.Add(new Token(TokenType.Error, "IndentationError", _line, _column));
                }
            }
        }

        // ... Los métodos ReadIdentifierOrKeyword, ReadNumber, ReadString y Advance se mantienen exactamente igual
        // (Asegúrate de dejarlos aquí dentro de la clase Tokenizer)
        
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