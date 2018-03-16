﻿using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using McMaster.Extensions.CommandLineUtils;
using RobotPlusPlus.Compiling;
using RobotPlusPlus.Parsing;
using RobotPlusPlus.Tokenizing;
using RobotPlusPlus.Tokenizing.Tokens;

namespace RobotPlusPlus.CLI
{
	public class ReaderWriter
	{
		private readonly ProgramOptions options;

		private Token[] tokenizedCode;
		private Token[] parsedCode;
		private string compiledCode;
		private string sourceCode;
		private string sourceFile;
		private readonly IConsole console;

		public ReaderWriter(ProgramOptions options, IConsole console)
		{
			this.options = options;
			this.console = console;
		}

		public async Task ReadCodeFromFile()
		{
			sourceFile = options.Script;

			if (!File.Exists(sourceFile))
				throw new FileNotFoundException("Script file was not found!", sourceFile);

			string initVerb = $"Reading from file \"{(options.Verbose ? sourceFile : Path.GetFileName(sourceFile))}\"";
			const string onErrorVerb = "reading from file";

			(_, sourceCode) = await TryExecActionAsync(initVerb, onErrorVerb, async () => await File.ReadAllTextAsync(options.Script));
			sourceCode = ReplaceNewLines(sourceCode);
		}

		public static string ReplaceNewLines(string multiline)
		{
			return multiline.Replace("\r\n", "\n").Replace("\n\r", "\n").Replace("\r", "\n");
		}

		/// <exception cref="ParseException"></exception>
		public void TokenizeCode()
		{
			if (sourceCode == null)
				throw new InvalidOperationException("Code haven't been read yet!");

			if (!TryExecAction("Tokenizing code", "tokenization", () => Tokenizer.Tokenize(sourceCode),
				out tokenizedCode)) return;

			if (options.Verbose)
			{
				LogInfo("Colorized tokenized code:");
				PrettyConsoleWriter.WriteCodeToConsole(tokenizedCode, console);
			}
		}

		/// <exception cref="ParseException"></exception>
		public void CompileCode()
		{
			if (tokenizedCode == null)
				throw new InvalidOperationException("Code haven't been tokenized yet!");

			if (TryExecAction("Compiling code", "compilation", () => Parser.Parse(tokenizedCode), out parsedCode))
				TryExecAction("Parsing code", "parsing", () => Compiler.Compile(parsedCode), out compiledCode);
		}

		public void WriteCompiledToDestination()
		{
			if (compiledCode == null)
				throw new InvalidOperationException("Code haven't been compiled yet!");
		}

		private void LogInfo(string info)
		{
			console.ResetColor();
			console.ForegroundColor = ConsoleColor.Yellow;
			console.Write("[INFO] ");
			console.ForegroundColor = ConsoleColor.Gray;
			console.WriteLine(info);
			console.ResetColor();
		}

		private void LogError(string error)
		{
			console.ResetColor();
			console.ForegroundColor = ConsoleColor.Red;
			console.Write("[ERROR] ");
			console.ForegroundColor = ConsoleColor.White;
			console.WriteLine(error);
			console.ResetColor();
		}

		private async Task<(bool success, TOut value)> TryExecActionAsync<TOut>(string initVerb,
			string onErrorVerb, Func<Task<TOut>> action)
		{
			Stopwatch watch = Stopwatch.StartNew();

			void ReportTime(ConsoleColor color, string status)
			{
				console.ResetColor();
				console.ForegroundColor = color;
				console.Write(status);
				watch.Stop();
				console.ForegroundColor = ConsoleColor.DarkGray;
				console.WriteLine($" (Took {watch.ElapsedMilliseconds}ms)");
				console.ResetColor();
			}

			try
			{
				console.ResetColor();
				console.ForegroundColor = ConsoleColor.Cyan;
				console.Write("[TASK] ");
				console.ResetColor();
				console.Write($"{initVerb}... ");
				console.ResetColor();

				TOut value = await action();

				ReportTime(ConsoleColor.Green, "Done.");
				return (true, value);
			}
			catch (ParseException e)
			{
				ReportTime(ConsoleColor.Red, "Error!");
				
				LogError($"{(options.Verbose ? sourceFile : Path.GetFileName(sourceFile))}:{e.Line}: {e.Message}");

				PrettyConsoleWriter.WriteCodeHighlightError(sourceCode, e, options.Verbose ? -1 : 5, console);

				console.ResetColor();
				return (false, default);
			}
			catch (Exception e)
			{
				ReportTime(ConsoleColor.Red, "Error!");
				Console.WriteLine();
				LogError($"Unexpected error during {onErrorVerb}!");
				Console.WriteLine();
#if DEBUG
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(e);
				Console.ResetColor();
#endif
				throw;
			}
		}

		private bool TryExecAction<TOut>(string initVerb, string onErrorVerb, Func<TOut> action, out TOut value)
		{
			bool success;
			(success, value) = TryExecActionAsync(initVerb, onErrorVerb, () => Task.FromResult(action())).Result;
			return success;
		}

	}
}