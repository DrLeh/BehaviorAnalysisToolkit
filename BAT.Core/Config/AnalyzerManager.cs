﻿using System;
using System.Linq;
using BAT.Core.Common;
using BAT.Core.Analyzers;

namespace BAT.Core.Config
{
    public class AnalyzerManager : TypeManager
	{
		public static IAnalyzer GetAnalyzer(string name)
		{
			var analyzers = GetInheritingTypes<IAnalyzer>();
			var type = analyzers.FirstOrDefault(x => x.Name == name + "Analyzer" || x.Name == name);
			if (type != null)
				return (IAnalyzer)Activator.CreateInstance(type);
			else
			{
				LogManager.Error($"Could not find analyzer named {name}");
				return null;
            }
		}
    }
}