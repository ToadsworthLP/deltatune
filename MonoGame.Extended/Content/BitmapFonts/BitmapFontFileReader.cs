﻿// Copyright (c) Craftwork Games. All rights reserved.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MonoGame.Extended.Content.BitmapFonts
{
    /// <summary>
    /// A utility class for reading the contents of a font file in the AngleCode BMFont file spec.
    /// </summary>
    public static class BitmapFontFileReader
    {
        /// <summary>
        /// Reads the content of the font file at the path specified.
        /// </summary>
        /// <param name="path">The path to the font file to read.</param>
        /// <returns>A <see cref="BitmapFontFileContent"/> instance containing the results of the read operation.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the header for the file contents does not match a known header format.
        /// </exception>
        public static BitmapFontFileContent Read(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                return Read(stream, path);
            }
        }

        /// <summary>
        /// Reads the content of the font file at the path specified.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> containing the font file contents to read.</param>
        /// <returns>A <see cref="BitmapFontFileContent"/> instance containing the results of the read operation.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the header for the file contents does not match a known header format.
        /// </exception>
        [Obsolete("Use the overload that takes an explicit name parameter.")]
        public static BitmapFontFileContent Read(FileStream stream)
        {
            return Read(stream, stream.Name);
        }

        /// <summary>
        /// Reads the content of the font file at the path specified.
        /// </summary>
        /// <param name="stream">A <see cref="Stream"/> containing the font file contents to read.</param>
        /// <param name="name">The name or path that uniquely identifies this <see cref="BitmapFontFileContent"/>.</param>
        /// <returns>A <see cref="BitmapFontFileContent"/> instance containing the results of the read operation.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the header for the file contents does not match a known header format.
        /// </exception>
        public static BitmapFontFileContent Read(Stream stream, string name)
        {
            long position = stream.Position;
            var sig = stream.ReadByte();
            stream.Position = position;

            BitmapFontFileContent bmfFile;
            switch (sig)
            {
                //  Binary header begins with [66, 77, 70, 3]
                case 66:
                    bmfFile = ReadBinary(stream);
                    break;
                //  Text format begins with [105, 110, 102, 111]
                case 105:
                    bmfFile = ReadText(stream);
                    break;
                default:
                    //  Unknown format
                    throw new InvalidOperationException("This does not appear to be a valid BMFont file!");
            }

            bmfFile.Path = name;

            return bmfFile;
        }



        #region ------------------------ Read BMFFont Binary Formatted File -----------------------------------------------

        private static BitmapFontFileContent ReadBinary(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                return ReadBinary(reader);
            }
        }

        private static BitmapFontFileContent ReadBinary(BinaryReader reader)
        {
            BitmapFontFileContent bmfFile = new BitmapFontFileContent();
            bmfFile.Header = AsType<BitmapFontFileContent.HeaderBlock>(reader.ReadBytes(BitmapFontFileContent.HeaderBlock.StructSize));

            if (!bmfFile.Header.IsValid)
            {
                throw new InvalidOperationException($"The BMFFont file header is invalid, this does not appear to be a valid BMFont file");
            }


            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte blockType = reader.ReadByte();
                int blockSize = reader.ReadInt32();

                switch (blockType)
                {
                    case 1:
                        bmfFile.Info = AsType<BitmapFontFileContent.InfoBlock>(reader.ReadBytes(BitmapFontFileContent.InfoBlock.StructSize));
                        int stringLen = blockSize - BitmapFontFileContent.InfoBlock.StructSize;
                        bmfFile.FontName = Encoding.UTF8.GetString(reader.ReadBytes(stringLen)).Replace("\0", string.Empty);
                        break;
                    case 2:
                        bmfFile.Common = AsType<BitmapFontFileContent.CommonBlock>(reader.ReadBytes(BitmapFontFileContent.CommonBlock.StructSize));
                        break;
                    case 3:
                        string[] pages = Encoding.UTF8.GetString(reader.ReadBytes(blockSize)).Split(new[] {'\0'}, StringSplitOptions.RemoveEmptyEntries);
                        bmfFile.Pages.AddRange(pages);
                        break;
                    case 4:
                        int characterCount = blockSize / BitmapFontFileContent.CharacterBlock.StructSize;
                        for (int c = 0; c < characterCount; c++)
                        {
                            BitmapFontFileContent.CharacterBlock character = AsType<BitmapFontFileContent.CharacterBlock>(reader.ReadBytes(BitmapFontFileContent.CharacterBlock.StructSize));
                            bmfFile.Characters.Add(character);
                        }
                        break;
                    case 5:
                        int kerningCount = blockSize / BitmapFontFileContent.KerningPairsBlock.StructSize;
                        for (int k = 0; k < kerningCount; k++)
                        {
                            BitmapFontFileContent.KerningPairsBlock kerning = AsType<BitmapFontFileContent.KerningPairsBlock>(reader.ReadBytes(BitmapFontFileContent.KerningPairsBlock.StructSize));
                            bmfFile.Kernings.Add(kerning);
                        }
                        break;
                    default:
                        reader.BaseStream.Seek(blockSize, SeekOrigin.Current);
                        break;
                }
            }

            return bmfFile;
        }

        private static T AsType<T>(byte[] buffer) where T : struct
        {
            T value;
            try
            {
                unsafe
                {
                    fixed (byte* ptr = buffer)
                    {
                        value = Marshal.PtrToStructure<T>((IntPtr)ptr);
                    }
                }
                return value;
            }
            catch
            {
                return default;
            }

        }

        #endregion --------------------- Read BMFFont Binary Formatted File -----------------------------------------------

        #region ------------------------ Read BMFFont Text Formatted File -------------------------------------------------

        private static BitmapFontFileContent ReadText(Stream stream)
        {
            var bmfFile = new BitmapFontFileContent();

            // Text does not contain the header like binary so we manually create it
            bmfFile.Header = new BitmapFontFileContent.HeaderBlock() { B = (byte)'B', M = (byte)'M', F = (byte)'F', Version = 3 };

            using (var reader = new StreamReader(stream))
            {
                string line = default;
                while ((line = reader.ReadLine()) != null)
                {
                    List<string> tokens = GetTokens(line.ToCharArray());
                    if (tokens.Count == 0)
                    {
                        continue;
                    }

                    string[] tokensPastFirst = tokens.GetRange(1, tokens.Count - 1).ToArray();
                    switch (tokens[0])
                    {
                        case "info":
                            ReadInfoTokens(bmfFile, tokensPastFirst);
                            break;
                        case "common":
                            ReadCommonTokens(bmfFile, tokensPastFirst);
                            break;
                        case "page":
                            ReadPageTokens(bmfFile, tokensPastFirst);
                            break;
                        case "char":
                            ReadCharacterTokens(bmfFile, tokensPastFirst);
                            break;
                        case "kerning":
                            ReadKerningTokens(bmfFile, tokensPastFirst);
                            break;
                    }
                }

                return bmfFile;
            }
        }

        private static void ReadInfoTokens(BitmapFontFileContent bmfFile, string[] tokens)
        {
            for (int i = 0; i < tokens.Length; ++i)
            {
                var split = tokens[i].Split('=');

                if (split.Length != 2)
                {
                    continue;
                }

                switch (split[0])
                {
                    case "face":
                        bmfFile.FontName = split[1].Replace("\"", string.Empty);
                        break;
                    case "size":
                        bmfFile.Info.FontSize = Convert.ToInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "smooth":
                        var smooth = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        bmfFile.Info.BitField |= (byte)(smooth << 7);
                        break;
                    case "unicode":
                        var unicode = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        bmfFile.Info.BitField |= (byte)(unicode << 6);
                        break;
                    case "italic":
                        var italic = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        bmfFile.Info.BitField |= (byte)(italic << 5);
                        break;
                    case "bold":
                        var bold = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        bmfFile.Info.BitField |= (byte)(bold << 4);
                        break;
                    case "fixedHeight":
                        byte fixedHeight = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        bmfFile.Info.BitField |= (byte)(fixedHeight << 3);
                        break;
                    case "stretchH":
                        bmfFile.Info.StretchH = Convert.ToUInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "aa":
                        bmfFile.Info.AA = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "padding":
                        var paddingValues = split[1].Split(',');
                        if (paddingValues.Length == 4)
                        {
                            bmfFile.Info.PaddingUp = Convert.ToByte(paddingValues[0], CultureInfo.InvariantCulture);
                            bmfFile.Info.PaddingRight = Convert.ToByte(paddingValues[1], CultureInfo.InvariantCulture);
                            bmfFile.Info.PaddingDown = Convert.ToByte(paddingValues[2], CultureInfo.InvariantCulture);
                            bmfFile.Info.PaddingLeft = Convert.ToByte(paddingValues[3], CultureInfo.InvariantCulture);
                        }
                        break;
                    case "spacing":
                        var spacingValues = split[1].Split(',');
                        if (spacingValues.Length == 2)
                        {
                            bmfFile.Info.SpacingHoriz = Convert.ToByte(spacingValues[0], CultureInfo.InvariantCulture);
                            bmfFile.Info.SpacingVert = Convert.ToByte(spacingValues[1], CultureInfo.InvariantCulture);
                        }
                        break;
                    case "outline":
                        bmfFile.Info.Outline = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        break;
                }
            }
        }

        private static void ReadCommonTokens(BitmapFontFileContent bmfFile, string[] tokens)
        {
            for (int i = 0; i < tokens.Length; ++i)
            {
                var split = tokens[i].Split('=');

                if (split.Length != 2)
                {
                    continue;
                }

                switch (split[0])
                {
                    case "lineHeight":
                        bmfFile.Common.LineHeight = Convert.ToUInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "base":
                        bmfFile.Common.Base = Convert.ToUInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "scaleW":
                        bmfFile.Common.ScaleW = Convert.ToUInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "scaleH":
                        bmfFile.Common.ScaleH = Convert.ToUInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "pages":
                        bmfFile.Common.Pages = Convert.ToUInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "packed":
                        var packed = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        bmfFile.Common.BitField |= (byte)(packed << 7);
                        break;
                    case "alphaChnl":
                        bmfFile.Common.AlphaChnl = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "redChnl":
                        bmfFile.Common.RedChnl = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "greenChnl":
                        bmfFile.Common.GreenChnl = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "blueChnl":
                        bmfFile.Common.BlueChnl = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        break;
                }
            }
        }

        private static void ReadPageTokens(BitmapFontFileContent bmfFile, string[] tokens)
        {
            for (var i = 0; i < tokens.Length; ++i)
            {
                var split = tokens[i].Split('=');

                if (split.Length != 2)
                {
                    continue;
                }

                if (split[0] == "file")
                {
                    var page = split[1].Replace("\"", string.Empty);
                    bmfFile.Pages.Add(page);
                }
            }
        }

        private static void ReadCharacterTokens(BitmapFontFileContent bmfFile, string[] tokens)
        {
            var character = default(BitmapFontFileContent.CharacterBlock);

            for (var i = 0; i < tokens.Length; ++i)
            {
                var split = tokens[i].Split('=');

                if (split.Length != 2)
                {
                    continue;
                }

                switch (split[0])
                {
                    case "id":
                        character.ID = Convert.ToUInt32(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "x":
                        character.X = Convert.ToUInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "y":
                        character.Y = Convert.ToUInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "width":
                        character.Width = Convert.ToUInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "height":
                        character.Height = Convert.ToUInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "xoffset":
                        character.XOffset = Convert.ToInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "yoffset":
                        character.YOffset = Convert.ToInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "xadvance":
                        character.XAdvance = Convert.ToInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "page":
                        character.Page = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "chnl":
                        character.Chnl = Convert.ToByte(split[1], CultureInfo.InvariantCulture);
                        break;
                }
            }

            bmfFile.Characters.Add(character);
        }

        private static void ReadKerningTokens(BitmapFontFileContent bmfFile, string[] tokens)
        {
            var kerning = default(BitmapFontFileContent.KerningPairsBlock);

            for (var i = 0; i < tokens.Length; ++i)
            {
                var split = tokens[i].Split('=');

                if (split.Length != 2)
                {
                    continue;
                }

                switch (split[0])
                {
                    case "first":
                        kerning.First = Convert.ToUInt32(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "second":
                        kerning.Second = Convert.ToUInt32(split[1], CultureInfo.InvariantCulture);
                        break;
                    case "amount":
                        kerning.Amount = Convert.ToInt16(split[1], CultureInfo.InvariantCulture);
                        break;
                }
            }

            bmfFile.Kernings.Add(kerning);
        }

        private static List<string> GetTokens(char[] line)
        {
            var tokens = new List<string>();
            var currentToken = new StringBuilder();
            var inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == ' ' && !inQuotes)
                {
                    if (currentToken.Length > 0)
                    {
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                    }
                }
                else if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else
                {
                    currentToken.Append(c);
                }
            }

            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            return tokens;
        }

        #endregion --------------------- Read BMFFont Text Formatted File -------------------------------------------------


    }
}
