using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using static RulesParser;

namespace Proiect_2__Compilator
{
    public class CodeExecutor : RulesBaseVisitor<object>
    {
        private readonly Dictionary<string, string> variables = new Dictionary<string, string>();
        private readonly Dictionary<string, string> globalVariables = new Dictionary<string, string>();
        private object returnValue = null;
        public override object VisitDeclaration(RulesParser.DeclarationContext context)
        {
            string varName = context.IDENTIFIER().GetText();
            string varType = context.type().GetText(); 

            object value;
            if (context.expression() != null)
            {
                value = VisitExpression(context.expression());
                if (value == null)
                {
                    Console.WriteLine($"Debug: Expresia este NULL. Setăm valoarea implicită pentru tipul '{varType}'.");
                    value = GetDefaultValueForType(varType);
                }
            }
            else
            {
                Console.WriteLine($"Debug: Nu există expresie. Setăm valoarea implicită pentru tipul '{varType}'.");
                value = GetDefaultValueForType(varType);
            }

            variables[varName] = value.ToString();

            Console.WriteLine($"Debug: Variabilă declarată – {varName} ({varType}) = {value ?? "NULL"}");
            return null;
        }


        public override object VisitGlobalVariable(RulesParser.GlobalVariableContext context)
        {
            string varName = context.IDENTIFIER().GetText();
            string varType = context.type().GetText();
            object value = context.expression() != null ? VisitExpression(context.expression()) : GetDefaultValueForType(varType);
            if (globalVariables.ContainsKey(varName))
            {
                Console.WriteLine($"Eroare: Variabila globala '{varName}' este deja definita.");
                return null; 
            }
            globalVariables[varName] = $"{varType} {varName}={value}";
            variables[varName] = value.ToString();
            Console.WriteLine($"Debug: Variabila globala adaugata – {varType} {varName}={value}");

            return null;
        }
        public override object VisitAssignment(RulesParser.AssignmentContext context)
        {

            string varName = context.IDENTIFIER().GetText();

            var expression = context.expression();
            if (expression.GetChild(0) is RulesParser.FunctionCallContext functionCallCtx)
            {
                var functionResult = VisitFunctionCall(functionCallCtx);

                if (variables.ContainsKey(varName))
                {
                    variables[varName] = functionResult.ToString();
                    Console.WriteLine($"Debug: Variabila '{varName}' actualizată cu rezultatul apelului funcției: {functionResult}");
                }
                else
                {
                    variables.Add(varName, functionResult.ToString());
                    Console.WriteLine($"Debug: Variabilă nouă '{varName}' creată cu rezultatul apelului funcției: {functionResult}");
                }
            }
            else
            {

                object value = VisitExpression(expression);

                if (variables.ContainsKey(varName))
                {
                    variables[varName] = value.ToString();
                    Console.WriteLine($"Debug: Atribuire procesată – {varName} = {value}");
                }
                else
                {
                    variables.Add(varName, value.ToString());
                    Console.WriteLine($"Debug: Variabilă nouă creată – {varName} = {value}");
                }
            }
            return null;
        }
        public override object VisitCompoundAssignment(RulesParser.CompoundAssignmentContext compoundAssignCtx)
        {
            string varName = compoundAssignCtx.IDENTIFIER().GetText();
            string op = compoundAssignCtx.ASSIGN_OP_COMPOUND().GetText();

            var rightValue = VisitExpression(compoundAssignCtx.expression());

            if (!variables.ContainsKey(varName))
            {
                throw new Exception($"Eroare: Variabila '{varName}' nu este definita.");
            }

            if (!double.TryParse(variables[varName], out double leftValue))
            {
                throw new Exception($"Eroare: Variabila '{varName}' nu este numerica.");
            }

            // Verificăm dacă rightValue este o variabilă
            double rightNumericValue;
            if (rightValue is string rightVarName && variables.ContainsKey(rightVarName))
            {
                if (!double.TryParse(variables[rightVarName], out rightNumericValue))
                {
                    throw new Exception($"Eroare: Variabila '{rightVarName}' nu este numerica.");
                }
            }
            else if (!double.TryParse(rightValue.ToString(), out rightNumericValue))
            {
                throw new Exception($"Eroare: Expresia '{rightValue}' nu este numerica.");
            }

            double result;
            if (op == "+=") result = leftValue + rightNumericValue;
            else if (op == "-=") result = leftValue - rightNumericValue;
            else if (op == "*=") result = leftValue * rightNumericValue;
            else if (op == "/=") result = rightNumericValue != 0 ? leftValue / rightNumericValue : throw new DivideByZeroException();
            else if (op == "%=") result = rightNumericValue != 0 ? leftValue % rightNumericValue : throw new DivideByZeroException();
            else throw new Exception($"Operator necunoscut: {op}");

            variables[varName] = result.ToString();

            Console.WriteLine($"Debug: Variabila '{varName}' actualizata cu valoarea {result} (operator: {op})");
            return result;
        }
        public override object VisitIncrement(RulesParser.IncrementContext incrementCtx)
        {
            string varName = null;

            if (incrementCtx.GetChild(0).GetText() == "++")
            {
                varName = incrementCtx.GetChild(1).GetText();
            }
            else
            {
                varName = incrementCtx.GetChild(0).GetText();
            }

            if (varName == null || !variables.ContainsKey(varName))
            {
                throw new Exception($"Eroare: Variabila '{varName}' nu este definita.");
            }

            if (double.TryParse(variables[varName], out double value))
            {
                double newValue;

                if (incrementCtx.GetChild(0).GetText() == "++")
                {
                    // Pre-increment (++a)
                    newValue = value + 1;
                    variables[varName] = newValue.ToString();
                }
                else
                {
                    // Post-increment (a++)
                    newValue = value;
                    variables[varName] = (value + 1).ToString();
                }

                Console.WriteLine($"Debug: Variabila '{varName}' incrementata la {variables[varName]}");
                return newValue;
            }
            else
            {
                throw new Exception($"Eroare: Variabila '{varName}' nu este numerica si nu poate fi incrementata.");
            }
        }
        public override object VisitDecrement(RulesParser.DecrementContext decrementCtx)
        {
            string varName = null;

            if (decrementCtx.GetChild(0).GetText() == "--")
            {
                varName = decrementCtx.GetChild(1).GetText();
            }
            else
            {
                varName = decrementCtx.GetChild(0).GetText();
            }

            if (varName == null || !variables.ContainsKey(varName))
            {
                throw new Exception($"Eroare: Variabila '{varName}' nu este definita.");
            }

            if (double.TryParse(variables[varName], out double value))
            {
                double newValue;

                if (decrementCtx.GetChild(0).GetText() == "--")
                {
                    newValue = value - 1;
                    variables[varName] = newValue.ToString();
                }
                else
                {
                    newValue = value;
                    variables[varName] = (value - 1).ToString();
                }

                Console.WriteLine($"Debug: Variabila '{varName}' decrementata la {variables[varName]}");
                return newValue;
            }
            else
            {
                throw new Exception($"Eroare: Variabila '{varName}' nu este numerica si nu poate fi decrementata.");
            }
        }

        //Functia VisitFunctionCall(RulesParser.FunctionCallContext context) trebuie comentata ca sa functioneze codul
        //deoarece MiniCopiler-ul nu suporta apelul functiilor sau utilizarea acestora in atribuire
        /*public override object VisitFunctionCall(RulesParser.FunctionCallContext context)
        {
            string funcName = context.IDENTIFIER().GetText();

            var calledFunction = functionContexts.FirstOrDefault(func => func.IDENTIFIER().GetText() == funcName);
            if (calledFunction == null)
            {
                throw new Exception($"Eroare: Functia '{funcName}' nu este definita!");
            }

            var argumentValues = new List<object>();
            var argumentsNode = context.arguments();
            if (argumentsNode != null)
            {
                foreach (var expression in argumentsNode.expression())
                {
                    var value = VisitExpression(expression); 
                    argumentValues.Add(value);
                }
            }

            Console.WriteLine($"Debug: Apel functie: {funcName}({string.Join(", ", argumentValues)})");

            var parameterDefinitions = calledFunction.parameters()?.type()
                .Zip(calledFunction.parameters().IDENTIFIER(), (typeCtx, identifierCtx) => new { Type = typeCtx.GetText(), Name = identifierCtx.GetText() })
                .ToList();

            if (parameterDefinitions?.Count != argumentValues.Count)
            {
                throw new Exception($"Eroare: Functia '{funcName}' asteapta {parameterDefinitions?.Count ?? 0} parametri, dar au fost furnizati {argumentValues.Count}!");
            }

            var localVariables = new Dictionary<string, string>();
            for (int i = 0; i < parameterDefinitions.Count; i++)
            {
                string paramName = parameterDefinitions[i].Name;
                string paramType = parameterDefinitions[i].Type;
                object argumentValue = argumentValues[i];

                if (!IsValueValidForType(paramType, argumentValue?.ToString()))
                {
                    throw new Exception($"Eroare: Parametrul '{paramName}' din functia '{funcName}' asteapta tipul '{paramType}', dar a primit valoarea '{argumentValue}'.");
                }

                localVariables[paramName] = argumentValue.ToString();
                Console.WriteLine($"Debug: Parametru '{paramName}' setat cu valoarea {argumentValue}");
            }

            var previousVariables = new Dictionary<string, string>(variables);

            foreach (var kvp in localVariables)
            {
                variables[kvp.Key] = kvp.Value;
            }

            object functionReturnValue = null;
            foreach (var statement in calledFunction.statement())
            {
                functionReturnValue = Visit(statement);

                if (functionReturnValue != null)
                {
                    break;
                }
            }


            variables.Clear();
            foreach (var kvp in previousVariables)
            {
                variables[kvp.Key] = kvp.Value;
            }

            Console.WriteLine($"Debug: Functia '{funcName}' a returnat valoarea {functionReturnValue}");
            return functionReturnValue;
        }*/

        public override object VisitExpression(RulesParser.ExpressionContext context)
        {
            Console.WriteLine($"Debug: VisitExpression apelata pentru context: {context.GetText()}");

            if (context is RulesParser.NumberExpressionContext numberCtx)
            {
                double value = double.Parse(numberCtx.GetText());
                Console.WriteLine($"Debug: Expresie numerica evaluata la {value}");
                return value;
            }
            if (context is RulesParser.IdentifierExpressionContext idCtx)
            {
                string varName = idCtx.GetText();

                var functionContext = functionContexts.FirstOrDefault(func => func.IDENTIFIER().GetText() == varName);
                if (functionContext != null)
                {
                    Console.WriteLine($"Debug: Identificatorul '{varName}' este o functie.");

                    ParserRuleContext currentContext = context.Parent as ParserRuleContext;
                    while (currentContext != null && !(currentContext is RulesParser.FunctionCallContext))
                    {
                        currentContext = currentContext.Parent as ParserRuleContext;
                    }
                    if (currentContext is RulesParser.FunctionCallContext functionCallCtx)
                    {
                        Console.WriteLine($"Debug: Apelam functia '{varName}' cu context complet.");
                        return VisitFunctionCall(functionCallCtx);
                    }
                    else
                    {
                        throw new Exception($"Eroare: Identificatorul '{varName}' este o functie, dar contextul complet al apelului nu a fost gasit.");
                    }
                }


                if (variables.ContainsKey(varName))
                {
                    object value = variables[varName];
                    Console.WriteLine($"Debug: Variabila gasita: {varName} = {value}");
                    return varName;
                }
                else
                {
                    string errorMessage = $"Eroare: Variabila '{varName}' nu este definita!";
                    Console.WriteLine(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            if (context is RulesParser.StringExpressionContext stringCtx)
            {
                string value = stringCtx.GetText().Trim('"');
                Console.WriteLine($"Debug: Expresie string evaluata la \"{value}\"");
                return value;
            }
            if (context is RulesParser.ArithmeticExpressionContext arithCtx)
            {
                var AuxExpressionVisited = arithCtx.GetText();
                Console.WriteLine($"Expresia aritmetica vizitata este AuxExpressionVisited: {AuxExpressionVisited}");
                var leftValue = VisitExpression(arithCtx.expression(0));
                string leftValueFinal = null;
                string leftVarName = leftValue as string;
                if (leftVarName != null)
                {
                    if (variables.ContainsKey(leftVarName))
                    {
                        leftValueFinal = variables[leftVarName];
                        Console.WriteLine($"Debug: Valoarea operandului stang din dictonar: {leftVarName} = leftValueFinal: {leftValueFinal}");
                    }
                    else
                    {
                        Console.WriteLine($"Eroare: Variabila '{leftVarName}' nu este definita.");
                        throw new Exception($"Eroare: Variabila '{leftVarName}' nu este definita.");
                    }
                }

                if (leftValueFinal == null)
                {
                    Console.WriteLine($"Eroare: Operandul stang este NULL sau este Numar! Se utilizeaza valoarea implicita pana se obtine valoarea.");
                    leftValue = GetDefaultValueForType("double");
                   
                }

                var rightValue = VisitExpression(arithCtx.expression(1));
                string rightValueFinal = null;
                string rightVarName = rightValue as string; 
                if (rightVarName != null)
                {
                    if (variables.ContainsKey(rightVarName))
                    {
                        rightValueFinal = variables[rightVarName];
                        Console.WriteLine($"Debug: Valoarea operandului drept din dictionar: {rightVarName} = rightValueFinal {rightValueFinal}");
                    }
                    else
                    {
                        Console.WriteLine($"Eroare: Variabila '{rightVarName}' nu este definita.");
                        throw new Exception($"Eroare: Variabila '{rightVarName}' nu este definita.");
                    }
                }
                if (rightValueFinal == null)
                {
                    Console.WriteLine($"Eroare: Operandul drept este NULL sau este Numar! Se utilizeaza valoarea implicita pana se obtine valoarea.");
                    rightValue = GetDefaultValueForType("double"); 
                }


                string op = arithCtx.ARITH_OP()?.GetText();
                double result;

                var leftNr = VisitExpression(arithCtx.expression(0));
                var rightNr= VisitExpression(arithCtx.expression(1));

                double left2 = 0, right2 = 0, left3 = 0, right3 = 0;

                bool left2Valid = double.TryParse(leftNr?.ToString() ?? "0", out left2);
                bool right2Valid = double.TryParse(rightNr?.ToString() ?? "0", out right2);
                bool left3Valid = double.TryParse(leftValueFinal?.ToString() ?? "0", out left3);
                bool right3Valid = double.TryParse(rightValueFinal?.ToString() ?? "0", out right3);
                //Daca unul este numar si unul variabila, tratam aritmetica
                if ((left2Valid && right2Valid) || (left3Valid && right3Valid))
                {
                    double leftComplex = left2Valid ? left2 : left3;
                    double rightComplex = right2Valid ? right2 : right3;

                    if (op == "+") result = leftComplex + rightComplex;
                    else if (op == "-") result = leftComplex - rightComplex;
                    else if (op == "*") result = leftComplex * rightComplex;
                    else if (op == "/") result = rightComplex != 0 ? leftComplex / rightComplex : throw new DivideByZeroException();
                    else if (op == "%") result = rightComplex != 0 ? leftComplex % rightComplex : throw new DivideByZeroException();
                    else throw new Exception($"Operator necunoscut: {op}");

                    Console.WriteLine($"Debug: Expresie aritmetica '{leftComplex} {op} {rightComplex}' evaluata la {result}");
                    return result;
                }

                // Daca ambele sunt numerice, tratam aritmetica
                if (double.TryParse(leftNr.ToString(), out double left1) &&
                   double.TryParse(rightNr.ToString(), out double right1))
                {

                    if (op == "+") result = left1 + right1;
                    else if (op == "-") result = left1 - right1;
                    else if (op == "*") result = left1 * right1;
                    else if (op == "/") result = right1 != 0 ? left1 / right1 : throw new DivideByZeroException();
                    else if (op == "%") result = right1 != 0 ? left1 % right1 : throw new DivideByZeroException();
                    else throw new Exception($"Operator necunoscut: {op}");

                    Console.WriteLine($"Debug: Expresie aritmetica '{left1} {op} {right1}' evaluata la {result}");
                    return result;
                }
                // Daca ambele sunt variabile, tratam aritmetica
                if (double.TryParse(leftValueFinal.ToString(), out double left) &&
                    double.TryParse(rightValueFinal.ToString(), out double right))
                {

                    if (op == "+") result = left + right;
                    else if (op == "-") result = left - right;
                    else if (op == "*") result = left * right;
                    else if (op == "/") result = right != 0 ? left / right : throw new DivideByZeroException();
                    else if (op == "%") result = right != 0 ? left % right : throw new DivideByZeroException();
                    else throw new Exception($"Operator necunoscut: {op}");

                    Console.WriteLine($"Debug: Expresie aritmetica '{left} {op} {right}' evaluata la {result}");
                    return result;
                }
                else
                {
                    throw new Exception($"Eroare: Operanzii '{leftValue}' si '{rightValue}' nu sunt numerici pentru operatorul '{op}'!");
                }


            }
            if (context is RulesParser.RelationalExpressionContext relCtx)
            {
                var leftValue = VisitExpression(relCtx.expression(0));
                double left;
                try
                {
                    if (leftValue is string leftVarName && variables.ContainsKey(leftVarName))
                    {
                        left = double.Parse(variables[leftVarName]);
                        Console.WriteLine($"Debug: Operandul stang '{leftVarName}' gasit în lista variabilelor cu valoarea {left}");
                    }
                    else if (leftValue is double numericLeftValue)
                    {
                        left = numericLeftValue;
                    }
                    else
                    {
                        throw new Exception($"Eroare: Operandul stang '{leftValue}' nu este definit ca variabila sau numeric!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Eroare la evaluarea operandului stang: {ex.Message}");
                    throw;
                }

                var rightValue = VisitExpression(relCtx.expression(1));
                double right;
                try
                {
                    if (rightValue is string rightVarName && variables.ContainsKey(rightVarName))
                    {
                        right = double.Parse(variables[rightVarName]);
                        Console.WriteLine($"Debug: Operandul drept '{rightVarName}' gasit în lista variabilelor cu valoarea {right}");
                    }
                    else if (rightValue is double numericRightValue)
                    {
                        right = numericRightValue;
                    }
                    else
                    {
                        throw new Exception($"Eroare: Operandul drept '{rightValue}' nu este definit ca variabila sau numeric!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Eroare la evaluarea operandului drept: {ex.Message}");
                    throw;
                }

                string op = relCtx.REL_OP()?.GetText();
                bool result;

                if (op == "<") result = left < right;
                else if (op == ">") result = left > right;
                else if (op == "<=") result = left <= right;
                else if (op == ">=") result = left >= right;
                else if (op == "==") result = left == right;
                else if (op == "!=") result = left != right;
                else throw new Exception($"Operator necunoscut: {op}");

                Console.WriteLine($"Debug: Expresie relationala '{left} {op} {right}' evaluata la {result}");
                return result;
            }
            if (context is RulesParser.LogicalExpressionContext logCtx)
            {
                string op = logCtx.LOGIC_OP()?.GetText();

                var leftValue = VisitExpression(logCtx.expression(0));
                var rightValue = VisitExpression(logCtx.expression(1));

                if (leftValue is string leftVarName && variables.ContainsKey(leftVarName))
                {
                    leftValue = variables[leftVarName];
                }
                if (rightValue is string rightVarName && variables.ContainsKey(rightVarName))
                {
                    rightValue = variables[rightVarName];
                }

                bool left = ConvertToBoolean(leftValue);
                bool right = ConvertToBoolean(rightValue);

                bool result = false;
                if (op == "&&") result = left && right;
                else if (op == "||") result = left || right;
                else throw new Exception($"Operator necunoscut: {op}");

                Console.WriteLine($"Debug: Expresie logica '{left} {op} {right}' evaluata la {result}");
                return result;
            }

            if (context is RulesParser.LogicalNegationExpressionContext negCtx)
            {

                var operandValue = VisitExpression(negCtx.expression());

                if (operandValue is string variableName && variables.ContainsKey(variableName))
                {
                    operandValue = variables[variableName];
                }

                bool boolValue = ConvertToBoolean(operandValue);

                bool result = !boolValue;
                Console.WriteLine($"Debug: Negarea logica '!{boolValue}' evaluata la {result}");
                return result;
            }

            if (context is RulesParser.ParenthesisExpressionContext parenCtx)
            {
                var innerValue = VisitExpression(parenCtx.expression());
                Console.WriteLine($"Debug: Expresie în paranteze evaluata la {innerValue}");
                return innerValue;
            }
            if (context is RulesParser.BoolAssignmentExpressionContext boolAssignCtx)
            {
                string boolValue = boolAssignCtx.GetText();
                bool value;
                if (boolValue == "true" || boolValue == "1")
                {
                    value = true;
                }
                else if (boolValue == "false" || boolValue == "0")
                {
                    value = false;
                }
                else
                {
                    throw new Exception($"Eroare: Literal boolean necunoscut: {boolValue}");
                }

                Console.WriteLine($"Debug: Valoare booleana procesata = {value}");
                return value;
            }


            Console.WriteLine("Debug: Expresie necunoscuta. Returnam NULL.");
            return null;
        }

        public void DisplayVariables()
        {
            Console.WriteLine("\nDictionarul variabilelor:");
            foreach (var kvp in variables)
            {
                Console.WriteLine($"Nume: {kvp.Key}, Valoare: {kvp.Value}");
            }
        }
        

        private readonly List<RulesParser.FunctionContext> functionContexts = new List<RulesParser.FunctionContext>();
        private List<string> functions = new List<string>();
        public class FunctionDetails
        {
            public string Name { get; set; }
            public string ReturnType { get; set; }
            public bool IsRecursive { get; set; }
            public bool IsMain { get; set; }
            public List<string> ControlStructures = new List<string>();
            public Dictionary<string, string> LocalVariables { get; set; } = new Dictionary<string, string>();
        }
        
        public override object VisitFunction(RulesParser.FunctionContext context)
        {
            string funcName = context.IDENTIFIER().GetText();
            string returnType = context.type().GetText();
            bool isMain = funcName == "main";

            bool mainWasExecuted = false;
            if(mainWasExecuted == false)
            {
                ExecuteMainAndRemainingFunctions();
                mainWasExecuted = true;
            }

            bool isRecursive = context.statement().Any(s => s.GetText().Contains(funcName));
            bool hasReturnOccurred = false;

            var functionDetails = new FunctionDetails
            {
                Name = funcName,
                ReturnType = returnType,
                IsRecursive = isRecursive,
                IsMain = isMain,
                ControlStructures = new List<string>(),
                LocalVariables = new Dictionary<string, string>() 
            };

            var paramTypes = context.parameters()?.type();
            var paramNames = context.parameters()?.IDENTIFIER();
            if (paramTypes != null && paramNames != null)
            {
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    string paramType = paramTypes[i].GetText();
                    string paramName = paramNames[i].GetText();
                    string defaultValue = GetDefaultValueForType(paramType).ToString();

                    if (functionDetails.LocalVariables.ContainsKey(paramName))
                    {
                        Console.WriteLine($"Eroare: Parametrul '{paramName}' este deja definit în functia '{funcName}'.");
                        return null;
                    }

                    functionDetails.LocalVariables[paramName] = $"{paramType} {paramName}={defaultValue}";
                    variables[paramName] = defaultValue;
                }
            }

            if (isMain)
            {
                Console.WriteLine($"Debug: Executing main function.");
                foreach (var statement in context.statement())
                {
                    var returnValue = Visit(statement);
                    if (hasReturnOccurred)
                    {
                        Console.WriteLine($"Debug: Return detectat. Sarim peste restul instructiunilor din functia '{funcName}'.");
                        return null;
                    }
                    if (returnValue != null)
                    {
                        Console.WriteLine($"Debug: Functia '{funcName}' a intalnit un return cu valoarea: {returnValue}");
                        hasReturnOccurred = true;
                    }
                    if (statement is RulesParser.StatementContext stmtContext &&
                        stmtContext.GetChild(0) is RulesParser.DeclarationContext declContext)
                    {
                        string varName = declContext.IDENTIFIER().GetText();
                        string varType = declContext.type().GetText();
                        string varValue = variables.ContainsKey(varName) ? variables[varName] : GetDefaultValueForType(varType).ToString();
                        if (functionDetails.LocalVariables.ContainsKey(varName))
                        {
                            Console.WriteLine($"Eroare: Variabila '{varName}' este deja definita în functia '{funcName}'.");
                            return null;
                        }

                        functionDetails.LocalVariables[varName] = $"{varType} {varName}={varValue}";
                        variables[varName] = varValue; 
                        UpdateLocalVariables(functionDetails);
                    }

                  
                    var childNode = statement.GetChild(0); 
                    if (childNode is RulesParser.IfStatementContext)
                    {
                        functionDetails.ControlStructures.Add("if-else");
                    }
                    else if (childNode is RulesParser.ForStatementContext)
                    {
                        functionDetails.ControlStructures.Add("for");
                    }
                    else if (childNode is RulesParser.WhileStatementContext)
                    {
                        functionDetails.ControlStructures.Add("while");
                    }
                }

                functions.Add($"Functie: {functionDetails.Name}; Tip: {functionDetails.ReturnType}; " +
                              $"Recursiva: {(functionDetails.IsRecursive ? "Yes" : "No")}; " +
                              $"Main: {(functionDetails.IsMain ? "Yes" : "No")}; " +
                              $"Structuri de control: [{string.Join(", ", functionDetails.ControlStructures)}]; " +
                              $"Variabile: {{ {string.Join(", ", functionDetails.LocalVariables.Values)} }}");
            }
            else
            {
                Console.WriteLine($"Debug: Registering function: {funcName}");
                functionContexts.Add(context);
                foreach (var statement in context.statement())
                {
                    var returnValue = Visit(statement);
                    if (hasReturnOccurred)
                    {
                        Console.WriteLine($"Debug: Return detectat. Sarim peste restul instructiunilor din functia '{funcName}'.");
                        break;
                    }
                    if (returnValue != null)
                    {
                        Console.WriteLine($"Debug: Functia '{funcName}' a intalnit un return cu valoarea: {returnValue}");
                        hasReturnOccurred = true;
                        break;
                    }

                    if (statement is RulesParser.StatementContext stmtContext &&
                        stmtContext.GetChild(0) is RulesParser.DeclarationContext declContext)
                    {
                        string varName = declContext.IDENTIFIER().GetText();
                        string varType = declContext.type().GetText();
                        string varValue = variables.ContainsKey(varName) ? variables[varName] : GetDefaultValueForType(varType).ToString();
                        if (functionDetails.LocalVariables.ContainsKey(varName))
                        {
                            Console.WriteLine($"Eroare: Variabila '{varName}' este deja definita în functia '{funcName}'.");
                            return null; 
                        }

                        functionDetails.LocalVariables[varName] = $"{varType} {varName}={varValue}";
                        variables[varName] = varValue;
                        UpdateLocalVariables(functionDetails);
                    }

                    var childNode = statement.GetChild(0); 
                    if (childNode is RulesParser.IfStatementContext)
                    {
                        functionDetails.ControlStructures.Add("if-else");
                    }
                    else if (childNode is RulesParser.ForStatementContext)
                    {
                        functionDetails.ControlStructures.Add("for");
                    }
                    else if (childNode is RulesParser.WhileStatementContext)
                    {
                        functionDetails.ControlStructures.Add("while");
                    }
                }

                functions.Add($"Functie: {functionDetails.Name}; Tip: {functionDetails.ReturnType}; " +
                              $"Recursiva: {(functionDetails.IsRecursive ? "Yes" : "No")}; " +
                              $"Main: {(functionDetails.IsMain ? "Yes" : "No")}; " +
                              $"Structuri de control: [{string.Join(", ", functionDetails.ControlStructures)}]; " +
                              $"Variabile: {{ {string.Join(", ", functionDetails.LocalVariables.Values)} }}");
            }

            return null;
        }

        public void ExecuteMainAndRemainingFunctions()
        {
            Console.WriteLine("\nExecutarea functiei main si a celorlalte functii:");

            var mainFunction = functionContexts.FirstOrDefault(func => func.IDENTIFIER().GetText() == "main");

            if (mainFunction != null)
            {
                Console.WriteLine("Debug: Executing main function.");
                foreach (var statement in mainFunction.statement())
                {
                    Visit(statement);
                }
            }
            else
            {
                Console.WriteLine("Eroare: Functia main nu a fost gasita!");
                return;
            }

            foreach (var funcContext in functionContexts)
            {
                string funcName = funcContext.IDENTIFIER().GetText();

                if (funcName == "main")
                {
                    continue;
                }

                Console.WriteLine($"Debug: Executing function: {funcName}");
                foreach (var statement in funcContext.statement())
                {
                    Visit(statement);
                }
            }
        }
        private void UpdateLocalVariables(FunctionDetails functionDetails)
        {
            foreach (var varName in variables.Keys)
            {
                if (functionDetails.LocalVariables.ContainsKey(varName))
                {
                    var existingValue = functionDetails.LocalVariables[varName];
                    var typePart = existingValue.Split(' ')[0]; 
                    functionDetails.LocalVariables[varName] = $"{typePart} {varName}={variables[varName]}";
                }
            }
        }
        
        public void DisplayGlobalVariables()
        {
            Console.WriteLine("\nVariabile globale:");
            foreach (var globalVar in globalVariables)
            {
                Console.WriteLine(globalVar.Value);
            }
        }
        public override object VisitReturnStatement(RulesParser.ReturnStatementContext context)
        {
            var value = context.expression() != null ? VisitExpression(context.expression()) : null;

            if (value == null)
            {
                Console.WriteLine($"Debug: Valoarea returnată din funcție este NULL.");
            }
            else
            {
                Console.WriteLine($"Debug: Valoare returnată din funcție : {value}");
            }

            returnValue = value;
            return value;
        }

        private object GetDefaultValueForType(string type)
        {
            switch (type)
            {
                case "int":
                    return 0;
                case "float":
                    return 0.0f;
                case "double":
                    return 0.0;
                case "string":
                    return ""; 
                case "bool":
                    return false; 
                default:
                    throw new Exception($"Tip necunoscut: {type}");
            }
        }
        public object GetReturnValue()
        {
            Console.WriteLine($"Debug: Valoare returnata : {returnValue}");
            return returnValue;
        }

        public Dictionary<string, string> GetVariables()
        {
            return variables;
        }

        public void DisplayFunctions()
        {
            Console.WriteLine("\nFunctii procesate:");
            foreach (var func in functions)
            {
                Console.WriteLine(func);
            }
        }
        public class StructDetails
        {
            public string Name { get; set; }
            public List<string> Fields { get; set; } = new List<string>();
            public List<string> Methods { get; set; } = new List<string>();
            public bool HasConstructor { get; set; }
            public bool HasDestructor { get; set; }
        }
        private readonly List<StructDetails> structDefinitions = new List<StructDetails>();

        public override object VisitStructDefinition(RulesParser.StructDefinitionContext context)
        {
            string structName = context.IDENTIFIER().GetText();
            Console.WriteLine($"Debug: Structura gasita: {structName}");

            var structDetails = new StructDetails
            {
                Name = structName,
                Fields = new List<string>(),
                Methods = new List<string>(),
                HasConstructor = false,
                HasDestructor = false
            };

            foreach (var member in context.structMember())
            {
                var child = member.GetChild(0);

                if (child is RulesParser.DeclarationContext decl)
                {
                    string fieldType = decl.type().GetText(); 
                    string fieldName = decl.IDENTIFIER().GetText(); 

                    string fieldValue = string.Empty;

                    if (decl.expression() != null)
                    {
                        var exprValue = VisitExpression(decl.expression());
                        fieldValue = exprValue != null ? exprValue.ToString() : "default";
                        fieldValue = $" = {fieldValue}"; 
                    }

                    structDetails.Fields.Add($"{fieldType} {fieldName}{fieldValue}");
                    Console.WriteLine($"Debug: Camp detectat in struct: {fieldType} {fieldName}{fieldValue}");
                }

                else if (child is RulesParser.FunctionContext func)
                {
                    string funcType = func.type().GetText();
                    string funcName = func.IDENTIFIER().GetText();

                    var parameters = func.parameters()?.GetText() ?? "none";
                    structDetails.Methods.Add($"{funcType} {funcName}({parameters})");
                    Console.WriteLine($"Debug: Functie detectata in struct: {funcType} {funcName}({parameters})");

                    var functionDetails = new FunctionDetails
                    {
                        Name = $"{structName}::{funcName}",
                        ReturnType = funcType,
                        IsRecursive = func.statement().Any(s => s.GetText().Contains(funcName)),
                        IsMain = false, 
                        ControlStructures = new List<string>(),
                        LocalVariables = new Dictionary<string, string>()
                    };

                    foreach (var statement in func.statement())
                    {
                        Visit(statement);

                        var childNode = statement.GetChild(0);
                        if (childNode is RulesParser.IfStatementContext)
                        {
                            functionDetails.ControlStructures.Add("if-else");
                        }
                        else if (childNode is RulesParser.ForStatementContext)
                        {
                            functionDetails.ControlStructures.Add("for");
                        }
                        else if (childNode is RulesParser.WhileStatementContext)
                        {
                            functionDetails.ControlStructures.Add("while");
                        }

                        if (statement is RulesParser.StatementContext stmtContext &&
                            stmtContext.GetChild(0) is RulesParser.DeclarationContext declContext)
                        {
                            string varName = declContext.IDENTIFIER().GetText();
                            string varType = declContext.type().GetText();
                            string varValue = variables.ContainsKey(varName) ? variables[varName] : GetDefaultValueForType(varType).ToString();
                            functionDetails.LocalVariables[varName] = $"{varType} {varName}={varValue}";
                        }
                    }

                    UpdateVariablesInContext(func.statement());

                    foreach (var varName in functionDetails.LocalVariables.Keys.ToList())
                    {
                        if (variables.ContainsKey(varName))
                        {
                            string updatedValue = variables[varName];
                            string type = functionDetails.LocalVariables[varName].Split(' ')[0]; 
                            functionDetails.LocalVariables[varName] = $"{type} {varName}={updatedValue}";
                        }
                    }

                    functions.Add($"Functie: {functionDetails.Name}; Tip: {functionDetails.ReturnType}; " +
                                  $"Recursiva: {(functionDetails.IsRecursive ? "Yes" : "No")}; " +
                                  $"Main: {(functionDetails.IsMain ? "Yes" : "No")}; " +
                                  $"Structuri de control: [{string.Join(", ", functionDetails.ControlStructures)}]; " +
                                  $"Variabile: {{ {string.Join(", ", functionDetails.LocalVariables.Values)} }}");
                }
                else if (child is RulesParser.ConstructorContext constructor)
                {
                    string constructorName = constructor.IDENTIFIER().GetText();
                    var parameters = constructor.parameters()?.GetText() ?? "none";
                    structDetails.HasConstructor = true;

                    Console.WriteLine($"Debug: Constructor detectat: {constructorName}({parameters})");
                }
                else if (child is RulesParser.DestructorContext destructor)
                {
                    structDetails.HasDestructor = true;
                    string destructorName = destructor.IDENTIFIER().GetText();

                    Console.WriteLine($"Debug: Destructor detectat: ~{destructorName}()");
                }
                else
                {
                    Console.WriteLine($"Debug: Membru necunoscut in struct: {member.GetText()}");
                }
            }

            structDefinitions.Add(structDetails);
            return null;
        }

        public void DisplayStructDefinitions()
        {
            Console.WriteLine("\nStructuri definite:");
            foreach (var structDef in structDefinitions)
            {
                Console.WriteLine($"Struct: {structDef.Name}");
                Console.WriteLine($"  Campuri: {string.Join(", ", structDef.Fields)}");
                Console.WriteLine($"  Functii: {string.Join(", ", structDef.Methods)}");
                Console.WriteLine($"  Are constructor: {(structDef.HasConstructor ? "Yes" : "No")}");
                Console.WriteLine($"  Are destructor: {(structDef.HasDestructor ? "Yes" : "No")}");
            }
        }
        public override object VisitProgram(RulesParser.ProgramContext context)
        {
            Console.WriteLine("Debug: Incepem procesarea programului.");

            foreach (var element in context.children)
            {
                if (element is RulesParser.StructDefinitionContext structDef)
                {
                    Console.WriteLine($"Debug: Structura detectata: {structDef.IDENTIFIER().GetText()}");
                    VisitStructDefinition(structDef);
                }
                else if (element is RulesParser.FunctionContext funcContext)
                {
                    Console.WriteLine($"Debug: Functia detectata: {funcContext.IDENTIFIER().GetText()}");
                    VisitFunction(funcContext);
                }
                else if (element is RulesParser.GlobalVariableContext globalVarContext)
                {
                    Console.WriteLine($"Debug: Variabila globala detectata: {globalVarContext.IDENTIFIER().GetText()}");
                    VisitGlobalVariable(globalVarContext);
                }
                else
                {
                    Console.WriteLine($"Debug: Element necunoscut detectat: {element.GetType().Name}");
                    Visit(element); 
                }
            }

            Console.WriteLine("Debug: Procesarea programului s-a incheiat.");
            return null;
        }


        public override object VisitIfStatement(RulesParser.IfStatementContext context)
        {
            Console.WriteLine($"Debug: Evaluam conditia if: {context.expression().GetText()}");
            var conditionValue = VisitExpression(context.expression());

            if (conditionValue is bool condition)
            {
                if (condition)
                {
                    Console.WriteLine("Debug: Conditia este adevarata. Executam blocul if.");

                    foreach (var statement in context.statement())
                    {
                        Visit(statement);
                        var child = statement.GetChild(0);
                        if (child is RulesParser.AssignmentContext assignmentCtx && condition == true)
                        {
                            if (returnValue != null)
                            {
                                Console.WriteLine("Debug: Return detectat in blocul if. Oprire executie.");
                                return null;
                            }
                            string varName = assignmentCtx.IDENTIFIER().GetText();
                            object value = VisitExpression(assignmentCtx.expression());
                            variables[varName] = value.ToString();
                            Console.WriteLine($"Debug: Variabila '{varName}' actualizata in if cu valoarea {value}");
                        }
                    }
                    return null;
                }
                else
                {
                    Console.WriteLine("Debug: Conditia este falsa.");

                    if (context.ELSE() != null)
                    {
                        Console.WriteLine("Debug: Executam blocul else.");
                        foreach (var statement in context.statement())
                        {
                            Visit(statement);
                            var child = statement.GetChild(0);
                            if (child is RulesParser.AssignmentContext assignmentCtx && condition == false)
                            {
                                if (returnValue != null)
                                {
                                    Console.WriteLine("Debug: Return detectat in blocul else. Oprire executie.");
                                    return null;
                                }
                                string varName = assignmentCtx.IDENTIFIER().GetText();
                                object value = VisitExpression(assignmentCtx.expression());
                                variables[varName] = value.ToString();
                                Console.WriteLine($"Debug: Variabila '{varName}' actualizata in else cu valoarea {value}");
                            }
                        }
                    }
                }
            }
            else
            {
                throw new Exception("Eroare: Conditia `if` nu este de tip boolean!");
            }

            return null;
        }

        public override object VisitWhileStatement(RulesParser.WhileStatementContext context)
        {
            Console.WriteLine($"Debug: Evaluam conditia while: {context.expression().GetText()}");
            while (true)
            {
                var conditionValue = VisitExpression(context.expression());
                bool condition;

                if (conditionValue is bool boolValue)
                {
                    condition = boolValue;
                }
                else if (conditionValue is double numericValue)
                {
                    condition = numericValue != 0;
                }
                else
                {
                    throw new Exception("Eroare: Conditia din while nu poate fi evaluata la boolean.");
                }

                if (!condition)
                {
                    Console.WriteLine("Debug: Conditia este falsa. Iesim din bucla while.");
                    break;
                }

                Console.WriteLine("Debug: Conditia este adevarata. Executam blocul while.");
                foreach (var statement in context.statement())
                {
                    
                    if (statement.GetText().Contains("="))
                    {
     
                        Console.WriteLine($"Debug: Procesam o atribuire: {statement.GetText()}");
                        Visit(statement);
                    }
                    else
                    {
                        Visit(statement);
                    }
                }
            }


            return null;
        }
        
        private void UpdateVariablesInContext(IEnumerable<RulesParser.StatementContext> statements)
        {
            foreach (var statement in statements)
            {
                Visit(statement);

                var child = statement.GetChild(0); 
                if (child is RulesParser.AssignmentContext assignmentCtx)
                {
                    string varName = assignmentCtx.IDENTIFIER().GetText();
                    object value = VisitExpression(assignmentCtx.expression());
                    variables[varName] = value.ToString();
                    Console.WriteLine($"Debug: Variabila '{varName}' actualizata cu valoarea {value}");
                }
            }
        }
       
        public override object VisitForStatement(RulesParser.ForStatementContext context)
        {
            Console.WriteLine($"Debug: Procesam bucla for.");

            Visit(context.assignment(0));

            while (true)
            {
                var conditionValue = VisitExpression(context.expression());
                bool condition;

                try
                {
                    condition = Convert.ToBoolean(conditionValue);
                }
                catch
                {
                    throw new Exception("Eroare: Conditia din bucla for nu poate fi evaluata la boolean.");
                }

                if (!condition)
                {
                    Console.WriteLine("Debug: Conditia este falsa. Iesim din bucla for.");
                    break;
                }

                Console.WriteLine("Debug: Conditia este adevarata. Executam blocul for.");

                UpdateVariablesInContext(context.statement());

                var thirdComponent = context.GetChild(4); 
                if (context.increment() != null)
                {
                    VisitIncrement(context.increment());
                }
                else if (context.decrement() != null)
                {
                    VisitDecrement(context.decrement());
                }
                else if (context.assignment(1) != null)
                {
                    VisitAssignment(context.assignment(1));
                }
                else
                {
                    throw new Exception("Eroare: Componenta a treia a buclei for trebuie sa fie assignment, increment sau decrement.");
                }
            }

            return null;
        }

        public Dictionary<string, string> GetVariablesFromProgram(RulesParser.ProgramContext programContext)
        {
            var variables = new Dictionary<string, string>();

            foreach (var element in programContext.children)
            {
                if (element is RulesParser.DeclarationContext declContext)
                {
                    string varName = declContext.IDENTIFIER().GetText();
                    Console.WriteLine($"Process: Se aplica VisitExpression pentru variabila: {varName} de tip: {declContext.type().GetText()}");
                    string varValue = declContext.expression() != null
                        ? VisitExpression(declContext.expression()).ToString() : null; 

                    variables[varName] = varValue;
                }
                else if (element is RulesParser.GlobalVariableContext globalVarContext)
                {
                    string varName = globalVarContext.IDENTIFIER().GetText();
                    Console.WriteLine($"Process: Se aplica VisitExpression pentru variabila: {varName} de tip: {globalVarContext.type().GetText()}");
                    string varValue = globalVarContext.expression() != null
                        ? VisitExpression(globalVarContext.expression()).ToString() : null;

                    variables[varName] = varValue;
                }
                else if (element is RulesParser.FunctionContext funcContext)
                {
                    var functionVariables = GetVariablesFromFunction(funcContext);
                    foreach (var kvp in functionVariables)
                    {
                        variables[kvp.Key] = kvp.Value;
                    }
                }
            }

            return variables;
        }
        private bool ConvertToBoolean(object value)
        {
            if (value == null)
            {
                throw new Exception("Eroare: Valoarea nu poate fi NULL pentru conversia la boolean.");
            }

            string strValue = value.ToString().ToLower();

            if (strValue == "true" || strValue == "1")
            {
                return true;
            }
            else if (strValue == "false" || strValue == "0")
            {
                return false;
            }
            else
            {
                throw new Exception($"Eroare: Valoarea '{value}' nu poate fi convertita la boolean.");
            }
        }

        private Dictionary<string, string> GetVariablesFromFunction(RulesParser.FunctionContext funcContext)
        {
            var variables = new Dictionary<string, string>();

            var types = funcContext.parameters()?.type();
            var identifiers = funcContext.parameters()?.IDENTIFIER();
            if (types != null && identifiers != null)
            {
                for (int i = 0; i < Math.Min(types.Length, identifiers.Length); i++)
                {
                    string paramName = identifiers[i].GetText();
                    variables[paramName] = null; 
                }
            }

            foreach (var statement in funcContext.statement())
            {
                if (statement.GetChild(0) is RulesParser.DeclarationContext declContext)
                {
                    string varName = declContext.IDENTIFIER().GetText();
                    Console.WriteLine($"Process: Se aplica VisitExpression pentru variabila: {varName} de tip: {declContext.type().GetText()}");
                    string varValue = declContext.expression() != null
                        ? VisitExpression(declContext.expression()).ToString()
                        : null;

                    variables[varName] = varValue;
                }
            }

            return variables;
        }


        // Aceasta declarare trebuie pusa in RuleBaseVisitor.cs daca este modificat fisierul Rule.g4
        /*public virtual Result VisitExpression([NotNull] RulesParser.ExpressionContext context)
        {
            return VisitChildren(context);
        }*/
    }
}
