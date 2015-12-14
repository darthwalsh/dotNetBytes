using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable 0649 // CS0649: Field '...' is never assigned to, and will always have its default value

// TODO Characteristics as enums

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

abstract class Custom : IVisitChildren, ICanRead, IHasLocation
{
    public abstract int Start { get; }
    public abstract int End { get; }
    
    public abstract void Read(Stream s);
    public abstract void VisitFields(int start, CodeNode parent);
}

abstract class OrderedCustom
{
    Dictionary<string, int> offsets;
    int lastRawAddress = -1;

    IEnumerable<FieldInfo> OrderedFields
    {
        get
        {
            return GetType().GetFields().OrderBy(field => ((OrderMeAttribute)field.GetCustomAttributes(typeof(OrderMeAttribute), false).Single()).Order);
        }
    }

    public void Read(Stream s)
    {
        foreach (var field in OrderedFields)
        {
            var fieldType = field.FieldType;

            if (fieldType.IsArray)
            {
                var elementType = fieldType.GetElementType();

                var os = Array.CreateInstance(elementType, GetCount(field.Name));
                for (int i = 0; i < os.Length; ++i)
                {
                    var read = s.Read(elementType);
                    os.SetValue(read, i);
                }

                field.SetValue(this, os);
            }
            else
            {
                var read = s.Read(fieldType);
                field.SetValue(this, read);
            }
        }
    }

    protected virtual int GetCount(string name)
    {
        throw new InvalidOperationException(name);
    }

    // TODO kill?
    public virtual int GetOffset(string name, int rawAddress)
    {
        int offset;

        if (offsets == null || lastRawAddress != rawAddress || !offsets.TryGetValue(name, out offset))
        {
            lastRawAddress = rawAddress;

            int relativeAt = 0;

            offsets = new Dictionary<string, int>();
            foreach (var field in OrderedFields)
            {
                offsets.Add(field.Name, relativeAt);
                relativeAt += field.GetValue(this).GetSize();
            }

            offset = offsets[name];
        }

        return offset;
    }

    protected sealed class OrderMeAttribute : Attribute
    {
        public int Order;
        public OrderMeAttribute([CallerLineNumber] int i = -1)
        {
            Order = i;
        }
    }
}


// S25
sealed class FileFormat : Custom
{
    public PEHeader PEHeader;
    public Section[] Sections;

    public override int Start { get { return 0; } }

    public override int End { get { return Sections.Max(s => s.End); } }

    public override void Read(Stream s)
    {
        PEHeader = s.ReadClass<PEHeader>();

        List<Section> sections = new List<Section>();
        foreach (var header in PEHeader.SectionHeaders)
        {
            int start = (int)header.PointerToRawData;

            var section = new Section(start, start + (int)header.SizeOfRawData, (int)header.VirtualAddress, PEHeader.PEOptionalHeader.PEHeaderHeaderDataDirectories);
            section.Read(s);

            sections.Add(section);
        }
        Sections = sections.ToArray();
    }

    public override void VisitFields(int start, CodeNode parent)
    {
        CodeNode current = new CodeNode
        {
            Name = "Root",

            Start = Start,
            End = End,
        };

        PEHeader.VisitFields(start, current);
        start += PEHeader.GetSize();

        foreach (var s in Sections)
        {
            s.VisitFields(-1, current);
        }

        parent.Children.Add(current);
    }
}

// S25.2.2
sealed class PEHeader : OrderedCustom
{
    [OrderMe]
    public DosHeader DosHeader;
    [OrderMe]
    public PESignature PESignature;
    [OrderMe]
    public PEFileHeader PEFileHeader;
    [OrderMe]
    public PEOptionalHeader PEOptionalHeader;
    [OrderMe]
    public SectionHeader[] SectionHeaders;

    protected override int GetCount(string name)
    {
        switch (name)
        {
            case "SectionHeaders": return PEFileHeader.NumberOfSections;
        }
        return base.GetCount(name);
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
    public ushort DLLFlags;
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
    [Description("Flags describing section’s characteristics; see below.")]
    public uint Characteristics;
}

class NodeObject
{
    public string Name;
    public string Description = "";
    public object Value;

    public int Start;
    public int End;

    public CodeNode CodeNode
    {
        get
        {
            return new CodeNode
            {
                Name = Name,
                Description = Description,
                Value = Value.GetString(),

                Start = Start,
                End = End,
            };
        }
    }
}

sealed class Section : Custom
{
    public int start;
    public int end;
    public int rva;

    PEHeaderHeaderDataDirectories data;

    List<NodeObject> children = new List<NodeObject>();

    public override int Start { get { return start; } }

    public override int End { get { return end; } }

    public Section(int start, int end, int rva, PEHeaderHeaderDataDirectories data)
    {
        this.start = start;
        this.end = end;
        this.rva = rva;
        this.data = data;
    }

    public override void Read(Stream s)
    {
        foreach (var nr in data.GetType().GetFields()
            .Where(field => field.FieldType == typeof(RVAandSize))
            .Select(field => new { name = field.Name, rva = (RVAandSize)field.GetValue(data) })
            .Where(nr => rva < nr.rva.RVA && nr.rva.RVA < rva + end - start )
            .OrderBy(nr => nr.rva.RVA))
        {
            var type = typeof(Section).Assembly.GetType(nr.name);
            if (type == null)
            {
                throw new InvalidOperationException(nr.name);
            }

            var startPos = start + nr.rva.RVA - rva;
            s.Position = startPos;

            var o = s.Read(type);
            children.Add(new NodeObject
            {
                Name = nr.name,
                Value = o,

                Start = (int)startPos,
                End = (int)s.Position,
            });
        }
    }

    public override void VisitFields(int start, CodeNode parent)
    {
        foreach (var obj in children)
        {
            CodeNode current = obj.CodeNode;

            obj.Value.VisitFields(obj.Start, current);

            parent.Children.Add(current);
        }
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
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct BaseRelocationTable
{
    public uint HintNameTableRVA;
    [Expected(0)]
    public uint NullTerminated;
}

// II.25.3.1
[StructLayout(LayoutKind.Sequential, Pack = 1)]
struct Relocations
{
    public uint HintNameTableRVA;
    [Expected(0)]
    public uint NullTerminated;
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
    public ulong MetaData;
    [Description("Flags describing this runtime image. (§II.25.3.3.1).")]
    public uint Flags;
    [Description("Token for the MethodDef or File of the entry point for the image")]
    public uint EntryPointToken;
    [Description("RVA and size of implementation-specific resources.")]
    public ulong Resources;
    [Description("RVA of the hash data for this PE file used by the CLI loader for binding and versioning")]
    public ulong StrongNameSignature;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong CodeManagerTable;
    [Description("RVA of an array of locations in the file that contain an array of function pointers (e.g., vtable slots), see below.")]
    public ulong VTableFixups;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong ExportAddressTableJumps;
    [Description("Always 0 (§II.24.1).")]
    [Expected(0)]
    public ulong ManagedNativeHeader;
}