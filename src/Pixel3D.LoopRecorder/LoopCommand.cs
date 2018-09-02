﻿using System;

namespace Pixel3D.LoopRecorder
{
	[Flags]
	public enum LoopCommand : byte
	{
		None = 0x0,
		StartPlaying = 1 << 0,
		Stop = 1 << 1,
		NextLoop = 1 << 2,
		PreviousLoop = 1 << 3,
		Record= 1 << 4,
		SnapshotOnly = 1 << 5,
		RecordHasFocus = 1 << 6
	}
}