﻿using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;

namespace RobotPlusPlus.CLI
{
	public class Program
	{
		public static int Main(string[] args)
		{
#if DEBUG
			return ProgramOptions.Execute(args);
#else
			try
			{
				return ProgramOptions.Execute(args);
			}
			catch (Exception e)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("\n[ UNEXPECTED EXCEPTION DURING EXECUTION! ]\n");
				Console.WriteLine(e);
				Console.ResetColor();
				return -1;
			}
#endif
		}
	}
}
