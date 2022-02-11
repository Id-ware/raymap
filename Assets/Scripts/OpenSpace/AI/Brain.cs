﻿using OpenSpace.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenSpace.AI {
    public class Brain {
        public Pointer offset;

        public Pointer off_mind;
        public uint unknown1;
        public uint unknown2;
        
        public Mind mind;

        public Brain(Pointer offset) {
            this.offset = offset;
        }

        public static Brain Read(Reader reader, Pointer offset) {
            Brain b = new Brain(offset);
			//MapLoader.Loader.print("Brain " + offset);
            b.off_mind = Pointer.Read(reader);
			if (CPA_Settings.s.game != CPA_Settings.Game.R2Revolution) b.unknown1 = reader.ReadUInt32(); // init at 0xCDCDCDCD
			if (CPA_Settings.s.game == CPA_Settings.Game.LargoWinch) {
				b.unknown2 = reader.ReadByte(); // 0
			} else {
				b.unknown2 = reader.ReadUInt32(); // 0
			}

            b.mind = MapLoader.Loader.FromOffsetOrRead<Mind>(reader, b.off_mind);

            return b;
        }
    }
}
