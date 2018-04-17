﻿using System.Collections.Generic;
using JetBrains.Annotations;
using RobotPlusPlus.Core.Compiling.Context;
using RobotPlusPlus.Core.Compiling.Context.Types;
using RobotPlusPlus.Core.Exceptions;
using RobotPlusPlus.Core.Parsing;
using RobotPlusPlus.Core.Structures;
using RobotPlusPlus.Core.Tokenizing.Tokens;

namespace RobotPlusPlus.Core.Compiling.CodeUnits
{
	public class AssignmentUnit : CodeUnit
	{
		public ExpressionUnit Expression { get; }
		public IdentifierToken VariableOriginalToken { get; }
		public Variable VariableGenerated { get; private set; }

		public AssignmentUnit([NotNull] OperatorToken token, [CanBeNull] CodeUnit parent = null)
			: base(token, parent)
		{
			if (token.OperatorType != OperatorToken.Type.Assignment)
				throw new CompileUnexpectedTokenException(token);

			if (!(token.LHS is IdentifierToken id))
				throw new CompileUnexpectedTokenException(token);

			Expression = new ExpressionUnit(token.RHS, this);
			VariableOriginalToken = id;
		}

		public static (CodeUnit, IdentifierTempToken) CreateTemporaryAssignment([NotNull] Token RHS, [CanBeNull] CodeUnit parent = null)
		{
			// Fabricate tokens
			TokenSource source = RHS.source;
			source.code = "=";
			var op = new OperatorToken(source);

			source.code = string.Empty;
			var id = new IdentifierTempToken(source);

			// Fabricate environment
			var env = new IteratedList<Token>(new List<Token>
			{
				id, op, RHS
			});

			// Parse
			env.ParseTokenAt(1);

			// Fabricate assignment unit
			CodeUnit tempUnit = CompileParsedToken(op, parent);

			return (tempUnit, id);
		}

		public override void Compile(Compiler compiler)
		{
			Expression.Compile(compiler);
			
			// Register variable, or use already registered
			VariableGenerated = compiler.Context.FindVariable(VariableOriginalToken)
				?? compiler.Context.RegisterVariable(VariableOriginalToken, Expression.OutputType);

			if (!TypeChecking.CanImplicitlyConvert(Expression.OutputType, VariableGenerated.Type))
				throw new CompileTypeConvertImplicitAssignmentException(VariableOriginalToken, Expression.OutputType, VariableGenerated.Type);
		}

		public override string AssembleIntoString()
		{
			var rows = new RowBuilder();

			foreach (CodeUnit pre in Expression.PreUnits)
			{
				rows.AppendLine(pre.AssembleIntoString());
			}

			rows.AppendLine("♥{0}={1}", VariableGenerated.Generated, Expression.AssembleIntoString());

			foreach (CodeUnit post in Expression.PostUnits)
			{
				rows.AppendLine(post.AssembleIntoString());
			}

			return rows.ToString();
		}
	}
}