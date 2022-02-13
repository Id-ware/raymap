﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSpace {
	/// <summary>
	/// Base type for structs in OpenSpace, made to more smartly parse the structs
	/// Recently introduced, so don't expect it to be used a lot yet
	/// </summary>
	public abstract class OpenSpaceStruct {
		public LegacyPointer Offset { get; protected set; }

		public void Init(LegacyPointer offset) {
			this.Offset = offset;
		}
		protected abstract void ReadInternal(Reader reader);
		public void Read(Reader reader, bool inline = false) {
			if (inline) {
				ReadInternal(reader);
				Size = LegacyPointer.Current(reader).offset - Offset.offset;
			} else {
				LegacyPointer.DoAt(ref reader, Offset, () => {
					ReadInternal(reader);
					Size = LegacyPointer.Current(reader).offset - Offset.offset;
				});
			}
		}

		public virtual uint Size { get; protected set; }
		public static MapLoader Load {
			get {
				return MapLoader.Loader;
			}
		}
	}
}
