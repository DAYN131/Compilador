using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilador
{


    public enum TokenType
    {
        // Palabras clave
        True,
        False,
        And,
        Or,
        Not,

        // Símbolos
        LParen,    // (
        RParen,    // )

        // Identificadores y literales
        Identifier,
        Number,

        // Operadores
        Plus,      // +
        Minus,     // -
        Multiply,  // *
        Divide,    // /

        // Fin de archivo
        EOF
    }
}
