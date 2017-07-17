[![Build status](https://ci.appveyor.com/api/projects/status/4ejfir3fhv80rhjv/branch/master?svg=true)](https://ci.appveyor.com/project/darthwalsh/dotnetbytes/branch/master)


dotNetBytes
===========

Have you ever wondered what was inside your C# EXE or DLL? Have you ever tried to modify a binary and got some weird CLR exception? dotNetBytes can help.

There are many good .NET dissassemblers out there, and there are many good visual explanations of what the pieces of a .NET assembly are. I wanted the best of both worlds, having a tool to create a custom visualization of my assemblies.

Features
--------
- See a structured view of the PE file header, CLI metadata, and of course the bytecode.
- Understand little endian numbers, bit flags, and string blobs though the detai view on the right.
- Errors are listed in the details.

Try It!
-------

Try it out at https://dotnetbytes.azurewebsites.net/

Contributing
============

There's two main parts to the app, a C# backend and a JavaScript frontend.

There are three projects:
 - dotNetBytes has the dissasembly library, a simple command line host, as well as the website in the `view` folder.
 - WebHost is a simple ASP.NET server, which hosts the dissasembly library.
 - Test is a bunch of test cases of different C# and IL features. Please make sure they all pass before you submit a PR.

When coding on the frontend, I normally run [http-server](https://www.npmjs.com/package/http-server) in the `view` folder, then use browser devtools.

The interface is the frontend POSTS the assembly, and the backend returns recursive JSON description of the entire assenbly, in this format:

	{
        "Name": "SomeUniqueName"
	  , "Description": "Notes about this node based on the language spec"
	  , "Value": "A ToString() view of the node"
	  , "Start": StartingByteIndex
	  , "End": EndingByteIndex
	  , "LinkPath": Path/To/Another/Node
	  , "Errors": ["Any problems in the bytes that violate the language spec"]
	  , "Children": [ { $DescriptionsOfTheInnerNodes }, ... ]
    }

