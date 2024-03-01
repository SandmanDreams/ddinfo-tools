using System.Runtime.InteropServices;

namespace DevilDaggersInfo.Tools.GameMemory.Enemies;

#pragma warning disable SA1134
[StructLayout(LayoutKind.Explicit, Size = StructSizes.Spider)]
public record struct Spider
{
	// [FieldOffset(000)] public int IsHiding;
	// [FieldOffset(004)] public int IsNotHiding; // ??
	[FieldOffset(228)] public int Hp;

	// TODO: SpiderType int enum.
}
#pragma warning restore SA1134
