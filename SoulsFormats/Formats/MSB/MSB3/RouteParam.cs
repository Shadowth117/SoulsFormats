﻿using System.Collections.Generic;

namespace SoulsFormats
{
    public partial class MSB3
    {
        /// <summary>
        /// A section containing routes. Purpose unknown.
        /// </summary>
        public class RouteParam : Param<Route>
        {
            internal override int Version => 3;
            internal override string Type => "ROUTE_PARAM_ST";

            /// <summary>
            /// The routes in this section.
            /// </summary>
            public List<Route> Routes { get; set; }

            /// <summary>
            /// Creates a new RouteParam with no routes.
            /// </summary>
            public RouteParam()
            {
                Routes = new List<Route>();
            }

            /// <summary>
            /// Returns every route in the order they will be written.
            /// </summary>
            public override List<Route> GetEntries()
            {
                return Routes;
            }

            internal override Route ReadEntry(BinaryReaderEx br)
            {
                return Routes.EchoAdd(new Route(br));
            }

            internal override void WriteEntry(BinaryWriterEx bw, int id, Route entry)
            {
                entry.Write(bw, id);
            }
        }

        /// <summary>
        /// Unknown.
        /// </summary>
        public class Route
        {
            /// <summary>
            /// The name of this route.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public int Unk08 { get; set; }

            /// <summary>
            /// Unknown.
            /// </summary>
            public int Unk0C { get; set; }

            /// <summary>
            /// Creates a new Route with default values.
            /// </summary>
            public Route()
            {
                Name = "";
            }

            internal Route(BinaryReaderEx br)
            {
                long start = br.Position;

                long nameOffset = br.ReadInt64();
                Unk08 = br.ReadInt32();
                Unk0C = br.ReadInt32();
                br.AssertInt32(4); // Type
                br.ReadInt32(); // ID
                br.AssertPattern(0x68, 0x00);

                Name = br.GetUTF16(start + nameOffset);
            }

            internal void Write(BinaryWriterEx bw, int id)
            {
                long start = bw.Position;

                bw.ReserveInt64("NameOffset");
                bw.WriteInt32(Unk08);
                bw.WriteInt32(Unk0C);
                bw.WriteInt32(4);
                bw.WriteInt32(id);
                bw.WritePattern(0x68, 0x00);

                bw.FillInt64("NameOffset", bw.Position - start);
                bw.WriteUTF16(Name, true);
                bw.Pad(8);
            }

            /// <summary>
            /// Returns the name and values of this route.
            /// </summary>
            public override string ToString()
            {
                return $"\"{Name}\" {Unk08} {Unk0C}";
            }
        }
    }
}