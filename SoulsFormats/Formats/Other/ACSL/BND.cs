﻿using SoulsFormats;
using System;
using System.Collections.Generic;

namespace SoulsFormatsExtended.ACSL
{
    /// <summary>
    /// The format for the Binder file found in Armored Core Silent Line.
    /// </summary>
    public class BND : SoulsFile<BND>
    {
        /// <summary>
        /// The alignment of each file.
        /// <para>The bigger the aligment, the more empty bytes are added as padding. This increases the size of the archive.</para>
        /// </summary>
        public short AlignmentSize { get; set; }

        /// <summary>
        /// Unknown; Seen set to 4.
        /// </summary>
        public short Unk1E { get; set; }

        /// <summary>
        /// Files in this <see cref="BND"/>.
        /// </summary>
        public List<File> Files { get; set; }

        /// <summary>
        /// Returns true if the data appears to be a <see cref="BND"/> of this type.
        /// </summary>
        protected override bool Is(BinaryReaderEx br)
        {
            // Ensure there is enough length to even read the header
            int headerLength = 32;
            if (br.Length < headerLength)
            {
                return false;
            }

            // Ensure the magic is valid
            bool validMagic = br.ReadASCII(4) == "BND\0";
            if (!validMagic)
            {
                return false;
            }

            bool expectedUnk04 = br.ReadASCII(4) == "LTL\0";
            bool expectedUnk08 = br.ReadInt32() == 0;
            br.Position += 4; // File Size
            int fileNum = br.ReadInt32();
            int totalFileSize = br.ReadInt32();
            bool expectedUnk18 = br.ReadInt32() == 0;
            bool expectedAligmentSize = br.ReadInt16() % 2 == 0;
            br.Position += 2; // Unk1E

            int fileEntrySize = 16;
            int entryLength = fileNum * fileEntrySize;
            int headerEntryLength = headerLength + entryLength;

            // Ensure file is long enough for the possible header + entry length
            if (br.Length < headerEntryLength)
            {
                return false;
            }

            int detectedTotalFileSize = 0;
            int previousOffset = -1;
            for (int i = 0; i < fileNum; i++)
            {
                br.Position += 4; // ID
                int size = br.ReadInt32();
                int offset = br.ReadInt32();

                // Ensure offset does not leave file
                if (offset > br.Length)
                {
                    return false;
                }

                // Ensure offset is not less than the previous offset
                if (offset < previousOffset)
                {
                    return false;
                }

                // Ensure that if size is greater than 0, offset is not 0 and does not land before the data even starts
                if (size > 0 && offset < headerEntryLength)
                {
                    return false;
                }

                if (br.ReadInt32() != 0) // Unk04
                {
                    return false;
                }

                detectedTotalFileSize += size;
                previousOffset = offset;
            }

            bool validTotalFileSize = detectedTotalFileSize == totalFileSize;
            return expectedUnk04 && expectedUnk08 && expectedUnk18 && expectedAligmentSize && validTotalFileSize;
        }

        /// <summary>
        /// Reads a <see cref="BND"/> from a stream.
        /// </summary>
        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = false;
            br.AssertASCII("BND\0");
            br.AssertASCII("LTL\0");
            br.AssertInt32(0);
            br.Position += 4; // File Size
            int fileNum = br.ReadInt32();
            br.Position += 4; // Total File Size (Not including padding)
            br.AssertInt32(0);
            AlignmentSize = br.ReadInt16();
            Unk1E = br.ReadInt16();
            br.Pad(AlignmentSize);

            for (int i = 0; i < fileNum; i++)
            {
                Files.Add(new File(br));
                br.Pad(AlignmentSize);
            }
        }

        /// <summary>
        /// Writes this <see cref="BND"/> to a stream.
        /// </summary>
        protected override void Write(BinaryWriterEx bw)
        {
            bw.BigEndian = false;
            bw.WriteASCII("BND\0");
            bw.WriteASCII("LTL\0");
            bw.WriteInt32(0);
            bw.ReserveInt32("FileSize");
            bw.WriteInt32(Files.Count);
            bw.ReserveInt32("TotalFileSize");
            bw.WriteInt32(0);
            bw.WriteInt16(AlignmentSize);
            bw.WriteInt16(Unk1E);

            for (int i = 0; i < Files.Count; i++)
            {
                Files[i].Write(bw, i);
            }
            bw.Pad(AlignmentSize);

            int totalFileSize = 0;
            for (int i = 0; i < Files.Count; i++)
            {
                bw.FillInt32($"FileOffset_{i}", (int)bw.Position);
                bw.WriteBytes(Files[i].Bytes);
                bw.Pad(AlignmentSize);
                totalFileSize += Files[i].Bytes.Length;
            }
            bw.FillInt32("TotalFileSize", totalFileSize);
            bw.FillInt32("FileSize", (int)bw.Position);
        }

        /// <summary>
        /// A file in a <see cref="BND"/>.
        /// </summary>
        public class File
        {
            /// <summary>
            /// The ID of this <see cref="File"/>.
            /// </summary>
            public int ID { get; set; }

            /// <summary>
            /// The raw data of this <see cref="File"/>.
            /// </summary>
            public byte[] Bytes { get; set; }

            /// <summary>
            /// Creates a new <see cref="File"/>.
            /// </summary>
            public File()
            {
                ID = -1;
                Bytes = Array.Empty<byte>();
            }

            /// <summary>
            /// Reads a <see cref="File"/> from a stream.
            /// </summary>
            internal File(BinaryReaderEx br)
            {
                ID = br.ReadInt32();
                int size = br.ReadInt32();
                int offset = br.ReadInt32();
                br.AssertInt32(0); // Potential name offset?

                if (offset > 0)
                {
                    if (size > 0)
                    {
                        Bytes = br.GetBytes(offset, size);
                    }
                    else
                    {
                        Bytes = Array.Empty<byte>();
                    }
                }
                else
                {
                    Bytes = Array.Empty<byte>();
                }
            }

            /// <summary>
            /// Writes this <see cref="File"/> entry to a stream.
            /// </summary>
            internal void Write(BinaryWriterEx bw, int index)
            {
                bw.WriteInt32(ID);
                bw.WriteInt32(Bytes.Length);
                bw.ReserveInt32($"FileOffset_{index}");
                bw.WriteInt32(0);
            }
        }
    }
}
