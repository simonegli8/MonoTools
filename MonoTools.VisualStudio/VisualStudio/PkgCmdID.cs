// PkgCmdID.cs
// MUST match PkgCmdID.h

namespace MonoTools
{
	static class PkgCmdID
	{
		public const uint XBuildSolution = 0x100;
		public const uint XRebuildSolution = 0x101;
		public const uint XBuildProject = 0x102;
		public const uint XRebuildProject = 0x103;
		public const uint AddPdb2MdbToProject = 0x104;
		public const uint SuppressXBuildForProject = 0x105;
		public const uint StartMono = 0x0106;
		public const uint DebugMono = 0x107;
		public const uint OpenLogFile = 0x108;
		public const uint MoMASolution = 0x109;
		public const uint MoMAProject = 0x10A;
		public const uint ServerSetup = 0x10B;
		public const uint Help = 0x10C;
	};

}