﻿using System;
using RobotPlusPlus.Tokenizing.Tokens;

namespace RobotPlusPlus.Exceptions
{
	public class CompileException : ParseTokenException
	{
		public CompileException(string msg, Token source)
			: base(msg, source)
		{ }

		public CompileException(string msg, Token source, Exception innerException)
			: base(msg, source, innerException)
		{ }
	}
}