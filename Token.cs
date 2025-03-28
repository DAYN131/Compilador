using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilador
{


    // Clase Token, aqui hacemos una clase formalamente de lo que es un token
    // el cual icluira:

    // Tipo-> Tipo de Token al que pertenece
    // Lexema -> Texto literal del codigo fuente
    // Line - > Linea de texto en la que esta
    // Posicion - > Posicion en la linea, es como el numero del caracter

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
