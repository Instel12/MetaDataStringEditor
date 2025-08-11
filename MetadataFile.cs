using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace MetaDataStringEditor
{
    class MetadataFile : IDisposable
    {
        public BinaryReader reader;

        private uint stringLiteralOffset;
        private uint stringLiteralCount;
        private long DataInfoPosition;
        private uint stringLiteralDataOffset;
        private uint stringLiteralDataCount;
        private List<StringLiteral> stringLiterals = new List<StringLiteral>();
        public List<byte[]> strBytes = new List<byte[]>();

        public MetadataFile(string fullName)
        {
            reader = new BinaryReader(File.OpenRead(fullName));

            // Read the file
            ReadHeader();

            // Read string table
            ReadLiteral();
            ReadStrByte();

            Logger.I("Base read completed");
        }

        private void ReadHeader()
        {
            Logger.I("Read header");
            uint vansity = reader.ReadUInt32();
            if (vansity != 0xFAB11BAF)
            {
                throw new Exception("Flag check failed");
            }
            int version = reader.ReadInt32();
            stringLiteralOffset = reader.ReadUInt32();      // Position of the list section (unchanged later)
            stringLiteralCount = reader.ReadUInt32();       // Size of the list section (unchanged later)
            DataInfoPosition = reader.BaseStream.Position;  // Store the current position (used later)
            stringLiteralDataOffset = reader.ReadUInt32();  // Position of the data section (may change)
            stringLiteralDataCount = reader.ReadUInt32();   // Length of the data section (may change)
        }

        private void ReadLiteral()
        {
            Logger.I("Read literal");
            ProgressBar.SetMax((int)stringLiteralCount / 8);

            reader.BaseStream.Position = stringLiteralOffset;
            for (int i = 0; i < stringLiteralCount / 8; i++)
            {
                stringLiterals.Add(new StringLiteral
                {
                    Length = reader.ReadUInt32(),
                    Offset = reader.ReadUInt32()
                });
                ProgressBar.Report();
            }
        }

        private void ReadStrByte()
        {
            Logger.I("Read string bytes");
            ProgressBar.SetMax(stringLiterals.Count);

            for (int i = 0; i < stringLiterals.Count; i++)
            {
                reader.BaseStream.Position = stringLiteralDataOffset + stringLiterals[i].Offset;
                strBytes.Add(reader.ReadBytes((int)stringLiterals[i].Length));
                ProgressBar.Report();
            }
        }

        public void WriteToNewFile(string fileName)
        {
            BinaryWriter writer = new BinaryWriter(File.Create(fileName));

            // Copy everything to the new file first
            reader.BaseStream.Position = 0;
            reader.BaseStream.CopyTo(writer.BaseStream);

            // Update literals
            Logger.I("Update literal");
            ProgressBar.SetMax(stringLiterals.Count);
            writer.BaseStream.Position = stringLiteralOffset;
            uint count = 0;
            for (int i = 0; i < stringLiterals.Count; i++)
            {

                stringLiterals[i].Offset = count;
                stringLiterals[i].Length = (uint)strBytes[i].Length;

                writer.Write(stringLiterals[i].Length);
                writer.Write(stringLiterals[i].Offset);
                count += stringLiterals[i].Length;

                ProgressBar.Report();
            }

            // Align to 4 bytes. Not sure if absolutely required,
            // but Unity does it, so better to include it.
            var tmp = (stringLiteralDataOffset + count) % 4;
            if (tmp != 0) count += 4 - tmp;

            // Check if there’s enough space to place the data
            if (count > stringLiteralDataCount)
            {
                // Check if there’s any other data after the data section
                // If not, we can just extend it
                if (stringLiteralDataOffset + stringLiteralDataCount < writer.BaseStream.Length)
                {
                    // Not enough space, and can’t extend directly,
                    // so move the whole section to the end of the file
                    stringLiteralDataOffset = (uint)writer.BaseStream.Length;
                }
            }
            stringLiteralDataCount = count;

            // Write the strings
            Logger.I("Update string");
            ProgressBar.SetMax(strBytes.Count);
            writer.BaseStream.Position = stringLiteralDataOffset;
            for (int i = 0; i < strBytes.Count; i++)
            {
                writer.Write(strBytes[i]);
                ProgressBar.Report();
            }

            // Update the header
            Logger.I("Update header");
            writer.BaseStream.Position = DataInfoPosition;
            writer.Write(stringLiteralDataOffset);
            writer.Write(stringLiteralDataCount);

            Logger.I("Update completed");
            writer.Close();
        }

        public void Dispose()
        {
            reader?.Dispose();
        }

        public class StringLiteral
        {
            public uint Length;
            public uint Offset;
        }
    }
}
