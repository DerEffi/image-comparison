using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;

namespace ImageComparison.Models
{
    public class Options
    {
        [Option('a', "action", Required = true, HelpText = @"Action to perform on found matches for the specified settings
    Search    : Search matches and return as list
    Move      : Moves matches into target folder
    Bin       : Moves matches to bin
                Deletes if no bin exists
    Delete    : Deletes matches permanently
    NoMatch   : Flags matches in the cache
                as incorrect. They wont be deleted/shown in future executions by using the specified cache")]
        public Action Action { get; set; } = Action.Search;

        [Option('l', "locations", Required = false, HelpText = "List of locations to process")]
        public List<string> Locations { get; set; } = new();

        [Option('m', "mode", Required = false, HelpText = @"Search mode, which images to compare to another

    All             : compare all images (default
    Inclusive       : compare only within directories
    Exclusive       : compare only in different directories
    ListInclusive   : compare only within the same
                      search location (top directory)
    ListExclusive   : compare only in different
                      search locations (top diretories)")]
        public SearchMode SearchMode { get; set; } = SearchMode.All;

        [Option('r', "recursive", Required = false, HelpText = "Search in all Subdirectories")]
        public bool Recursive { get; set; } = false;

        [Option('s', "similarity", Required = false, HelpText = "Minimum Similarity to treat as a match (0 - 10000) (default: 7500)")]
        public int Similarity { get; set; } = 7500;

        [Option('d', "detail", Required = false, HelpText = "Detail for the generation of the comparison hashes (default: 8)\nFor DHashes recommended above 16")]
        public int HashDetail { get; set; } = 8;

        [Option('h', "hash", Required = false, HelpText = @"Hash Algorithm used for comparison
    PHash         : most accurate (default)
    DHashDouble   : compromise
    DHash         : fastest")]
        public HashAlgorithm HashAlgorithm { get; set; } = HashAlgorithm.PHash;

        [Option('o', "output", Required = false, HelpText = @"Output level, to use the output from search mode recommended to error or quiet
    Info      : Displays all logs (default)
    Warning   : Displays only warn and error messages
    Error     : Displays only error messages
                Will still exit with the respective codes on warn and error
    Quiet     : Supresses all logs except search result
                Will still exit with the respective codes on warn and error")]
        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        [Option('t', "target", Required = false, HelpText = "Target folder to move images to")]
        public string Target { get; set; } = "";

        [Option('c', "cache", Required = false, HelpText = "Cache file for analysed images\nor nomatches")]
        public string Cache { get; set; } = "";

        [Option('f', "force", Required = false, HelpText = "Force execution and ignore errors")]
        public bool Force { get; set; } = false;

    }
}
