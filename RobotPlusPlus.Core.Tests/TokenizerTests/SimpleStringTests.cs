﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using RobotPlusPlus.Core.Exceptions;
using RobotPlusPlus.Core.Tokenizing;
using RobotPlusPlus.Core.Tokenizing.Tokens;

namespace RobotPlusPlus.Core.Tests.TokenizerTests
{
	[TestClass]
	public class SimpleStringTests
	{
		[TestMethod]
		public void Tokenize_OneDoubleQuoteString()
		{
			// Arrange
			const string input = @"""hello world""";
			// Act
			Token[] result = Tokenizer.Tokenize(input);

			// Assert
			CollectionAssert.That.TokensAreOfTypes(result,
				typeof(LiteralToken));

			Assert.AreEqual(input, result[0].SourceCode);
		}

		[TestMethod]
		public void Tokenize_OneSingleQuoteString()
		{
			// Arrange
			const string input = @"'hello world'";
			// Act
			Token[] result = Tokenizer.Tokenize(input);

			// Assert
			CollectionAssert.That.TokensAreOfTypes(result,
				typeof(LiteralToken));

			Assert.AreEqual(input, result[0].SourceCode);
		}

		[TestMethod]
		[ExpectedException(typeof(ParseException))]
		public void Tokenize_IncompleteDoubleQuoteString()
		{
			// Arrange
			const string input = @"""hello world";
			// Act
			Tokenizer.Tokenize(input);

			// Assert
			Assert.Fail();
		}

		[TestMethod]
		[ExpectedException(typeof(ParseException))]
		public void Tokenize_IncompleteSingleQuoteString()
		{
			// Arrange
			const string input = @"'hello world";
			// Act
			Tokenizer.Tokenize(input);

			// Assert
			Assert.Fail();
		}

		[TestMethod]
		public void Tokenize_NestedSingleInDoubleQuoteString()
		{
			// Arrange
			const string input = @"""nested 'strings' are cool""";
			// Act
			Token[] result = Tokenizer.Tokenize(input);

			// Assert
			CollectionAssert.That.TokensAreOfTypes(result,
				typeof(LiteralToken));

			Assert.AreEqual(input, result[0].SourceCode);
		}

		[TestMethod]
		public void Tokenize_NestedDoubleInSingleQuoteString()
		{
			// Arrange
			const string input = @"'I dont use ""airquotes"" correctly'";
			// Act
			Token[] result = Tokenizer.Tokenize(input);

			// Assert
			CollectionAssert.That.TokensAreOfTypes(result,
				typeof(LiteralToken));

			Assert.AreEqual(input, result[0].SourceCode);
		}

		[TestMethod]
		public void Tokenize_EscapedString()
		{
			// Arrange
			const string input = @"'You\'re fine'";

			// Act
			Token[] result = Tokenizer.Tokenize(input);

			// Assert
			CollectionAssert.That.TokensAreOfTypes(result,
				typeof(LiteralToken));

			Assert.AreEqual(input, result[0].SourceCode);
		}

		[TestMethod]
		public void Tokenize_TwoStrings()
		{
			// Arrange
			const string str1 = @"""hello world""";
			const string str2 = @"'some sheep don\'t sleep'";
			string input = $"{str1} {str2}";

			// Act
			Token[] result = Tokenizer.Tokenize(input);

			// Assert
			CollectionAssert.That.TokensAreOfTypes(result,
				typeof(LiteralToken),
				typeof(WhitespaceToken),
				typeof(LiteralToken));

			Assert.AreEqual(str1, result[0].SourceCode);
			Assert.AreEqual(str2, result[2].SourceCode);
		}
	}
}