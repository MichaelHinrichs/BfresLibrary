﻿using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Syroot.BinaryData;
using Syroot.NintenTools.Bfres.Core;
using System.ComponentModel;

namespace Syroot.NintenTools.Bfres
{
    /// <summary>
    /// Represents a NintendoWare for Cafe (NW4F) graphics data archive file.
    /// </summary>
    [DebuggerDisplay(nameof(ResFile) + " {" + nameof(Name) + "}")]
    public class ResFile : IResData
    {
        // ---- CONSTANTS ----------------------------------------------------------------------------------------------

        private const string _signature = "FRES";

        // ---- CONSTRUCTORS & DESTRUCTOR ------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance of the <see cref="ResFile"/> class.
        /// </summary>
        public ResFile()
        {
            Name = "";
            DataAlignment = 8192;

            VersionMajor = 3;
            VersionMajor2 = 4;
            VersionMinor = 0;
            VersionMinor2 = 4;

            //Initialize Dictionaries
            Models = new ResDict<Model>();
            Textures = new ResDict<TextureShared>();
            SkeletalAnims = new ResDict<SkeletalAnim>();
            ShaderParamAnims = new ResDict<MaterialAnim>();
            ColorAnims = new ResDict<MaterialAnim>();
            TexSrtAnims = new ResDict<MaterialAnim>();
            TexPatternAnims = new ResDict<MaterialAnim>();
            BoneVisibilityAnims = new ResDict<VisibilityAnim>();
            MatVisibilityAnims = new ResDict<MaterialAnim>();
            ShapeAnims = new ResDict<ShapeAnim>();
            SceneAnims = new ResDict<SceneAnim>();
            ExternalFiles = new ResDict<ExternalFile>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResFile"/> class from the given <paramref name="stream"/> which
        /// is optionally left open.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to load the data from.</param>
        /// <param name="leaveOpen"><c>true</c> to leave the stream open after reading, otherwise <c>false</c>.</param>
        public ResFile(Stream stream, bool leaveOpen = false)
        {
            if (IsSwitchBinary(stream))
            {
                using (ResFileLoader loader = new Switch.Core.ResFileSwitchLoader(this, stream)) {
                    loader.Execute();
                }
            }
            else
            {
                using (ResFileLoader loader = new WiiU.Core.ResFileWiiULoader(this, stream)) {
                    loader.Execute();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResFile"/> class from the file with the given
        /// <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">The name of the file to load the data from.</param>
        public ResFile(string fileName)
        {
            if (IsSwitchBinary(fileName))
            {
                using (ResFileLoader loader = new Switch.Core.ResFileSwitchLoader(this, fileName)) {
                    loader.Execute();
                }
            }
            else
            {
                using (ResFileLoader loader = new WiiU.Core.ResFileWiiULoader(this, fileName)) {
                    loader.Execute();
                }
            }
        }

        // ---- METHODS (PUBLIC) ---------------------------------------------------------------------------------------

        /// <summary>
        /// Saves the contents in the given <paramref name="stream"/> and optionally leaves it open
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to save the contents into.</param>
        /// <param name="leaveOpen"><c>true</c> to leave the stream open after writing, otherwise <c>false</c>.</param>
        public void Save(Stream stream, bool leaveOpen = false)
        {
            if (IsPlatformSwitch) {
                using (ResFileSaver saver = new Switch.Core.ResFileSwitchSaver(this, stream, leaveOpen)) {
                    saver.Execute();
                }
            }
            else
            {
                using (ResFileSaver saver = new WiiU.Core.ResFileWiiUSaver(this, stream, leaveOpen)) {
                    saver.Execute();
                }
            }
        }

        /// <summary>
        /// Saves the contents in the file with the given <paramref name="fileName"/>.
        /// </summary>
        /// <param name="fileName">The name of the file to save the contents into.</param>
        public void Save(string fileName)
        {
            if (IsPlatformSwitch) {
                using (ResFileSaver saver = new Switch.Core.ResFileSwitchSaver(this, fileName)) {
                    saver.Execute();
                }
            }
            else
            {
                using (ResFileSaver saver = new WiiU.Core.ResFileWiiUSaver(this, fileName)) {
                    saver.Execute();
                }
            }
        }

        internal uint SaveVersion()
        {
            return VersionMajor << 24 | VersionMajor2 << 16 | VersionMinor << 8 | VersionMinor2;
        }


        public static bool IsSwitchBinary(string fileName) {
            return IsSwitchBinary(File.OpenRead(fileName));
        }

        public static bool IsSwitchBinary(Stream stream)
        {
            using (var reader = new BinaryDataReader(stream, true)) {
                reader.ByteOrder = ByteOrder.LittleEndian;

                reader.Seek(4, SeekOrigin.Begin);
                uint paddingCheck = reader.ReadUInt32();
                reader.Position = 0;

                return paddingCheck == 0x20202020;
            }
        }

        // ---- PROPERTIES ---------------------------------------------------------------------------------------------

        public bool IsPlatformSwitch { get; set; }

        /// <summary>
        /// Gets or sets the alignment to use for raw data blocks in the file.
        /// </summary>
        [Browsable(true)]
        [Category("Binary Info")]
        [DisplayName("Alignment")]
        public uint Alignment { get; set; } = 0xC;

        public int DataAlignment
        {
            get
            {
                if (IsPlatformSwitch)
                    return (1 << (int)Alignment);
                else
                    return (int)Alignment;
            }
            set
            {
                if (IsPlatformSwitch)
                    Alignment = (uint)(value >> 7);
                else
                    Alignment = (uint)value;
            }
        }

        /// <summary>
        /// Gets or sets the target adress size to use for raw data blocks in the file.
        /// </summary>
        [Browsable(false)]
        public uint TargetAddressSize { get; set; }

        /// <summary>
        /// Gets or sets a name describing the contents.
        /// </summary>
        [Browsable(true)]
        [Category("Binary Info")]
        [DisplayName("Name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the revision of the BFRES structure formats.
        /// </summary>
        internal uint Version { get; set; }

        /// <summary>
        /// Gets or sets the flag. Unknown purpose.
        /// </summary>
        internal uint Flag { get; set; }

        /// <summary>
        /// Gets or sets the BlockOffset. 
        /// </summary>
        internal uint BlockOffset { get; set; }

        /// <summary>
        /// Gets or sets the stored <see cref="MemoryPool"/> instances. 
        /// </summary>
        internal MemoryPool MemoryPool { get; set; }

        /// <summary>
        /// Gets or sets the stored <see cref="BufferInfo"/> instances.
        /// </summary>
        internal BufferInfo BufferInfo { get; set; }

        internal StringTable StringTable { get; set; }

        /// <summary>
        /// Combination of all the material animations into one.
        /// This is used for switch material animations
        /// </summary>
        internal ResDict<MaterialAnim> MaterialAnims { get; set; }

        /// <summary>
        /// Gets or sets the major revision of the BFRES structure formats.
        /// </summary>
        [Browsable(true)]
        [ReadOnly(true)]
        [Category("Version")]
        [DisplayName("Version Major")]
        public string VersioFull
        {
            get
            {
                return $"{VersionMajor},{VersionMajor2},{VersionMinor},{VersionMinor2}";
            }
        }

        /// <summary>
        /// Gets or sets the major revision of the BFRES structure formats.
        /// </summary>
        [Browsable(true)]
        [Category("Version")]
        [DisplayName("Version Major")]
        public uint VersionMajor { get; set; }
        /// <summary>
        /// Gets or sets the second major revision of the BFRES structure formats.
        /// </summary>
        [Browsable(true)]
        [Category("Version")]
        [DisplayName("Version Major 2")]
        public uint VersionMajor2 { get; set; }
        /// <summary>
        /// Gets or sets the minor revision of the BFRES structure formats.
        /// </summary>
        [Browsable(true)]
        [Category("Version")]
        [DisplayName("Version Minor")]
        public uint VersionMinor { get; set; }
        /// <summary>
        /// Gets or sets the second minor revision of the BFRES structure formats.
        /// </summary>
        [Browsable(true)]
        [Category("Version")]
        [DisplayName("Version Minor 2")]
        public uint VersionMinor2 { get; set; }

        /// <summary>
        /// Gets the byte order in which data is stored. Must be the endianness of the target platform.
        /// </summary>
        [Browsable(false)]
        public ByteOrder ByteOrder { get; internal set; } = ByteOrder.BigEndian;

        /// <summary>
        /// Gets or sets the stored <see cref="Model"/> (FMDL) instances.
        /// </summary>
        [Browsable(false)]
        public ResDict<Model> Models { get; set; }

        /// <summary>
        /// Gets or sets the stored <see cref="Texture"/> (FTEX) instances.
        /// </summary>
        [Browsable(false)]
        public ResDict<TextureShared> Textures { get; set; }

        /// <summary>
        /// Gets or sets the stored <see cref="SkeletalAnim"/> (FSKA) instances.
        /// </summary>
        [Browsable(false)]
        public ResDict<SkeletalAnim> SkeletalAnims { get; set; }

        /// <summary>
        /// Gets or sets the stored <see cref="ShaderParamAnim"/> (FSHU) instances.
        /// </summary>
        [Browsable(false)]
        public ResDict<MaterialAnim> ShaderParamAnims { get; set; }

        /// <summary>
        /// Gets or sets the stored <see cref="ShaderParamAnim"/> (FSHU) instances for color animations.
        /// </summary>
        [Browsable(false)]
        public ResDict<MaterialAnim> ColorAnims { get; set; }

        /// <summary>
        /// Gets or sets the stored <see cref="ShaderParamAnim"/> (FSHU) instances for texture SRT animations.
        /// </summary>
        [Browsable(false)]
        public ResDict<MaterialAnim> TexSrtAnims { get; set; }

        /// <summary>
        /// Gets or sets the stored <see cref="TexPatternAnim"/> (FTXP) instances.
        /// </summary>
        [Browsable(false)]
        public ResDict<MaterialAnim> TexPatternAnims { get; set; }

        /// <summary>
        /// Gets or sets the stored <see cref="VisibilityAnim"/> (FVIS) instances for bone visibility animations.
        /// </summary>
        [Browsable(false)]
        public ResDict<VisibilityAnim> BoneVisibilityAnims { get; set; }

        /// <summary>
        /// Gets or sets the stored <see cref="VisibilityAnim"/> (FVIS) instances for material visibility animations.
        /// </summary>
        [Browsable(false)]
        public ResDict<MaterialAnim> MatVisibilityAnims { get; set; }

        /// <summary>
        /// Gets or sets the stored <see cref="ShapeAnim"/> (FSHA) instances.
        /// </summary>
        [Browsable(false)]
        public ResDict<ShapeAnim> ShapeAnims { get; set; }

        /// <summary>
        /// Gets or sets the stored <see cref="SceneAnim"/> (FSCN) instances.
        /// </summary>
        [Browsable(false)]
        public ResDict<SceneAnim> SceneAnims { get; set; }

        /// <summary>
        /// Gets or sets attached <see cref="ExternalFile"/> instances. The key of the dictionary typically represents
        /// the name of the file they were originally created from.
        /// </summary>
        [Browsable(false)]
        public ResDict<ExternalFile> ExternalFiles { get; set; }

        // ---- METHODS (INTERNAL) ---------------------------------------------------------------------------------------

        internal void SetVersionInfo(uint Version)
        {
            VersionMajor = Version >> 24;
            VersionMajor2 = Version >> 16 & 0xFF;
            VersionMinor = Version >> 8 & 0xFF;
            VersionMinor2 = Version & 0xFF;
        }

        // ---- METHODS ------------------------------------------------------------------------------------------------

        public void ChangePlatform(bool isSwitch, int alignment,
            byte versionA, byte versionB, byte versionC, byte versionD)
        {
            if (!IsPlatformSwitch && isSwitch) {
                ConvertTexturesToBntx(Textures.Values.ToList());
            }

            //Order to read the existing data
            ByteOrder byteOrder = IsPlatformSwitch ? ByteOrder.LittleEndian : ByteOrder.BigEndian;
            //Order to set the target data
            ByteOrder targetOrder = isSwitch ? ByteOrder.LittleEndian : ByteOrder.BigEndian;

            IsPlatformSwitch = isSwitch;
            DataAlignment = alignment;
            VersionMajor = versionA;
            VersionMajor2 = versionB;
            VersionMinor = versionC;
            VersionMinor2 = versionD;

            foreach (var model in Models.Values)
            {
                UpdateVertexBufferByteOrder(model, byteOrder, targetOrder);
                foreach (var shp in model.Shapes.Values) {
                    foreach (var mesh in shp.Meshes) {
                        mesh.UpdateIndexBufferByteOrder(targetOrder);
                    }
                }
            }
        }

        void IResData.Load(ResFileLoader loader)
        {
            IsPlatformSwitch = loader.IsSwitch;
            if (loader.IsSwitch)
                 Switch.ResFileParser.Load((Switch.Core.ResFileSwitchLoader)loader, this);
            else
                WiiU.ResFileParser.Load((WiiU.Core.ResFileWiiULoader)loader, this);
        }

        void IResData.Save(ResFileSaver saver) {
            PreSave();
            if (saver.IsSwitch)
                Switch.ResFileParser.Save((Switch.Core.ResFileSwitchSaver)saver, this);
            else
                WiiU.ResFileParser.Save((WiiU.Core.ResFileWiiUSaver)saver, this);
        }

        internal void PreSave()
        {
            Version = SaveVersion();
            MaterialAnims = new ResDict<MaterialAnim>();

            for (int i = 0; i < Models.Count; i++) {
                for (int s = 0; s < Models[i].Shapes.Count; s++) {

                    Models[i].Shapes[s].VertexBuffer = Models[i].VertexBuffers[Models[i].Shapes[s].VertexBufferIndex];

                    //Link texture sections for wii u texture refs
                    if (Textures != null)
                    {
                        foreach (var texRef in Models[i].Materials[Models[i].Shapes[s].MaterialIndex].TextureRefs)
                        {
                            if (Textures.ContainsKey(texRef.Name))
                                texRef.Texture = Textures[texRef.Name];
                        }
                    }
                }
            }

            for (int i = 0; 
                i < SkeletalAnims.Count; i++)
            {
                int curveIndex = 0;
                for (int s = 0; s < SkeletalAnims[i].BoneAnims.Count; s++)
                {
                    SkeletalAnims[i].BoneAnims[s].BeginCurve = curveIndex;
                    curveIndex += SkeletalAnims[i].BoneAnims[s].Curves.Count;
                }
            }

            // Update ShapeAnim instances.
            foreach (ShapeAnim anim in ShapeAnims.Values)
            {
                int curveIndex = 0;
                int infoIndex = 0;
                foreach (VertexShapeAnim subAnim in anim.VertexShapeAnims)
                {
                    subAnim.BeginCurve = curveIndex;
                    subAnim.BeginKeyShapeAnim = infoIndex;
                    curveIndex += subAnim.Curves.Count;
                    infoIndex += subAnim.KeyShapeAnimInfos.Count;
                }
            }
        }

        private void UpdateVertexBufferByteOrder(Model model, ByteOrder byteOrder, ByteOrder target)
        {
            foreach (var buffer in model.VertexBuffers)
                buffer.UpdateVertexBufferByteOrder(byteOrder, target);
        }

        private void ConvertTexturesToBntx(List<TextureShared> textures)
        {
            ExternalFiles.Add("textures.bntx", new ExternalFile()
            {
                Data = new byte[0],
            });
        }

        //Reserved for saving offsets 
        internal long ModelOffset = 0;
        internal long SkeletonAnimationOffset = 0;
        internal long MaterialAnimationOffset = 0;
        internal long ShapeAnimationOffset = 0;
        internal long BoneVisAnimationOffset = 0;
        internal long SceneAnimationOffset = 0;
        internal long ExternalFileOffset = 0;

        internal long ModelDictOffset = 0;
        internal long SkeletonAnimationDictOffset = 0;
        internal long MaterialAnimationnDictOffset = 0;
        internal long ShapeAnimationDictOffset = 0;
        internal long BoneVisAnimationDictOffset = 0;
        internal long SceneAnimationDictOffset = 0;
        internal long ExternalFileDictOffset = 0;

        internal long BufferInfoOffset = 0;
    }
}