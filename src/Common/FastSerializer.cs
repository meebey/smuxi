// FastSerializer.cs.  Provides SerializationWriter and SerializationReader classes to help high speed serialization.
// This short example shows how they're used:
//
//  [Serializable]
//  public class TestObject : ISerializable {                       // Class must be ISerializable
//    public long   x;
//    public string y;
//
//    public void GetObjectData (SerializationInfo info, StreamingContext ctxt) {  // Serialization method
//      SerializationWriter sw = SerializationWriter.GetWriter ();                 // Get a Writer
//      sw.Write(x);                                                              // Write fields
//      sw.Write(y);                                                              // ditto
//      sw.AddToInfo (info);                                                       // Add the Writer to info
//    }
//
//    public TestObject (SerializationInfo info, StreamingContext ctxt) {          // Deserialization .ctor
//      SerializationReader sr = SerializationReader.GetReader (info);             // Get a Reader from info
//      x = sr.ReadInt64 ();                                                       // Read a field
//      y = sr.ReadInt64 ();                                                       // ditto
//    }
//
//  }
//
// Author: Tim Haynes, May 2006.  Use freely as you see fit.

// Author: Mirco Bauer <m.bauer@gsd-software.net>, Aug 2007.
// Added .NET 1.1 support
// Added BinaryFormatter optimization
// Using "is" operator instead of switch on GetType() string.

// Author: Mirco Bauer <meebey@meebey.net>, Sep 2007
// Applied smuxi Coding-Standards

using System;
using System.IO;
using System.Reflection;
using System.Collections;
#if NET_2_0
using System.Collections.Generic;
#endif
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Smuxi.Common
{
    // Enum for the standard types handled by Read/WriteObject()
    internal enum ObjType : byte
    {
        Null = 0,
        Boolean,
        Byte,
        UInt16,
        UInt32,
        UInt64,
        SByte,
        Int16,
        Int32,
        Int64,
        Char,
        String,
        Single,
        Double,
        Decimal,
        DateTime,
        ByteArray,
        CharArray,
        Unknown
    }
    
    /// <summary> SerializationWriter.  Extends BinaryWriter to add additional data types,
    /// handle null strings and simplify use with ISerializable. </summary>
    public class SerializationWriter : BinaryWriter
    {
        private static BinaryFormatter _BinaryFormatter = new BinaryFormatter(); 

        private SerializationWriter(Stream s) : base(s)
        {
        }

        /// <summary> Static method to initialise the writer with a suitable MemoryStream. </summary>
        public static SerializationWriter GetWriter()
        {
            MemoryStream ms = new MemoryStream(1024);
            return new SerializationWriter(ms);
        }

        internal void Write(ObjType type)
        {
            Write((byte) type);
        }
        
        /// <summary> Writes a string to the buffer.  Overrides the base implementation so it can cope with nulls </summary>
        public override void Write(string str)
        {
            if (str == null) {
              Write(ObjType.Null);
              return;
            }
            
            Write(ObjType.String);
            base.Write(str);
        }

        /// <summary> Writes a byte array to the buffer.  Overrides the base implementation to
        /// send the length of the array which is needed when it is retrieved </summary>

        public override void Write(byte[] b) {
            WriteBytes(b);
        }

        public void WriteBytes(byte[] b) {
         if (b==null) {
           Write(-1);
         } else {
           int len = b.Length;
           Write(len);
           if (len>0) base.Write(b);
         }
        }

        /// <summary> Writes a char array to the buffer.  Overrides the base implementation to
        /// sends the length of the array which is needed when it is read. </summary>
        public override void Write(char[] c) {
         if (c==null) {
           Write(-1);
         } else {
           int len = c.Length;
           Write(len);
           if (len>0) base.Write(c);
         }
        }

        /// <summary> Writes a DateTime to the buffer. <summary>
        public void Write(DateTime dt)
        {
            Write(dt.Ticks);
        }

        public void Write(ICollection c)
        {
            if (c == null) {
                Write(-1);
                return;
            }
            
            Write(c.Count);
            foreach (Object item in c) {
                WriteObject(item);
            }
        }

        public void Write(IDictionary d)
        {
            if (d == null) {
                Write(-1);
                return;
            }
         
            Write(d.Count);
            foreach (DictionaryEntry de in d) {
                WriteObject(de.Key);
                WriteObject(de.Value);
            }
        }

#if NET_2_0
        /// <summary> Writes a generic ICollection (such as an IList<T>) to the buffer. </summary>
        public void Write<T> (ICollection<T> c) {
         if (c==null) {
           Write(-1);
         } else {
           Write(c.Count);
           foreach (T item in c) WriteObject (item);
         }
        }

        /// <summary> Writes a generic IDictionary to the buffer. </summary>
        public void Write<T,U> (IDictionary<T,U> d) {
         if (d==null) {
           Write(-1);
         } else {
           Write(d.Count);
           foreach (KeyValuePair<T,U> kvp in d) {
             WriteObject (kvp.Key);
             WriteObject (kvp.Value);
           }
         }
        }
#endif 

        /// <summary> Writes an arbitrary object to the buffer.  Useful where we have something of type "object"
        /// and don't know how to treat it.  This works out the best method to use to write to the buffer. </summary>
        public void WriteObject(object obj)
        {
            if (obj == null) {
                Write(ObjType.Null);
                return;
            }
            
            if (obj is Boolean) {
                Write(ObjType.Boolean);
                Write((Boolean) obj);
            } else if (obj is Byte) {
                Write(ObjType.Byte);
                Write((Byte) obj);
            } else if (obj is UInt16) {
                Write(ObjType.UInt16);
                Write((ushort) obj);
            } else if (obj is UInt32) {
                Write(ObjType.UInt32);
                Write((uint) obj);
            } else if (obj is UInt64) {
                Write(ObjType.UInt64);
                Write((ulong) obj);
            } else if (obj is SByte) {
                Write(ObjType.SByte);
                Write((sbyte) obj);
            } else if (obj is Int16) {
                Write(ObjType.Int16);
                Write((short) obj);
            } else if (obj is Int32) {
                Write(ObjType.Int32);
                Write((int) obj);
            } else if (obj is Int64) {
                Write(ObjType.Int64);
                Write((long) obj);
            } else if (obj is Char) {
                Write(ObjType.Char);
                base.Write((char) obj);
            } else if (obj is String) {
                Write(ObjType.String);
                base.Write((string) obj);
            } else if (obj is Single) {
                Write(ObjType.Single);
                Write((float) obj);
            } else if (obj is Double) {
                Write(ObjType.Double);
                Write((double) obj);
            } else if (obj is Decimal) {
                Write(ObjType.Decimal);
                Write((decimal) obj);
            } else if (obj is DateTime) {
                Write(ObjType.DateTime);
                Write((DateTime) obj);
            } else if (obj is Byte[]) {
                Write(ObjType.ByteArray);
                base.Write((byte[]) obj);
            } else if (obj is Char[]) {
                Write(ObjType.CharArray);
                base.Write((char[]) obj);
            } else {
                Write(ObjType.Unknown);
                _BinaryFormatter.Serialize(BaseStream, obj);
            }
        }

        /// <summary> Adds the SerializationWriter buffer to the SerializationInfo at the end of GetObjectData(). </summary>
        public void AddToInfo(SerializationInfo info)
        {
            var b = GetData();
            info.AddValue("X", b, typeof(byte[]));
        }

        public byte[] GetData()
        {
            return ((MemoryStream) BaseStream).ToArray();
        }
    }

    /// <summary> SerializationReader.  Extends BinaryReader to add additional data types,
    /// handle null strings and simplify use with ISerializable. </summary>
    public class SerializationReader : BinaryReader
    {
        private static BinaryFormatter _BinaryFormatter = new BinaryFormatter();

        protected SerializationReader(Stream s) : base(s)
        {
        }
        
        internal ObjType ReadObjType()
        {
            return (ObjType) ReadByte();
        }
        
        /// <summary> Static method to take a SerializationInfo object (an input to an ISerializable constructor)
        /// and produce a SerializationReader from which serialized objects can be read </summary>.
        public static SerializationReader GetReader(SerializationInfo info)
        {
            byte[] byteArray = (byte[]) info.GetValue("X", typeof(byte[]));
            MemoryStream ms = new MemoryStream(byteArray);
            return new SerializationReader(ms);
        }

        public static SerializationReader GetReader(byte[] data)
        {
            MemoryStream ms = new MemoryStream(data);
            return new SerializationReader(ms);
        }

        /// <summary> Reads a string from the buffer.  Overrides the base implementation so it can cope with nulls. </summary>
        public override string ReadString()
        {
            ObjType t = ReadObjType();
            if (t == ObjType.Null) {
                return null;
            }
            if (t == ObjType.String) {
                return base.ReadString();
            }
            throw new SerializationException();
        }

        /// <summary> Reads a byte array from the buffer, handling nulls and the array length. </summary>
        public byte[] ReadByteArray ()
        {
         int len = ReadInt32 ();
         if (len>0) return ReadBytes (len);
         if (len<0) return null;
         return new byte[0];
        }

        /// <summary> Reads a char array from the buffer, handling nulls and the array length. </summary>
        public char[] ReadCharArray ()
        {
         int len = ReadInt32 ();
         if (len>0) return ReadChars (len);
         if (len<0) return null;
         return new char[0];
        }

        /// <summary> Reads a DateTime from the buffer. </summary>
        public DateTime ReadDateTime()
        {
            return new DateTime(ReadInt64());
        }

        public IList ReadList()
        {
            int count = ReadInt32();
            if (count < 0) {
              return null;
            }
            
            IList list = new ArrayList(count);
            for (int i = 0; i < count; i++) {
                list.Add(ReadObject());
            }
            
            return list;
        }

        public IDictionary ReadDictionary()
        {
            int count = ReadInt32();
            if (count < 0) {
                return null;
            }
            
            // BUG: if the dictionary was not a hashtable or custom comparer were
            // used, we might get problems
            IDictionary dict = new Hashtable(count);
            for (int i = 0; i < count; i++) {
                dict.Add(ReadObject(), ReadObject());
            }
            return dict;
        }

#if NET_2_0
        /// <summary> Reads a generic list from the buffer. </summary>
        public IList<T> ReadList<T>()
        {
            int count = ReadInt32();
            if (count < 0) {
                return null;
            }
            
            IList<T> list = new List<T>(count);
            for (int i = 0; i<count; i++) {
                list.Add((T) ReadObject());
            }
            return list;
        }

        /// <summary> Reads a generic Dictionary from the buffer. </summary>
        public IDictionary<K, V> ReadDictionary<K, V>()
        {
            int count = ReadInt32();
            if (count < 0) {
               return null;
            }
            
            // BUG: if the dictionary was not a Dictionary or custom comparer were
            // used, we might get problems
            IDictionary<K, V> dict = new Dictionary<K, V>(count);
            for (int i = 0; i<count; i++) {
               dict.Add((K) ReadObject(), (V) ReadObject());
            }
            return dict;
        }
#endif

        /// <summary> Reads an object which was added to the buffer by WriteObject. </summary>
        public object ReadObject()
        {
            ObjType t = (ObjType) ReadByte();
            switch (t) {
                case ObjType.Null:
                    return null;
                case ObjType.Boolean:
                    return ReadBoolean();
                case ObjType.Byte:
                    return ReadByte();
                case ObjType.UInt16:
                    return ReadUInt16();
                case ObjType.UInt32:
                    return ReadUInt32();
                case ObjType.UInt64:
                    return ReadUInt64();
                case ObjType.SByte:
                    return ReadSByte();
                case ObjType.Int16: 
                    return ReadInt16();
                case ObjType.Int32: 
                    return ReadInt32();
                case ObjType.Int64:
                    return ReadInt64();
                case ObjType.Char:
                    return ReadChar();
                case ObjType.String:
                    return base.ReadString();
                case ObjType.Single:
                    return ReadSingle();
                case ObjType.Double: 
                    return ReadDouble();
                case ObjType.Decimal:
                    return ReadDecimal();
                case ObjType.DateTime:
                    return ReadDateTime();
                case ObjType.ByteArray: 
                    return ReadByteArray();
                case ObjType.CharArray:
                    return ReadCharArray();
                case ObjType.Unknown:
                    return _BinaryFormatter.Deserialize(BaseStream);
                default:
                    throw new SerializationException();
            }
        }
    }
}
