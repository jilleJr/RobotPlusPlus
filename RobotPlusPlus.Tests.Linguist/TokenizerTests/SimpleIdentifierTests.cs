﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using RobotPlusPlus.Linguist.Exceptions;
using RobotPlusPlus.Linguist.Tokenizing;
using RobotPlusPlus.Linguist.Tokenizing.Tokens;

namespace RobotPlusPlus.Tests.TokenizerTests
{
	[TestClass]
	public class SimpleIdentifierTests
	{

		[TestMethod]
		public void Tokenize_SingleIdentifierSingleChar()
		{
			// Arrange
			const string input = "i";

			// Act
			Token[] result = Tokenizer.Tokenize(input);

			// Assert
			CollectionAssert.That.TokensAreOfTypes(result,
				typeof(Identifier));
		}

		[TestMethod]
		public void Tokenize_SingleIdentifierMultipleChar()
		{
			// Arrange
			const string input = "myVariable";

			// Act
			Token[] result = Tokenizer.Tokenize(input);

			// Assert
			CollectionAssert.That.TokensAreOfTypes(result,
				typeof(Identifier));
		}

		[TestMethod]
		public void Tokenize_SingleIdentifierMultipleCharWithSpaces()
		{
			// Arrange
			const string input = "	 myVariable		";

			// Act
			Token[] result = Tokenizer.Tokenize(input);

			// Assert
			CollectionAssert.That.TokensAreOfTypes(result,
				typeof(Whitespace),
				typeof(Identifier),
				typeof(Whitespace));
		}

		[TestMethod]
		[ExpectedException(typeof(ParseException))]
		public void Tokenize_SingleIdentifierWithInvalidFirstChar()
		{
			// Arrange
			const string input = "10dogs";

			// Act
			Tokenizer.Tokenize(input);

			Assert.Fail();
		}

		[TestMethod]
		public void Tokenize_MultipleIdentifiersSimple()
		{
			// Arrange
			const string input = "my varIab13s shine";

			// Act
			Token[] result = Tokenizer.Tokenize(input);

			// Assert
			CollectionAssert.That.TokensAreOfTypes(result,
				typeof(Identifier),
				typeof(Whitespace),
				typeof(Identifier),
				typeof(Whitespace),
				typeof(Identifier));
		}
	}
}
