using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

// MyCodeNode is written though reflection
#pragma warning disable 0649 // CS0649: Field '...' is never assigned to

// II.25 File format extensions to PE 

sealed class FileFormat : MyCodeNode
{
  [OrderedField] public PEHeader PEHeader;
  [OrderedField] public Section[] Sections;

  protected override int GetCount(string field) => field switch {
    nameof(Sections) => PEHeader.SectionHeaders.Length,
    _ => base.GetCount(field),
  };
}

// II.25.2
sealed class PEHeader : MyCodeNode
{
  [OrderedField] public DosHeader DosHeader;
  [OrderedField] public PESignature PESignature;
  [OrderedField] public PEFileHeader PEFileHeader;
  [OrderedField] public PEOptionalHeader PEOptionalHeader;
  [OrderedField] public SectionHeader[] SectionHeaders;

  protected override int GetCount(string field) => field switch {
    nameof(SectionHeaders) => PEFileHeader.NumberOfSections,
    _ => base.GetCount(field),
  };
}

// II.25.2.1
sealed class DosHeader : MyCodeNode
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
  [Description("Pointer to the Relocation Table, which is size 0.")]
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
  public byte[] Reserved2;
  [Expected(0x80)]
  public uint LfaNew; //TODO(solonode) links to FileFormat/PEHeader/PESignature
  [Expected(new byte[] { 0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD,
                         0x21, 0xb8, 0x01, 0x4C, 0xCD, 0x21 })]
  public byte[] DosCode;
  [Expected("This program cannot be run in DOS mode.\r\r\n$")]
  public char[] Message;
  [Expected(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
  public byte[] Reserved3;
}

class PESignature : MyCodeNode
{
  [Expected('P')]
  public char MagicP;
  [Expected('E')]
  public char MagicE;
  [Expected(0)]
  public ushort Reserved;
}

// II.25.2.2 
class PEFileHeader : MyCodeNode
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
sealed class PEOptionalHeader : MyCodeNode
{
  //TODO(Descriptions)

  public PEHeaderStandardFields PEHeaderStandardFields;
  //TODO(solonode) uncomment [Description("RVA of the data section. (This is a hint to the loader.) Only present in PE32, not PE32+")]
  public int BaseOfData = -1;
  public PEHeaderWindowsNtSpecificFields<uint> PEHeaderWindowsNtSpecificFields32;
  public PEHeaderWindowsNtSpecificFields<ulong> PEHeaderWindowsNtSpecificFields64; //TODO(solonode) naming PE32+
  public PEHeaderHeaderDataDirectories PEHeaderHeaderDataDirectories;

  public override void Read() {
    MarkStarting();

    AddChild(nameof(PEHeaderStandardFields));

    switch (PEHeaderStandardFields.Magic) {
      case PE32Magic.PE32:
        AddChild(nameof(BaseOfData));
        AddChild(nameof(PEHeaderWindowsNtSpecificFields32));
        Children.Last().NodeName = "PEHeaderWindowsNtSpecificFields"; //TODO(solonode)
        Children.Last().NodeValue = "PEHeaderWindowsNtSpecificFields`1[System.UInt32]";  //TODO(solonode)
        break;
      case PE32Magic.PE32plus:
        AddChild(nameof(PEHeaderWindowsNtSpecificFields64));
        Children.Last().NodeName = "PEHeaderWindowsNtSpecificFields"; //TODO(solonode)
        Children.Last().NodeValue = "PEHeaderWindowsNtSpecificFields`1[System.UInt64]";  //TODO(solonode)
        break;
      default:
        throw new InvalidOperationException($"Magic not recognized: {PEHeaderStandardFields.Magic:X}");
    }

    AddChild(nameof(PEHeaderHeaderDataDirectories));

    MarkEnding();
  }
}

// II.25.2.3.1
sealed class PEHeaderStandardFields : MyCodeNode
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
sealed class PEHeaderWindowsNtSpecificFields<Tint> : MyCodeNode
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
sealed class PEHeaderHeaderDataDirectories : MyCodeNode
{
  //TODO(link) all Raw Address from understandingCIL
  //TODO(link) RVAandSize all 

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
  //TODO(pedant) What's the right behavior? Multiple expected attributes? [Expected(0)]
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

sealed class RVAandSize : MyCodeNode
{
  public RVAandSize() {
    NodeValue = "RVAandSize"; //TODO(solonode)
  }

  [OrderedField] public uint RVA;
  [OrderedField] public uint Size;
}

// II.25.3
sealed class SectionHeader : MyCodeNode
{
  [Description("An 8-byte, null-padded ASCII string. There is no terminating null if the string is exactly eight characters long.")]
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

  protected override int GetCount(string field) => field switch {
    nameof(Name) => 8,
    _ => base.GetCount(field),
  };
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

sealed class Section : MyCodeNode
{
  int rva;
  public CLIHeader CLIHeader;
  public MetadataRoot MetadataRoot;
  public StringHeap StringHeap;
  public UserStringHeap UserStringHeap;
  public BlobHeap BlobHeap;
  public GuidHeap GuidHeap;
  public TildeStream TildeStream;
  public ResourceEntry[] ResourceEntries;

  public ImportTable ImportTable;
  public ImportLookupTable ImportLookupTable;
  public ImportAddressHintNameTable ImportAddressHintNameTable;
  public NullTerminatedString RuntimeEngineName = new NullTerminatedString(Encoding.ASCII, 1);
  public NativeEntryPoint NativeEntryPoint;
  public ImportAddressTable ImportAddressTable;
  public Relocations Relocations;

  public Methods Methods;

  int sectionI;
  public Section(int i) {
    sectionI = i;
  }

  //TODO(cleanup) reorder children in order (?)
  public override void Read() {
    var header = Bytes.fileFormat.PEHeader.SectionHeaders[sectionI];

    Start = (int)header.PointerToRawData;
    End = Start + (int)header.SizeOfRawData;
    rva = (int)header.VirtualAddress;
    string name = new string(header.Name);
    Description = name;

    var optionalHeader = Bytes.fileFormat.PEHeader.PEOptionalHeader;
    var dataDirs = optionalHeader.PEHeaderHeaderDataDirectories;

    foreach (var nr in dataDirs.GetType().GetFields()
        .Where(field => field.FieldType == typeof(RVAandSize))
        .Select(field => new { name = field.Name, rva = (RVAandSize)field.GetValue(dataDirs) })
        .Where(nr => nr.rva.RVA > 0)
        .Where(nr => rva <= nr.rva.RVA && nr.rva.RVA < rva + End - Start)
        .OrderBy(nr => nr.rva.RVA)) {
      Reposition(nr.rva.RVA);

      switch (nr.name) {
        case "CLIHeader":
          Bytes.CLIHeaderSection = this;
          AddChild(nameof(CLIHeader));

          Reposition(CLIHeader.MetaData.RVA);
          AddChild(nameof(MetadataRoot));

          foreach (var streamHeader in MetadataRoot.StreamHeaders.OrderBy(h => h.Name.Str.IndexOf('~'))) // Read #~ after heaps
          {
            Reposition(streamHeader.Offset + CLIHeader.MetaData.RVA);

            switch (streamHeader.Name.Str) {
              case "#Strings":
                StringHeap = new StringHeap((int)streamHeader.Size);
                AddChild(nameof(StringHeap));
                break;
              case "#US":
                UserStringHeap = new UserStringHeap((int)streamHeader.Size);
                AddChild(nameof(UserStringHeap));
                break;
              case "#Blob":
                BlobHeap = new BlobHeap((int)streamHeader.Size);
                AddChild(nameof(BlobHeap));
                break;
              case "#GUID":
                GuidHeap = new GuidHeap((int)streamHeader.Size);
                AddChild(nameof(GuidHeap));
                break;
              case "#~":
                TildeStream = new TildeStream(this);
                AddChild(nameof(TildeStream));

                if (TildeStream.ManifestResources != null) {
                  AddChildren(nameof(ResourceEntries), TildeStream.ManifestResources.Length);
                }

                AddChild(nameof(Methods));
                break;
              default:
                Errors.Add("Unexpected stream name: " + streamHeader.Name.Str);
                break;
            }
          }

          break;

        case "ImportTable":
          AddChild(nameof(ImportTable));

          Reposition(ImportTable.ImportLookupTable);
          AddChild(nameof(ImportLookupTable));

          Reposition(ImportLookupTable.HintNameTableRVA);
          AddChild(nameof(ImportAddressHintNameTable));

          Reposition(ImportTable.Name);
          AddChild(nameof(RuntimeEngineName));

          Reposition(optionalHeader.PEHeaderStandardFields.EntryPointRVA);
          AddChild(nameof(NativeEntryPoint));
          break;
        case "ImportAddressTable":
          AddChild(nameof(ImportAddressTable));
          break;
        case "BaseRelocationTable":
          AddChild(nameof(Relocations));
          break;
        default:
          throw new NotImplementedException($"{name} {nr.name}");
      }
    }
  }

  public void Reposition(long dataRVA) => Bytes.Stream.Position = Start + dataRVA - rva;
}

// II.25.3.1
sealed class ImportTable : MyCodeNode
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
  public byte[] Reserved;
}

sealed class ImportAddressTable : MyCodeNode
{
  [OrderedField]
  public uint HintNameTableRVA;
  [Expected(0)]
  public uint NullTerminated;
}

sealed class ImportLookupTable : MyCodeNode
{
  [OrderedField]
  public uint HintNameTableRVA;
  [Expected(0)]
  public uint NullTerminated;
}

sealed class ImportAddressHintNameTable : MyCodeNode
{
  [Description("Shall be 0.")]
  [Expected(0)]
  public ushort Hint;
  [Description("Case sensitive, null-terminated ASCII string containing name to import. Shall be “_CorExeMain” for a.exe file and “_CorDllMain” for a.dll file.")]
  public char[] Message;

  protected override int GetCount(string field) => field switch {
    nameof(Message) => 12,
    _ => base.GetCount(field),
  };
}

// It would be nice to parse all x86 or other kind of assemblies like ARM or JAR, but that's out-of-scope
sealed class NativeEntryPoint : MyCodeNode
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
sealed class Relocations : MyCodeNode
{
  [OrderedField]
  public BaseRelocationTable BaseRelocationTable;
  [OrderedField]
  public Fixup[] Fixups;

  protected override int GetCount(string field) => field switch {
    nameof(Fixups) => ((int)BaseRelocationTable.BlockSize - 8) / 2,
    _ => base.GetCount(field),
  };
}

sealed class BaseRelocationTable : MyCodeNode
{
  [OrderedField] public uint PageRVA;
  [OrderedField] public uint BlockSize;
}

sealed class Fixup : MyCodeNode
{
  //TODO(Descriptions)

  [Description(/*"Stored in high 4 bits of word, type IMAGE_REL_BASED_HIGHLOW (0x3)."*/ "")] //TODO(solonode) 
  public byte Type;
  [Description(/*"Stored in remaining 12 bits of word. Offset from starting address specified in the Page RVA field for the block. This offset specifies where the fixup is to be applied."*/ "")] //TODO(solonode) 
  public short Offset;

  protected override MyCodeNode ReadField(string fieldName) {
    switch (fieldName) {
      case nameof(Type):
        var type = new MyStructNode<byte> { Bytes = Bytes };
        type.Read();
        Offset = (short)((type.t << 8) & 0x0F00);
        Type = (byte)(type.t >> 4);
        type.NodeValue = Type.GetString();
        return type;
      case nameof(Offset):
        var offset = new MyStructNode<byte> { Bytes = Bytes };
        offset.Read();
        Offset |= (short)offset.t;
        offset.NodeValue = Offset.GetString();
        return offset;
      default:
        throw new InvalidOperationException();
    }
  }
}

// II.25.3.3
sealed class CLIHeader : MyCodeNode
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

sealed class Methods : MyCodeNode
{
  public Method[] Method;

  public override void Read() {
    Method = (Bytes.TildeStream.MethodDefs ?? Array.Empty<MethodDef>())
          .Where(m => m.RVA > 0)
          .GroupBy(m => m.RVA)
          .Select(g => g.First()) // Disctinct by RVA
          .OrderBy(m => m.RVA)
          .Select(m => new Method(m)).ToArray();

    base.Read();
  }
}

// II.25.4
sealed class Method : MyCodeNode
{
  MethodDef def;
  public Method(MethodDef def) {
    this.def = def;
    def.SetLink(this);
  }

  public byte Header; //TODO(pedant) ? enum
  public FatFormat FatFormat;
  public MethodDataSection[] DataSections;
  public InstructionStream CilOps;

  public override void Read() {
    Bytes.CLIHeaderSection.Reposition(def.RVA);
    MarkStarting();

    AddChild(nameof(Header));
    var header = Children.Single();

    int length;
    var moreSects = false;
    var type = (MethodHeaderType)(Header & 0x03);
    switch (type) {
      case MethodHeaderType.Tiny:
        length = Header >> 2;
        header.Description = $"Tiny Header, 0x{length:X} bytes long";
        break;
      case MethodHeaderType.Fat:
        AddChild(nameof(FatFormat));

        if ((FatFormat.FlagsAndSize & 0xF0) != 0x30) {
          Errors.Add("Expected upper bits of FlagsAndSize to be 3");
        }

        length = (int)FatFormat.CodeSize;
        header.Description = $"Fat Header, 0x{length:X} bytes long";
        moreSects = ((MethodHeaderType)Header).HasFlag(MethodHeaderType.MoreSects);
        break;
      default:
        throw new InvalidOperationException("Invalid MethodHeaderType " + type);
    }

    CilOps = new InstructionStream(length) { Bytes = Bytes };
    AddChild(nameof(CilOps));

    if (moreSects) {
      while (Bytes.Stream.Position % 4 != 0) {
        _ = Bytes.Stream.ReallyReadByte();
      }

      var dataSections = new MethodDataSections { Bytes = Bytes };
      dataSections.Read();

      Children.Add(dataSections.Children.Count == 1 ? dataSections.Children.Single() : dataSections);
    }
    MarkEnding();
  }
}

sealed class MethodDataSections : MyCodeNode
{
  public List<MethodDataSection> dataSections { get; }= new List<MethodDataSection>();
  public override void Read() {
    MarkStarting();
    MethodDataSection dataSection = null;
    do {
      dataSection = new MethodDataSection() { Bytes = Bytes };
      dataSection.Read();
      dataSection.NodeName = $"{nameof(MethodDataSections)}[{Children.Count}]";
      Children.Add(dataSection);
    }
    while (dataSection.Header.HasFlag(MethodHeaderSection.MoreSects));
    MarkEnding();
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
sealed class FatFormat : MyCodeNode
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
sealed class MethodDataSection : MyCodeNode
{
  public MethodHeaderSection Header;
  public LargeMethodHeader LargeMethodHeader;
  public SmallMethodHeader SmallMethodHeader;

  public override void Read() {
    AddChild(nameof(MethodHeaderSection));

    if (!Header.HasFlag(MethodHeaderSection.EHTable)) {
      throw new InvalidOperationException("Only kind of section data is exception header");
    }

    if (Header.HasFlag(MethodHeaderSection.FatFormat)) {
      AddChild(nameof(LargeMethodHeader));
    } else {
      AddChild(nameof(SmallMethodHeader));
    }
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

sealed class SmallMethodHeader : MyCodeNode
{
  [Description("Size of the data for the block, including the header, say n * 12 + 4.")]
  public byte DataSize;
  [Description("Padding, always 0.")]
  [Expected(0)]
  public ushort Reserved;
  [OrderedField]
  public SmallExceptionHandlingClause[] Clauses;

  public override void Read() {
    base.Read();

    if (Clauses.Length * 12 + 4 != DataSize) {
      Errors.Add("DataSize was not of the form n * 12 + 4");
    }
  }

  protected override int GetCount(string field) => field switch {
    nameof(Clauses) => (DataSize - 4) / 12,
    _ => base.GetCount(field),
  };
}

sealed class LargeMethodHeader : MyCodeNode
{
  [Description("Size of the data for the block, including the header, say n * 24 + 4.")]
  public UInt24 DataSize;
  [OrderedField]
  public SmallExceptionHandlingClause[] Clauses; // TODO(solonode) LargeExceptionHandlingClause?

  public override void Read() {
    base.Read();

    if (Clauses.Length * 24 + 4 != DataSize.IntValue) {
      Errors.Add("DataSize was not of the form n * 24 + 4");
    }
  }

  protected override int GetCount(string field) => field switch {
    nameof(Clauses) => (DataSize.IntValue - 4) / 12, // TODO(solonode) should this be 24???
    _ => base.GetCount(field),
  };
}

// II.25.4.6
sealed class SmallExceptionHandlingClause : MyCodeNode
{
  [Description("Flags")]
  public ushort SmallExceptionClauseFlags;
  [Description("Offset in bytes of try block from start of method body.")]
  public ushort TryOffset; //TODO(links)
  [Description("Length in bytes of the try block")]
  public byte TryLength;
  [Description("Location of the handler for this try block")]
  public ushort HandlerOffset;
  [Description("Size of the handler code in bytes")]
  public byte HandlerLength;
  [Description("Meta data token for a type-based exception handler OR Offset in method body for filter-based exception handler")]
  public uint ClassTokenOrFilterOffset; //TODO(links)
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

sealed class LargeExceptionHandlingClause : MyCodeNode
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

sealed class UInt24 : MyCodeNode
{
  public int IntValue { get; private set; }
  public override string NodeValue => IntValue.GetString(); // TODO(solonode) test this is right

  public override void Read() {
    MarkStarting();

    var b = new byte[3];
    Bytes.Stream.ReadWholeArray(b);
    IntValue = (b[2] << 16) + (b[1] << 8) + b[0];

    MarkEnding();
  }
}

sealed class NullTerminatedString : MyCodeNode // MAYBE refactor all to record types
{
  public string Str { get; private set; }

  Encoding encoding;
  int byteBoundary;
  public NullTerminatedString(Encoding encoding, int byteBoundary) {
    this.encoding = encoding;
    this.byteBoundary = byteBoundary;
    NodeValue = "oops unset!!";
  }

  public override void Read() {
    MarkStarting();

    var builder = new List<byte>();
    var buffer = new byte[byteBoundary];

    while (true) {
      Bytes.Stream.ReadWholeArray(buffer);
      builder.AddRange(buffer);

      if (buffer.Contains((byte)'\0')) {
        MarkEnding();
        Str = encoding.GetString(builder.TakeWhile(b => b != (byte)'\0').ToArray());
        NodeValue = Str.GetString();
        return;
      }
    }
  }
}
