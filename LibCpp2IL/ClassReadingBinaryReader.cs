using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace LibCpp2IL
{
    public class ClassReadingBinaryReader : BinaryReader
    {
        private readonly object PositionShiftLock = new object();
        
        private MethodInfo readClass;
        public bool is32Bit;

        public ClassReadingBinaryReader(Stream input) : base(input)
        {
            readClass = GetType().GetMethod("ReadClass")!;
        }

        public long Position
        {
            get => BaseStream.Position;
            set => BaseStream.Position = value;
        }

        private object? ReadPrimitive(MemberInfo type)
        {
            var typename = type.Name;
            switch (typename)
            {
                case "Int32":
                    return ReadInt32();
                case "UInt32":
                    return ReadUInt32();
                case "Int16":
                    return ReadInt16();
                case "UInt16":
                    return ReadUInt16();
                case "Byte":
                    return ReadByte();
                case "Int64" when is32Bit:
                    return ReadInt32();
                case "Int64":
                    return ReadInt64();
                case "UInt64" when is32Bit:
                    return ReadUInt32();
                case "UInt64":
                    return ReadUInt64();
                default:
                    return null;
            }
        }

        public T ReadClass<T>(long offset) where T : new()
        {
            var t = new T();
            lock (PositionShiftLock)
            {
                if (offset >= 0) Position = offset;

                var type = typeof(T);
                if (type.IsPrimitive)
                {
                    var value = ReadPrimitive(type);

                    //32-bit fixes...
                    if (value is uint && typeof(T).Name == "UInt64")
                        value = Convert.ToUInt64(value);
                    if (value is int && typeof(T).Name == "Int64")
                        value = Convert.ToInt64(value);

                    return (T) value!;
                }
                
                foreach (var i in t.GetType().GetFields())
                {
                    var attr = (VersionAttribute?) Attribute.GetCustomAttribute(i, typeof(VersionAttribute));
                    var nonSerializedAttribute = (NonSerializedAttribute?) Attribute.GetCustomAttribute(i, typeof(NonSerializedAttribute));

                    if (nonSerializedAttribute != null) continue;

                    if (attr != null)
                    {
                        if (LibCpp2IlMain.MetadataVersion < attr.Min || LibCpp2IlMain.MetadataVersion > attr.Max)
                            continue;
                    }

                    if (i.FieldType.IsPrimitive)
                    {
                        i.SetValue(t, ReadPrimitive(i.FieldType));
                    }
                    else
                    {
                        var gm = readClass.MakeGenericMethod(i.FieldType);
                        var o = gm.Invoke(this, new object[] {-1});
                        i.SetValue(t, o);
                        break;
                    }
                }
            }

            return t;
        }

        public T[] ReadClassArray<T>(long offset, long count) where T : new()
        {
            var t = new T[count];
            lock (PositionShiftLock)
            {

                if (offset != -1) Position = offset;

                for (var i = 0; i < count; i++)
                {
                    t[i] = ReadClass<T>(-1);
                }
            }

            return t;
        }

        public string ReadStringToNull(long offset)
        {
            var bytes = new List<byte>();
            lock (PositionShiftLock)
            {
                Position = offset;
                byte b;
                while ((b = ReadByte()) != 0)
                    bytes.Add(b);
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
    }
}