﻿using Newtonsoft.Json;
using OpenSpace.FileFormat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenSpace {
    [JsonConverter(typeof(PointerJsonConverter))]
    public class LegacyPointer : IEquatable<LegacyPointer> {
        public uint offset;
        public FileWithPointers file;
        public LegacyPointer(uint offset, FileWithPointers file) {
            this.offset = offset;
            this.file = file;
        }

        public uint FileOffset {
            get {
                if (file != null) {
                    return (uint)(offset + file.baseOffset);
                } else return offset;
            }
        }

        public uint AbsoluteOffset {
            get {
                return offset;
            }
        }

        public override bool Equals(System.Object obj) {
            return obj is LegacyPointer && this == (LegacyPointer)obj;
        }
        public override int GetHashCode() {
            return offset.GetHashCode() ^ file.GetHashCode();
        }

        public bool Equals(LegacyPointer other) {
            return this == (LegacyPointer)other;
        }

        public static bool operator ==(LegacyPointer x, LegacyPointer y) {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            return x.offset == y.offset && x.file == y.file;
        }
        public static bool operator !=(LegacyPointer x, LegacyPointer y) {
            return !(x == y);
        }
        public static LegacyPointer operator +(LegacyPointer x, long y) {
            return new LegacyPointer((uint)((long)x.offset + y), x.file);
        }
        public static LegacyPointer operator -(LegacyPointer x, long y) {
            return new LegacyPointer((uint)((long)x.offset - y), x.file);
        }
        public override string ToString() {
            if (file != null && file.baseOffset != 0) {
                return file.name + "|" + String.Format("0x{0:X8}", offset) + "[" + String.Format("0x{0:X8}", offset + file.baseOffset) + "]";
            } else {
                return file.name + "|" + String.Format("0x{0:X8}", offset);
            }
        }

        public string StringFileOffset {
            get {
                return String.Format("{0:X8}", FileOffset);
            }
        }

        public string StringAbsoluteOffset {
            get {
                return String.Format("{0:X8}", AbsoluteOffset);
            }
        }

        public static LegacyPointer GetPointerAtOffset(LegacyPointer pointer) {
            MapLoader l = MapLoader.Loader;
            LegacyPointer ptr = null;
            if (pointer.file.pointers.ContainsKey(pointer.offset)) {
                ptr = pointer.file.pointers[pointer.offset];
                if (ptr.offset == 0) return null;
                return ptr;
            } else if (pointer.file.allowUnsafePointers) {
                Reader reader = pointer.file.reader;
                LegacyPointer.DoAt(ref reader, pointer, () => {
                    uint current_off = (uint)(reader.BaseStream.Position);
                    uint value = reader.ReadUInt32();
                    FileWithPointers file = pointer.file;
                    uint fileOff = (uint)(current_off - file.baseOffset);

                    if (file.pointers.ContainsKey(fileOff)) {
                        ptr = file.pointers[fileOff];
                    } else {
                        if (value == 0 || value == 0xFFFFFFFF) {
                            ptr = null;
                        } else {
                            ptr = file.GetUnsafePointer(value);
                        }
                    }
                });
                return ptr;
            }
            return null;
        }

        public static LegacyPointer Read(Reader reader, bool allowMinusOne = false) {
            MapLoader l = MapLoader.Loader;
            uint current_off = (uint)(reader.BaseStream.Position);
            LegacyPointer readFrom = LegacyPointer.Current(reader);
            uint value = reader.ReadUInt32();
            FileWithPointers file = l.GetFileByReader(reader);
            if (file == null) throw new PointerException("Reader wasn't recognized.", "Pointer.Read");
            uint fileOff = (uint)(current_off - file.baseOffset);
            if (!file.pointers.ContainsKey(fileOff)) {
                if (value == 0 || (allowMinusOne && value == 0xFFFFFFFF)) return null;
                if (!l.allowDeadPointers && !file.allowUnsafePointers) {
                    throw new PointerException("Not a valid pointer at " + (LegacyPointer.Current(reader) - 4) + ": " + value, "Pointer.Read");
                }
                if (file.allowUnsafePointers) {
                    LegacyPointer ptr = file.GetUnsafePointer(value);
                    if (ptr == null) {
                        throw new PointerException("Not a valid pointer at " + (LegacyPointer.Current(reader) - 4) + ": " + value, "Pointer.Read");
                    }
                    return LogPointer(ptr, readFrom, l);
                }
                return null;
            }
            // Hack for R3GC US
            if (l.allowDeadPointers && file.name == "test" && file.pointers[fileOff].file.name == "fix") return null;
            return LogPointer(file.pointers[fileOff], readFrom, l);
        }

        public static LegacyPointer LogPointer(LegacyPointer pointer, LegacyPointer readFrom, MapLoader loader)
        {
            if (UnitySettings.TracePointers && pointer!=null)
            {
                if (!loader.pointerTraces.ContainsKey(pointer))
                {
                    var sf = new StackFrame(2, true);
                    loader.pointerTraces.Add(pointer, new PointerTrace()
                    {
                        lineNumber = sf.GetFileLineNumber(),
                        column = sf.GetFileColumnNumber(),
                        fileName = sf.GetFileName(),
                        methodName = sf.GetMethod().ToString(),
                        code = File.ReadAllLines(sf.GetFileName())[sf.GetFileLineNumber()-1].Trim(),
                        readFrom = readFrom,
                    });
                }
            }
            return pointer;
        }

        public static void Write(Writer writer, LegacyPointer pointer) {
            MapLoader l = MapLoader.Loader;
            uint current_off = (uint)(writer.BaseStream.Position);
            FileWithPointers file = l.GetFileByWriter(writer);
            if (file == null) throw new FormatException("Writer wasn't recognized.");
            file.WritePointer(pointer);
        }

        public void Write(Writer writer) {
            LegacyPointer.Write(writer, this);
        }

        // For readers
        public LegacyPointer Goto(ref Reader reader) {
            LegacyPointer oldPos = Current(reader);
            reader = file.reader;
            reader.BaseStream.Seek(offset + file.baseOffset, SeekOrigin.Begin);
            return oldPos;
        }

        public static LegacyPointer Goto(ref Reader reader, LegacyPointer newPos) {
            if (newPos != null) return newPos.Goto(ref reader);
            return null;
        }

        public static LegacyPointer Current(Reader reader) {
            MapLoader l = MapLoader.Loader;
            uint curPos = (uint)reader.BaseStream.Position;
            FileWithPointers curFile = l.GetFileByReader(reader);
            return new LegacyPointer((uint)(curPos - curFile.baseOffset), curFile);
        }

        public void DoAt(ref Reader reader, Action action) {
            LegacyPointer off_current = Goto(ref reader, this);
            action();
            Goto(ref reader, off_current);
        }

        public static void DoAt(ref Reader reader, LegacyPointer newPos, Action action) {
            if (newPos != null) newPos.DoAt(ref reader, action);
        }

        // For writers
        public LegacyPointer Goto(ref Writer writer) {
            LegacyPointer oldPos = Current(writer);
            writer = file.writer;
            writer.BaseStream.Seek(offset + file.baseOffset, SeekOrigin.Begin);
            return oldPos;
        }

        public static LegacyPointer Goto(ref Writer writer, LegacyPointer newPos) {
            if (newPos != null) return newPos.Goto(ref writer);
            return null;
        }

        public static LegacyPointer Current(Writer writer) {
            MapLoader l = MapLoader.Loader;
            uint curPos = (uint)writer.BaseStream.Position;
            FileWithPointers curFile = l.GetFileByWriter(writer);
            return new LegacyPointer((uint)(curPos - curFile.baseOffset), curFile);
        }

        public void DoAt(ref Writer writer, Action action)
        {
            LegacyPointer off_current = Goto(ref writer, this);
            action();
            Goto(ref writer, off_current);
        }

        public static void DoAt(ref Writer writer, LegacyPointer newPos, Action action)
        {
            if (newPos != null) newPos.DoAt(ref writer, action);
        }

        public struct PointerTrace
        {
            public string methodName;
            public string fileName;
            public int column;
            public int lineNumber;
            public string code;
            public LegacyPointer readFrom;

            public override string ToString()
            {
                return $"from {readFrom}, method {methodName}{Environment.NewLine}{fileName}:{lineNumber}:{column}{Environment.NewLine}{code}";
            }
        }
    }

    public class Pointer<T> where T : OpenSpaceStruct, new() {
        public LegacyPointer pointer;
        public T Value { get; set; }

        public Pointer(Reader reader, bool resolve = false, Action<T> onPreRead = null) {
            pointer = LegacyPointer.Read(reader);
            if (resolve) {
                Resolve(reader, onPreRead: onPreRead);
            }
        }

        public Pointer(LegacyPointer pointer, Reader reader = null, bool resolve = false, Action<T> onPreRead = null) {
            this.pointer = pointer;
            if (resolve) {
                Resolve(reader, onPreRead: onPreRead);
            }
        }

        public Pointer(LegacyPointer pointer, T value) {
            this.pointer = pointer;
            this.Value = value;
        }
        public Pointer() {
            this.pointer = null;
            this.Value = null;
        }

        public Pointer<T> Resolve(Reader reader, Action<T> onPreRead = null) {
            MapLoader l = MapLoader.Loader;
            Value = l.FromOffsetOrRead<T>(reader, pointer, onPreRead: onPreRead);
            return this;
        }

        public static implicit operator T(Pointer<T> a) {
            return a.Value;
        }
        public static implicit operator Pointer<T>(T t) {
            if (t == null) {
                return new Pointer<T>(null, null);
            } else {
                return new Pointer<T>(t.Offset, t);
            }
        }

    }
}
