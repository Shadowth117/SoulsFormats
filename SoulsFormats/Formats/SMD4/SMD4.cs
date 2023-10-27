﻿using System.Collections.Generic;
using System.Numerics;

namespace SoulsFormats
{
    /// <summary>
    /// A shadow mesh model format in Armored Core 4thgen and 5thgen games.
    /// </summary>
    public partial class SMD4 : SoulsFile<SMD4>
    {
        /// <summary>
        /// General values for this model.
        /// </summary>
        public SMDHeader Header { get; set; }

        /// <summary>
        /// Unknown indices of some kind.
        /// </summary>
        public List<int> UnkIndices { get; set; }

        /// <summary>
        /// Bones used by this model, may or may not be the full skeleton.
        /// </summary>
        public List<Bone> Bones { get; set; }

        /// <summary>
        /// Individual chunks of the model.
        /// </summary>
        public List<Mesh> Meshes { get; set; }

        /// <summary>
        /// Create a new SMD4 with default values.
        /// </summary>
        public SMD4()
        {
            Header = new SMDHeader();
            UnkIndices = new List<int>();
            Bones = new List<Bone>();
            Meshes = new List<Mesh>();
        }

        /// <summary>
        /// Clone an existing SMD4.
        /// </summary>
        public SMD4(SMD4 smd)
        {
            Header = new SMDHeader();
            UnkIndices = new List<int>();
            Bones = new List<Bone>();
            Meshes = new List<Mesh>();

            Header.Version = smd.Header.Version;
            Vector3 min = smd.Header.BoundingBoxMin;
            Vector3 max = smd.Header.BoundingBoxMax;
            Header.BoundingBoxMin = new Vector3(min.X, min.Y, min.Z);
            Header.BoundingBoxMax = new Vector3(max.X, max.Y, max.Z);

            for (int i = 0; i < smd.UnkIndices.Count; i++)
                UnkIndices.Add(smd.UnkIndices[i]);
            foreach (Bone bone in smd.Bones)
                Bones.Add(new Bone(bone));
            foreach (Mesh mesh in smd.Meshes)
                Meshes.Add(new Mesh(mesh));
        }

        /// <summary>
        /// Returns true if the data appears to be an SMD4 model.
        /// </summary>
        protected override bool Is(BinaryReaderEx br)
        {
            if (br.Length < 128)
                return false;
            return br.ReadASCII(4) == "SMD4";
        }

        /// <summary>
        /// Reads SMD4 data from a BinaryReaderEx.
        /// </summary>
        protected override void Read(BinaryReaderEx br)
        {
            br.BigEndian = true;
            br.AssertASCII("SMD4");
            Header = new SMDHeader();

            Header.Version = br.ReadInt32();
            int dataOffset = br.ReadInt32();
            int dataSize = br.ReadInt32();
            int unkIndicesCount = br.ReadInt32();
            int boneCount = br.ReadInt32();
            int meshCount = br.ReadInt32();
            int vertexBufferCount = br.AssertInt32(meshCount);

            Header.BoundingBoxMin = br.ReadVector3();
            Header.BoundingBoxMax = br.ReadVector3();
            int faceCount = br.ReadInt32();
            int totalFaceCount = br.ReadInt32();

            for (int i = 0; i < 16; i++)
                br.AssertInt32(0);

            UnkIndices = new List<int>();
            Bones = new List<Bone>();
            Meshes = new List<Mesh>();

            UnkIndices.AddRange(br.ReadInt32s(unkIndicesCount));
            for (int i = 0; i < boneCount; i++)
                Bones.Add(new Bone(br));
            for (int i = 0; i < meshCount; i++)
                Meshes.Add(new Mesh(br, dataOffset));
        }

        /// <summary>
        /// Writes SMD4 data to a BinaryWriterEx.
        /// </summary>
        protected override void Write(BinaryWriterEx bw)
        {
            bw.BigEndian = true;
            bw.WriteASCII("SMD4", false);
            bw.WriteInt32(Header.Version);
            bw.ReserveInt32("dataOffset");
            bw.ReserveInt32("dataSize");
            bw.WriteInt32(UnkIndices.Count);
            bw.WriteInt32(Bones.Count);
            bw.WriteInt32(Meshes.Count);
            bw.WriteInt32(Meshes.Count); // Vertex Buffer Count

            bw.WriteVector3(Header.BoundingBoxMin);
            bw.WriteVector3(Header.BoundingBoxMax);

            int faceCount = GetFaceCount();
            bw.WriteInt32(faceCount);
            bw.WriteInt32(faceCount); // Not entirely accurate but oh well
            bw.WriteInt32s(new int[16]);

            bw.WriteInt32s(UnkIndices);
            foreach (Bone bone in Bones)
                bone.Write(bw);
            for (int i = 0; i < Meshes.Count; i++)
                Meshes[i].Write(bw, i);

            // Fill Data
            bw.Pad(0x800);
            int dataStart = (int)bw.Position;
            bw.FillInt32("dataOffset", dataStart);
            for (int i = 0; i < Meshes.Count; i++)
            {
                Mesh mesh = Meshes[i];
                bw.FillInt32($"vertexIndicesOffset_{i}", (int)bw.Position);
                bw.WriteUInt16s(mesh.VertexIndices);
                bw.Pad(0x10);

                bw.FillInt32($"vertexBufferOffset_{i}", (int)bw.Position);
                for (int k = 0; k < mesh.Vertices.Count; i++)
                    mesh.Vertices[i].Write(bw);
            }
            bw.Pad(0x800);

            int dataEnd = (int)bw.Position;
            bw.FillInt32("dataSize", dataEnd - dataStart);
        }

        /// <summary>
        /// Get the total calculated face count from the VertexIndices of all Meshes in this model.
        /// </summary>
        public int GetFaceCount()
        {
            int count = 0;
            foreach (Mesh mesh in Meshes)
            {
                count += mesh.GetFaceCount();
            }
            return count;
        }

        /// <summary>
        /// Get the total calculated strip count from the VertexIndices of all Meshes in this model.
        /// </summary>
        public int GetStripCount()
        {
            int count = 0;
            foreach (Mesh mesh in Meshes)
            {
                count += mesh.GetStripCount();
            }
            return count;
        }

        /// <summary>
        /// An SMD4 header containing general values for this model.
        /// </summary>
        public class SMDHeader
        {
            /// <summary>
            /// Version of the format indicating presence of various features.
            /// </summary>
            public int Version { get; set; }

            /// <summary>
            /// Minimum extent of the entire model.
            /// </summary>
            public Vector3 BoundingBoxMin { get; set; }

            /// <summary>
            /// Maximum extent of the entire model.
            /// </summary>
            public Vector3 BoundingBoxMax { get; set; }

            /// <summary>
            /// Creates a SMDHeader with default values.
            /// </summary>
            public SMDHeader()
            {
                Version = 0x40001;
            }
        }
    }
}
