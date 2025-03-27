using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilador
{
    public class Tokenizer
    {
        private readonly string _source;
        private int _currentPosition;
        private int _currentLine;
        private int _linePosition;

        public Tokenizer(string source)
        {
            _source = source;
            _currentPosition = 0;
            _currentLine = 1;
            _linePosition = 1;
        }

        public List<Token> Tokenize()
        {
            var tokens = new List<Token>();

            while (!IsAtEnd())
            {
                char current = Advance();

                // Ignorar espacios en blanco
                if (char.IsWhiteSpace(current))
                {
                    if (current == '\n')
                    {
                        _currentLine++;
                        _linePosition = 1;
                    }
                    continue;
                }

                // Identificar tokens de un solo carácter
                switch (current)
                {
                    case '(':
                        AddToken(tokens, TokenType.LParen, "(");
                        continue;
                    case ')':
                        AddToken(tokens, TokenType.RParen, ")");
                        continue;
                    case '+':
                        AddToken(tokens, TokenType.Plus, "+");
                        continue;
                    case '-':
                        AddToken(tokens, TokenType.Minus, "-");
                        continue;
                    case '*':
                        AddToken(tokens, TokenType.Multiply, "*");
                        continue;
                    case '/':
                        AddToken(tokens, TokenType.Divide, "/");
                        continue;
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

                // Identificar identificadores y palabras clave
                if (char.IsLetter(current))
                {
                    string identifier = current.ToString();
                    while (char.IsLetterOrDigit(Peek()))
                    {
                        identifier += Advance();
                    }

                    // Verificar si es palabra clave
                    switch (identifier.ToLower())
                    {
                        case "true":
                            AddToken(tokens, TokenType.True, identifier);
                            continue;
                        case "false":
                            AddToken(tokens, TokenType.False, identifier);
                            continue;
                        case "and":
                            AddToken(tokens, TokenType.And, identifier);
                            continue;
                        case "or":
                            AddToken(tokens, TokenType.Or, identifier);
                            continue;
                        case "not":
                            AddToken(tokens, TokenType.Not, identifier);
                            continue;
                        default:
                            AddToken(tokens, TokenType.Identifier, identifier);
                            continue;
                    }
                }

                // Si llegamos aquí, es un carácter no reconocido
                throw new Exception($"Error léxico en línea {_currentLine}, posición {_linePosition}: Carácter no reconocido '{current}'");
            }

            // Añadir token de fin de archivo
            AddToken(tokens, TokenType.EOF, "");
            return tokens;
        }

        private char Advance()
        {
            _currentPosition++;
            _linePosition++;
            return _source[_currentPosition - 1];
        }

        private char Peek()
        {
            if (IsAtEnd()) return '\0';
            return _source[_currentPosition];
        }

        private bool IsAtEnd()
        {
            return _currentPosition >= _source.Length;
        }

        private void AddToken(List<Token> tokens, TokenType type, string lexeme)
        {
            tokens.Add(new Token(type, lexeme, _currentLine, _linePosition - lexeme.Length));
        }
    }

}
