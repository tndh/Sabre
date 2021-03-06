﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;

namespace Sabre
{
    class WADFile
    {
        public BinaryReader br;
        public Header header;
        public List<Entry> Entries = new List<Entry>();
        public WADFile(string fileLocation, Logger log, List<string> Hashes)
        {
            br = new BinaryReader(File.Open(fileLocation, FileMode.Open));
            header = new Header(br);
            for(int i = 0; i < header.FileCount; i++)
            {
                Entries.Add(new Entry(br));
            }
            foreach(Entry e in Entries)
            {
                if(e.Compression == CompressionType.Compressed)
                {
                    br.BaseStream.Seek(e.FileDataOffset, SeekOrigin.Begin);
                    e.Data = br.ReadBytes((int)e.CompressedSize);
                    e.Data = Functions.DecompressGZip(e.Data);
                    e.Name = Hashes.Find(x => Hash.XXHash(x) == e.XXHash);
                    if (e.Data[0] == 0x50 && e.Data[1] == 0x52 && e.Data[2] == 0x4F && e.Data[3] == 0x50)
                    {
                        e.Compression = CompressionType.BIN;
                    }
                }
                else if(e.Compression == CompressionType.String)
                {
                    br.BaseStream.Seek(e.FileDataOffset, SeekOrigin.Begin);
                    e.Name = Encoding.ASCII.GetString(br.ReadBytes((int)br.ReadUInt32()));
                    if(e.Name.Contains("events.bnk"))
                    {
                        e.Compression = CompressionType.StringEventBank;
                    }
                    else if(e.Name.Contains("audio.bnk"))
                    {
                        e.Compression = CompressionType.StringAudioBank;
                    }
                }
                else if(e.Compression == CompressionType.Uncompressed)
                {
                    br.BaseStream.Seek(e.FileDataOffset, SeekOrigin.Begin);
                    e.Data = br.ReadBytes((int)e.UncompressedSize);
                    e.Name = Hashes.Find(x => Hash.XXHash(x) == e.XXHash);
                }
                else
                {
                    e.Compression = CompressionType.Unknown;
                }
            }
        }
        public class Header
        {
            public string Magic;
            public byte Major;
            public byte Minor;
            public byte ECDSALength;
            public byte[] ECDSA {get; set;}
            public byte[] ZeroPadding;
            public UInt64 Checksum;
            public UInt16 TOCStartOffset;
            public UInt16 TOCFileEntrySize;
            public UInt32 FileCount;
            public Header(BinaryReader br)
            {
                Magic = Encoding.ASCII.GetString(br.ReadBytes(2));
                Major = br.ReadByte();
                Minor = br.ReadByte();
                ECDSALength = br.ReadByte();
                ECDSA = br.ReadBytes(80);
                ZeroPadding = br.ReadBytes(3);
                Checksum = br.ReadUInt64();
                TOCStartOffset = br.ReadUInt16();
                TOCFileEntrySize = br.ReadUInt16();
                FileCount = br.ReadUInt32();
            }
        }
        public class Entry
        {
            public string FileSize { get; set; }
            public string Name { get; set; }
            public byte[] Data;
            public string XXHash { get; set; }
            public UInt32 FileDataOffset;
            public UInt32 CompressedSize;
            public UInt32 UncompressedSize;
            public CompressionType Compression { get; set; }
            public UInt64 SHA256;
            public Entry(BinaryReader br)
            {
                XXHash = br.ReadUInt64().ToString("X2");
                FileDataOffset = br.ReadUInt32();
                CompressedSize = br.ReadUInt32();
                UncompressedSize = br.ReadUInt32();
                Compression = (CompressionType)br.ReadUInt32();
                SHA256 = br.ReadUInt64();
                FileSize = Functions.SizeSuffix(UncompressedSize);
            }
        }
        public enum CompressionType : UInt32
        {
            Uncompressed = 0,
            Compressed = 1,
            String = 2,
            BIN,
            StringAudioBank,
            StringEventBank,
            Unknown
        }
    }
}
