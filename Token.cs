using System;

namespace PythonCompiler
{
    // Definir los tipos básicos que encontraremos en Python
    public enum TokenType
    {
        Keyword,      // def, if, else, print, return...
        Identifier,   // Nombres de variables o funciones
        Number,       // 123, 3.14
        String,       // "Hola mundo"
        Operator,     // +, -, =, ==, !=
        Punctuation,  // (), {}, [], :, ,
        Whitespace,   // Espacios
        NewLine,      // Saltos de línea
        Indent,       // Nuevo: Inicio de bloque
        Dedent,       // Nuevo: Fin de bloque
        EOF,          // Fin de archivo
        Error         // Símbolos no reconocidos
    }

    public class Token
    {
        public TokenType Type { get; set; }
        public string Value { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public Token(TokenType type, string value, int line, int column)
        {
            Type = type;
            Value = value;
            Line = line;
            Column = column;
        }
    }
}