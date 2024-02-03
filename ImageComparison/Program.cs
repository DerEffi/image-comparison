using CommandLine;
using CommandLine.Text;
using ImageComparison.Models;
using ImageComparison.Services;
using System.Reflection;

namespace ImageComparison
{
    internal class Program
    {
        public static LogLevel logLevel = LogLevel.Info;
        public static ExitCode exit = ExitCode.Success;
        public static bool force = false;
        public static ProgressBar? progress = null;

        private static readonly List<string> preOptions =
            new List<string>(){
                "\n\nUSAGE:",
                $"\n  {Assembly.GetExecutingAssembly().GetName().Name}.exe [options...]",
                "\n\nOPTIONS:",
            };
        private static readonly List<string> postOptions = new List<string>()
            {
                "EXIT CODES:",
                "",
                "    0 - Success",
                "    1 - General Error",
                "   11 - Warning",
                "  127 - Bad Request"
            };

        public static int Main(string[] args)
        {
            ParserResult<Options> parser = new Parser(with => {
                with.HelpWriter = null;
                with.CaseInsensitiveEnumValues = true;
            }).ParseArguments<Options>(args);
            
            parser.WithParsed(options =>
            {
                logLevel = options.LogLevel;
                force = options.Force;
                LogService.OnLog += OnLog;
                CompareService.OnProgress += OnProgress;

                // custom validation
                List<string> customErrors = new();

                if (options.Processors.Count() == 0 && (options.Action == Models.Action.Move || options.Action == Models.Action.Bin || options.Action == Models.Action.Delete))
                    customErrors.Add($"Required option 'p, processors' missing on action '{options.Action}'");

                if (options.Similarity < 0 || options.Similarity > 10000)
                    customErrors.Add("Option 's, similarity' is out of range. Valid 0 - 10000");

                if (options.HashDetail <= 0 || options.HashDetail >= 10)
                    customErrors.Add("Option 'd, detail' is out of range. Valid 1 - 99");

                if (options.Target.Length == 0 && options.Action == Models.Action.Move)
                    customErrors.Add($"Required option 't, target' missing on action '{options.Action}'");

                if (options.Cache.Length == 0 && options.Action == Models.Action.NoMatch)
                    customErrors.Add($"Required option 'c, cache' missing on action '{options.Action}'");

                if (customErrors.Count > 0)
                {
                    ParserResult<Options> errorParser = new Parser(with =>
                    {
                        with.HelpWriter = null;
                    }).ParseArguments<Options>(new string[] { "--help" });

                    List<string> customErrorsText = new()
                    {
                        "\n\nERROR(S):"
                    };
                    customErrorsText.AddRange(customErrors.Select(e => "  " + e));
                    customErrorsText.AddRange(preOptions);

                    errorParser.WithNotParsed(error =>
                    {
                        Console.WriteLine(
                            HelpText.AutoBuild(errorParser, h =>
                            {
                                h.AddEnumValuesToHelpText = false;
                                return h;
                            })
                            .AddPreOptionsLines(customErrorsText)
                            .AddPostOptionsLines(postOptions)
                        );
                    });
                    return;
                }

                try
                {
                    string hashVersion = HashService.GetIdentifier(options.HashDetail, options.HashAlgorithm);
                    ulong scantime = (ulong)(DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalSeconds;

                    options.Locations = options.Locations.Select(l => Path.IsPathRooted(l) ? l : Path.Combine(Assembly.GetExecutingAssembly().Location, l));
                    List<List<FileInfo>> searchLocations = FileService.SearchProcessableFiles(options.Locations.ToArray(), options.Recursive);

                    FileService.DataDirectory = Path.IsPathRooted(options.Cache) ? (Path.GetDirectoryName(options.Cache) ?? FileService.DataDirectory) : Path.Combine(Assembly.GetExecutingAssembly().Location, Path.GetDirectoryName(options.Cache) ?? ".\\");
                    FileService.CacheFile = Path.GetFileName(options.Cache).NullIfEmpty() ?? FileService.CacheFile;
                    if (options.Cache.Length >= 1)
                        CacheService.Init();
                    List<CacheItem> cachedAnalysis = options.Cache.Length >= 1 ? CacheService.GetImages(hashVersion) : new();

                    List<List<ImageAnalysis>> analysedImages = CompareService.AnalyseImages(searchLocations, options.HashDetail, options.HashAlgorithm, cachedAnalysis);
                    if (options.Cache.Length >= 1)
                        CacheService.UpdateImages(analysedImages.SelectMany(i => i).ToList(), hashVersion, scantime);

                    List<NoMatch> nomatches = options.Cache.Length >= 1 ? CacheService.GetNoMatches() : new();
                    List<ImageMatch> Matches = CompareService.SearchForDuplicates(analysedImages, options.Similarity, options.SearchMode, nomatches);

                    if (options.Action == Models.Action.Search)
                    {
                        if (logLevel < LogLevel.Warning)
                            Console.WriteLine("---------------------------------------------");
                        Matches.ForEach(m =>
                        {
                            Console.WriteLine($"\"{m.Image1.Image.FullName}\",\"{m.Image2.Image.FullName}\",{m.Similarity}");
                        });
                        return;
                    }

                    if (options.Action == Models.Action.NoMatch)
                    {
                        LogService.Log("Filling no-match cache with current results due to user request", LogLevel.Warning);
                        CacheService.AddNoMatches(Matches);
                        return;
                    }

                    // just to be sure that files dont get processed on an unexpected action input
                    if (options.Action == Models.Action.Move || options.Action == Models.Action.Bin || options.Action == Models.Action.Delete) {
                        DeleteAction deleteAction;

                        switch (options.Action)
                        {
                            case Models.Action.Move:
                                deleteAction = DeleteAction.Move;
                                if (!options.Target.EndsWith("\\") && !options.Target.EndsWith("/"))
                                    options.Target += "\\";
                                break;
                            case Models.Action.Delete:
                                deleteAction = DeleteAction.Delete;
                                break;
                            default:
                                deleteAction = DeleteAction.RecycleBin;
                                break;
                        }

                        List<string> processedFiles = new();
                        Matches.ForEach(m =>
                        {
                            if(processedFiles.Any(p => p == m.Image1.Image.FullName || p == m.Image2.Image.FullName))
                                return;

                            for (int i = 0; i < options.Processors.Count(); i++)
                            {
                                int processingResult = AutoProcessorService.Processors.First(p => p.DisplayName == options.Processors.ElementAt(i)).Process(m.Image1.Image, m.Image2.Image);
                                if (processingResult != 0)
                                {
                                    try
                                    {
                                        ImageAnalysis? image = null;

                                        if (processingResult < 0)
                                            image = m.Image1;
                                        else if (processingResult > 0)
                                            image = m.Image2;
                                        
                                        if(image != null)
                                        {
                                            try
                                            {
                                                processedFiles.Add(image.Image.FullName);
                                                FileService.DeleteFile(image.Image.FullName, deleteAction, options.Target, !Path.IsPathRooted(options.Target));
                                            }
                                            catch { }
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        LogService.Log($"Error Auto-Processing current match: '{m.Image1.Image.FullName}' - '{m.Image2.Image.FullName}'", LogLevel.Error);
                                    }
                                }
                            }
                        });
                    }

                } catch(Exception)
                {
                    LogService.Log("Unexpected Error", LogLevel.Error);
                    exit = ExitCode.GeneralError;
                }

            })
            .WithNotParsed(errors =>
            {
                Console.WriteLine(
                    HelpText.AutoBuild(parser, h =>
                    {
                        h.AddEnumValuesToHelpText = false;
                        return h;
                    })
                    .AddPreOptionsLines(preOptions)
                    .AddPostOptionsLines(postOptions)
                );

                if (!errors.Any(e => e.GetType() == typeof(HelpRequestedError)))
                {
                    exit = ExitCode.BadRequest;
                }
            });

            return (int)exit;
        }

        public static void OnProgress(object? sender, ImageComparerEventArgs e)
        {
            if (e.Target > 0)
            {
                progress ??= new();
                double percent = (double)decimal.Divide(e.Current, e.Target);
                progress.Report(percent);
                if(percent >= 1)
                {
                    progress?.Dispose();
                    progress = null;
                }
            }
        }

        /*
         * Log implementation for console output
         */
        private static void OnLog(object? sender, LogEventArgs logEvent)
        {
            if (logLevel <= logEvent.Log.LogLevel)
            {
                if (progress != null)
                {
                    progress.Dispose();
                    progress = null;
                }
                Console.Write($"{logEvent.Log.Time.ToLongTimeString()} {logEvent.Log.LogLevel} -> {ToLiteral(logEvent.Log.Text)}\n");
            }

            if(!force && logEvent.Log.LogLevel >= LogLevel.Warning)
            {
                if(logEvent.Log.LogLevel == LogLevel.Warning && exit != ExitCode.Warning)
                    exit = ExitCode.Warning;

                if (logEvent.Log.LogLevel == LogLevel.Error && exit != ExitCode.GeneralError)
                    Environment.Exit((int)ExitCode.GeneralError);
            }
        }

        private static string ToLiteral(string valueTextForCompiler)
        {
            return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(valueTextForCompiler, false);
        }
    }
}