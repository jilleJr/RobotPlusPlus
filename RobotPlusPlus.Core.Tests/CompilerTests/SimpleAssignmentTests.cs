﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using RobotPlusPlus.Core.Compiling;

namespace RobotPlusPlus.Core.Tests.CompilerTests
{
	[TestClass]
	public class SimpleAssignmentTests
	{
		[TestMethod]
		public void Compile_Integer()
		{
			// Act
			string output = Compiler.Compile("x=1");

			// Assert
			Assert.That.AreCodeEqual("♥x=1", output);
		}

		[TestMethod]
		public void Compile_IntegerWithSpaces()
		{
			// Act
			string output = Compiler.Compile("x = 1");

			// Assert
			Assert.That.AreCodeEqual("♥x=1", output);
		}

		[TestMethod]
		public void Compile_IntegerWithLottaSpaces()
		{
			// Act
			string output = Compiler.Compile("	x   =		  1   ");

			// Assert
			Assert.That.AreCodeEqual("♥x=1", output);
		}

		[TestMethod]
		public void Compile_DecimalPoint()
		{
			// Act
			string pointzero = Compiler.Compile("x = 1.0");
			string pointnull = Compiler.Compile("x = 1.");
			string pointb4 = Compiler.Compile("x = .0");

			// Assert
			Assert.That.AreCodeEqual("♥x=1.0", pointzero);
			Assert.That.AreCodeEqual("♥x=1.0", pointnull);
			Assert.That.AreCodeEqual("♥x=0.0", pointb4);
		}

		[TestMethod]
		public void Compile_DecimalSuffix()
		{
			// Act
			string suffixupper = Compiler.Compile("x = 1F");
			string suffixlower = Compiler.Compile("x = 1f");

			// Assert
			Assert.That.AreCodeEqual("♥x=1.0", suffixupper);
			Assert.That.AreCodeEqual("♥x=1.0", suffixlower);
		}

		[TestMethod]
		public void Compile_DecimalPointAndSuffix()
		{
			// Act
			string pointzero_suffixupper = Compiler.Compile("x = 1.0F");
			string pointzero_suffixlower = Compiler.Compile("x = 1.0f");
			string pointnull_suffixupper = Compiler.Compile("x = 1.F");
			string pointnull_suffixlower = Compiler.Compile("x = 1.f");

			// Assert
			Assert.That.AreCodeEqual("♥x=1.0", pointzero_suffixupper);
			Assert.That.AreCodeEqual("♥x=1.0", pointzero_suffixlower);
			Assert.That.AreCodeEqual("♥x=1.0", pointnull_suffixupper);
			Assert.That.AreCodeEqual("♥x=1.0", pointnull_suffixlower);
		}

		[TestMethod]
		public void Compile_String()
		{
			// Act
			string output = Compiler.Compile(@"x = ""foo""");

			// Assert
			Assert.That.AreCodeEqual("♥x=‴foo‴", output);
		}

		[TestMethod]
		public void Compile_Boolean()
		{
			// Act
			string output = Compiler.Compile("x = true");

			// Assert
			Assert.That.AreCodeEqual("♥x=true", output);
		}

	}
}