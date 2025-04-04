using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compilador
{



    // Diccionario de  todos los posibles tokens
    public enum TokenType
    {
        // Operadores Logicos
        True, False, And, Or, Not,

        // Nuevas palabras clave
        Var, Val, Int, Str, Bool, Fun, For, While, Print, Println,

        // Tipos de declaraciones
        TypeInt, TypeStr, TypeBool,

        // Símbolos
        // ()
        // {}
        // ,
        // ;
        LParen, RParen, LBrace, RBrace, Comma, Semicolon,

        // Operadores
        Plus, Minus, Multiply, Divide, Assign,

        // Literales e identificadores
        // Identifier : Nombres de variables/funciones
        // Number: Valores numericos
        // String: Texto entre comillas ("")
        Identifier, Number, String,

        // Comentarios
        //CommentLine: Comentarios de una línea (-- comentario)
        //CommentBlock: Comentarios multilínea (-! comentario !-)
        CommentLine, CommentBlock,

        Import,
        // EOF (End Of File): Marca el final del archivo
        Return,
        EOF
    }
}
