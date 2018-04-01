﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RobotPlusPlus.Core.Exceptions;
using RobotPlusPlus.Core.Structures;
using RobotPlusPlus.Core.Structures.G1ANT;
using RobotPlusPlus.Core.Tokenizing.Tokens;
using RobotPlusPlus.Core.Utility;

namespace RobotPlusPlus.Core.Compiling.CodeUnits
{
	public class CommandUnit : CodeUnit
	{
		public List<Argument> Arguments { get; }

		public string CommandName { get; }
		public string CommandFamilyName { get; }

		public G1ANTRepository.CommandFamilyElement CommandFamilyElement { get; private set; }
		public G1ANTRepository.CommandElement CommandElement { get; private set; }
		public List<G1ANTRepository.ArgumentElement> CommandArgumentElements { get; private set; }

		public CommandUnit([NotNull] FunctionCallToken token, [CanBeNull] CodeUnit parent = null) : base(token, parent)
		{
			Arguments = SplitArguments(token.ParentasesGroup);

			switch (token.LHS)
			{
				case IdentifierToken id:
					CommandName = id.SourceCode;
					CommandFamilyName = null;
					break;

				case PunctuatorToken dot when dot.PunctuatorType == PunctuatorToken.Type.Dot:
					if (dot.TryFirstRecursive(t => !(t is IdentifierToken), out Token faulty))
						throw new CompileException($"Dot operation for non-identifier token <{faulty?.SourceCode.EscapeString() ?? "null"}> are not supported!", faulty);

					if (PunctuatorToken.IsPunctuatorOfType(dot.DotLHS, PunctuatorToken.Type.Dot))
						throw new CompileException("Multidepth command family names are not supported!", dot.DotLHS);

					CommandFamilyName = dot.DotLHS.SourceCode;
					CommandName = dot.DotRHS.SourceCode;
					break;

				default:
					throw new CompileUnexpectedTokenException(token.LHS);
			}
		}

		public override void PreCompile(Compiler compiler)
		{
			foreach (Argument argument in Arguments)
				argument.expression.PreCompile(compiler);
		}

		public override void PostCompile(Compiler compiler)
		{
			foreach (Argument argument in Arguments)
				argument.expression.PostCompile(compiler);
		}

		public override void Compile(Compiler compiler)
		{
			foreach (Argument argument in Arguments)
				argument.expression.Compile(compiler);

			CommandFamilyElement = CommandFamilyName == null
				? null
				: compiler.G1ANTRepository.FindCommandFamily(CommandFamilyName)
				  ?? throw new CompileFunctionException($"Command family <{CommandFamilyName}> does not exist!", Token);

			CommandElement = CommandFamilyName == null
				? compiler.G1ANTRepository.FindCommand(CommandName)
				  ?? throw new CompileFunctionException($"Command <{CommandName}> does not exist!", Token)
				: compiler.G1ANTRepository.FindCommand(CommandName, CommandFamilyName)
				  ?? throw new CompileFunctionException($"Command <{CommandName}> in family <{CommandFamilyName}> does not exist!", Token);

			CommandArgumentElements = compiler.G1ANTRepository.ListCommandArguments(CommandElement);

			for (var i = 0; i < Arguments.Count; i++)
			{
				// Convert positional to named
				if (Arguments[i] is PositionalArgument pos)
				{
					G1ANTRepository.ArgumentElement arg = CommandArgumentElements.TryGet(pos.index)
														  ?? throw new CompileFunctionException(
															  $"Command <{CommandName}> can at max take {CommandArgumentElements.Count} arguments!",
															  Token);

					Arguments[i] = new NamedArgument(arg.Name, Arguments[i].expression);
				}
				else if (Arguments[i] is NamedArgument named)
				{
					// Validate its name
					if (CommandArgumentElements.All(a => a.Name != named.name))
						throw new CompileFunctionException($"Command <{CommandName}> does not have a parameter named <{named.name}>.", Token);
				}
				else
					throw new InvalidOperationException();
			}

			// Validate all required ones
			var missingReqArgs = new List<string>();

			foreach (List<G1ANTRepository.ArgumentElement> group in CommandElement.RequiredArguments)
			{
				int matches = Arguments.OfType<NamedArgument>().Count(a => group.Any(g => g.Name == a.name));

				string displayName = group.Count == 1
					? $"<{group[0].Name}>"
					: string.Join(" or ", group.Select(a => $"<{a.Name}>"));

				if (matches == 0)
					missingReqArgs.Add(displayName);
				else if (matches > 1)
					throw new CompileFunctionException($"Command <{CommandName}> requires single value amoung arguments {displayName}.", Token);
			}

			if (missingReqArgs.Count == 1)
				throw new CompileFunctionException($"Command <{CommandName}> requires argument {missingReqArgs[0]}.", Token);
			if (missingReqArgs.Count > 1)
				throw new CompileFunctionException($"Command <{CommandName}> requires arguments {string.Join("; ",missingReqArgs)}.", Token);
		}

		public override string AssembleIntoString()
		{
			var row = new RowBuilder();

			// Pre units
			foreach (Argument argument in Arguments)
				foreach (CodeUnit preUnit in argument.expression.PreUnits)
					row.AppendLine(preUnit.AssembleIntoString());

			// Assemble command with arguments
			var cmd = new StringBuilder();
			cmd.Append(CommandFamilyName == null ? CommandName : $"{CommandFamilyName}.{CommandName}");

			foreach (NamedArgument argument in Arguments.OfType<NamedArgument>())
			{
				cmd.AppendFormat(" {0} {1}", argument.name, argument.expression.AssembleIntoString());
			}

			row.AppendLine(cmd.ToString());

			// Post units
			foreach (Argument argument in Arguments)
				foreach (CodeUnit postUnit in argument.expression.PostUnits)
					row.AppendLine(postUnit.AssembleIntoString());

			return row.ToString();
		}

		private List<Argument> SplitArguments(PunctuatorToken parentases)
		{
			var named = false;
			var arguments = new List<Argument>();

			var iterator = new IteratedList<Token>(parentases);
			foreach (Token token in iterator)
			{
				// Check if named or positional
				if (token is PunctuatorToken pun
					&& pun.PunctuatorType == PunctuatorToken.Type.Colon)
				{
					if (iterator.Next == null
						|| PunctuatorToken.IsSeparatorOfChar(iterator.Next, ','))
						throw new CompileFunctionException(
							$"Missing expression for named parameter <{pun.ColonName.SourceCode}>.", token);

					arguments.Add(new NamedArgument(pun.ColonName, new ExpressionUnit(iterator.PopNext(), this)));
					named = true;
				}
				else
				{
					if (named)
						throw new CompileFunctionException("Positional argument cannot precede named argument.", token);

					arguments.Add(new PositionalArgument(arguments.Count, new ExpressionUnit(token, this)));
				}

				// Check for comma seperators
				if (PunctuatorToken.IsSeparatorOfChar(iterator.Next, ','))
				{
					if (iterator.Count - iterator.Index == 2)
						throw new CompileFunctionException("Unexpected separator <,> with no followup parameter.", iterator.Next);

					// Remove the comma
					iterator.PopNext();
				}
			}

			return arguments;
		}

		#region Argument classes

		public abstract class Argument
		{
			public readonly ExpressionUnit expression;

			protected Argument(ExpressionUnit expression)
			{
				this.expression = expression;
			}
		}

		public class NamedArgument : Argument
		{
			public readonly string name;
			public readonly Token nameToken;

			public NamedArgument(Token nameToken, ExpressionUnit expression)
				: base(expression)
			{
				this.nameToken = nameToken;
				name = nameToken.SourceCode;
			}

			public NamedArgument(string name, ExpressionUnit expression)
				: base(expression)
			{
				this.name = name;
			}
		}

		public class PositionalArgument : Argument
		{
			public readonly int index;

			public PositionalArgument(int index, ExpressionUnit expression)
				: base(expression)
			{
				this.index = index;
			}
		}

		#endregion
	}
}