using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

// CodeNode is written though reflection
#pragma warning disable 0649 // CS0649: Field '...' is never assigned to

// II.25 File format extensions to PE 

sealed class FileFormat : CodeNode
{
  public PEHeader PEHeader;
  public Section[] Sections;

  protected override void InnerRead() {
    AddChild(nameof(PEHeader));

    Sections = PEHeader.SectionHeaders.Select(h => new Section(h)).ToArray();
    AddChildren(nameof(Sections));

    End = Sections.Max(s => s.End);
  }
}

// II.25.2
sealed class PEHeader : CodeNode
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
sealed class DosHeader : CodeNode
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
  public LfaNewNode LfaNew;
  [Expected(new byte[] { 0x0E, 0x1F, 0xBA, 0x0E, 0x00, 0xB4, 0x09, 0xCD,
                         0x21, 0xb8, 0x01, 0x4C, 0xCD, 0x21 })]
  public byte[] DosCode;
  [Expected("This program cannot be run in DOS mode.\r\r\n$")]
  public char[] Message;
  [Expected(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
  public byte[] Reserved3;

  public sealed class LfaNewNode : CodeNode
  {
    public uint val;
    protected override void InnerRead() {
      base.InnerRead();
      NodeValue = Children.Single().NodeValue;
      Children.Clear();
    }

    public override CodeNode Link => Bytes.FileFormat.PEHeader.PESignature;
  }
}

class PESignature : CodeNode
{
  [Expected('P')]
  public char MagicP;
  [Expected('E')]
  public char MagicE;
  [Expected(0)]
  public ushort Reserved;
}

// II.25.2.2 
class PEFileHeader : CodeNode
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
  public ushort OptionalHeaderSize; //TODO(size)
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
sealed class PEOptionalHeader : CodeNode
{
  public PEHeaderStandardFields PEHeaderStandardFields;
  [Description("RVA of the data section. (This is a hint to the loader.) Only present in PE32, not PE32+")]
  public int BaseOfData = -1;
  public PEHeaderWindowsNtSpecificFields<uint> PEHeaderWindowsNtSpecificFields32;
  public PEHeaderWindowsNtSpecificFields<ulong> PEHeaderWindowsNtSpecificFields64;
  public PEHeaderHeaderDataDirectories PEHeaderHeaderDataDirectories;

  protected override void InnerRead() {
    AddChild(nameof(PEHeaderStandardFields));

    switch (PEHeaderStandardFields.Magic) {
      case PE32Magic.PE32:
        AddChild(nameof(BaseOfData));
        AddChild(nameof(PEHeaderWindowsNtSpecificFields32));
        break;
      case PE32Magic.PE32plus:
        AddChild(nameof(PEHeaderWindowsNtSpecificFields64));
        break;
      default:
        throw new InvalidOperationException($"Magic not recognized: {PEHeaderStandardFields.Magic:X}");
    }
    Children.Last().NodeName = "PEHeaderWindowsNtSpecificFields";

    AddChild(nameof(PEHeaderHeaderDataDirectories));
  }
}

// II.25.2.3.1
sealed class PEHeaderStandardFields : CodeNode
{
  [Description("Identifies version.")]
  public PE32Magic Magic;
  [Description("Spec says always 6, sometimes more (§II.24.1).")]
  public byte LMajor;
  [Description("Always 0 (§II.24.1).")]
  [Expected(0)]
  public byte LMinor;
  [Description("Size of the code (text) section, or the sum of all code sections if there are multiple sections.")]
  public uint CodeSize; //TODO(size)
  [Description("Size of the initialized data section, or the sum of all such sections if there are multiple data sections.")]
  public uint InitializedDataSize; //TODO(size)
  [Description("Size of the uninitialized data section, or the sum of all such sections if there are multiple unitinitalized data sections.")]
  public uint UninitializedDataSize; //TODO(size)
  [Description("RVA of entry point , needs to point to bytes 0xFF 0x25 followed by the RVA in a section marked execute/read for EXEs or 0 for DLLs")]
  public uint EntryPointRVA;
  [Description("RVA of the code section. (This is a hint to the loader.)")]
  public uint BaseOfCode; //TODO(link) -- TODO: assert that any field with RVA in Name or Description should have .Link?
}

enum PE32Magic : ushort
{
  PE32 = 0x10b,
  PE32plus = 0x20b,
}

// II.25.2.3.2
sealed class PEHeaderWindowsNtSpecificFields<Tint> : CodeNode
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
  public uint ImageSize; //TODO(size)
  [Description("Combined size of MS-DOS Header, PE Header, PE Optional Header and padding; shall be a multiple of the file alignment.")]
  public uint HeaderSize; //TODO(size)
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
  public uint NumberOfDataDirectories; //TODO(size) PEHeaderHeaderDataDirectories but not assert byte count. MAYBE can do math 0x10 x4bytes?
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
sealed class PEHeaderHeaderDataDirectories : CodeNode
{
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

sealed class RVAandSize : CodeNode
{
  [OrderedField] public uint RVA;
  [OrderedField] public uint Size; //TODO(size)
}

// II.25.3
sealed class SectionHeader : CodeNode
{
  [Description("An 8-byte, null-padded ASCII string. There is no terminating null if the string is exactly eight characters long.")]
  public char[] Name;
  [Description("Total size of the section in bytes. If this value is greater than SizeOfRawData, the section is zero-padded.")]
  public uint VirtualSize; //TODO(size) check if there are any .Error from validating this byte size
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

// https://docs.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-image_section_header constants with IMAGE_SCN_
[Flags]
enum SectionHeaderCharacteristics : uint
{
  [Description("The section should not be padded to the next boundary. This flag is obsolete and is replaced by IMAGE_SCN_ALIGN_1BYTES.")]
  TYPE_NO_PAD = 0x00000008,
  [Description("The section contains executable code.")]
  CNT_CODE = 0x00000020,
  [Description("The section contains initialized data.")]
  CNT_INITIALIZED_DATA = 0x00000040,
  [Description("The section contains uninitialized data.")]
  CNT_UNINITIALIZED_DATA = 0x00000080,
  [Description("Reserved.")]
  LNK_OTHER = 0x00000100,
  [Description("The section contains comments or other information. This is valid only for object files.")]
  LNK_INFO = 0x00000200,
  [Description("The section will not become part of the image. This is valid only for object files.")]
  LNK_REMOVE = 0x00000800,
  [Description("The section contains COMDAT data. This is valid only for object files.")]
  LNK_COMDAT = 0x00001000,
  [Description("Reset speculative exceptions handling bits in the TLB entries for this section.")]
  NO_DEFER_SPEC_EXC = 0x00004000,
  [Description("The section contains data referenced through the global pointer.")]
  GPREL = 0x00008000,
  [Description("Reserved.")]
  MEM_PURGEABLE = 0x00020000,
  [Description("Reserved.")]
  MEM_LOCKED = 0x00040000,
  [Description("Reserved.")]
  MEM_PRELOAD = 0x00080000,
  [Description("Align data on a 1-byte boundary. This is valid only for object files.")]
  ALIGN_1BYTES = 0x00100000,
  [Description("Align data on a 2-byte boundary. This is valid only for object files.")]
  ALIGN_2BYTES = 0x00200000,
  [Description("Align data on a 4-byte boundary. This is valid only for object files.")]
  ALIGN_4BYTES = 0x00300000,
  [Description("Align data on a 8-byte boundary. This is valid only for object files.")]
  ALIGN_8BYTES = 0x00400000,
  [Description("Align data on a 16-byte boundary. This is valid only for object files.")]
  ALIGN_16BYTES = 0x00500000,
  [Description("Align data on a 32-byte boundary. This is valid only for object files.")]
  ALIGN_32BYTES = 0x00600000,
  [Description("Align data on a 64-byte boundary. This is valid only for object files.")]
  ALIGN_64BYTES = 0x00700000,
  [Description("Align data on a 128-byte boundary. This is valid only for object files.")]
  ALIGN_128BYTES = 0x00800000,
  [Description("Align data on a 256-byte boundary. This is valid only for object files.")]
  ALIGN_256BYTES = 0x00900000,
  [Description("Align data on a 512-byte boundary. This is valid only for object files.")]
  ALIGN_512BYTES = 0x00A00000,
  [Description("Align data on a 1024-byte boundary. This is valid only for object files.")]
  ALIGN_1024BYTES = 0x00B00000,
  [Description("Align data on a 2048-byte boundary. This is valid only for object files.")]
  ALIGN_2048BYTES = 0x00C00000,
  [Description("Align data on a 4096-byte boundary. This is valid only for object files.")]
  ALIGN_4096BYTES = 0x00D00000,
  [Description("Align data on a 8192-byte boundary. This is valid only for object files.")]
  ALIGN_8192BYTES = 0x00E00000,
  [Description("The section contains extended relocations. The count of relocations for the section exceeds the 16 bits that is reserved for it in the section header. If the NumberOfRelocations field in the section header is 0xffff, the actual relocation count is stored in the VirtualAddress field of the first relocation. It is an error if IMAGE_SCN_LNK_NRELOC_OVFL is set and there are fewer than 0xffff relocations in the section.")]
  LNK_NRELOC_OVFL = 0x01000000,
  [Description("The section can be discarded as needed.")]
  MEM_DISCARDABLE = 0x02000000,
  [Description("The section cannot be cached.")]
  MEM_NOT_CACHED = 0x04000000,
  [Description("The section cannot be paged.")]
  MEM_NOT_PAGED = 0x08000000,
  [Description("The section can be shared in memory.")]
  MEM_SHARED = 0x10000000,
  [Description("The section can be executed as code.")]
  MEM_EXECUTE = 0x20000000,
  [Description("The section can be read.")]
  MEM_READ = 0x40000000,
  [Description("The section can be written to.")]
  MEM_WRITE = 0x80000000,
}

sealed class Section : CodeNode
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

  SectionHeader header;
  public Section(SectionHeader header) {
    this.header = header;
  }

  protected override void InnerRead() {
    Start = (int)header.PointerToRawData;
    header.Child(nameof(header.PointerToRawData)).Link = this;

    End = Start + (int)header.SizeOfRawData;
    rva = (int)header.VirtualAddress;
    string name = new string(header.Name);
    Description = name;

    var optionalHeader = Bytes.FileFormat.PEHeader.PEOptionalHeader;
    var dataDirs = optionalHeader.PEHeaderHeaderDataDirectories;

    foreach (var nr in dataDirs.GetType().GetFields()
        .Where(field => field.FieldType == typeof(RVAandSize))
        .Select(field => new { name = field.Name, rva = (RVAandSize)field.GetValue(dataDirs) })
        .Where(nr => nr.rva.RVA > 0)
        .Where(nr => rva <= nr.rva.RVA && nr.rva.RVA < rva + End - Start)
        .OrderBy(nr => nr.rva.RVA)) {
      LinkReposition(nr.rva, nameof(nr.rva.RVA));

      switch (nr.name) {
        case "CLIHeader":
          Bytes.CLIHeaderSection = this;
          AddChild(nameof(CLIHeader));

          LinkReposition(CLIHeader.MetaData, nameof(CLIHeader.MetaData.RVA));

          AddChild(nameof(MetadataRoot));

          foreach (var streamHeader in MetadataRoot.StreamHeaders.OrderBy(h => h.Name.Str.IndexOf('~'))) // Read #~ after heaps
          {
            LinkReposition(streamHeader.Child(nameof(streamHeader.Offset)), streamHeader.Offset + CLIHeader.MetaData.RVA);

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

                ReadManifestResources();

                AddChild(nameof(Methods));
                if (!Methods.Children.Any()) {
                  Children.RemoveAt(Children.Count - 1); // Don't ResizeLastChild
                }
                break;
              default:
                Errors.Add("Unexpected stream name: " + streamHeader.Name.Str);
                break;
            }
          }

          break;

        case "ImportTable":
          AddChild(nameof(ImportTable));

          LinkReposition(ImportTable, nameof(ImportTable.ImportLookupTable));
          AddChild(nameof(ImportLookupTable));

          LinkReposition(ImportLookupTable, nameof(ImportLookupTable.HintNameTableRVA));
          AddChild(nameof(ImportAddressHintNameTable));

          LinkReposition(ImportTable, nameof(ImportTable.Name));
          AddChild(nameof(RuntimeEngineName));

          var standardFields = optionalHeader.PEHeaderStandardFields;
          LinkReposition(standardFields, nameof(standardFields.EntryPointRVA));
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

  void ReadManifestResources() {
    if (TildeStream.ManifestResources == null) return;

    var entries = new List<ResourceEntry>();
    for (int i = 0; i < TildeStream.ManifestResources.Length; ++i) {
      var manifest = TildeStream.ManifestResources[i];

      LinkReposition(manifest.Child(nameof(manifest.Offset)), manifest.Offset + CLIHeader.Resources.RVA); 
      var entry = Bytes.ReadClass<ResourceEntry>();

      entry.NodeName = $"{nameof(ResourceEntries)}[{i}]";
      entries.Add(entry);
    }

    ResourceEntries = entries.ToArray();
    Children.AddRange(entries);
  }

  public void RepositionWithoutLink(long dataRVA) => Bytes.Stream.Position = Start + dataRVA - rva;

  public void LinkReposition(CodeNode parent, string childName) {
    var child = parent.Child(childName);
    LinkReposition(child, child.GetInt32());
  }

  public void LinkReposition(CodeNode linker, long dataRVA) {
    Bytes.PendingLink = linker;
    RepositionWithoutLink(dataRVA);
  }
}

// II.25.3.1
sealed class ImportTable : CodeNode
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
  public uint ImportAddressTableRVA; //TODO(link)
  [Description("End of Import Table. Shall be filled with zeros.")]
  [Expected(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                           0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                           0x00, 0x00, 0x00, 0x00})]
  public byte[] Reserved;
}

sealed class ImportAddressTable : CodeNode
{
  [OrderedField]
  public uint HintNameTableRVA; //TODO(link)
  [Expected(0)]
  public uint NullTerminated;
}

sealed class ImportLookupTable : CodeNode
{
  [OrderedField]
  public uint HintNameTableRVA;
  [Expected(0)]
  public uint NullTerminated;
}

sealed class ImportAddressHintNameTable : CodeNode
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
sealed class NativeEntryPoint : CodeNode
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
sealed class Relocations : CodeNode
{
  [OrderedField]
  public BaseRelocationTable BaseRelocationTable;
  [OrderedField]
  public Fixup[] Fixups;

  protected override int GetCount(string field) {
    switch (field) {
      case nameof(Fixups):
        var n = ((int)BaseRelocationTable.BlockSize - 8) / 2;
        if (n >= 0) return n;
        Errors.Add($"Calculated Fixups length {n} was negative!");
        return 0;
      default: return base.GetCount(field);
    }
  }
}

sealed class BaseRelocationTable : CodeNode
{
  [OrderedField] public uint PageRVA;
  [OrderedField] public uint BlockSize;
}

sealed class Fixup : CodeNode
{

  [Description("Stored in high 4 bits of word, type IMAGE_REL_BASED_HIGHLOW (0x3).")]
  public byte Type;
  [Description("Stored in remaining 12 bits of word. Offset from starting address specified in the Page RVA field for the block. This offset specifies where the fixup is to be applied.")]
  public short Offset;

  protected override CodeNode ReadField(string fieldName) { // MAYBE just override Read() and no children
    switch (fieldName) {
      case nameof(Type):
        var type = new StructNode<byte> { Bytes = Bytes };
        type.Read();
        Offset = (short)((type.t << 8) & 0x0F00);
        Type = (byte)(type.t >> 4);
        type.NodeValue = Type.GetString();
        return type;
      case nameof(Offset):
        var offset = new StructNode<byte> { Bytes = Bytes };
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
sealed class CLIHeader : CodeNode
{
  [Description("Size of the header in bytes")]
  public uint Cb; //TODO(size) rename to CbHeaderSize
  [Description("The minimum version of the runtime required to run this program, currently 2.")]
  public ushort MajorRuntimeVersion;
  [Description("The minor portion of the version, currently 0.")]
  public ushort MinorRuntimeVersion;
  [Description("RVA and size of the physical metadata (§II.24).")]
  public RVAandSize MetaData;
  [Description("Flags describing this runtime image. (§II.25.3.3.1).")]
  public CliHeaderFlags Flags;
  [Description("Token for the MethodDef or File of the entry point for the image")]
  public uint EntryPointToken; //TODO(link) should this be MetadataToken?
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
  [Description("Shall be 1.")]
  ILOnly = 0x01,
  [Description("Image can only be loaded into a 32-bit process, for instance if there are 32-bit vtablefixups, or casts from native integers to int32. CLI implementations that have 64-bit native integers shall refuse loading binaries with this flag set.")]
  Required32Bit = 0x02,
  [Description("Image has a strong name signature.")]
  StrongNameSigned = 0x08,
  [Description("Shall be 0.")]
  NativeEntryPoint = 0x10,
  [Description("Should be 0 (§II.24.1).")]
  TrackDebugData = 0x10000,
}

sealed class Methods : CodeNode
{
  protected override void InnerRead() {
    if (Bytes.TildeStream.MethodDefs == null) return;

    var methodDefs = Bytes.TildeStream.MethodDefs.GroupBy(m => m.RVA).OrderBy(g => g.Key);
    foreach (var methodDefGroup in methodDefs) {
      var rva = methodDefGroup.Key;
      if (rva == 0) continue;

      Bytes.CLIHeaderSection.RepositionWithoutLink(rva);
      var method = Bytes.ReadClass<Method>();
      foreach (var methodDef in methodDefGroup) {
        methodDef.Child(nameof(methodDef.RVA)).Link = method;
      }

      method.NodeName = $"{nameof(Method)}[{Children.Count}]";
      Children.Add(method);
    }

    Start = Children.Min(m => m.Start);
    End = Children.Max(m => m.End);
  }
}

// II.25.4
sealed class Method : CodeNode
{
  public byte Header; //TODO(pedant) ? enum
  public FatFormat FatFormat;
  public MethodDataSection[] DataSections;
  public InstructionStream CilOps;

  public int CodeSize { get; private set; }
  public int MaxStack { get; private set; }

  protected override void InnerRead() {
    AddChild(nameof(Header));
    var header = Children.Single();

    var moreSects = false;
    var type = (MethodHeaderType)(Header & 0x03);
    switch (type) {
      case MethodHeaderType.Tiny:
        CodeSize = Header >> 2;
        MaxStack = 8;
        header.Description = $"Tiny Header, 0x{CodeSize:X} bytes long";
        break;
      case MethodHeaderType.Fat:
        AddChild(nameof(FatFormat));

        if ((FatFormat.FlagsAndSize & 0xF0) != 0x30) {
          Errors.Add("Expected upper bits of FlagsAndSize to be 3");
        }

        CodeSize = (int)FatFormat.CodeSize;
        MaxStack = FatFormat.MaxStack;
        header.Description = $"Fat Header, 0x{CodeSize:X} bytes long";
        moreSects = ((MethodHeaderType)Header).HasFlag(MethodHeaderType.MoreSects);
        break;
      default:
        throw new InvalidOperationException("Invalid MethodHeaderType " + type);
    }

    CilOps = new InstructionStream(this) { Bytes = Bytes };
    AddChild(nameof(CilOps));

    if (moreSects) {
      while (Bytes.Stream.Position % 4 != 0) {
        _ = Bytes.Read<byte>();
      }

      var dataSections = Bytes.ReadClass<MethodDataSections>();
      dataSections.NodeName = "MethodDataSections";
      Children.Add(dataSections.Children.Count == 1 ? dataSections.Children.Single() : dataSections);
    }
  }
}

sealed class MethodDataSections : CodeNode
{
  protected override void InnerRead() {
    MethodDataSection dataSection = null;
    do {
      dataSection = Bytes.ReadClass<MethodDataSection>();
      dataSection.NodeName = $"{nameof(MethodDataSections)}[{Children.Count}]";
      Children.Add(dataSection);
    }
    while (dataSection.MethodHeaderSection.HasFlag(MethodHeaderSection.MoreSects));
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
sealed class FatFormat : CodeNode
{
  [Description("Lower four bits is rest of Flags, Upper four bits is size of this header expressed as the count of 4-byte integers occupied (currently 3)")]
  public byte FlagsAndSize;
  [Description("Maximum number of items on the operand stack")]
  public ushort MaxStack;
  [Description("Size in bytes of the actual method body")]
  public uint CodeSize;
  [Description("Meta Data token for a signature describing the layout of the local variables for the method")]
  public MetadataToken LocalVarSigTok; // MAYBE is "Meta Data token? defined somewhere
}

// II.25.4.5
sealed class MethodDataSection : CodeNode
{
  public MethodHeaderSection MethodHeaderSection;
  public LargeMethodHeader LargeMethodHeader;
  public SmallMethodHeader SmallMethodHeader;

  protected override void InnerRead() {
    AddChild(nameof(MethodHeaderSection));

    if (!MethodHeaderSection.HasFlag(MethodHeaderSection.EHTable)) {
      throw new InvalidOperationException("Only kind of section data is exception header");
    }

    if (MethodHeaderSection.HasFlag(MethodHeaderSection.FatFormat)) {
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

sealed class SmallMethodHeader : CodeNode
{

  [Description("Size of the data for the block, including the header, say n * 12 + 4.")]
  public byte DataSize;
  [Description("Padding, always 0.")]
  [Expected(0)]
  public ushort Reserved;
  [OrderedField]
  public SmallExceptionHandlingClause[] Clauses;

  protected override void InnerRead() {
    base.InnerRead();

    if (Clauses.Length * 12 + 4 != DataSize) {
      Errors.Add("DataSize was not of the form n * 12 + 4");
    }
  }

  protected override int GetCount(string field) => field switch {
    nameof(Clauses) => (DataSize - 4) / 12,
    _ => base.GetCount(field),
  };
}

sealed class LargeMethodHeader : CodeNode
{
  [Description("Size of the data for the block, including the header, say n * 24 + 4.")]
  public UInt24 DataSize;
  [OrderedField]
  public SmallExceptionHandlingClause[] Clauses; //TODO(fixme) LargeExceptionHandlingClause?

  protected override void InnerRead() {
    base.InnerRead();

    if (Clauses.Length * 24 + 4 != DataSize.IntValue) {
      Errors.Add("DataSize was not of the form n * 24 + 4");
    }
  }

  protected override int GetCount(string field) => field switch {
    nameof(Clauses) => (DataSize.IntValue - 4) / 12, //TODO(fixme) should this be 24???
    _ => base.GetCount(field),
  };
}

// II.25.4.6
sealed class SmallExceptionHandlingClause : CodeNode
{
  [Description("Flags")]
  public ushort SmallExceptionClauseFlags;
  [Description("Offset in bytes of try block from start of method body.")]
  public ushort TryOffset; //TODO(link)
  [Description("Length in bytes of the try block")]
  public byte TryLength; //TODO(size) maybe
  [Description("Location of the handler for this try block")]
  public ushort HandlerOffset; //TODO(link)
  [Description("Size of the handler code in bytes")]
  public byte HandlerLength; //TODO(size) maybe
  [Description("Meta data token for a type-based exception handler OR Offset in method body for filter-based exception handler")]
  public uint ClassTokenOrFilterOffset; //TODO(link)
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

sealed class LargeExceptionHandlingClause : CodeNode
{
  [Description("Flags.")]
  public uint LargeExceptionClauseFlags;
  [Description("Offset in bytes of try block from start of method body.")]
  public uint TryOffset; //TODO(link) ditto all from SmallExceptionHandlingClause
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

sealed class UInt24 : CodeNode
{
  public int IntValue { get; private set; }
  public override string NodeValue => IntValue.GetString();

  protected override void InnerRead() {
    var b = new byte[3];
    Bytes.Stream.ReadWholeArray(b);
    IntValue = (b[2] << 16) + (b[1] << 8) + b[0];
  }
}

sealed class NullTerminatedString : CodeNode
{
  public string Str { get; private set; }

  Encoding encoding;
  int byteBoundary;
  public NullTerminatedString(Encoding encoding, int byteBoundary) {
    this.encoding = encoding;
    this.byteBoundary = byteBoundary;
    NodeValue = "oops unset!!";
  }

  protected override void InnerRead() {
    var builder = new List<byte>();
    var buffer = new byte[byteBoundary];

    while (true) {
      Bytes.Stream.ReadWholeArray(buffer);
      builder.AddRange(buffer);

      if (buffer.Contains((byte)'\0')) {
        Str = encoding.GetString(builder.TakeWhile(b => b != (byte)'\0').ToArray());
        NodeValue = Str.GetString();
        return;
      }
    }
  }
}
