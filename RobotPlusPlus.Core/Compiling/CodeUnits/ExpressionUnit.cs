﻿using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using RobotPlusPlus.Core.Compiling.CodeUnits.ControlFlow;
using RobotPlusPlus.Core.Compiling.Context;
using RobotPlusPlus.Core.Compiling.Context.Types;
using RobotPlusPlus.Core.Exceptions;
using RobotPlusPlus.Core.Structures;
using RobotPlusPlus.Core.Tokenizing.Tokens;
using RobotPlusPlus.Core.Tokenizing.Tokens.Literals;
using RobotPlusPlus.Core.Utility;

namespace RobotPlusPlus.Core.Compiling.CodeUnits
{
	public class ExpressionUnit : CodeUnit
	{
		public FlexibleList<CodeUnit> PreUnits { get; }
		public FlexibleList<CodeUnit> PostUnits { get; }

		/// <summary>Value type from this expression</summary>
		public AbstractValue OutputType { get; private set; }
		/// <summary>Inbound type when this is LHS of an assignment</summary>
		public AbstractValue InputType { get; internal set; }
		public bool NeedsCSSnippet { get; set; }
		public UsageType Usage { get; }

		/// <summary>
		/// <para>Used upon assigning to structs and classes.</para>
		///
		/// Example: rect.width = 10
		/// where this="width"
		/// and Container="rect"
		/// </summary>
		public Token ContainerToken { get; private set; }
		public AbstractValue ContainerType { get; private set; }

		public Dictionary<IdentifierToken, Type> StaticVariables { get; private set; }
			= new Dictionary<IdentifierToken, Type>();

		public enum UsageType
		{
			Read,
			Write
		}

		public ExpressionUnit([NotNull] Token token, [CanBeNull] CodeUnit parent = null, UsageType usage = UsageType.Read)
			: base(token, parent)
		{
			Usage = usage;

			PreUnits = new FlexibleList<CodeUnit>();
			PostUnits = new FlexibleList<CodeUnit>();

			token = RemoveParentases(token);
			token = RemoveUnaries(token);
			Token = ExtractInnerAssignments(token);
		}

		public override void Compile(Compiler compiler)
		{
			NeedsCSSnippet = false;

			foreach (CodeUnit pre in PreUnits)
				pre.Compile(compiler);

			OutputType = EvalTokenType(Token, compiler, Usage, InputType, out bool needsCsSnippet);
			NeedsCSSnippet = needsCsSnippet;

			// Keep track of static vars
			StaticVariables.Clear();
			Token.ForEachRecursive(token =>
			{
				if (!(token is IdentifierToken id)) return;
				var variable = compiler.Context.FindIdentifier(id) as Variable;
				if (variable?.IsStaticType == true)
					StaticVariables[id] = variable.Type;
			}, true);

			if (Usage == UsageType.Write)
			{
				ContainerToken = GetLeftmostToken(Token);
				ContainerType = EvalTokenReadType(ContainerToken, compiler);

				if (!(InputType is CSharpType csInput) || !(OutputType is CSharpType csOutput))
					throw new CompileTypeConvertImplicitAssignmentException(Token, InputType.GetType(), OutputType.GetType());

				if (!TypeChecking.CanImplicitlyConvert(csInput.Type, csOutput.Type))
					throw new CompileTypeConvertImplicitAssignmentException(Token, csInput.Type, csOutput.Type);
			}

			foreach (CodeUnit post in PostUnits)
				post.Compile(compiler);
		}

		private static Token GetLeftmostToken(Token token)
		{
			while (true)
			{
				if (!(token is PunctuatorToken pun) || pun.PunctuatorType != PunctuatorToken.Type.Dot)
					return token;

				token = pun.DotLHS;
			}
		}

		public override string AssembleIntoString()
		{
			return NeedsCSSnippet && !(Parent is AbstractFlowUnit)
				? $"⊂{StringifyToken(Token)}⊃"
				: StringifyToken(Token);
		}

		#region Public utility

		public (ExpressionUnit, IdentifierTempToken) ExtractIntoTempAssignment()
		{
			(CodeUnit tempAssignment, IdentifierTempToken id)
				= AssignmentUnit.CreateTemporaryAssignment(Token, Parent);

			var exp = new ExpressionUnit(id, Parent);

			// Old preunits
			foreach (CodeUnit pre in PreUnits)
				exp.PreUnits.Add(pre);
			// Temp assignment
			exp.PreUnits.Add(tempAssignment);
			// Old postunits
			foreach (CodeUnit post in PostUnits)
				exp.PreUnits.Add(post);

			return (exp, id);
		}

		[CanBeNull, Pure]
		public static AbstractValue EvalTokenReadType([NotNull] Token token, [NotNull] Compiler compiler)
		{
			return EvalTokenType(token, compiler, UsageType.Read, null, out bool _);
		}

		[CanBeNull]
		public static AbstractValue EvalTokenWriteType([NotNull] Token token, [NotNull] Compiler compiler, [NotNull] AbstractValue inputType)
		{
			return EvalTokenType(token, compiler, UsageType.Write, inputType, out bool _);
		}

		[NotNull]
		private static AbstractValue EvalTokenType([NotNull] Token token, [NotNull] Compiler compiler, UsageType usage, AbstractValue inputType, out bool needsCSSnippet)
		{
			bool csSnippet = false, containsStr = false, containsOp = false;

			AbstractValue type = RecursiveCheck(token);

			needsCSSnippet = usage == UsageType.Read && (csSnippet || (containsStr && containsOp));
			return type;

			AbstractValue RecursiveCheck(Token t)
			{
				switch (t)
				{
					case IdentifierToken id:
						// Check variables for registration
						AbstractValue value = compiler.Context.FindIdentifier(id);
						
						if (value == null && inputType is CSharpType csType && csType.Type != null)
						{
							if (usage == UsageType.Write)
								value = compiler.Context.RegisterVariable(id, csType.Type);
							else
								throw new CompileVariableUnassignedException(id);
						}

						if (value is Variable variable)
						{
							if (usage == UsageType.Write && variable.IsReadOnly)
								throw new CompileTypeReadOnlyException(variable.Token);

							if (variable.IsStaticType)
								csSnippet = true;

							// Check generated name
							if (string.IsNullOrEmpty(id.GeneratedName))
							{
								if (id is IdentifierTempToken)
									throw new CompileException("Name not generated for temporary variable.", id);
								throw new CompileException($"Name not generated for variable <{id.Identifier}>.", id);
							}

							if (variable.Type == typeof(string))
								containsStr = true;
						}
						return value;

					case LiteralNumberToken num:
						if (usage == UsageType.Write)
							throw new CompileExpressionCannotAssignException(num);
						return new CSharpType(num.Value.GetType());

					case LiteralStringToken str:
						if (usage == UsageType.Write)
							throw new CompileExpressionCannotAssignException(str);
						containsStr = true;
						if (str.NeedsEscaping) csSnippet = true;
						return new CSharpType(typeof(string));

					case LiteralKeywordToken key:
						if (usage == UsageType.Write)
							throw new CompileExpressionCannotAssignException(key);
						return new CSharpType(key.Value?.GetType());

					case PunctuatorToken pun when pun.PunctuatorType == PunctuatorToken.Type.Dot:
						string identifier = pun.DotRHS.Identifier;
						AbstractValue lhs = RecursiveCheck(pun.DotLHS)
								   ?? throw new CompileException($"Unvaluable property from dot LHS, <{pun.DotLHS}>.", pun.DotLHS);

						if (lhs is CSharpType lhsCS)
						{
							if (lhsCS.Type == null)
								return lhsCS;

							BindingFlags flags = BindingFlags.Instance
							                     | BindingFlags.Public
							                     | BindingFlags.FlattenHierarchy;

							// If base is identifier...
							if (GetLeftmostToken(pun) is IdentifierToken baseType)
							{
								// And its static..
								var baseVariable = compiler.Context.FindIdentifier(baseType) as Variable;
								if (baseVariable?.IsStaticType == true)
								{
									// Change to search for static fields
									flags &= ~BindingFlags.Instance;
									flags |= BindingFlags.Static;
								}
							}

							// Do the search
							MemberInfo memberInfo = (MemberInfo) lhsCS.Type.GetProperty(identifier, flags)
							                        ?? lhsCS.Type.GetField(identifier, flags);

							// Validate
							if (memberInfo == null)
								throw new CompileTypePropertyDoesNotExistException(pun, lhsCS.Type, identifier);
							if (usage == UsageType.Read && !memberInfo.CanRead())
								throw new CompileTypePropertyNoGetterException(pun, lhsCS.Type, identifier);
							if (usage == UsageType.Write && !memberInfo.CanWrite())
								throw new CompileTypePropertyNoSetterException(pun, lhsCS.Type, identifier);

							csSnippet = true;
							return new CSharpType(memberInfo.GetValueType());
						}

						return lhs;

					case OperatorToken op when op.OperatorType == OperatorToken.Type.Unary:
						if (usage == UsageType.Write)
							throw new CompileExpressionCannotAssignException(op);
						containsOp = true;

						AbstractValue valType = RecursiveCheck(op.UnaryValue);
						if (valType is CSharpType valCSharp)
							return new CSharpType(op.EvaluateType(valCSharp.Type));

						throw new CompileTypeInvalidOperationException(op, valType.GetType());

					case OperatorToken op:
						if (usage == UsageType.Write)
							throw new CompileExpressionCannotAssignException(op);
						containsOp = true;
						AbstractValue lhsType = RecursiveCheck(op.LHS);
						AbstractValue rhsType = RecursiveCheck(op.RHS);

						if (lhsType is CSharpType lhsCSharp
						&& rhsType is CSharpType rhsCSharp)
							return new CSharpType(op.EvaluateType(lhsCSharp.Type, rhsCSharp.Type));

						throw new CompileTypeInvalidOperationException(op, lhsType.GetType(), rhsType.GetType());
				}

				throw new CompileUnexpectedTokenException(t);
			}
		}

		#endregion

		#region Stringify expression tokens

		public string StringifyToken(Token token)
		{
			switch (token)
			{
				case LiteralStringToken str:
					return NeedsCSSnippet
						? $"\"{str.Value.EscapeString()}\""
						: $"‴{str.Value}‴";

				case LiteralNumberToken num:
					return num.AssembleIntoString();

				case LiteralKeywordToken _:
					return token.SourceCode;

				case IdentifierToken id:
					return StaticVariables.TryGetValue(id, out Type type)
						? type.FullName
						: $"♥{id.GeneratedName}";

				case OperatorToken op when op.OperatorType == OperatorToken.Type.Assignment
					|| op.SourceCode == "++"
					|| op.SourceCode == "--":
					// Should've been extracted
					throw new CompileUnexpectedTokenException(token);

				case OperatorToken op when op.LHS != null && op.RHS != null:
					return $"{StringifyOperatorChildToken(op, op.LHS)}{op.SourceCode}{StringifyOperatorChildToken(op, op.RHS)}";

				case OperatorToken op when op.OperatorType == OperatorToken.Type.Unary:
					return $"{op.SourceCode}{StringifyOperatorChildToken(op, op.UnaryValue)}";

				case PunctuatorToken pun when pun.PunctuatorType == PunctuatorToken.Type.Dot:
					return $"{StringifyToken(pun.DotLHS)}.{pun.DotRHS}";

				default:
					throw new CompileUnexpectedTokenException(token);
			}
		}

		private string StringifyOperatorChildToken(OperatorToken parent, Token child)
		{
			if (child is OperatorToken op && op.OperatorType > parent.OperatorType)
				return $"({StringifyToken(child)})";

			return StringifyToken(child);
		}

		#endregion

		#region Construction alterations

		private Token ExtractInnerAssignments(Token token, Token parent = null)
		{
			// Convert command call to assignment
			if (token is FunctionCallToken
			&& parent != null)
			{
				(CodeUnit unit, IdentifierTempToken temp) = AssignmentUnit.CreateTemporaryAssignment(token, this);
				PreUnits.Add(unit);
				token = temp;
			}

			// Convert prefix expressions
			if (token is OperatorToken pre && pre.OperatorType == OperatorToken.Type.PreExpression)
			{
				PreUnits.Add(CompileParsedToken(pre, this));
				token = pre.UnaryValue;
			}

			// Convert postfix expressions
			if (token is OperatorToken post && post.OperatorType == OperatorToken.Type.PostExpression)
			{
				PostUnits.Add(CompileParsedToken(post, this));
				token = post.UnaryValue;
			}

			// Convert assignment to expression
			if (token is OperatorToken op
				&& op.OperatorType == OperatorToken.Type.Assignment)
			{
				PreUnits.Add(CompileParsedToken(op, this));
				token = op.LHS;
			}

			// Run on childs
			for (var i = 0; i < token.Count; i++)
			{
				token[i] = ExtractInnerAssignments(token[i], token);
			}

			// Returns altered
			return token;
		}

		public static Token RemoveParentases(Token token, Token parent = null)
		{
			Repeat:

			if (token is PunctuatorToken pun
				&& pun.PunctuatorType == PunctuatorToken.Type.OpeningParentases
				&& pun.Character == '('
				&& !(parent is FunctionCallToken))
			{
				if (pun.Count != 1)
					throw new CompileIncorrectTokenCountException(1, pun);

				token = token[0];
				goto Repeat;
			}

			for (var i = 0; i < token.Count; i++)
			{
				token[i] = RemoveParentases(token[i], token);
			}

			return token;
		}

		public static Token RemoveUnaries(Token token, Token parent = null)
		{
			Repeat:

			if (token is OperatorToken op
				&& op.OperatorType == OperatorToken.Type.Unary)
			{
				// Remove + unary
				if (op.SourceCode == "+")
				{
					token = op.UnaryValue;
					goto Repeat;
				}

				// Remove double unary -(-x), !!x, ~~x
				if (op.UnaryValue is OperatorToken op2
					&& op.OperatorType == op2.OperatorType
					&& op.SourceCode == op2.SourceCode)
				{
					token = op2.UnaryValue;
					goto Repeat;
				}
			}

			for (var i = 0; i < token.Count; i++)
			{
				token[i] = RemoveUnaries(token[i], token);
			}

			return token;
		}

		#endregion

	}
}