using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

// II.25 File format extensions to PE 

sealed class FileFormat : ICanRead
{
    public PEHeader PEHeader;
    public Section[] Sections;

    public CodeNode Read(Stream stream)
    {
        Singletons.Reset();

        CodeNode node = new CodeNode
        {
            stream.ReadClass(ref PEHeader),
        };

        Sections = PEHeader.SectionHeaders.Select(header =>
            new Section(header, PEHeader.PEOptionalHeader.PEHeaderHeaderDataDirectories, PEHeader.PEOptionalHeader.PEHeaderStandardFields.EntryPointRVA)).ToArray();

        node.Add(stream.ReadClasses(ref Sections));
        for (int i = 0; i < Sections.Length; ++i)
        {
            Sections[i].CallBack();
        }

        return node;
    }
}

// II.25.2
sealed class PEHeader : ICanRead
{
    public DosHeader DosHeader;
    public PESignature PESignature;
    public PEFileHeader PEFileHeader;
    public PEOptionalHeader PEOptionalHeader;
    public SectionHeader[] SectionHeaders;

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadStruct(out DosHeader),
            stream.ReadStruct(out PESignature),
            stream.ReadStruct(out PEFileHeader),
            stream.ReadClass(ref PEOptionalHeader),
            stream.ReadStructs(out SectionHeaders, PEFileHeader.NumberOfSections),
        };
    }
}

// II.25.2.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct DosHeader
{
    [Expected('M')]
    public char MagicM;
    [Expected('Z')]
    public char MagicZ;
    [Expected(0x90)]
    public ushort BytesOnLastPageOfFile;
    [Expected(3)]
    public ushort PagesInFile;
    [Expected(0)]
    public ushort Relocations;
    [Expected(4)]
    public ushort SizeOfHeaderInParagraphs;
    [Expected(0)]
    public ushort MinimumExtraParagraphsNeeded;
    [Expected(0xFFFF)]
    public ushort MaximumExtraParagraphsNeeded;
    [Expected(0)]
    public ushort InitialRelativeStackSegmentValue;
    [Expected(0xB8)]
    public ushort InitialSP;
    [Expected(0)]
    public ushort Checksum;
    [Expected(0)]
    public ushort InitialIP;
    [Expected(0)]
    public ushort InitialRelativeCS;
    [Expected(0x40)]
    public ushort RawAddressOfRelocation; // TODO link all Raw Address, RVA, (sizes?) from understandingCIL
    [Expected(0)]
    public ushort OverlayNumber;
    [Expected(0)]
    public ulong Reserved;
    [Expected(0)]
    public ushort OemIdentifier;
    [Expected(0)]
    public ushort OemInformation;
    [Expected(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                           0x00, 0x00, 0x00, 0x00})]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] Reserved2;
    [Expected(0x80)]
    public uint LfaNew;
    [Expected(new byte[] { 0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD,
                           0x21, 0xb8, 0x01, 0x4C, 0xCD, 0x21 })]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
    public byte[] DosCode;
    [Expected("This program cannot be run in DOS mode.\r\r\n$")]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 43)]
    public char[] Message;
    [Expected(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
    public byte[] Reserved3;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PESignature
{
    [Expected('P')]
    public char MagicP;
    [Expected('E')]
    public char MagicE;
    [Expected(0)]
    public ushort Reserved;
}

// II.25.2.2 
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEFileHeader
{
    [Description("0x14c is I386.")]
    public MachineType Machine;
    [Description("Number of sections; indicates size of the Section Table, which immediately follows the headers.")]
    public ushort NumberOfSections;
    [Description("Time and date the file was created in seconds since January 1st 1970 00:00:00 or 0.")]
    public uint TimeDateStamp;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public uint PointerToSymbolTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public uint NumberOfSymbols;
    [Description("Size of the optional header, the format is described below.")]
    public ushort OptionalHeaderSize;
    [Description("Flags indicating attributes of the file, see §II.25.2.2.1.")]
    public ushort Characteristics;
}

enum MachineType : ushort
{
    Unknown = 0x0,
    Am33 = 0x1d3,
    Amd64 = 0x8664,
    Arm = 0x1c0,
    Armnt = 0x1c4,
    Arm64 = 0xaa64,
    Ebc = 0xebc,
    I386 = 0x14c,
    Ia64 = 0x200,
    M32r = 0x9041,
    Mips16 = 0x266,
    Mipsfpu = 0x366,
    Mipsfpu16 = 0x466,
    Powerpc = 0x1f0,
    Powerpcfp = 0x1f1,
    R4000 = 0x166,
    Sh3 = 0x1a2,
    Sh3dsp = 0x1a3,
    Sh4 = 0x1a6,
    Sh5 = 0x1a8,
    Thumb = 0x1c2,
    Wcemipsv2 = 0x169
}

// II.25.2.3
class PEOptionalHeader : ICanRead
{
    // TODO (Descriptions)

    public PEHeaderStandardFields PEHeaderStandardFields;
    [Description("RVA of the data section. (This is a hint to the loader.) Only present in PE32, not PE32+")]
    public int? BaseOfData;
    public PEHeaderWindowsNtSpecificFields32? PEHeaderWindowsNtSpecificFields32;
    public PEHeaderWindowsNtSpecificFields64? PEHeaderWindowsNtSpecificFields64;
    public PEHeaderHeaderDataDirectories PEHeaderHeaderDataDirectories;

    public CodeNode Read(Stream stream)
    {
        var node = new CodeNode
        {
            stream.ReadStruct(out PEHeaderStandardFields),
        };

        switch (PEHeaderStandardFields.Magic)
        {
            case PE32Magic.PE32:
                node.Add(stream.ReadStruct(out BaseOfData, nameof(BaseOfData)));
                node.Add(stream.ReadStruct(out PEHeaderWindowsNtSpecificFields32).Children.Single());
                break;
            case PE32Magic.PE32plus:
                node.Add(stream.ReadStruct(out PEHeaderWindowsNtSpecificFields64).Children.Single());
                break;
            default:
                throw new InvalidOperationException($"Magic not recognized: {PEHeaderStandardFields.Magic:X}");
        }

        node.Add(stream.ReadStruct(out PEHeaderHeaderDataDirectories));

        return node;
    }
}

// II.25.2.3.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEHeaderStandardFields
{
    [Description("Identifies version.")]
    public PE32Magic Magic;
    [Description("Spec says always 6, sometimes more (§II.24.1).")]
    public byte LMajor;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public byte LMinor;
    [Description("Size of the code (text) section, or the sum of all code sections if there are multiple sections.")]
    public uint CodeSize;
    [Description("Size of the initialized data section, or the sum of all such sections if there are multiple data sections.")]
    public uint InitializedDataSize;
    [Description("Size of the uninitialized data section, or the sum of all such sections if there are multiple unitinitalized data sections.")]
    public uint UninitializedDataSize;
    [Description("RVA of entry point , needs to point to bytes 0xFF 0x25 followed by the RVA in a section marked execute/read for EXEs or 0 for DLLs")]
    public uint EntryPointRVA;
    [Description("RVA of the code section. (This is a hint to the loader.)")]
    public uint BaseOfCode;
}

enum PE32Magic : ushort
{
    PE32 = 0x10b,
    PE32plus = 0x20b,
}

// II.25.2.3.2
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEHeaderWindowsNtSpecificFields<Tint>
{
    [Description("Shall be a multiple of 0x10000.")]
    public Tint ImageBase;
    [Description("Shall be greater than File Alignment.")]
    public uint SectionAlignment;
    [Description("Should be 0x200 (§II.24.1).")]
    [Expected(0x200)]
    public uint FileAlignment;
    [Description("Should be 5 (§II.24.1).")]
    public ushort OSMajor;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public ushort OSMinor;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public ushort UserMajor;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public ushort UserMinor;
    [Description("Should be 5 (§II.24.1).")]
    //[Expected(5)]
    public ushort SubSysMajor;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public ushort SubSysMinor;
    [Description("Shall be zero")]
    [Expected(0)]
    public uint Reserved;
    [Description("Size, in bytes, of image, including all headers and padding; shall be a multiple of Section Alignment.")]
    public uint ImageSize;
    [Description("Combined size of MS-DOS Header, PE Header, PE Optional Header and padding; shall be a multiple of the file alignment.")]
    public uint HeaderSize;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public uint FileChecksum;
    [Description("Subsystem required to run this image. Shall be either IMAGE_SUBSYSTEM_WINDOWS_CUI (0x3) or IMAGE_SUBSYSTEM_WINDOWS_GUI (0x2).")]
    public ushort SubSystem;
    [Description("Bits 0x100f shall be zero.")]
    public DllCharacteristics DLLFlags;
    [Description("Often 1Mb for x86 or 4Mb for x64 (§II.24.1).")]
    public Tint StackReserveSize;
    [Description("Often 4Kb for x86 or 16Kb for x64 (§II.24.1).")]
    public Tint StackCommitSize;
    [Description("Should be 0x100000 (1Mb) (§II.24.1).")]
    [Expected(0x100000)]
    public Tint HeapReserveSize;
    [Description("Often 4Kb for x86 or 8Kb for x64 (§II.24.1).")]
    public Tint HeapCommitSize;
    [Description("Shall be 0")]
    [Expected(0)]
    public uint LoaderFlags;
    [Description("Shall be 0x10")]
    [Expected(0x10)]
    public uint NumberOfDataDirectories;
}

// Generic structs aren't copyable by Marshal.PtrToStructure, so make non-generic "subclasses"
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEHeaderWindowsNtSpecificFields32
{
    public PEHeaderWindowsNtSpecificFields<uint> PEHeaderWindowsNtSpecificFields;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEHeaderWindowsNtSpecificFields64
{
    public PEHeaderWindowsNtSpecificFields<ulong> PEHeaderWindowsNtSpecificFields;
}

[Flags]
enum DllCharacteristics : ushort
{
    Reserved1 = 0x0001,
    Reserved2 = 0x0002,
    Reserved3 = 0x0004,
    Reserved4 = 0x0008,
    [Description("The DLL can be relocated at load time.")]
    DYNAMIC_BASE = 0x0040,
    [Description("Code integrity checks are forced. If you set this flag and a section contains only uninitialized data, set the PointerToRawData member of IMAGE_SECTION_HEADER for that section to zero; otherwise, the image will fail to load because the digital signature cannot be verified.")]
    FORCE_INTEGRITY = 0x0080,
    [Description("The image is compatible with data execution prevention (DEP).")]
    NX_COMPAT = 0x0100,
    [Description("The image is isolation aware, but should not be isolated.")]
    NO_ISOLATION = 0x0200,
    [Description("The image does not use structured exception handling (SEH). No handlers can be called in this image.")]
    NO_SEH = 0x0400,
    [Description("Do not bind the image. ")]
    NO_BIND = 0x0800,
    Reserved5 = 0x1000,
    [Description("A WDM driver. ")]
    WDM_DRIVER = 0x2000,
    Reserved6 = 0x4000,
    [Description("The image is terminal server aware.")]
    TERMINAL_SERVER_AWARE = 0x8000,
}

// II.25.2.3.3
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEHeaderHeaderDataDirectories
{
    // TODO RVAandSize all

    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong ExportTable;
    [Description("RVA and Size of Import Table, (§II.25.3.1).")]
    public RVAandSize ImportTable;
    [Description("Always 0, unless resources are compiled in (§II.24.1).")]
    public ulong ResourceTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong ExceptionTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong CertificateTable;
    [Description("Relocation Table; set to 0 if unused (§).")]
    public RVAandSize BaseRelocationTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong Debug;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong Copyright;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong GlobalPtr;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong TLSTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong LoadConfigTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong BoundImport;
    [Description("RVA and Size of Import Address Table,(§II.25.3.1).")]
    public RVAandSize ImportAddressTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong DelayImportDescriptor;
    [Description("CLI Header with directories for runtime data,(§II.25.3.1).")]
    public RVAandSize CLIHeader;
    [Description("Always 0 (§II.24.1)")]
    [Expected(0)]
    public ulong Reserved;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct RVAandSize
{
    public uint RVA;
    public uint Size;
}

// II.25.3
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct SectionHeader
{
    [Description("An 8-byte, null-padded ASCII string. There is no terminating null if the string is exactly eight characters long.")]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public char[] Name;
    [Description("Total size of the section in bytes. If this value is greater than SizeOfRawData, the section is zero-padded.")]
    public uint VirtualSize;
    [Description("For executable images this is the address of the first byte of the section, when loaded into memory, relative to the image base.")]
    public uint VirtualAddress;
    [Description("Size of the initialized data on disk in bytes, shall be a multiple of FileAlignment from the PE header. If this is less than VirtualSize the remainder of the section is zero filled. Because this field is rounded while the VirtualSize field is not it is possible for this to be greater than VirtualSize as well. When a section contains only uninitialized data, this field should be 0.")]
    public uint SizeOfRawData;
    [Description("Offset of section's first page within the PE file. This shall be a multiple of FileAlignment from the optional header. When a section contains only uninitialized data, this field should be 0.")]
    public uint PointerToRawData;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public uint PointerToRelocations;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public uint PointerToLinenumbers;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public ushort NumberOfRelocations;
    [Description("Should be 0 (§II.24.1).")]
    [Expected(0)]
    public ushort NumberOfLinenumbers;
    [Description("Flags describing section’s characteristics.")]
    public SectionHeaderCharacteristics Characteristics;
}

[Flags]
enum SectionHeaderCharacteristics : uint
{
    ShouldNotBePadded = 0x00000008,
    ContainsCode = 0x00000020,
    ContainsInitializedData = 0x00000040,
    ContainsUninitializedData = 0x00000080,
    LinkContainsComments = 0x00000200,
    LinkShouldBeRemoved = 0x00000800,
    Link_COMDAT = 0x00001000,
    MemoryCanBeDiscarded = 0x02000000,
    MemoryCannotBeCached = 0x04000000,
    MemoryCannotBePaged = 0x08000000,
    MemoryCanBeShared = 0x10000000,
    MemoryCanBeExecutedAsCode = 0x20000000,
    MemoryCanBeRead = 0x40000000,
    MemoryCanBeWrittenTo = 0x80000000,
}

sealed class Section : ICanRead
{
    int start;
    int end;
    int rva;
    string name;
    PEHeaderHeaderDataDirectories data;
    uint entryPointRVA;

    public CLIHeader CLIHeader;
    public MetadataRoot MetadataRoot;
    public ImportTable ImportTable;
    public ImportLookupTable ImportLookupTable;
    public ImportAddressHintNameTable ImportAddressHintNameTable;
    public NativeEntryPoint NativeEntryPoint;
    public ImportAddressTable ImportAddressTable;
    public Relocations Relocations;

    CodeNode node;

    public Section(SectionHeader header, PEHeaderHeaderDataDirectories data, uint entryPointRVA)
    {
        start = (int)header.PointerToRawData;
        end = start + (int)header.SizeOfRawData;
        rva = (int)header.VirtualAddress;
        name = new string(header.Name);
        this.data = data;
        this.entryPointRVA = entryPointRVA;
    }

    //TODO reorder children in order 
    public CodeNode Read(Stream stream)
    {
        node = new CodeNode
        {
            Description = name,
        };

        foreach (var nr in data.GetType().GetFields()
            .Where(field => field.FieldType == typeof(RVAandSize))
            .Select(field => new { name = field.Name, rva = (RVAandSize)field.GetValue(data) })
            .Where(nr => nr.rva.RVA > 0)
            .Where(nr => rva <= nr.rva.RVA && nr.rva.RVA < rva + end - start)
            .OrderBy(nr => nr.rva.RVA))
        {
            Reposition(stream, nr.rva.RVA);

            switch (nr.name)
            {
                case "CLIHeader":
                    node.Add(stream.ReadStruct(out CLIHeader));

                    Reposition(stream, CLIHeader.MetaData.RVA);
                    node.Add(stream.ReadClass(ref MetadataRoot));

                    foreach (var streamHeader in MetadataRoot.StreamHeaders.OrderBy(h => h.Name.IndexOf('~'))) // Read #~ after heaps
                    {
                        Reposition(stream, streamHeader.Offset + CLIHeader.MetaData.RVA);

                        switch (streamHeader.Name)
                        {
                            case "#Strings":
                                StringHeap stringHeap = new StringHeap((int)streamHeader.Size);
                                Singletons.Instance.StringHeap = stringHeap;
                                node.Add(stream.ReadClass(ref stringHeap));
                                break;
                            case "#US":
                                UserStringHeap userStringHeap = new UserStringHeap((int)streamHeader.Size);
                                Singletons.Instance.UserStringHeap = userStringHeap;
                                node.Add(stream.ReadClass(ref userStringHeap));
                                break;
                            case "#Blob":
                                BlobHeap blobHeap = new BlobHeap((int)streamHeader.Size);
                                Singletons.Instance.BlobHeap = blobHeap;
                                node.Add(stream.ReadClass(ref blobHeap));
                                break;
                            case "#GUID":
                                GuidHeap guidHeap = new GuidHeap((int)streamHeader.Size);
                                Singletons.Instance.GuidHeap = guidHeap;
                                node.Add(stream.ReadClass(ref guidHeap));
                                break;
                            case "#~":
                                TildeStream TildeStream = new TildeStream(this);
                                Singletons.Instance.TildeStream = TildeStream;
                                node.Add(stream.ReadClass(ref TildeStream));
                                
                                CodeNode methods = new CodeNode
                                {
                                    Name = "Methods",
                                };

                                foreach (var rva in (TildeStream.MethodDefs ?? new MethodDef[0])
                                    .Select(def => def.RVA)
                                    .Where(rva => rva > 0)
                                    .Distinct()
                                    .OrderBy(rva => rva))
                                {
                                    Reposition(stream, rva);

                                    Method method = null;
                                    methods.Add(stream.ReadClass(ref method));
                                    Singletons.Instance.MethodsByRVA.Add(rva, method);
                                }

                                if (methods.Children.Any())
                                {
                                    node.Add(methods);
                                }
                                break;
                            default:
                                node.AddError("Unexpected stream name: " + streamHeader.Name);
                                break;
                        }
                    }

                    break;
                case "ImportTable":
                    node.Add(stream.ReadStruct(out ImportTable));

                    Reposition(stream, ImportTable.ImportLookupTable);
                    node.Add(stream.ReadStruct(out ImportLookupTable));

                    Reposition(stream, ImportLookupTable.HintNameTableRVA);
                    node.Add(stream.ReadStruct(out ImportAddressHintNameTable));

                    Reposition(stream, ImportTable.Name);
                    string RuntimeEngineName;
                    node.Add(stream.ReadAnything(out RuntimeEngineName, StreamExtensions.ReadNullTerminated(Encoding.ASCII, 1), "RuntimeEngineName"));

                    Reposition(stream, entryPointRVA);
                    node.Add(stream.ReadStruct(out NativeEntryPoint));
                    break;
                case "ImportAddressTable":
                    node.Add(stream.ReadStruct(out ImportAddressTable));
                    break;
                case "BaseRelocationTable":
                    node.Add(stream.ReadClass(ref Relocations));
                    break;
                default:
                    node.AddError("Unexpected data directoriy name: " + nr.name);
                    break;
            }
        }

        foreach (var toRead in toReads)
        {
            node.Add(toRead(stream));
        }

        return node;
    }

    List<Func<Stream, CodeNode>> toReads = new List<Func<Stream, CodeNode>>();
    public void ReadNode(Func<Stream, CodeNode> read)
    {
        toReads.Add(read);
    }

    public void Reposition(Stream stream, long dataRVA)
    {
        stream.Position = start + dataRVA - rva;
    }

    public void CallBack()
    {
        node.Start = start;
        node.End = end;
    }
}


// II.25.3.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct ImportTable
{
    [Description("RVA of the Import Lookup Table")]
    public uint ImportLookupTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public uint DateTimeStamp;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public uint ForwarderChain;
    [Description("RVA of null-terminated ASCII string “mscoree.dll”.")]
    public uint Name;
    [Description("RVA of Import Address Table (this is the same as the RVA of the IAT descriptor in the optional header).")]
    public uint ImportAddressTableRVA;
    [Description("End of Import Table. Shall be filled with zeros.")]
    [Expected(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                           0x00, 0x00, 0x00, 0x00})]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] Reserved;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct ImportAddressTable
{
    public uint HintNameTableRVA;
    [Expected(0)]
    public uint NullTerminated;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct ImportLookupTable
{
    public uint HintNameTableRVA;
    [Expected(0)]
    public uint NullTerminated;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct ImportAddressHintNameTable
{
    [Description("Shall be 0.")]
    [Expected(0)]
    public ushort Hint;
    [Description("Case sensitive, null-terminated ASCII string containing name to import. Shall be “_CorExeMain” for a.exe file and “_CorDllMain” for a.dll file.")]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
    public char[] Message;
}

//It woul be nice to parse all x86 or other kind of assemblies like ARM or JAR, but that's probably out-of-scope
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct NativeEntryPoint
{
    [Description("JMP op code in X86")]
    [Expected(0xFF)]
    public byte JMP;
    [Description("Specifies the jump is in absolute indirect mode")]
    [Expected(0x25)]
    public byte Mod;
    [Description("Jump target RVA.")]
    public uint JumpTarget;
}

// II.25.3.2
class Relocations : ICanRead
{
    public BaseRelocationTable BaseRelocationTable;
    public Fixup[] Fixups;

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadStruct(out BaseRelocationTable),
            stream.ReadClasses(ref Fixups, ((int)BaseRelocationTable.BlockSize - 8) / 2),
        };
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct BaseRelocationTable
{
    public uint PageRVA;
    public uint BlockSize;
}

class Fixup : ICanRead
{
    //TODO (Descriptions)

    [Description("Stored in high 4 bits of word, type IMAGE_REL_BASED_HIGHLOW (0x3).")]
    public byte Type;
    [Description("Stored in remaining 12 bits of word. Offset from starting address specified in the Page RVA field for the block. This offset specifies where the fixup is to be applied.")]
    public short Offset;

    public CodeNode Read(Stream stream)
    {
        byte tmp = 0xCC;

        return new CodeNode
        {
            stream.ReadStruct(out Type, nameof(Type), (byte b) => {
                tmp = b;
                return (byte)(b >> 4);
            }),
            stream.ReadStruct(out Offset, nameof(Offset), (byte b) => {
                return (short)(((tmp << 8) & 0x0F00) | b);
            }),
        };
    }
}

// II.25.3.3
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct CLIHeader
{
    [Description("Size of the header in bytes")]
    public uint Cb;
    [Description("The minimum version of the runtime required to run this program, currently 2.")]
    public ushort MajorRuntimeVersion;
    [Description("The minor portion of the version, currently 0.")]
    public ushort MinorRuntimeVersion;
    [Description("RVA and size of the physical metadata (§II.24).")]
    public RVAandSize MetaData;
    [Description("Flags describing this runtime image. (§II.25.3.3.1).")]
    public CliHeaderFlags Flags;
    [Description("Token for the MethodDef or File of the entry point for the image")]
    public uint EntryPointToken;
    [Description("RVA and size of implementation-specific resources.")]
    public RVAandSize Resources;
    [Description("RVA of the hash data for this PE file used by the CLI loader for binding and versioning")]
    public RVAandSize StrongNameSignature;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong CodeManagerTable;
    [Description("RVA of an array of locations in the file that contain an array of function pointers (e.g., vtable slots), see below.")]
    public RVAandSize VTableFixups;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong ExportAddressTableJumps;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong ManagedNativeHeader;
}

[Flags]
enum CliHeaderFlags : uint
{
    ILOnly = 0x01,
    Required32Bit = 0x02,
    StrongNameSigned = 0x08,
    NativeEntryPoint = 0x10,
    TrackDebugData = 0x10000,
}

// II.25.4
sealed class Method : ICanRead, IHaveAName, IHaveValueNode
{
    public byte Header; // TODO enum
    public FatFormat FatFormat;
    public MethodDataSection[] DataSections;
    public byte[] CilOps; //TODO(HACK) parse the op codes, detect if any jumps are invalid, and link the jumps and method calls

    public string Name { get; } = $"{nameof(Method)}[{Singletons.Instance.MethodCount++}]";

    public object Value => ""; //TODO clean up all "" Value. Should this just implment IHaveValue? How does that work with CodeNode.DelayedValueNode?

    public CodeNode Node { get; private set; }

    public CodeNode Read(Stream stream)
    {
        Node = new CodeNode
        {
            stream.ReadStruct(out Header, nameof(Header)),
        };

        int length;
        bool moreSects = false;
        MethodHeaderType type = (MethodHeaderType)(Header & 0x03);
        switch (type)
        {
            case MethodHeaderType.Tiny:
                length = Header >> 2;
                break;
            case MethodHeaderType.Fat:
                Node.Add(stream.ReadStruct(out FatFormat, nameof(FatFormat)));

                if ((FatFormat.FlagsAndSize & 0xF0) != 0x30)
                {
                    Node.AddError("Expected upper bits of FlagsAndSize to be 3");
                }

                length = (int)FatFormat.CodeSize;
                moreSects = ((MethodHeaderType)Header).HasFlag(MethodHeaderType.MoreSects);
                break;
            default:
                throw new InvalidOperationException("Invalid MethodHeaderType " + type);
        }

        Node.Add(stream.ReadAnything(out CilOps, StreamExtensions.ReadByteArray(length), "CilOps"));

        if (moreSects)
        {
            while (stream.Position % 4 != 0)
            {
                var b = stream.ReadByte();
            }

            var dataSections = new List<MethodDataSection>();
            var dataSectionNode = new CodeNode { Name = "MethodDataSection" };

            MethodDataSection dataSection = null;
            do
            {
                dataSectionNode.Add(stream.ReadClass(ref dataSection, $"MethodDataSections[{dataSections.Count}]"));
                dataSections.Add(dataSection);
            }
            while (dataSection.Header.HasFlag(MethodHeaderSection.MoreSects));

            DataSections = dataSections.ToArray();
            Node.Add(dataSectionNode.Children.Count == 1 ? dataSectionNode.Children.Single() : dataSectionNode);
        }

        return Node;
    }
}

// II.25.4.1 and .4
enum MethodHeaderType : byte
{
    Tiny = 0x02,
    Fat = 0x03,
    MoreSects = 0x08,
    InitLocals = 0x10,
}

// II.25.4.3
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct FatFormat
{
    [Description("Lower four bits is rest of Flags, Upper four bits is size of this header expressed as the count of 4-byte integers occupied (currently 3)")]
    public byte FlagsAndSize;
    [Description("Maximum number of items on the operand stack")]
    public ushort MaxStack;
    [Description("Size in bytes of the actual method body")]
    public uint CodeSize;
    [Description("Meta Data token for a signature describing the layout of the local variables for the method")]
    public uint LocalVarSigTok;
}

// II.25.4.5
sealed class MethodDataSection : ICanRead
{
    public MethodHeaderSection Header;
    public LargeMethodHeader LargeMethodHeader;
    public SmallMethodHeader SmallMethodHeader;

    public CodeNode Read(Stream stream)
    {
        var node = new CodeNode
        {
            stream.ReadStruct(out Header, nameof(MethodHeaderSection))
        };

        if (!Header.HasFlag(MethodHeaderSection.EHTable))
        {
            throw new InvalidOperationException("Only kind of section data is exception header");
        }
        
        if (Header.HasFlag(MethodHeaderSection.FatFormat))
        {
            node.Add(stream.ReadClass(ref LargeMethodHeader, nameof(LargeMethodHeader)));
        }
        else
        {
            node.Add(stream.ReadClass(ref SmallMethodHeader, nameof(SmallMethodHeader)));
        }

        return node;
    }
}

[Flags]
enum MethodHeaderSection : byte
{
    [Description("Exception handling data.")]
    EHTable = 0x1,
    [Description("Reserved, shall be 0.")]
    OptILTable = 0x2,
    [Description("Data format is of the fat variety, meaning there is a 3-byte length least-significant byte first format. If not set, the header is small with a 1-byte length")]
    FatFormat = 0x40,
    [Description("Another data section occurs after this current section")]
    MoreSects = 0x80,
}

sealed class SmallMethodHeader : ICanRead
{
    [Description("Size of the data for the block, including the header, say n * 12 + 4.")]
    public byte DataSize;
    [Description("Padding, always 0.")]
    [Expected(0)]
    public ushort Reserved;
    public SmallExceptionHandlingClause[] Clauses;

    public CodeNode Read(Stream stream)
    {
        var node = new CodeNode
        {
            stream.ReadStruct(out DataSize, nameof(DataSize)),
            stream.ReadStruct(out Reserved, nameof(Reserved)),
        };

        var n = (DataSize - 4) / 12;
        if (n * 12 + 4 != DataSize)
        {
            node.AddError("DataSize was not of the form n * 12 + 4");
        }

        node.Add(stream.ReadStructs(out Clauses, n, nameof(Clauses)));

        return node;
    }
}

sealed class LargeMethodHeader : ICanRead
{
    [Description("Size of the data for the block, including the header, say n * 24 + 4.")]
    public UInt24 DataSize;
    public SmallExceptionHandlingClause[] Clauses;

    public CodeNode Read(Stream stream)
    {
        var node = new CodeNode
        {
            stream.ReadClass(ref DataSize, nameof(DataSize)),
        };

        var n = (DataSize.IntValue - 4) / 12;
        if (n * 24 + 4 != DataSize.IntValue)
        {
            node.AddError("DataSize was not of the form n * 24 + 4");
        }

        node.Add(stream.ReadStructs(out Clauses, n, nameof(Clauses)));

        return node;
    }
}

// II.25.4.6
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct SmallExceptionHandlingClause
{
    [Description("Flags")]
    public ushort SmallExceptionClauseFlags;
    [Description("Offset in bytes of try block from start of method body.")]
    public ushort TryOffset; //TODO (links)
    [Description("Length in bytes of the try block")]
    public byte TryLength;
    [Description("Location of the handler for this try block")]
    public ushort HandlerOffset;
    [Description("Size of the handler code in bytes")]
    public byte HandlerLength;
    [Description("Meta data token for a type-based exception handler OR Offset in method body for filter-based exception handler")]
    public uint ClassTokenOrFilterOffset; //TODO (links)
}

[Flags]
enum SmallExceptionClauseFlags : ushort
{
    [Description("A typed exception clause")]
    Exception = 0x0000,
    [Description("An exception filter and handler clause")]
    Filter = 0x0001,
    [Description("A finally clause")]
    Finally = 0x0002,
    [Description("Fault clause (finally that is called on exception only)")]
    Fault = 0x0004,
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct LargeExceptionHandlingClause
{
    [Description("Flags.")]
    public uint LargeExceptionClauseFlags;
    [Description("Offset in bytes of try block from start of method body.")]
    public uint TryOffset;
    [Description("Length in bytes of the try block")]
    public uint TryLength;
    [Description("Location of the handler for this try block")]
    public uint HandlerOffset;
    [Description("Size of the handler code in bytes")]
    public uint HandlerLength;
    [Description("Meta data token for a type-based exception handler OR offset in method body for filter-based exception handler")]
    public uint ClassTokenOrFilterOffset;
}

[Flags]
enum LargeExceptionClauseFlags : uint
{
    [Description("A typed exception clause")]
    Exception = 0x0000,
    [Description("An exception filter and handler clause")]
    Filter = 0x0001,
    [Description("A finally clause")]
    Finally = 0x0002,
    [Description("Fault clause (finally that is called on exception only)")]
    Fault = 0x0004,
}

sealed class UInt24 : ICanRead, IHaveValue
{
    public int IntValue { get; private set; }
    public object Value => IntValue;

    public CodeNode Read(Stream stream)
    {
        byte[] b;
        stream.ReadStructs(out b, 3);

        IntValue = (b[2] << 16) + (b[1] << 8) + b[0];

        return new CodeNode();
    }
}
