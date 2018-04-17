﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using RobotPlusPlus.Core.Compiling;
using RobotPlusPlus.Core.Parsing;
using RobotPlusPlus.Core.Structures;
using RobotPlusPlus.Core.Utility;

namespace RobotPlusPlus.Core.Tokenizing.Tokens.Literals
{
	public class LiteralStringToken : LiteralToken
	{
		public string Value { get; }
		public string ValueEscaped { get; }
		public bool NeedsEscaping { get; }

		public LiteralStringToken(TokenSource source) : base(source)
		{
			Value = Regex.Unescape(SourceCode.Substring(1, SourceCode.Length - 2));
			ValueEscaped = Value.EscapeString();
			NeedsEscaping = Value != ValueEscaped;
		}

		public override void ParseToken(IteratedList<Token> parent)
		{ }

		//public string AssembleIntoString()
		//{
		//	string escaped = Value.EscapeString();
		//	if (escaped == Value && !compiler.assignmentNeedsCSSnipper)
		//		return $"‴{Value}‴";

		//	compiler.assignmentNeedsCSSnipper = true;
		//	return $"\"{escaped}\"";
		//}
	}
}