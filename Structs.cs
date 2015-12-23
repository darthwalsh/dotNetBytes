using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable 0649 // CS0649: Field '...' is never assigned to, and will always have its default value

sealed class ExpectedAttribute : Attribute
{
    public object Value;
    public ExpectedAttribute(object v)
    {
        Value = v;
    }
}

sealed class DescriptionAttribute : Attribute
{
    public string Description;
    public DescriptionAttribute(string d)
    {
        Description = d;
    }
}


// S25
sealed class FileFormat : ICanRead
{
    public PEHeader PEHeader;
    public Section[] Sections;

    public CodeNode Read(Stream stream)
    {
        CodeNode node = new CodeNode
        {
            stream.ReadClass(ref PEHeader),
        };
        
        Sections = PEHeader.SectionHeaders.Select(header => new Section(header, PEHeader.PEOptionalHeader.PEHeaderHeaderDataDirectories)).ToArray();

        IEnumerable<CodeNode> sections;
        node.Add(sections = stream.ReadClasses(ref Sections));
        var ss = sections.ToArray();

        for (int i = 0; i < Sections.Length; ++i)
        {
            Sections[i].CallBack(ss[i]);
        }

        return node;
    }
}

// S25.2.2
sealed class PEHeader : ICanRead
{
    public DosHeader DosHeader;
    public PESignature PESignature;
    public PEFileHeader PEFileHeader;
    public PEOptionalHeader PEOptionalHeader;
    public SectionHeader[] SectionHeaders;

    public CodeNode Read(Stream stream)
    {
        CodeNode node = new CodeNode
        {
            stream.ReadStruct(out DosHeader),
            stream.ReadStruct(out PESignature),
            stream.ReadStruct(out PEFileHeader),
            stream.ReadStruct(out PEOptionalHeader),
            stream.ReadStructs(out SectionHeaders, PEFileHeader.NumberOfSections),
        };

        return node;
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
    public ushort RawAddressOfRelocation;
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
    [Description("Always 0x14c.")]
    [Expected(0x14c)]
    public ushort Machine;
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
    [Description("Size Size of the optional header, the format is described below.")]
    public ushort OptionalHeader;
    [Description("Flags indicating attributes of the file, see §II.25.2.2.1.")]
    public ushort Characteristics;
}

// II.25.2.3
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEOptionalHeader
{
    public PEHeaderStandardFields PEHeaderStandardFields;
    public PEHeaderWindowsNtSpecificFields PEHeaderWindowsNtSpecificFields;
    public PEHeaderHeaderDataDirectories PEHeaderHeaderDataDirectories;
}

// II.25.2.3.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEHeaderStandardFields
{
    [Description("Always 0x10B.")]
    [Expected(0x10B)]
    public ushort Magic;
    [Description("Always 6 (§II.24.1).")]
    [Expected(6)]
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
    [Description("RVA of the data section. (This is a hint to the loader.)")]
    public uint BaseOfData;
}

// II.25.2.3.2
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct PEHeaderWindowsNtSpecificFields
{
    [Description("Shall be a multiple of 0x10000.")]
    public uint ImageBase;
    [Description("Shall be greater than File Alignment.")]
    public uint SectionAlignment;
    [Description("Should be 0x200 (§II.24.1).")]
    [Expected(0x200)]
    public uint FileAlignment;
    [Description("Should be 5 (§II.24.1).")]
    //[Expected(5)]
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
    [Description("Should be 0x100000 (1Mb) (§II.24.1).")]
    [Expected(0x100000)]
    public uint StackReserveSize;
    [Description("Should be 0x1000 (4Kb) (§II.24.1).")]
    [Expected(0x1000)]
    public uint StackCommitSize;
    [Description("Should be 0x100000 (1Mb) (§II.24.1).")]
    [Expected(0x100000)]
    public uint HeapReserveSize;
    [Description("Should be 0x1000 (4Kb) (§II.24.1).")]
    [Expected(0x1000)]
    public uint HeapCommitSize;
    [Description("Shall be 0")]
    [Expected(0)]
    public uint LoaderFlags;
    [Description("Shall be 0x10")]
    [Expected(0x10)]
    public uint NumberOfDataDirectories;
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
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong ExportTable;
    [Description("RVA and Size of Import Table, (§II.25.3.1).")]
    public RVAandSize ImportTable;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
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
    public RVAandSize ImportAddressTableDirectory;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong DelayImportDescriptor;
    [Description("CLI Header with directories for runtime data,(§II.25.3.1).")]
    public RVAandSize CLIHeader;
    [Description("Always 0 (§II.24.1)")]
    [Expected(0)]
    public ulong Reserved;
}

// II.25.3
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

    public object[] Members;

    public Section(SectionHeader header, PEHeaderHeaderDataDirectories data)
    {
        start = (int)header.PointerToRawData;
        end = start + (int)header.SizeOfRawData;
        rva = (int)header.VirtualAddress;
        name = new string(header.Name);
        this.data = data;

        sections.Add(this);
    }

    // TODO provide RVA to Raw mapping
    static List<Section> sections = new List<Section>();

    public CodeNode Read(Stream stream)
    {
        CodeNode node = new CodeNode
        {
            Description = name,
        };

        var ss = new List<object>();

        foreach (var nr in data.GetType().GetFields()
            .Where(field => field.FieldType == typeof(RVAandSize))
            .Select(field => new { name = field.Name, rva = (RVAandSize)field.GetValue(data) })
            .Where(nr => rva <= nr.rva.RVA && nr.rva.RVA < rva + end - start)
            .OrderBy(nr => nr.rva.RVA))
        {
            Reposition(stream, nr.rva.RVA);

            switch (nr.name)
            {
                case "CLIHeader":
                    CLIHeader CLIHeader;
                    node.Add(stream.ReadStruct(out CLIHeader));
                    CLIHeader.Instance = CLIHeader;

                    Reposition(stream, CLIHeader.MetaData.RVA);
                    MetadataRoot MetadataRoot = null;
                    node.Add(stream.ReadClass(ref MetadataRoot));

                    foreach (var streamHeader in MetadataRoot.StreamHeaders.OrderBy(h => h.Name.IndexOf('~'))) // Read #~ after heaps
                    {
                        Reposition(stream, streamHeader.Offset + CLIHeader.MetaData.RVA);

                        switch (streamHeader.Name)
                        {
                            case "#Strings":
                                StringHeap StringHeap = new StringHeap((int)streamHeader.Size);
                                node.Add(stream.ReadClass(ref StringHeap));
                                break;
                            case "#US":
                                UserStringHeap UserStringHeap = new UserStringHeap((int)streamHeader.Size);
                                node.Add(stream.ReadClass(ref UserStringHeap));
                                break;
                            case "#Blob":
                                BlobHeap BlobHeap = new BlobHeap((int)streamHeader.Size);
                                node.Add(stream.ReadClass(ref BlobHeap));
                                break;
                            case "#GUID":
                                GuidHeap GuidHeap = new GuidHeap((int)streamHeader.Size);
                                node.Add(stream.ReadClass(ref GuidHeap));
                                break;
                            case "#~":
                                TildeStream TildeStream = null;
                                node.Add(stream.ReadClass(ref TildeStream));
                                break;
                            default: //TODO add new entries
                                node.Errors.Add("Unknown stream name: " + streamHeader.Name);
                                break;
                        }
                    }

                    break;
                case "ImportTable":
                    ImportTable ImportTable;
                    node.Add(stream.ReadStruct(out ImportTable));
                    break;
                case "ImportAddressTableDirectory":
                    ImportAddressTableDirectory ImportAddressTableDirectory;
                    node.Add(stream.ReadStruct(out ImportAddressTableDirectory));
                    break;
                case "BaseRelocationTable":
                    Relocations Relocations = null;
                    node.Add(stream.ReadClass(ref Relocations));
                    break;
                default:
                    throw new NotImplementedException(nr.name);
            }
        }

        Members = ss.ToArray();

        return node;
    }

    void Reposition(Stream stream, long dataRVA)
    {
        stream.Position = start + dataRVA - rva;
    }

    public void CallBack(CodeNode node)
    {
        node.Start = start;
        node.End = end;
    }
}


// II.25.3.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct ImportTable
{
    public uint HintNameTableRVA;
    [Expected(0)]
    public uint NullTerminated;
}

//TODO use
// II.25.3.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct ImportAddressTable
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


// II.25.3.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct ImportAddressTableDirectory
{
    public uint HintNameTableRVA;
    [Expected(0)]
    public uint NullTerminated;
}

// II.25.3.1
class Relocations : ICanRead
{
    public BaseRelocationTable BaseRelocationTable;
    public Fixup[] Fixups;

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadStruct(out BaseRelocationTable),
            stream.ReadStructs(out Fixups, ((int)BaseRelocationTable.BlockSize - 8) / 2),
        };
    }
}

// II.25.3.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct BaseRelocationTable
{
    public uint PageRVA;
    public uint BlockSize;
}

// II.25.3.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct Fixup
{
    [Description("Type is stored in high 4 bits of word, type IMAGE_REL_BASED_HIGHLOW (0x3). Offset stored in remaining 12 bits of word. Offset from starting address specified in the Page RVA field for the block. This offset specifies where the fixup is to be applied.")]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public byte[] Data;
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

    public static CLIHeader Instance; // TODO good idea?
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


// S24.2.1
sealed class MetadataRoot : ICanRead
{
    //TODO(descriptions)

    [Description("Magic signature for physical metadata : 0x424A5342.")]
    public uint Signature;
    [Description("Major version, 1 (ignore on read)")]
    [Expected(1)]
    public ushort MajorVersion;
    [Description("Minor version, 1 (ignore on read)")]
    [Expected(1)]
    public ushort MinorVersion;
    [Description("Reserved, always 0 (§II.24.1).")]
    public uint Reserved;
    [Description("Number of bytes allocated to hold version string, rounded up to a multiple of four.")]
    public uint Length;
    [Description("UTF8-encoded null-terminated version string.")]
    public string Version;
    [Description("Reserved, always 0 (§II.24.1).")]
    [Expected(0)]
    public ushort Flags;
    [Description("Number of streams.")]
    public ushort Streams;
    public StreamHeader[] StreamHeaders;

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadStruct(out Signature, "Signature"),
            stream.ReadStruct(out MajorVersion, "MajorVersion"),
            stream.ReadStruct(out MinorVersion, "MinorVersion"),
            stream.ReadStruct(out Reserved, "Reserved"),
            stream.ReadStruct(out Length, "Length"),
            stream.ReadAnything(out Version, StreamExtensions.ReadNullTerminated(Encoding.UTF8, 4), "Version"),
            stream.ReadStruct(out Flags, "Flags"),
            stream.ReadStruct(out Streams, "Streams"),
            stream.ReadClasses(ref StreamHeaders, Streams),
        };
    }
}

// S24.2.2
sealed class StreamHeader : ICanRead
{
    //TODO(descriptions)

    [Description("Memory offset to start of this stream from start of the metadata root(§II.24.2.1)")]
    public uint Offset;
    [Description("Size of this stream in bytes, shall be a multiple of 4.")]
    public uint Size;
    [Description("Name of the stream as null-terminated variable length array of ASCII characters, padded to the next 4 - byte boundary with null characters.")]
    public string Name;

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadStruct(out Offset, "Offset"),
            stream.ReadStruct(out Size, "Size"),
            stream.ReadAnything(out Name, StreamExtensions.ReadNullTerminated(Encoding.ASCII, 4), "Name"),
        };
    }
}

// II.24.2.3
sealed class StringHeap : ICanRead
{
    byte[] data;

    public StringHeap(int size)
    {
        data = new byte[size];

        instance = this;
    }

    public CodeNode Read(Stream stream)
    {
        // Parsing the whole array now isn't sensible
        stream.ReadWholeArray(data);

        return new CodeNode();
    }

    static StringHeap instance;
    public static string Get(StringHeapIndex i)
    {
        var stream = new MemoryStream(instance.data);
        stream.Position = i.Index;
        return StreamExtensions.ReadNullTerminated(Encoding.UTF8, 1)(stream);
    }
}


// II.24.2.4
sealed class UserStringHeap : ICanRead
{
    byte[] data;

    public UserStringHeap(int size)
    {
        data = new byte[size];

        instance = this;
    }

    public CodeNode Read(Stream stream)
    {
        // Parsing the whole array now isn't sensible
        stream.ReadWholeArray(data);

        return new CodeNode();
    }

    static UserStringHeap instance;
    public static string Get(UserStringHeapIndex i)
    {
        throw new NotImplementedException(); //TODO(implement UserStringHeap)
    }
}

sealed class BlobHeap : ICanRead
{
    byte[] data;

    public BlobHeap(int size)
    {
        data = new byte[size];

        instance = this;
    }

    public CodeNode Read(Stream stream)
    {
        // Parsing the whole array now isn't sensible
        stream.ReadWholeArray(data);

        return new CodeNode();
    }

    static BlobHeap instance;
    public static byte[] Get(BlobHeapIndex i)
    {
        throw new NotImplementedException(); //TODO(implement BlobHeap)
    }
}

sealed class GuidHeap : ICanRead
{
    byte[] data;

    public GuidHeap(int size)
    {
        data = new byte[size];

        instance = this;
    }

    public CodeNode Read(Stream stream)
    {
        // Parsing the whole array now isn't sensible
        stream.ReadWholeArray(data);

        return new CodeNode();
    }

    static GuidHeap instance;
    public static Guid Get(GuidHeapIndex i)
    {
        const int size = 16;
        int startAt = (i.Index - 1) * size; // GuidHeap is indexed from 1

        return new Guid(instance.data.Skip(startAt).Take(size).ToArray());
    }
}

// II.24.2.6
sealed class TildeStream : ICanRead
{
    public TildeData TildeData;
    public uint[] Rows;


    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadStruct(out TildeData),
            stream.ReadStructs(out Rows, ((ulong)TildeData.Valid).CountSetBits(), "Rows"),
            Enum.GetValues(typeof(MetadataTableFlags))
                .Cast<MetadataTableFlags>()
                .Where(flag => TildeData.Valid.HasFlag(flag))
                .SelectMany((flag, row) => ReadTable(stream, flag, row))
        };
    }

    IEnumerable<CodeNode> ReadTable(Stream stream, MetadataTableFlags flag, int row)
    {
        int count = (int)Rows[row];

        switch (flag)
        {
            case MetadataTableFlags.Module:
                ModuleTableRow[] ModuleTableRows = null;
                return stream.ReadClasses(ref ModuleTableRows, count);
            case MetadataTableFlags.TypeRef:
                TypeRefTableRow[] TypeRefTableRows = null;
                return stream.ReadClasses(ref TypeRefTableRows, count);
            default:
                return new[] { new CodeNode { Name = flag.ToString(), Errors = new List<string> { "Unknown MetadataTableFlags" + flag.ToString() } } };
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct TildeData
{
    [Description("Reserved, always 0 (§II.24.1).")]
    [Expected(0)]
    public uint Reserved;
    [Description("Major version of table schemata; shall be 2 (§II.24.1).")]
    [Expected(2)]
    public byte MajorVersion;
    [Description("Minor version of table schemata; shall be 0 (§II.24.1).")]
    [Expected(0)]
    public byte MinorVersion;
    [Description("Bit vector for heap sizes. (Allowed to be non-zero but I haven't implemented that...)")]
    [Expected(0)]
    public TildeDateHeapSizes HeapSizes;
    [Description("Reserved, always 1 (§II.24.1).")]
    [Expected(1)]
    public byte Reserved2;
    [Description("Bit vector of present tables, let n be the number of bits that are 1.")]
    public MetadataTableFlags Valid;
    [Description("Bit vector of sorted tables.")]
    public MetadataTableFlags Sorted;
}

[Flags]
enum TildeDateHeapSizes : byte
{
    StringHeapIndexWide = 0x01,
    GuidHeapIndexWide = 0x02,
    BlobHeapIndexWide = 0x04,
}

// II.22
[Flags]
public enum MetadataTableFlags : ulong
{
    Module = 1L << 0x00,
    TypeRef = 1L << 0x01,
    TypeDef = 1L << 0x02,
    Field = 1L << 0x04,
    MethodDef = 1L << 0x06,
    Param = 1L << 0x08,
    InterfaceImpl = 1L << 0x09,
    MemberRef = 1L << 0x0A,
    Constant = 1L << 0x0B,
    CustomAttribute = 1L << 0x0C,
    FieldMarshal = 1L << 0x0D,
    DeclSecurity = 1L << 0x0E,
    ClassLayout = 1L << 0x0F,
    FieldLayout = 1L << 0x10,
    StandAloneSig = 1L << 0x11,
    EventMap = 1L << 0x12,
    Event = 1L << 0x14,
    PropertyMap = 1L << 0x15,
    Property = 1L << 0x17,
    MethodSemantics = 1L << 0x18,
    MethodImpl = 1L << 0x19,
    ModuleRef = 1L << 0x1A,
    TypeSpec = 1L << 0x1B,
    ImplMap = 1L << 0x1C,
    FieldRVA = 1L << 0x1D,
    Assembly = 1L << 0x20,
    AssemblyProcessor = 1L << 0x21,
    AssemblyOS = 1L << 0x22,
    AssemblyRef = 1L << 0x23,
    AssemblyRefProcessor = 1L << 0x24,
    AssemblyRefOS = 1L << 0x25,
    File = 1L << 0x26,
    ExportedType = 1L << 0x27,
    ManifestResource = 1L << 0x28,
    NestedClass = 1L << 0x29,
    GenericParam = 1L << 0x2A,
    MethodSpec = 1L << 0x2B,
    GenericParamConstraint = 1L << 0x2C,
}

sealed class StringHeapIndex : ICanRead, IHaveValue
{
    ushort? shortIndex;
    uint? intIndex;

    public int Index { get { return (int)(intIndex ?? shortIndex); } }

    public object Value { get { return StringHeap.Get(this); } }

    public CodeNode Read(Stream stream)
    {
        //TODO make link

        ushort index;
        var node = new CodeNode
        {
            stream.ReadStruct(out index, "index"),
        };

        shortIndex = index;

        return node;
    }
}

sealed class UserStringHeapIndex : ICanRead, IHaveValue
{
    ushort? shortIndex;
    uint? intIndex;

    public int Index { get { return (int)(intIndex ?? shortIndex); } }

    public object Value { get { return UserStringHeap.Get(this); } }

    public CodeNode Read(Stream stream)
    {
        //TODO make link

        ushort index;
        var node = new CodeNode
        {
            stream.ReadStruct(out index, "index"),
        };

        shortIndex = index;

        return node;
    }
}

sealed class BlobHeapIndex : ICanRead, IHaveValue
{
    ushort? shortIndex;
    uint? intIndex;

    public int Index { get { return (int)(intIndex ?? shortIndex); } }

    public object Value { get { return BlobHeap.Get(this); } }

    public CodeNode Read(Stream stream)
    {
        //TODO make link

        ushort index;
        var node = new CodeNode
        {
            stream.ReadStruct(out index, "index"),
        };

        shortIndex = index;

        return node;
    }
}

sealed class GuidHeapIndex : ICanRead, IHaveValue
{
    ushort? shortIndex;
    uint? intIndex;

    public int Index { get { return (int)(intIndex ?? shortIndex); } }
    public object Value { get { return GuidHeap.Get(this); } }

    public CodeNode Read(Stream stream)
    {
        //TODO make link

        ushort index;
        var node = new CodeNode
        {
            stream.ReadStruct(out index, "index"),
        };

        shortIndex = index;
        return node;
    }
}

sealed class CodedIndex : ICanRead
{
    public CodeNode Read(Stream stream)
    {
        //TODO make link

        //TODO variable width?

        ushort index;
        return new CodeNode
        {
            stream.ReadStruct(out index, "index"),
        };
    }
}

// II.22.30
sealed class ModuleTableRow : ICanRead
{
    public ushort Generation;
    public StringHeapIndex Name;
    public GuidHeapIndex Mvid;
    public GuidHeapIndex EncId;
    public GuidHeapIndex EncBaseId;

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadStruct(out Generation, "Generation"),
            stream.ReadClass(ref Name, "Name"),
            stream.ReadClass(ref Mvid, "Mvid"),
            stream.ReadClass(ref EncId, "EncId"),
            stream.ReadClass(ref EncBaseId, "EncBaseId"),
        };
    }
}

// II.22.38
sealed class TypeRefTableRow : ICanRead
{
    public CodedIndex ResolutionScope;
    public StringHeapIndex TypeName;
    public StringHeapIndex TypeNamespace;

    public CodeNode Read(Stream stream)
    {
        return new CodeNode
        {
            stream.ReadClass(ref ResolutionScope, "ResolutionScope"),
            stream.ReadClass(ref TypeName, "TypeName"),
            stream.ReadClass(ref TypeNamespace, "TypeNamespace"),
        };
    }
}