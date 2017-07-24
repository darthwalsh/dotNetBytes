[![Build status](https://ci.appveyor.com/api/projects/status/4ejfir3fhv80rhjv/branch/master?svg=true)](https://ci.appveyor.com/project/darthwalsh/dotnetbytes/branch/master)


dotNetBytes
===========

Have you ever wondered what was inside your C# EXE or DLL? Have you ever tried to modify a binary and got some weird CLR exception? dotNetBytes can help.

There are many good .NET dissassemblers out there, and there are many good visual explanations of what the pieces of a .NET assembly are. I wanted the best of both worlds, having a tool to create a custom visualization of my assemblies.

Features
--------
- See a structured view of the PE file header, CLI metadata, and the bytecode
  - look in the Table of Contents on the left
- See the raw hex codes and ASCII view of the bytes
  - bytes are colored based on grouping and ToC selection
- Understand little endian numbers, bit flags, and string blobs
  - look though the detail view on the right
- Errors are listed in red details
- Relative and absolute addresses are hyperlinked
  - look for the cursor to change

Try It!
-------

Try it out at https://dotnetbytes.azurewebsites.net/

Contributing
============

There are many ways to help out!
- You can try out the app at the website or try cloning it yourself.
- You can file issues for any problems or questions you run into.
- You can file issues with any suggetions or features requests you have.
- You can submit pull requests for any issues or to add testing.

### Software design

There's two main parts to the app, a C# backend and a JavaScript frontend.

There are four major parts:
 - dotNetBytes has the dissasembly library, and a command line host
 - dotNetBytes/view is the web frontend
 - WebHost is a ASP.NET server, hosting the dissasembly library.
 - Test is a bunch of test cases of different C# and IL features. Please make sure they all pass before you submit a PR.

When coding on the frontend, I normally:
 - run [http-server](https://www.npmjs.com/package/http-server) in the `view` folder
 - open http://127.0.0.1:8080?Example
 - use Chrome devtools to debug HTML, and VS Code / devtools for coding or debugging JS

The interface is the frontend POSTS the assembly, and the backend returns recursive JSON description of the entire assenbly, in this recursive format:

	{
        "Name": "SomeUniqueName"
	  , "Description": "Notes about this node based on the language spec"
	  , "Value": "A ToString() view of the node\n Can Be multiple lines"
	  , "Start": StartingByteIndex
	  , "End": EndingByteIndex
	  , "LinkPath": Path/To/Another/Node
	  , "Errors": ["Any problems in the bytes that violate the language spec"]
	  , "Children": [ { $DescriptionsOfTheInnerNodes }, ... ]
    }

