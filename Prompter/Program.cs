using DiffPatch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using SharpToken;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text.Json;

namespace Prompter;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var apiKey = Environment.GetEnvironmentVariable("PROMPTER_OPENAI_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("Please set the PROMPTER_OPENAI_KEY environment variable with your OpenAI API key.");
        }

        Option<FileInfo> promptOption = new("--prompt")
        {
            Description = "Path to the prompt file to execute.",
            Required = true,
        };

        Option<string> modelIdOption = new("--modelId")
        {
            Description = "The id of the model to use default 'gpt-5'.",
            DefaultValueFactory = result => "gpt-5"
        };

        Option<DirectoryInfo> diffOption = new("--diffs")
        {
            Description = "Path to the folder that contains all the diffs to bind with the prompt file.",
            Required = true,
        };

        Option<DirectoryInfo> outOption = new("--output-dir")
        {
            Description = "Path to the folder that contains all the diffs to bind with the prompt file. Default is '.\\out.'",
            DefaultValueFactory = result => new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "out")),
        };

        RootCommand rootCommand = new("Console app to execute a prompt file against a set of diffs");
        rootCommand.Options.Add(promptOption);
        rootCommand.Options.Add(modelIdOption);
        rootCommand.Options.Add(diffOption);
        rootCommand.Options.Add(outOption);

        ParseResult parseResult = rootCommand.Parse(args);
        if (parseResult.Errors.Count == 0
                && parseResult.GetValue(promptOption) is FileInfo promptTemplateFile
                && parseResult.GetValue(modelIdOption) is string modelId
                && parseResult.GetValue(diffOption) is DirectoryInfo diffDirPath
                && parseResult.GetValue(outOption) is DirectoryInfo outDirPath)
        {
            if(!File.Exists(promptTemplateFile.FullName))
            {
                throw new Exception($"Prompt file '{promptTemplateFile.FullName}' does not exist.");
            }

            if (!Directory.Exists(diffDirPath.FullName))
            {
                throw new Exception($"Input directory '{diffDirPath.FullName}' does not exist.");
            }

            if (!outDirPath.Exists)
            {
                outDirPath.Create();
            }

            var promptTemplate = File.ReadAllText(promptTemplateFile.FullName);

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatClient(modelId, apiKey);
            var kernel = builder.Build();

            var summarize = kernel.CreateFunctionFromPrompt(promptTemplate, executionSettings: new OpenAIPromptExecutionSettings { });
            var stopwatch = new System.Diagnostics.Stopwatch();
            var diffFiles = Directory.GetFiles(diffDirPath.FullName, "*.diff");

            foreach (var diffFile in diffFiles)
            {
                var diffs = GetDiffContent(diffFile);
                foreach (var diff in diffs)
                {
                    Console.WriteLine($"Processing {diff.Filename}");

                    stopwatch.Restart();
                    var response = (kernel.InvokeAsync(summarize, new() { ["oldCode"] = diff.OldCode, ["newCode"] = diff.NewCode })).GetAwaiter().GetResult();
                    stopwatch.Stop();

                    var totalTokens = 0;
                    if (response.Metadata != null && response.Metadata.TryGetValue("Usage", out object? usageObject))
                    {
                        var usage = usageObject as ChatTokenUsage;
                        if (usage != null)
                        {
                            totalTokens = usage.TotalTokenCount;
                        }
                    }
                    else
                    {
                        var encoding = GptEncoding.GetEncoding("cl100k_base");
                        var tokens = encoding.Encode(string.Join(response.RenderedPrompt, Environment.NewLine, response));
                        totalTokens = tokens.Count;
                    }

                    var promptResponse = new PromptResponse
                    {
                        PromptText = promptTemplate,
                        ResponseText = response.ToString(),
                        Input = diff,
                        ModelId = modelId,
                        TokensUsed = totalTokens,
                        TimeTakenMs = stopwatch.ElapsedMilliseconds
                    };

                    var resultFile = $"Result-{Path.GetFileNameWithoutExtension(promptTemplateFile.FullName)}-{diff.Filename}.json";
                    File.WriteAllText(Path.Combine(outDirPath.FullName, resultFile), JsonSerializer.Serialize(promptResponse, new JsonSerializerOptions { WriteIndented = true }));
                    Console.WriteLine($"Procesed {diff.Filename} used {totalTokens} tokens and took {stopwatch.ElapsedMilliseconds}ms");
                }
            }
        }
        foreach (ParseError parseError in parseResult.Errors)
        {
            Console.Error.WriteLine(parseError.Message);
        }
        return 0;
    }

    private static List<MiniDiff> GetDiffContent(string DiffFilePath)
    {
        var results = new List<MiniDiff>();
        var diffContent = File.ReadAllText(DiffFilePath);
        var files = DiffParserHelper.Parse(diffContent, Environment.NewLine);

        var index = 0;
        foreach (var file in files)
        {
            var diff = new MiniDiff(); 
            diff.Filename = $"File-{index}-{Path.GetFileName(DiffFilePath)}";
            diff.OldCode = file.From;
            diff.NewCode = file.To;

            results.Add(diff);
            index++;
        }
        return results;
    }

    private class MiniDiff
    {
        public string Filename { get; set; }
        public string OldCode { get; set; }
        public string NewCode { get; set; }
    }

    private class PromptResponse
    {
        public string PromptText { get; set; }
        public string ResponseText { get; set; }
        public MiniDiff Input { get; set; }
        public string ModelId { get; set; }
        public long TokensUsed { get; set; }
        public long TimeTakenMs { get; set; }

    }
}