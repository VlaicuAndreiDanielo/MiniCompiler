using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Proiect_2__Compilator
{
    public class CodeErrorCatcher
    {
        private readonly List<string> lexicalErrors = new List<string>();
        private readonly List<string> syntaxErrors = new List<string>();
        private readonly List<string> semanticErrors = new List<string>();

        public void ProcessLexicalErrors(RulesLexer lexer)
        {
            lexer.Reset(); 
            var token = lexer.NextToken();

            while (token.Type != Antlr4.Runtime.TokenConstants.EOF)
            {
                if (token.Type == RulesLexer.ERROR_CHAR)
                {
                    string error = $"Eroare lexicala: Caracter necunoscut '{token.Text}' pe linia {token.Line}, coloana {token.Column}.";
                    lexicalErrors.Add(error);
                }
                token = lexer.NextToken();
            }
        }

        public void DisplayLexicalErrors()
        {
            if (lexicalErrors.Count == 0)
            {
                Console.WriteLine("\nNu au fost gasite erori lexicale.");
                return;
            }

            Console.WriteLine("\nErori lexicale gasite:");
            foreach (var error in lexicalErrors)
            {
                Console.WriteLine(error);
            }
        }

        public bool HasLexicalErrors()
        {
            return lexicalErrors.Count > 0;
        }
        

        public void ProcessSemanticErrors(Dictionary<string, (string Type, string Value)> variables)
        {
            foreach (var variable in variables)
            {
                string varName = variable.Key;
                string varType = variable.Value.Type;
                string varValue = variable.Value.Value;

              
                if (!IsValueValidForType(varType, varValue))
                {
                    string error = $"Eroare semantică: Variabila '{varName}' are valoarea '{varValue}', care nu corespunde tipului '{varType}'.";
                    semanticErrors.Add(error);
                }
                if (string.IsNullOrEmpty(varValue))
                {
                    string error = $"Eroare semantică: Variabila '{variable.Key}' este utilizata dar nu are o valoare atribuita.";
                    semanticErrors.Add(error);
                }
            }
        }
        private bool IsValueValidForType(string type, string value)
        {
            switch (type)
            {
                case "int":
                    return int.TryParse(value, out _);
                case "float":
                    return float.TryParse(value, out _);
                case "double":
                    return double.TryParse(value, out _);
                case "string":
                    return value.StartsWith("\"") && value.EndsWith("\""); 
                case "bool":
                    return value == "true" || value == "false" || value == "1" || value == "0";
                default:
                    return false; 
            }
        }

        public void DisplaySemanticErrors()
        {
            if (semanticErrors.Count == 0)
            {
                Console.WriteLine("\nNu au fost găsite erori semantice.");
                return;
            }
            Console.WriteLine("\nErori semantice găsite:");
            foreach (var error in semanticErrors)
            {
                Console.WriteLine(error);
            }
        }

        public Dictionary<string, (string Type, string Value)> ExtractVariables(RulesParser.ProgramContext programContext)
        {
            var variables = new Dictionary<string, (string Type, string Value)>();

            foreach (var child in programContext.children)
            {
                if (child is RulesParser.DeclarationContext declContext)
                {
                    string type = declContext.type().GetText();
                    string name = declContext.IDENTIFIER().GetText();
                    string value = declContext.expression() != null
                        ? declContext.expression().GetText()
                        : GetDefaultValueForType(type).ToString();

                    variables[name] = (type, value);
                }
                else if (child is RulesParser.GlobalVariableContext globalVarContext)
                {
                    string type = globalVarContext.type().GetText();
                    string name = globalVarContext.IDENTIFIER().GetText();
                    string value = globalVarContext.expression() != null
                        ? globalVarContext.expression().GetText()
                        : GetDefaultValueForType(type).ToString();

                    variables[name] = (type, value);
                }
                else if (child is RulesParser.FunctionContext funcContext)
                {
                    foreach (var statement in funcContext.statement())
                    {
                        ExtractVariablesFromStatement(statement, variables);
                    }
                }
                else if (child is RulesParser.StructDefinitionContext structContext)
                {
                    foreach (var member in structContext.structMember())
                    {
                        if (member.GetChild(0) is RulesParser.DeclarationContext structDeclContext)
                        {
                            string type = structDeclContext.type().GetText();
                            string name = structDeclContext.IDENTIFIER().GetText();
                            string value = structDeclContext.expression() != null
                                ? structDeclContext.expression().GetText()
                                : GetDefaultValueForType(type).ToString();

                            variables[name] = (type, value);
                        }
                    }
                }
            }

            return variables;
        }

        private void ExtractVariablesFromStatement(RulesParser.StatementContext statement, Dictionary<string, (string Type, string Value)> variables)
        {
            if (statement.GetChild(0) is RulesParser.DeclarationContext declContext)
            {
                string type = declContext.type().GetText();
                string name = declContext.IDENTIFIER().GetText();
                string value = declContext.expression() != null
                    ? declContext.expression().GetText()
                    : GetDefaultValueForType(type).ToString();

                variables[name] = (type, value);
            }
            else if (statement.GetChild(0) is RulesParser.AssignmentContext assignContext)
            {
                string name = assignContext.IDENTIFIER().GetText();
                if (variables.ContainsKey(name))
                {
                    string value = assignContext.expression().GetText();
                    string type = variables[name].Type;
                    variables[name] = (type, value);
                }
            }
        }

        private object GetDefaultValueForType(string type)
        {
            if (type == "int")
            {
                return 0;
            }
            else if (type == "float")
            {
                return 0.0f;
            }
            else if (type == "double")
            {
                return 0.0;
            }
            else if (type == "string")
            {
                return "\"\""; 
            }
            else if (type == "bool")
            {
                return "false"; 
            }
            else
            {
                throw new Exception($"Tip necunoscut: {type}");
            }
        }


        public void ProcessSyntaxErrors(RulesParser.ProgramContext programContext)
        {
            foreach (var child in programContext.children)
            {
                CheckNodeForErrors(child);
            }
        }

        private void CheckNodeForErrors(IParseTree node)
        {
            if (node is RulesParser.DeclarationContext declContext)
            {
                if (declContext.type() == null || declContext.IDENTIFIER() == null || declContext.PCTVIR() == null)
                {
                    syntaxErrors.Add($"Eroare sintactica: Declaratie incompleta pe linia {GetLine(declContext)}.");
                }
            }
            else if (node is RulesParser.GlobalVariableContext globalVarContext)
            {
                if (globalVarContext.type() == null || globalVarContext.IDENTIFIER() == null || globalVarContext.expression() == null || globalVarContext.PCTVIR() == null)
                {
                    syntaxErrors.Add($"Eroare sintactica: Variabila globala incompleta pe linia {GetLine(globalVarContext)}.");
                }
            }
            else if (node is RulesParser.AssignmentContext assignContext)
            {
                if (assignContext.IDENTIFIER() == null || assignContext.ASSIGN_OP() == null || assignContext.expression() == null || assignContext.PCTVIR() == null)
                {
                    syntaxErrors.Add($"Eroare sintactica: Atribuire incompleta pe linia {GetLine(assignContext)}.");
                }
            }
            else if (node is RulesParser.WhileStatementContext whileContext)
            {
                if (whileContext.expression() == null || whileContext.ADESC() == null || whileContext.AINCH() == null)
                {
                    syntaxErrors.Add($"Eroare sintactica: Declaratie 'while' incompleta pe linia {GetLine(whileContext)}.");
                }
            }
            else if (node is RulesParser.ForStatementContext forContext)
            {
                if (forContext.assignment(0) == null || forContext.expression() == null || (forContext.assignment(1) == null && forContext.increment() == null && forContext.decrement() == null) || forContext.ADESC() == null || forContext.AINCH() == null)
                {
                    syntaxErrors.Add($"Eroare sintactica: Declaratie 'for' incompleta pe linia {GetLine(forContext)}.");
                }
            }
            else if (node is RulesParser.FunctionContext funcContext)
            {
                if (funcContext.type() == null || funcContext.IDENTIFIER() == null || funcContext.PDESC() == null 
                    || funcContext.PINCH() == null || (funcContext.ADESC() == null && funcContext.PCTVIR() == null))
                {
                    syntaxErrors.Add($"Eroare sintactica: Functie incompleta pe linia {GetLine(funcContext)}.");
                }
            }
            else if (node is RulesParser.StructDefinitionContext structContext)
            {

                if (structContext.IDENTIFIER() == null || structContext.ADESC() == null || structContext.AINCH() == null)
                {
                    syntaxErrors.Add($"Eroare sintactica: Structura incompleta pe linia {GetLine(structContext)}.");
                }
                if (structContext.structMember() == null || structContext.structMember().Length == 0)
                {
                    syntaxErrors.Add($"Eroare sintactica: Structura '{structContext.IDENTIFIER()?.GetText()}' fara membri pe linia {GetLine(structContext)}.");
                }
            }

            else if (node is RulesParser.IfStatementContext ifContext)
            {
                if (ifContext.expression() == null || ifContext.ADESC() == null || ifContext.AINCH() == null)
                {
                    syntaxErrors.Add($"Eroare sintactica: Declaratie 'if' incompleta pe linia {GetLine(ifContext)}.");
                }
            }
            else if (node is RulesParser.ReturnStatementContext returnContext)
            {
                if (returnContext.PCTVIR() == null)
                {
                    syntaxErrors.Add($"Eroare sintactica: Declaratie 'return' fara punct-virgula pe linia {GetLine(returnContext)}.");
                }
            }
            else if (node is RulesParser.ExpressionContext exprContext)
            {

                if (exprContext.ChildCount == 0)
                {
                    syntaxErrors.Add($"Eroare sintactica: Expresie incompleta pe linia {GetLine(exprContext)}.");
                }
            }
            else if (node is TerminalNodeImpl terminalNode)
            {
                return;
            }
            
            for (int i = 0; i < node.ChildCount; i++)
            {
                CheckNodeForErrors(node.GetChild(i));
            }
        }

        private int GetLine(ParserRuleContext context)
        {
            return context.Start.Line; 
        }

        public void DisplaySyntaxErrors()
        {
            if (syntaxErrors.Count == 0)
            {
                Console.WriteLine("\nNu au fost gasite erori sintactice.");
                return;
            }

            Console.WriteLine("\nErori sintactice gasite:");
            foreach (var error in syntaxErrors)
            {
                Console.WriteLine(error);
            }
        }
    }
}
