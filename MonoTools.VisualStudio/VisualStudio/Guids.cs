// Guids.cs
// MUST match guids.h

using MonoProgram.Package.Debuggers;

namespace MonoTools
{
	using System;

	static class Guids
	{
		public const string MonoToolsPkgString = "fbcafcd5-87dc-44f0-83c0-0a5be15709d8";
		public const string MonoToolsCmdSetString = "66ae7e29-9859-4e84-b953-1502a786e958";
		public const string EngineId = MonoEngine.ClassGuid;

		public static readonly Guid MonoToolsCmdSet = new Guid(MonoToolsCmdSetString);
	};
}