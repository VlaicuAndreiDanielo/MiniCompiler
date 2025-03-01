grammar Rules;

// Tipuri de date
INT: 'int';
FLOAT: 'float';
DOUBLE: 'double';
STRING_TYPE: 'string';
VOID: 'void';
BOOL: 'bool';

// Alte cuvinte cheie
RETURN: 'return';
IF: 'if';
ELSE: 'else';
FOR: 'for';
WHILE: 'while';
STRUCT: 'struct';

// Lexer Rules (Token definitions)
NUMBER: ('+' | '-')? [0-9]+ ('.' [0-9]+)?;
BOOL_LITERAL: 'true' | 'false' | '1' | '0';
IDENTIFIER: [a-zA-Z_][a-zA-Z0-9_]*;
STRING: '"' .*? '"';

// Operatorii și delimitatorii
ARITH_OP: '+' | '-' | '*' | '/' | '%';
REL_OP: '<' | '>' | '<=' | '>=' | '==' | '!=';
LOGIC_OP: '&&' | '||';
LOGIC_OP_NEG: '!';
ASSIGN_OP: '=';
ASSIGN_OP_COMPOUND: '+=' | '-=' | '*=' | '/=' | '%=';
INCREMENT_OP: '++';
DECREMENT_OP: '--';
PDESC: '(';
PINCH: ')';
ADESC: '{';
AINCH: '}';
PCTVIR: ';';
VIR: ',';
//DELIMITERS: PDESC | PINCH | ADESC | AINCH | ',' | ';';

// Ignorare spații albe și comentarii
WS: [ \t\r\n]+ -> skip;
COMMENT: '//' ~[\r\n]* -> skip;
BLOCK_COMMENT: '/*' .*? '*/' -> skip;

ERROR_CHAR: .; // Prinde un singur caracter invalid
//ERROR_CHAR:.+?; // Orice listade caractere care nu corespund altor reguli (ia mai multe caractere consecitive si le considera o singura eroare)

// Parser Rules (Grammar for parsing)
declaration: type IDENTIFIER (ASSIGN_OP expression)? PCTVIR;
globalVariable: type IDENTIFIER ASSIGN_OP expression PCTVIR;
program: (
		globalVariable
		| declaration
		| function
		| structDefinition
	)*;
increment: (
		(IDENTIFIER INCREMENT_OP)
		| (INCREMENT_OP IDENTIFIER)
	) PCTVIR;
decrement: (
		(IDENTIFIER DECREMENT_OP)
		| (DECREMENT_OP IDENTIFIER)
	) PCTVIR;
//bool_assignment: IDENTIFIER ASSIGN_OP BOOL_LITERAL PCTVIR;
assignment:
	IDENTIFIER ASSIGN_OP (expression | functionCall) PCTVIR;

// Regula pentru funcții
function:
	type IDENTIFIER PDESC parameters PINCH (
		(ADESC statement* AINCH)
		| PCTVIR
	);

parameters: (type IDENTIFIER (VIR type IDENTIFIER)*)?;

// Regula pentru apeluri de funcții
functionCall: IDENTIFIER PDESC arguments? PINCH PCTVIR?;

arguments: expression (VIR expression)*;

// Reguli pentru structuri
structDefinition:
	STRUCT IDENTIFIER ADESC structMember* AINCH PCTVIR?;
constructor:
	IDENTIFIER (PDESC parameters PINCH) (
		(ADESC statement* AINCH)?
		| PCTVIR
	); // Permite implementare sau punct-virgulă

destructor:
	'~' IDENTIFIER PDESC PINCH (
		(ADESC statement* AINCH)?
		| PCTVIR
	); // Implementare opțională
structMember:
	declaration // Declarații de câmpuri
	| function // Funcții membre
	| constructor // Constructor
	| destructor; // Destructor

// Reguli pentru declarațiile de control
statement:
	declaration
	| assignment
	| increment
	| decrement
	| functionCall
	| ifStatement
	| forStatement
	| whileStatement
	| returnStatement
	| compoundAssignment;

compoundAssignment:
	IDENTIFIER ASSIGN_OP_COMPOUND expression PCTVIR;
ifStatement:
	IF /*PDESC expression PINCH*/ expression ADESC statement* AINCH (
		ELSE ADESC statement* AINCH
	)?;

forStatement:
	FOR PDESC assignment expression PCTVIR (
		assignment
		| increment
		| decrement
	) PINCH ADESC statement* AINCH;

whileStatement:
	WHILE /*PDESC expression PINCH*/ expression ADESC statement* AINCH;

returnStatement: RETURN expression? PCTVIR;

// Expresii
expression:
	//IDENTIFIER ASSIGN_OP_COMPOUND expression	# CompoundAssignmentExpression
	expression ARITH_OP expression		# ArithmeticExpression
	| expression REL_OP expression		# RelationalExpression
	| expression LOGIC_OP expression	# LogicalExpression
	/*IDENTIFIER INCREMENT_OP PCTVIR # PostIncrementExpression */
	/*IDENTIFIER DECREMENT_OP PCTVIR # PostDecrementExpression */
	| LOGIC_OP_NEG expression			# LogicalNegationExpression
	| (BOOL_LITERAL /*| '1' | '0'*/)	# BoolAssignmentExpression
	| IDENTIFIER						# IdentifierExpression
	| NUMBER							# NumberExpression
	| STRING							# StringExpression
	| PDESC expression PINCH			# ParenthesisExpression; //| functionCall						# FunctionCallExpression

// Tipuri de date
type: INT | FLOAT | DOUBLE | STRING_TYPE | VOID | BOOL;