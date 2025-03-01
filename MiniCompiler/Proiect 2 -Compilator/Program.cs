using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Proiect_2__Compilator
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Se copiaza pathul de la fisierul inpt.txt de fiecare data;
            string filePath = "C:MiniCompiler\\Proiect 2 -Compilator\\input.txt"; // Calea către fișierul sursă

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Fișierul nu a fost găsit!");
                return;
            }

            string sourceCode = File.ReadAllText(filePath);

            try
            {
                AntlrInputStream inputStream = new AntlrInputStream(sourceCode);

                RulesLexer lexer = new RulesLexer(inputStream);
                CommonTokenStream tokenStream = new CommonTokenStream(lexer);

                RulesParser parser = new RulesParser(tokenStream);

                var tree = parser.program();

                DisplayTokens(lexer);

                CodeExecutor executor = new CodeExecutor();
                executor.Visit(tree);

                executor.DisplayFunctions();
                executor.DisplayVariables();
                executor.DisplayGlobalVariables();
                executor.DisplayStructDefinitions();

                CodeErrorCatcher codeErrorCatcher = new CodeErrorCatcher();

                codeErrorCatcher.ProcessLexicalErrors(lexer);
                codeErrorCatcher.DisplayLexicalErrors();

                var extractedVariables = codeErrorCatcher.ExtractVariables(tree); 
                codeErrorCatcher.ProcessSemanticErrors(extractedVariables);
                codeErrorCatcher.DisplaySemanticErrors();

                codeErrorCatcher.ProcessSyntaxErrors(tree);
                codeErrorCatcher.DisplaySyntaxErrors();

                Console.WriteLine("\nVariabile procesate:");
                foreach (var variable in executor.GetVariables())
                {
                    Console.WriteLine($"Variabila: {variable.Key}, Valoare: {variable.Value}");
                }
                var returnValue = executor.GetReturnValue();
                if (returnValue != null)
                {
                    Console.WriteLine($"\nRezultatul functiei returnat: {returnValue}");
                }
                else
                {
                    Console.WriteLine("\nFuncția nu a returnat nicio valoare.");
                }
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eroare la procesarea codului: {ex.Message}");
            }
        }

        static void DisplayTokens(RulesLexer lexer)
        {
            Console.WriteLine("\nLista tokenilor gasiti:");

            lexer.Reset();
            var token = lexer.NextToken();
            while (token.Type != Antlr4.Runtime.TokenConstants.EOF)
            {
                Console.WriteLine($"Token: '{token.Text}', Tip: {lexer.Vocabulary.GetSymbolicName(token.Type)}, Linie: {token.Line}, Coloană: {token.Column}");
                token = lexer.NextToken();
            }
            Console.WriteLine("---------------------------------------------\n");

        }
    }

}