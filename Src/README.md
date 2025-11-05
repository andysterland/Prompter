# Prompter

A console application that executes prompt templates against code diffs using OpenAI's language models. This tool is designed to analyze code changes and generate AI-powered insights, summaries, or reviews based on custom prompts.

## Features

- Execute custom prompt templates against code diffs
- Support for multiple diff files in a single run
- Configurable OpenAI model selection
- Detailed output with token usage and performance metrics
- JSON output format for easy integration with other tools

## Prerequisites

- .NET 10.0 SDK or later
- An OpenAI API key

## Installation

1. Clone the repository:
```bash
git clone https://github.com/andysterland/Prompter
cd Prompter
```

2. Build the project:
```bash
dotnet build
```

3. Set up your OpenAI API key:

**Windows (PowerShell):**
```powershell
$env:PROMPTER_OPENAI_KEY="your-api-key-here"
```

**Windows (Command Prompt):**
```cmd
set PROMPTER_OPENAI_KEY=your-api-key-here
```

**Linux/MacOS:**
```bash
export PROMPTER_OPENAI_KEY="your-api-key-here"
```

## Usage

### Basic Command Structure

```bash
dotnet run --project Prompter -- --prompt <path-to-prompt-file> --diffs <path-to-diffs-folder> [OPTIONS]
```

### Required Options

- `--prompt` : Path to the prompt template file (required)
- `--diffs` : Path to the folder containing .diff files (required)

### Optional Options

- `--modelId` : The OpenAI model to use (default: `gpt-5`)
- `--output-dir` : Path to the output directory (default: `.\out`)

### Example Commands

**Basic usage with defaults:**
```bash
dotnet run --project Prompter -- --prompt prompts/review.txt --diffs ./diffs
```

**Specify a custom model:**
```bash
dotnet run --project Prompter -- --prompt prompts/review.txt --diffs ./diffs --modelId gpt-4
```

**Custom output directory:**
```bash
dotnet run --project Prompter -- --prompt prompts/review.txt --diffs ./diffs --output-dir ./results
```

**Full example:**
```bash
dotnet run --project Prompter -- --prompt prompts/summarize.txt --diffs ./code-changes --modelId gpt-4-turbo --output-dir ./analysis-results
```

## Prompt Template Format

Create a text file with your prompt template. Use placeholders `{{oldCode}}` and `{{newCode}}` to reference the code before and after changes:

**Example prompt template (review.txt):**
```
Please review the following code change and provide feedback:

OLD CODE:
{{oldCode}}

NEW CODE:
{{newCode}}

Provide a summary of:
1. What changed
2. Potential issues or bugs
3. Suggestions for improvement
```

## Diff File Format

The application expects standard unified diff format (.diff files). You can generate these using Git:

```bash
# Generate a diff for staged changes
git diff --cached > changes.diff

# Generate a diff for all uncommitted changes
git diff > changes.diff

# Generate a diff between commits
git diff commit1 commit2 > changes.diff
```

## Output Format

The application generates JSON files in the output directory with the following structure:

```json
{
  "PromptText": "The original prompt template",
  "ResponseText": "AI-generated response",
  "Input": {
    "Filename": "File-0-changes.diff",
    "OldCode": "Original code content",
    "NewCode": "Modified code content"
  },
  "ModelId": "gpt-4",
  "TokensUsed": 1234,
  "TimeTakenMs": 5678
}
```

Output files are named using the pattern:
```
Result-{PromptFileName}-{DiffFileName}.json
```

## Example Workflow

1. **Create a prompt template:**
```bash
echo "Summarize the changes in the following code:\n\nOLD:\n{{oldCode}}\n\nNEW:\n{{newCode}}" > prompts/summarize.txt
```

2. **Generate diffs from your Git repository:**
```bash
mkdir diffs
git diff HEAD~1 HEAD > diffs/recent-changes.diff
```

3. **Run Prompter:**
```bash
dotnet run --project Prompter -- --prompt prompts/summarize.txt --diffs ./diffs
```

4. **Review the results:**
```bash
cat out/Result-summarize-File-0-recent-changes.diff.json
```

## Performance Monitoring

The application tracks and reports:
- **Token usage**: Total tokens consumed per API call
- **Processing time**: Milliseconds taken for each diff analysis
- Console output shows real-time progress

Example console output:
```
Processing File-0-changes.diff
Processed File-0-changes.diff used 1234 tokens and took 5678ms
```

## Troubleshooting

### "Please set the PROMPTER_OPENAI_KEY environment variable"
Make sure you've set the environment variable with your OpenAI API key.

### "Prompt file does not exist"
Verify the path to your prompt template file is correct.

### "Input directory does not exist"
Ensure the diffs directory exists and contains .diff files.

### Model not found errors
Check that the specified model ID is valid and available with your OpenAI API key.

## Dependencies

- **Microsoft.SemanticKernel**: AI orchestration framework
- **DiffPatch**: Diff parsing library
- **SharpToken**: Token counting for OpenAI models
- **System.CommandLine**: Command-line parsing

## License

See LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
