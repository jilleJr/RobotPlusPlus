﻿using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RobotPlusPlus.Core.Compiling;
using RobotPlusPlus.Core.Exceptions;

namespace RobotPlusPlus.Core.Tests.CompilerTests
{
	[TestClass]
	public class IntermediateDotInstanceTests
	{
		[TestMethod]
		[ExpectedException(typeof(CompileFunctionValueOfVoidException))]
		public void Compile_CallOnVoidMethod()
		{
			// Arrange
			const string code = "x = screen.Inflate(screen.Size)";

		    // Act
		    string result = Compiler.Compile(code);

		    // Assert
		    Assert.Fail("Unexpected result: {0}", result);
        }

		[TestMethod]
		public void Compile_StandaloneCallOnVoidMethod()
		{
			// Arrange
			const string code = "screen.Inflate(screen.Size)";
			const string expected = "♥screen=⊂new Func<System.Drawing.Rectangle, " +
			                        "System.Drawing.Size, System.Drawing.Rectangle>" +
			                        "((System.Drawing.Rectangle _, System.Drawing.Size a1)=>{" +
			                        "_.Inflate(a1);" +
			                        "return _;})(♥screen, ♥screen.Size)⊃";

			// Act
			string compiled = Compiler.Compile(code);

			// Assert
			Assert.That.AreCodeEqual(expected, compiled);
		}

		[TestMethod]
		public void Compile_PropertyCallOnVoidMethod()
		{
		    // Arrange
			const string code = "screen.Location.Offset(screen.Location)";
            const string expected = "♥screen=⊂new Func<System.Drawing.Rectangle, " +
                                    "System.Drawing.Point, System.Drawing.Rectangle>" +
                                    "((System.Drawing.Rectangle _, System.Drawing.Point a1)=>{" +
                                    "_.Location.Offset(a1);" +
                                    "return _;})(♥screen, ♥screen.Location)⊃";

			// Act
			string compiled = Compiler.Compile(code);

		    // Assert
		    Assert.That.AreCodeEqual(expected, compiled);
        }
		
		[TestMethod]
		public void Compile_PropertyAssignment()
		{
			// Arrange
			const string code = "screen.X = 1";
            const string expected = "♥screen=⊂new Func<System.Drawing.Rectangle, " +
                                    "System.Drawing.Rectangle>(" +
                                    "(System.Drawing.Rectangle _)=>{" +
                                    "_.X=1;return _;})(♥screen)⊃";

			// Act
			string compiled = Compiler.Compile(code);

			// Assert
			Assert.That.AreCodeEqual(expected, compiled);
		}
	}
}