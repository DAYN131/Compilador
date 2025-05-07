grammar SiriusLanguage;



// Parser rules
program: (declaration | statement)* EOF;

declaration: 
    importDeclaration
    | variableDeclaration SEMICOLON  // Punto y coma obligatorio
    | functionDeclaration
    ;

importDeclaration: IMPORT IDENTIFIER SEMICOLON;

variableDeclaration: 
    (VAR | VAL) IDENTIFIER (COLON type)? ASSIGN expression;

functionDeclaration: 
    FUN IDENTIFIER LPAREN parameterList? RPAREN (COLON type)? block;

parameterList: parameter (COMMA parameter)*;
parameter: IDENTIFIER COLON type;  // Tipo obligatorio en parámetros

type: TYPE_INT | TYPE_STR | TYPE_BOOL;

statement: 
    printStatement SEMICOLON       // Punto y coma obligatorio
    | ifStatement
    | forStatement
    | whileStatement
    | returnStatement SEMICOLON    // Punto y coma obligatorio
    | block
    | expression SEMICOLON         // Punto y coma obligatorio
    ;

printStatement: (PRINT | PRINTLN) LPAREN expression RPAREN;

ifStatement: 
    IF LPAREN expression RPAREN block 
    (ELSE IF LPAREN expression RPAREN block)* 
    (ELSE block)?;

forStatement: 
    FOR LPAREN 
    (variableDeclaration | expression)? SEMICOLON  // Inicialización
    expression? SEMICOLON                          // Condición
    expression?                                    // Actualización
    RPAREN block;

whileStatement: WHILE LPAREN expression RPAREN block;

returnStatement: RETURN expression?;  // Expresión opcional

block: LBRACE (statement)* RBRACE;

expression: 
    assignment
    | logicOr
    ;

assignment: IDENTIFIER ASSIGN expression | logicOr;

logicOr: logicAnd (OR logicAnd)*;
logicAnd: equality (AND equality)*;
equality: comparison ((EQUAL | NOTEQUAL) comparison)*;
comparison: term ((LT | GT | LTEQ | GTEQ) term)*;
term: factor ((PLUS | MINUS) factor)*;
factor: unary ((MULTIPLY | DIVIDE) unary)*;
unary: (NOT | MINUS)? primary;

primary: 
    literal
    | IDENTIFIER
    | LPAREN expression RPAREN
    | functionCall
    ;

functionCall: IDENTIFIER LPAREN (expression (COMMA expression)*)? RPAREN;

literal: NUMBER | STRING | TRUE | FALSE;

// Lexer rules
TRUE: 'true';
FALSE: 'false';
AND: 'and';
OR: 'or';
NOT: 'not';

VAR: 'var';
VAL: 'val';
TYPE_INT: 'int';
TYPE_STR: 'str';
TYPE_BOOL: 'bool';
FUN: 'fun';
FOR: 'for';
WHILE: 'while';
IF: 'if';
ELSE: 'else';
PRINT: 'print';
PRINTLN: 'println';
IMPORT: 'import';
RETURN: 'return';

LPAREN: '(';
RPAREN: ')';
LBRACE: '{';
RBRACE: '}';
COMMA: ',';
SEMICOLON: ';';
COLON: ':';

PLUS: '+';
MINUS: '-';
MULTIPLY: '*';
DIVIDE: '/';
ASSIGN: '=';

EQUAL: '==';
NOTEQUAL: '!=';
LT: '<';
GT: '>';
LTEQ: '<=';
GTEQ: '>=';



IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;

NUMBER: [0-9]+;

STRING: '"' (~["\\\r\n] | '\\' .)* '"';

COMMENT_LINE: '--' ~[\r\n]* -> skip;
COMMENT_BLOCK: '-!' .*? '!-' -> skip;

WS: [ \t\r\n]+ -> skip;
