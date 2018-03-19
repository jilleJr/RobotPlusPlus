﻿using System;
using System.Linq;
using RobotPlusPlus.Core.Compiling;
using RobotPlusPlus.Core.Exceptions;
using RobotPlusPlus.Core.Parsing;
using RobotPlusPlus.Core.Tokenizing.Tokens.Literals;
using RobotPlusPlus.Core.Utility;

namespace RobotPlusPlus.Core.Tokenizing.Tokens
{
	/// <summary>Assignment and comparisson. Ex: =, >, +</summary>
	public class Operator : Token
	{
		public Type OperatorType { get; }
		public Token LHS => this[_LHS];
		public Token RHS => this[_RHS];
		public const int _LHS = 0;
		public const int _RHS = 1;

		public Operator(TokenSource source) : base(source)
		{
			switch (SourceCode)
			{
				case "++":
				case "--":
					OperatorType = Type.Expression;
					break;

				case "!":
				case "~":
					OperatorType = Type.Unary;
					break;

				case "*":
				case "/":
				case "%":
					OperatorType = Type.Multiplicative;
					break;

				case "+":
				case "-":
					OperatorType = Type.Additive;
					break;

				case "<<":
				case ">>":
					OperatorType = Type.BitwiseShift;
					break;

				case ">=":
				case "<=":
				case ">":
				case "<":
					OperatorType = Type.Relational;
					break;

				case "==":
				case "!=":
					OperatorType = Type.Equality;
					break;

				case "&":
					OperatorType = Type.BitwiseAND;
					break;
				case "^":
					OperatorType = Type.BitwiseXOR;
					break;
				case "|":
					OperatorType = Type.BitwiseOR;
					break;

				case "&&":
					OperatorType = Type.BooleanAND;
					break;
				case "||":
					OperatorType = Type.BooleanAND;
					break;

				case "=":
				case "+=":
				case "-=":
				case "*=":
				case "/=":
				case "%=":
				case "^=":
				case "|=":
				case "&=":
				case "<<=":
				case ">>=":
					OperatorType = Type.Assignment;
					break;

				default:
					throw new ParseTokenException($"Unregistered operator type <{SourceCode}>", this);

			}
		}

		public static bool ExpressionHasValue(Token token)
		{
			switch (token)
			{
				case null:
					return false;

				case Literal lit:
				case Identifier id:
					return true;

				case Operator op:
					if (op.OperatorType == Type.Unary)
						return ExpressionHasValue(op.LHS) && op.RHS == null;
					else
						return ExpressionHasValue(op.LHS) && ExpressionHasValue(op.RHS);

				case Punctuator pun when pun.PunctuatorType == Punctuator.Type.OpeningParentases && pun.Character == '(':
					return pun.Any(ExpressionHasValue);

				default:
					return false;
			}
		}

		public override void ParseToken(Parser parser)
		{
			Token prev = parser.PrevToken;
			Token next = parser.NextToken;

			switch (OperatorType)
			{
				// Expression
				case Type.Expression when prev is Identifier:
					parser.TakePrevToken(_LHS); // LHS
					break;
				case Type.Expression:
					throw new NotImplementedException("Expressions are not yet implemented! (ex: ++x, --x)");

				// Unary
				case Type.Unary:
					parser.TakeNextToken(_RHS);
					break;

				// Two sided expressions
				case Type.Multiplicative:
				case Type.Additive:
				case Type.BitwiseShift:
				case Type.Relational:
				case Type.Equality:
				case Type.BitwiseAND:
				case Type.BitwiseXOR:
				case Type.BitwiseOR:
				case Type.BooleanAND:
				case Type.BooleanOR:
					if (ExpressionHasValue(prev))
						parser.TakePrevToken(_LHS);
					else
						throw new ParseUnexpectedLeadingTokenException(this, prev);

					if (ExpressionHasValue(next))
						parser.TakeNextToken(_RHS);
					else
						throw new ParseUnexpectedTrailingTokenException(this, next);
					break;

				case Type.Assignment:
					if (prev is Identifier)
						parser.TakePrevToken(_LHS);
					else
						throw new ParseUnexpectedLeadingTokenException(this, prev);

					if (ExpressionHasValue(next))
						parser.TakeNextToken(_RHS);
					else
						throw new ParseUnexpectedTrailingTokenException(this, next);

					// ex: <<=, +=, %=
					if (SourceCode != "=")
					{
						// Add identifier & operator & old RHS to pool, then take again
						var id = new Identifier(LHS.source);
						var op = new Operator(new TokenSource(source.code.Substring(0, SourceCode.Length - 1), source.file, source.line, source.column));
						Token old_rhs = RHS;
						this[_RHS] = null;

						parser.AddTokensAfterAndParse(id, op, old_rhs);
						parser.TakeNextToken(_RHS);
					}
					break;

				default:
					throw new ParseTokenException($"Unexpected operator type <{OperatorType}>.", this);
			}
		}

		public override string CompileToken(Compiler compiler)
		{
			switch (OperatorType)
			{
				case Type.Assignment:
					if (this.AnyRecursive(t => t is LiteralNumber)
					    && this.AnyRecursive(t => t is LiteralString))
						compiler.assignmentNeedsCSSnipper = true;

					string c_rhs = RHS.CompileToken(compiler);
					compiler.RegisterVariable(LHS as Identifier ?? throw new CompileException("Missing identifier for assignment.", this));
					string c_lhs = LHS.CompileToken(compiler);

					string formatString = compiler.assignmentNeedsCSSnipper
						? "{0}=⊂{1}⊃"
						: "{0}={1}";

					return string.Format(formatString, c_lhs, c_rhs);

				default:
					return string.Format("{0}{1}{2}", LHS?.CompileToken(compiler), SourceCode, RHS?.CompileToken(compiler));
			}
		}

		public enum Type
		{
			///<summary>++, --</summary>
			Expression,
			///<summary>-x, !x, ~x</summary>
			Unary,
			///<summary>x*y, x/y, x%y</summary>
			Multiplicative,
			///<summary>x+y, x-y</summary>
			Additive,
			///<summary>x&lt;&lt;y, x&gt;&gt;y</summary>
			BitwiseShift,
			///<summary>x&lt;y, x&gt;y, x&lt;=y, x&gt;=y</summary>
			Relational,
			///<summary>x==y, x!=y</summary>
			Equality,
			///<summary>x&amp;y</summary>
			BitwiseAND,
			///<summary>x^y</summary>
			BitwiseXOR,
			///<summary>x|y</summary>
			BitwiseOR,
			///<summary>x&amp;&amp;y</summary>
			BooleanAND,
			///<summary>x||y</summary>
			BooleanOR,
			/////<summary>x?true:false</summary>
			//Conditional,
			///<summary>x=y, x+=y, x-=y, x*=y, x/=y, etc.</summary>
			Assignment,
		}
	}
}