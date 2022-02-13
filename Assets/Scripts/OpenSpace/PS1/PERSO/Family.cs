﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSpace.PS1 {
	public class Family : OpenSpaceStruct { // Animation/state related
		public uint uint_00;
		public uint uint_04;
		public LegacyPointer off_animations;
		public uint num_animations;
		public uint uint_10;
		public uint uint_14;
		public int int_18;
		public int int_1C;

		// Parsed
		public PS1Animation[] animations;
		public string name;

		// Added for easier access later
		public uint startState; // inclusive
		public uint endState; // exclusive
		public State[] states;

		protected override void ReadInternal(Reader reader) {
			LegacyPointer.DoAt(ref reader, Offset - 0x24, () => { // Hack
				name = reader.ReadString(0x24);
			});
			//Load.print(name + " " + Offset);
			uint_00 = reader.ReadUInt32();
			uint_04 = reader.ReadUInt32();
			off_animations = LegacyPointer.Read(reader);
			num_animations = reader.ReadUInt32();
			uint_10 = reader.ReadUInt32();
			uint_14 = reader.ReadUInt32();
			int_18 = reader.ReadInt32();
			int_1C = reader.ReadInt32();

			animations = Load.ReadArray<PS1Animation>(num_animations, reader, off_animations);
		}
	}
}
