using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Compilador
{
    public class Parser
    {
        private readonly List<Token> tokens;
        private int current = 0;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        public void Parse()
        {
            while (!IsAtEnd())
            {
                ParseDeclaration();
            }
        }

        private void ParseDeclaration()
        {
            if (Match(TokenType.Import))
            {
                ParseImport();
            }
            else if (Match(TokenType.Fun))
            {
                ParseFunction();
            }
            else if (Match(TokenType.Var, TokenType.Val))
            {
                ParseVariable();
            }
            else if (Match(TokenType.Print, TokenType.Println))
            {
                ParsePrint();
            }
            else if (Match(TokenType.For))
            {
                ParseFor();
            }
            else if (Match(TokenType.While))
            {
                ParseWhile();
            }
            else
            {
                throw Error(Peek(), "Instrucción no reconocida.");
            }
        }


        private void ParseImport()
        {
            Token name = Consume(TokenType.Identifier, "Se esperaba un nombre después de 'import'.");
            Consume(TokenType.Semicolon, "Se esperaba ';' después de import.");
        }

        private void ParseVariable()
        {
            Token tipo = Previous();
            Token name = Consume(TokenType.Identifier, "Se esperaba el nombre de la variable.");
            Token dataType = ConsumeAny(new[] { TokenType.TypeInt, TokenType.TypeStr, TokenType.TypeBool }, "Se esperaba tipo de dato.");
            Consume(TokenType.Semicolon, "Se esperaba ';' al final de la declaración.");
        }

        private void ParsePrint()
        {
            Token tipo = Previous(); // print o println
            Consume(TokenType.LParen, "Se esperaba '(' después de print.");
       
            if (!Check(TokenType.RParen))
            {
               if (Check(TokenType.Identifier) || Check(TokenType.Number) || Check(TokenType.String))
                {
                    Advance(); // consume el valor a imprimir
                }
                else
                {
                    throw new Exception($"Se esperaba una cadena, número o identificador después de 'print(', pero se encontró '{Peek().Lexeme}' en línea {Peek().Line}.");
                }
            }
            Consume(TokenType.RParen, "Se esperaba ')' después de print.");
            Consume(TokenType.Semicolon, "Se esperaba ';' al final del print.");
        }

        private void ParseFunction()
        {
            Consume(TokenType.LParen, "Se esperaba '(' después de 'fun'.");
            Token returnType = ConsumeAny(new[] { TokenType.TypeInt, TokenType.TypeStr, TokenType.TypeBool }, "Se esperaba tipo de retorno.");
            Consume(TokenType.RParen, "Se esperaba ')' después del tipo.");

            Token name = Consume(TokenType.Identifier, "Se esperaba nombre de función.");

            Consume(TokenType.LParen, "Se esperaba '(' para los parámetros.");
            // Aquí podrías implementar parámetros si deseas
            Consume(TokenType.RParen, "Se esperaba ')' al cerrar los parámetros.");

            Consume(TokenType.LBrace, "Se esperaba '{' para el cuerpo de la función.");
            // Puedes hacer un ciclo mientras no encuentres }
            Consume(TokenType.Return, "Se esperaba 'return' dentro de la función.");
            // Una expresión o identificador válido
            Consume(TokenType.Identifier, "Se esperaba valor a retornar.");
            Consume(TokenType.Semicolon, "Se esperaba ';' después del return.");
            Consume(TokenType.RBrace, "Se esperaba '}' para cerrar la función.");
        }

        private void ParseFor()
        {
            Consume(TokenType.LParen, "Se esperaba '(' en for.");
            // Aquí podrías permitir expresión condicional
            ParseExpression();
            Consume(TokenType.RParen, "Se esperaba ')' en for.");
            Consume(TokenType.LBrace, "Se esperaba '{' en for.");
            // Puedes permitir declaraciones aquí
            Consume(TokenType.RBrace, "Se esperaba '}' al final de for.");
        }

        private void ParseWhile()
        {
            Consume(TokenType.LParen, "Se esperaba '(' en while.");
            ParseExpression();
            Consume(TokenType.RParen, "Se esperaba ')' en while.");
            Consume(TokenType.LBrace, "Se esperaba '{' en while.");
            Consume(TokenType.RBrace, "Se esperaba '}' al final de while.");
        }

        private void ParseExpression()
        {
            ParsePrimary();
        }

        private void ParsePrimary()
        {
            if (Match(TokenType.Number, TokenType.String, TokenType.Identifier, TokenType.True, TokenType.False))
            {
                return;
            }

            if (Match(TokenType.LParen))
            {
                ParseExpression();
                Consume(TokenType.RParen, "Se esperaba ')' después de la expresión.");
                return;
            }

            throw Error(Peek(), "Expresión inválida.");
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw Error(Peek(), message);
        }

        private Token ConsumeAny(TokenType[] types, string message)
        {
            foreach (var type in types)
            {
                if (Check(type)) return Advance();
            }
            throw Error(Peek(), message);
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd()) return false;
            return Peek().Type == type;
        }

        private Token Advance()
        {
            if (!IsAtEnd()) current++;
            return Previous();
        }

        private bool IsAtEnd()
        {
            return Peek().Type == TokenType.EOF;
        }

        private Token Peek()
        {
            return tokens[current];
        }

        private Token Previous()
        {
            return tokens[current - 1];
        }

        private Exception Error(Token token, string message)
        {
            return new Exception($"[Línea {token.Line}] Error en '{token.Lexeme}': {message}");
        }

    }
}

