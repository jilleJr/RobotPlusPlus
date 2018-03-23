﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using RobotPlusPlus.Core.Parsing;
using RobotPlusPlus.Core.Tokenizing;
using RobotPlusPlus.Core.Tokenizing.Tokens;

namespace RobotPlusPlus.Core.Tests.ParserTests
{
	[TestClass]
	public class SimpleAssignmentTests
	{
		[TestMethod]
		public void Parse_SimpleAssignment()
		{
			// Arrange
			const string input = "x = 4";

			// Act
			Token[] tokenized = Tokenizer.Tokenize(input);
			Token[] parsed = Parser.Parse(tokenized);

			// Assert
			CollectionAssert.That.TokensAreParsed(parsed);
			Assert.AreEqual(1, parsed.Length);

			Token assi = parsed[0];
			Assert.That.TokenIsOperator(assi, OperatorToken.Type.Assignment, "=");
			Assert.That.TokenIsOfType<IdentifierToken>(assi[0], "x");
			Assert.That.TokenIsLiteralInteger(assi[1], 4);
		}

		[TestMethod]
		public void Parse_SimpleAssignmentWithOperation()
		{
			// Arrange
			const string input = "x = 4 * 3";

			// Act
			Token[] tokenized = Tokenizer.Tokenize(input);
			Token[] parsed = Parser.Parse(tokenized);

			// Assert
			CollectionAssert.That.TokensAreParsed(parsed);
			Assert.AreEqual(1, parsed.Length);

			Token assi = parsed[0];
			Assert.That.TokenIsOperator(assi, OperatorToken.Type.Assignment, "=");
			Assert.That.TokenIsOfType<IdentifierToken>(assi[0], "x");

			Token mult = assi[1];
			Assert.That.TokenIsOperator(mult, OperatorToken.Type.Multiplicative, "*");
			Assert.That.TokenIsLiteralInteger(mult[0], 4);
			Assert.That.TokenIsLiteralInteger(mult[1], 3);
		}

		[TestMethod]
		public void Parse_SimplePriorityCheck()
		{
			// Arrange
			const string input = "60 - 15 / (2 + 5)";

			// Act
			Token[] tokenized = Tokenizer.Tokenize(input);
			Token[] parsed = Parser.Parse(tokenized);

			// Assert
			CollectionAssert.That.TokensAreParsed(parsed);
			Assert.AreEqual(1, parsed.Length);

			Token minus = parsed[0];
			Assert.That.TokenIsOperator(minus, OperatorToken.Type.Additive, "-");
			Assert.That.TokenIsLiteralInteger(minus[0], 60);

			Token div = minus[1];
			Assert.That.TokenIsOperator(div, OperatorToken.Type.Multiplicative, "/");
			Assert.That.TokenIsLiteralInteger(div[0], 15);

			Token par = div[1];
			Assert.That.TokenIsParentases(par, '(', 1);

			Token plus = par[0];
			Assert.That.TokenIsOperator(plus, OperatorToken.Type.Additive, "+");
			Assert.That.TokenIsLiteralInteger(plus[0], 2);
			Assert.That.TokenIsLiteralInteger(plus[1], 5);
		}

		[TestMethod]
		public void Parse_SimpleTwoExpressions()
		{
			// Arrange
			const string input = "10 - 12 5 * 25";

			// Act
			Token[] tokenized = Tokenizer.Tokenize(input);
			Token[] parsed = Parser.Parse(tokenized);

			// Assert
			CollectionAssert.That.TokensAreParsed(parsed);
			Assert.AreEqual(2, parsed.Length);

			Token minus = parsed[0];
			Assert.That.TokenIsOperator(minus, OperatorToken.Type.Additive, "-");
			Assert.That.TokenIsLiteralInteger(minus[0], 10);
			Assert.That.TokenIsLiteralInteger(minus[1], 12);

			Token mult = parsed[1];
			Assert.That.TokenIsOperator(mult, OperatorToken.Type.Multiplicative, "*");
			Assert.That.TokenIsLiteralInteger(mult[0], 5);
			Assert.That.TokenIsLiteralInteger(mult[1], 25);
		}


		[TestMethod]
		public void Parse_SimpleTwoAssignments()
		{
			// Arrange
			const string input = "x = 10 y+= .5*x";

			// Act
			Token[] tokenized = Tokenizer.Tokenize(input);
			Token[] parsed = Parser.Parse(tokenized);

			// Assert
			CollectionAssert.That.TokensAreParsed(parsed);
			Assert.AreEqual(2, parsed.Length);

			Token x = parsed[0];
			Assert.That.TokenIsOperator(x, OperatorToken.Type.Assignment, "=");
			Assert.That.TokenIsOfType<IdentifierToken>(x[0], "x");
			Assert.That.TokenIsLiteralInteger(x[1], 10);

			Token y = parsed[1];
			Assert.That.TokenIsOperator(y, OperatorToken.Type.Assignment, "+=");
			Assert.That.TokenIsOfType<IdentifierToken>(y[0], "y");

			// should parse to y=(y+(.5*x))
			Token add = y[1];
			Assert.That.TokenIsOperator(add, OperatorToken.Type.Additive, "+");
			Assert.That.TokenIsOfType<IdentifierToken>(add[0], "y");

			Token mult = add[1];
			Assert.That.TokenIsOperator(mult, OperatorToken.Type.Multiplicative, "*");
			Assert.That.TokenIsLiteralReal(mult[0], 0.5d);
			Assert.That.TokenIsOfType<IdentifierToken>(mult[1], "x");
		}

		[TestMethod]
		public void Parse_SimpleTwoAssignmentsSemicolon()
		{
			// Arrange
			const string input = "x = 10; y+= .5*x";

			// Act
			Token[] tokenized = Tokenizer.Tokenize(input);
			Token[] parsed = Parser.Parse(tokenized);

			// Assert
			CollectionAssert.That.TokensAreParsed(parsed);
			Assert.AreEqual(3, parsed.Length);

			Token x = parsed[0];
			Assert.That.TokenIsOperator(x, OperatorToken.Type.Assignment, "=");
			Assert.That.TokenIsOfType<IdentifierToken>(x[0], "x");
			Assert.That.TokenIsLiteralInteger(x[1], 10);

			Assert.That.TokenIsOfType<PunctuatorToken>(parsed[1], ";");

			Token y = parsed[2];
			Assert.That.TokenIsOperator(y, OperatorToken.Type.Assignment, "+=");
			Assert.That.TokenIsOfType<IdentifierToken>(y[0], "y");

			// should parse to y=(y+(.5*x))
			Token add = y[1];
			Assert.That.TokenIsOperator(add, OperatorToken.Type.Additive, "+");
			Assert.That.TokenIsOfType<IdentifierToken>(add[0], "y");

			Token mult = add[1];
			Assert.That.TokenIsOperator(mult, OperatorToken.Type.Multiplicative, "*");
			Assert.That.TokenIsLiteralReal(mult[0], 0.5d);
			Assert.That.TokenIsOfType<IdentifierToken>(mult[1], "x");
		}
	}
}