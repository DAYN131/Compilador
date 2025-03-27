using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilador
{
    public class Token
    {
        public TokenType Type { get; }
        public string Lexeme { get; }
        public int Line { get; }
        public int Position { get; }

        public Token(TokenType type, string lexeme, int line, int position)
        {
            Type = type;
            Lexeme = lexeme;
            Line = line;
            Position = position;
        }

        public override string ToString()
        {
            return $"{Type} '{Lexeme}' (Linea: {Line}, Posición: {Position})";
        }
    }

}
