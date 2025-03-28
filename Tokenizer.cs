using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilador
{

    // Clase para separar el contenido en Tokens y sus lexemas
    public class Tokenizer
    {
        private readonly string _source; // codigo fuente (lo que escribio el usuario)
        private int _currentPosition; // posicion actual
        private int _currentLine;      // Linea actual
        private int _linePosition;     // Posicion en la Linea actual



        // Constructor de nuestra clase Tokenizer
        public Tokenizer(string source)
        {
            _source = source;
            _currentPosition = 0;
            _currentLine = 1;
            _linePosition = 1;
        }


        // Metodo tokenize que devolvera una lista de tipo Token
        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            // Mientras no hayamos llegado al final del código
            while (!IsAtEnd())
            {
                // Obtener el caracter actual y avanzar
                char current = Advance();

                // Ignorar espacios en blanco
                if (char.IsWhiteSpace(current))
                {

                    // Salto de Linea
                    if (current == '\n')
                    {
                        _currentLine++; //  aumentar el valor de la linea actual
                        _linePosition = 1;  // Reiniciar el valor de la posicion en la linea a 1
                    }
                    continue;
                }

                // Comentarios de línea (--)

                // Si nuestro caracter actual es  - y el siguiente a este es -
                // Crearrenos el comentario
                if (current == '-' && Peek() == '-')
                {
                    Advance(); // Pasar por el segundo '-'

                    string comment = "--";
                    while (Peek() != '\n' && !IsAtEnd())
                    {
                        comment += Advance();
                    }
                    AddToken(tokens, TokenType.CommentLine, comment);
                    continue;
                }

                // Comentarios de bloque (-! ... !-)

                // Comprobar si es apertura de comentario
                if (current == '-' && Peek() == '!')
                {
                    Advance(); // Nos saltamos  el '!'
                    string comment = "-!";
                    // bandera para saber si se cerro el comentario de bloque
                    bool commentClosed = false;

                    while (!IsAtEnd())
                    {
                        // Si es cierre de comentario
                        if (current == '!' && Peek() == '-')
                        {
                            comment += "!-";
                            Advance(); // Consumir el '-'
                            commentClosed = true;
                            break;
                        }
                        comment += Advance();
                    }

                    if (!commentClosed)
                    {
                        throw new Exception($"Comentario de bloque no cerrado en línea {_currentLine}");
                    }
                    AddToken(tokens, TokenType.CommentBlock, comment);
                    continue;
                }

                // Identificar tokens de un solo carácter
                switch (current)
                {
                    case '(': AddToken(tokens, TokenType.LParen, "("); continue;
                    case ')': AddToken(tokens, TokenType.RParen, ")"); continue;
                    case '{': AddToken(tokens, TokenType.LBrace, "{"); continue;
                    case '}': AddToken(tokens, TokenType.RBrace, "}"); continue;
                    case ',': AddToken(tokens, TokenType.Comma, ","); continue;
                    case ';': AddToken(tokens, TokenType.Semicolon, ";"); continue;
                    case '=': AddToken(tokens, TokenType.Assign, "="); continue;
                    case '+': AddToken(tokens, TokenType.Plus, "+"); continue;
                    case '-': AddToken(tokens, TokenType.Minus, "-"); continue;
                    case '*': AddToken(tokens, TokenType.Multiply, "*"); continue;
                    case '/': AddToken(tokens, TokenType.Divide, "/"); continue;
                }

                // Identificar números
                if (char.IsDigit(current))
                {
                    string number = current.ToString();
                    while (char.IsDigit(Peek()))
                    {
                        number += Advance();
                    }
                    AddToken(tokens, TokenType.Number, number);
                    continue;
                }

                // Identificar strings (entre comillas dobles)
                if (current == '"')
                {
                    string str = "\"";
                    while (Peek() != '"' && !IsAtEnd())
                    {
                        str += Advance();
                    }

                    if (IsAtEnd())
                    {
                        throw new Exception($"String no cerrado en línea {_currentLine}");
                    }

                    str += Advance(); // Consumir la comilla de cierre
                    AddToken(tokens, TokenType.String, str);
                    continue;
                }

                // Identificar identificadores y palabras clave
                if (char.IsLetter(current))
                {
                    string identifier = current.ToString();
                    while (char.IsLetterOrDigit(Peek()) || Peek() == '_')
                    {
                        identifier += Advance();
                    }

                    // Verificar palabras clave y tipos
                    switch (identifier.ToLower())
                    {
                        case "var": AddToken(tokens, TokenType.Var, identifier); continue;
                        case "val": AddToken(tokens, TokenType.Val, identifier); continue;
                        case "int": AddToken(tokens, TokenType.TypeInt, identifier); continue;
                        case "str": AddToken(tokens, TokenType.TypeStr, identifier); continue;
                        case "bool": AddToken(tokens, TokenType.TypeBool, identifier); continue;
                        case "fun": AddToken(tokens, TokenType.Fun, identifier); continue;
                        case "for": AddToken(tokens, TokenType.For, identifier); continue;
                        case "while": AddToken(tokens, TokenType.While, identifier); continue;
                        case "print": AddToken(tokens, TokenType.Print, identifier); continue;
                        case "println": AddToken(tokens, TokenType.Println, identifier); continue;
                        case "true": AddToken(tokens, TokenType.True, identifier); continue;
                        case "false": AddToken(tokens, TokenType.False, identifier); continue;
                        case "and": AddToken(tokens, TokenType.And, identifier); continue;
                        case "or": AddToken(tokens, TokenType.Or, identifier); continue;
                        case "not": AddToken(tokens, TokenType.Not, identifier); continue;
                        default: AddToken(tokens, TokenType.Identifier, identifier); continue;
                    }
                }

                throw new Exception($"Carácter no reconocido '{current}' en línea {_currentLine}");
            }

            AddToken(tokens, TokenType.EOF, "");
            return tokens;
        }


        // Metodo para avanzar en los caracteres actualizando la posicion actual
        private char Advance()
        {
            _currentPosition++; // Aumentar nuestro contador de posicion
            _linePosition++; // Movemos posición en línea
            return _source[_currentPosition - 1];
        }

        // Verificar que valor hay d
        private char Peek()
        {
            if (IsAtEnd()) return '\0'; // Caracter nulo si es fin
            return _source[_currentPosition]; // Mira el siguiente  caracter sin avanzar
        }

        // Determinar si estamos en el final del codigo fuente
        // al comparar la posicion actual con la longitud del documento
        private bool IsAtEnd()
        {
            return _currentPosition >= _source.Length;
        }
        

        // Metodo para añadir Token a la lista de tokens
        private void AddToken(List<Token> tokens, TokenType type, string lexeme)
        {
            tokens.Add(new Token(type, lexeme, _currentLine, _linePosition - lexeme.Length));
        }
    }

}
