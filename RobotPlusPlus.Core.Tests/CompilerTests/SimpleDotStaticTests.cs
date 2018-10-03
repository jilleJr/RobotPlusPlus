﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RobotPlusPlus.Core.Compiling;
using RobotPlusPlus.Core.Exceptions;

namespace RobotPlusPlus.Core.Tests.CompilerTests
{
	[TestClass]
	public class SimpleDotStaticTests
	{
		[TestMethod]
		public void Compile_Property()
		{
			// Arrange
			const string code = "x = Rectangle.Empty";
			const string expected = "♥x=⊂System.Drawing.Rectangle.Empty⊃";

			// Act
			string compiled = Compiler.Compile(code);

			// Assert
			Assert.That.AreCodeEqual(expected, compiled);
		}

		[TestMethod]
		[ExpectedException(typeof(CompileTypePropertyDoesNotExistException))]
		public void Compile_PropertyUndefined()
		{
			// Arrange
			const string code = "x = Rectangle.Lorem";

		    // Act
		    string result = Compiler.Compile(code);

		    // Assert
		    Assert.Fail("Unexpected result: {0}", result);
        }

		[TestMethod]
		[ExpectedException(typeof(CompileTypePropertyDoesNotExistException))]
		public void Compile_PropertyInstance()
		{
			// Arrange
			const string code = "x = Rectangle.Width";

		    // Act
		    string result = Compiler.Compile(code);

		    // Assert
		    Assert.Fail("Unexpected result: {0}", result);
        }

		[TestMethod]
		public void Compile_CallOnString_1Arg()
		{
			// Arrange
			const string code = "x = string.IsNullOrEmpty('')";
			const string expected = "♥x=⊂System.String.IsNullOrEmpty(\"\")⊃";

			// Act
			string compiled = Compiler.Compile(code);

			// Assert
			Assert.That.AreCodeEqual(expected, compiled);
		}

		[TestMethod]
		[ExpectedException(typeof(CompileParameterTypeConvertImplicitException))]
		public void Compile_CallOnString_WrongArgType()
		{
			// Arrange
			const string code = "x = string.IsNullOrEmpty(true)";

		    // Act
		    string result = Compiler.Compile(code);

		    // Assert
		    Assert.Fail("Unexpected result: {0}", result);
        }

		[TestMethod]
		[ExpectedException(typeof(CompileTypePropertyDoesNotExistException))]
		public void Compile_CallOnString_NonExisting()
		{
			// Arrange
			const string code = "x = string.LoremIpsum()";

		    // Act
		    string result = Compiler.Compile(code);

		    // Assert
		    Assert.Fail("Unexpected result: {0}", result);
        }

		[TestMethod]
		public void Compile_CallOnNumber_1Arg()
		{
			// Arrange
			const string code = "x = int.Parse('1')";
			const string expected = "♥x=⊂System.Int32.Parse(\"1\")⊃";

			// Act
			string compiled = Compiler.Compile(code);

			// Assert
			Assert.That.AreCodeEqual(expected, compiled);
		}

		[TestMethod]
		[ExpectedException(typeof(CompileFunctionNoMatchingOverloadException))]
		public void Compile_CallOnNumber_WrongArgType()
		{
			// Arrange
			const string code = "x = int.Parse(true)";

		    // Act
		    string result = Compiler.Compile(code);

		    // Assert
		    Assert.Fail("Unexpected result: {0}", result);
        }

		[TestMethod]
		[ExpectedException(typeof(CompileTypePropertyDoesNotExistException))]
		public void Compile_CallOnNumber_NonExisting()
		{
			// Arrange
			const string code = "x = int.LoremIpsum()";

		    // Act
		    string result = Compiler.Compile(code);

		    // Assert
		    Assert.Fail("Unexpected result: {0}", result);
        }

		[TestMethod]
		public void Compile_UseCallResultInIf()
		{
			// Arrange
			const string code = "if string.IsNullOrEmpty('b') {}";
			const string expected = "jump label ➜ifend if ⊂System.String.IsNullOrEmpty(\"b\")⊃\n" +
									"➜ifend";

			// Act
			string compiled = Compiler.Compile(code);

			// Assert
			Assert.That.AreCodeEqual(expected, compiled);
		}

		[TestMethod]
		[ExpectedException(typeof(CompileTypeConvertImplicitException))]
		public void Compile_UseCallResultInIf_WrongType()
		{
			// Arrange
			const string code = "if int.Parse('1') {}";

		    // Act
		    string result = Compiler.Compile(code);

		    // Assert
		    Assert.Fail("Unexpected result: {0}", result);
        }

		[TestMethod]
		public void Compile_UsePropertyResultInIf()
		{
			// Arrange
			const string code = "if string.Empty == '' {}";
			const string expected = "jump label ➜ifend if ⊂System.String.Empty==\"\"⊃\n" +
									"➜ifend";

			// Act
			string compiled = Compiler.Compile(code);

			// Assert
			Assert.That.AreCodeEqual(expected, compiled);
		}

		[TestMethod]
		[ExpectedException(typeof(CompileTypeConvertImplicitException))]
		public void Compile_UsePropertyResultInIf_WrongType()
		{
			// Arrange
			const string code = "if string.Empty {}";

		    // Act
		    string result = Compiler.Compile(code);

		    // Assert
		    Assert.Fail("Unexpected result: {0}", result);
        }
	}
}