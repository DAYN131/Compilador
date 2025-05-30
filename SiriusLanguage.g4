grammar SiriusLanguage;

// Parser rules
program: (declaration | statement)* EOF;

declaration: 
    importDeclaration
    | variableDeclaration SEMICOLON
    | functionDeclaration;

importDeclaration: IMPORT IDENTIFIER SEMICOLON;

variableDeclaration: 
    (VAR | VAL) IDENTIFIER (COLON type)? ASSIGN expression;

functionDeclaration: 
    FUN IDENTIFIER LPAREN parameterList? RPAREN (COLON type)? block;

parameterList: parameter (COMMA parameter)*;
parameter: IDENTIFIER COLON type;

type: TYPE_INT | TYPE_STR | TYPE_BOOL;

statement: 
    printStatement SEMICOLON
    | ifStatement
    | forStatement
    | whileStatement
    | returnStatement SEMICOLON
    | variableDeclaration SEMICOLON
    | block
    | expression SEMICOLON;

printStatement: (PRINT | PRINTLN) LPAREN expression RPAREN;

ifStatement
    : 'if' '(' expression ')' block ('else' 'if' '(' expression ')' block)* ('else' block)?;

forStatement: 
    FOR LPAREN 
    (variableDeclaration | expression)? SEMICOLON
    expression? SEMICOLON
    expression?
    RPAREN block;

whileStatement: WHILE LPAREN expression RPAREN block;

returnStatement: RETURN expression?;

block: LBRACE statement* RBRACE;

// Jerarquía de expresiones corregida
expression
    : logicOr
    | assignment
    ;

logicOr: logicAnd (OR logicAnd)*;
logicAnd: equality (AND equality)*;
equality: comparison ((EQUAL | NOTEQUAL) comparison)?;  // Operador opcional
comparison: additive ((LT | GT | LTEQ | GTEQ) additive)?;
additive: multiplicative ((PLUS | MINUS) multiplicative)*;
multiplicative: unary ((MULTIPLY | DIVIDE) unary)*;
unary: (NOT | MINUS)? primary;

primary: 
    literal
    | IDENTIFIER
    | LPAREN expression RPAREN
    | functionCall;

assignment: IDENTIFIER ASSIGN expression;

functionCall: IDENTIFIER LPAREN (expression (COMMA expression)*)? RPAREN;

// Literales mejor estructurados
literal: 
    TRUE       #booleanLiteral
    | FALSE    #booleanLiteral
    | NUMBER   #numberLiteral
    | STRING   #stringLiteral;

// Lexer rules (sin cambios)
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