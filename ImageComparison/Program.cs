using CommandLine;
using CommandLine.Text;
using ImageComparison.Models;
using ImageComparison.Services;
using System.Reflection;

namespace ImageComparison
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            ExitCode exit = ExitCode.Success;

            ParserResult<Options> parser = new Parser(with => with.HelpWriter = null).ParseArguments<Options>(args);
            
            parser.WithParsed(o =>
            {
                LogService.OnLog += OnLog;
            })
            .WithNotParsed(errors =>
            {
                if(errors.Any(e => e.GetType() == typeof(HelpRequestedError)))
                {
                    DisplayHelp(parser);
                }
                else
                {
                    exit = ExitCode.BadRequest;
                }
            });

            return (int)exit;
        }

        private static void OnLog(object? sender, LogEventArgs logEvent)
        {
            Console.Write($"{logEvent.Log.Time.ToLongTimeString()} {logEvent.Log.LogLevel} -> {ToLiteral(logEvent.Log.Text)}\n");
        }

        private static string ToLiteral(string valueTextForCompiler)
        {
            return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(valueTextForCompiler, false);
        }

        private static void DisplayHelp<T>(ParserResult<T> result)
        {
            Console.WriteLine(
                HelpText.AutoBuild(result, h =>
                {
                    h.AddEnumValuesToHelpText = false;
                    return h;
                })
                .AddPreOptionsLines(
                    new List<string>()
                    {
                        "\n\nUsage:",
                        $"\n  {Assembly.GetExecutingAssembly().GetName().Name}.exe [options...]",
                        "\n\nOptions:"
                    }
                )
                .AddPostOptionsLines(
                    new List<string>()
                    {
                        "Exit Codes:",
                        "",
                        "    0 - Success",
                        "    1 - General Error",
                        "   11 - Warning",
                        "  127 - Bad Request"
                    }
                )
            );
        }
    }
}