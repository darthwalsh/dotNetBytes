dotNetBytes
===========

[![Build status](https://ci.appveyor.com/api/projects/status/4ejfir3fhv80rhjv/branch/main?svg=true)](https://ci.appveyor.com/project/darthwalsh/dotnetbytes/branch/main)

Have you ever wondered what was inside your C# EXE or DLL? Have you ever tried to modify a binary and got some weird CLR exception? dotNetBytes can help.

There are many good .NET disassemblers out there, and there are many good visual explanations of what the pieces of a .NET assembly are. I wanted the best of both worlds, having a tool to create a custom visualization of my assemblies.

Features
--------
- See a structured view of the PE file header, CLI metadata, and the byte-code
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

https://dotnet.carlwa.com/

Motivation
----------

I was working on a [C# assembler](https://github.com/darthwalsh/bootstrappingCIL) and I was frustrated when running the EXE failed with useless errors. Without this app, you might need to resort to reading through the [EMCA-335 spec](http://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf) to find the off-by-one error in your metadata tables or op codes.

Contributing
============

There are many ways to help out!
- You can try out the app at the website or try cloning it yourself.
- You can file issues for any problems or questions you run into.
- You can file issues with any suggestions or features requests you have.
- You can submit pull requests for any issues or to add testing.

### Debugging

To debug everything, the vscode debug task `CloudFunction/Client` runs both in watch mode..

When coding on the frontend, I normally:
 - use [Live Preview extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.live-server)
 - run vscode debug task `Client` for a separate chrome PWA window, or open http://127.0.0.1:5500/view/?Example=true
 - use Chrome dev-tools to debug HTML, and VS Code / dev-tools for coding or debugging JS

### Software design

There's two main parts to the app, a C# back-end and a JavaScript frontend.

There are four major parts:
 - [`Lib/`](Lib/) has the disassembly library, and can be executed on the command line to see a pseudo-YAML view of the nested objects
 - [`view`/](view/) is the web frontend
 - [`CloudFunction`/](CloudFunction/) is a Google Cloud Function serverless function running `Lib`.
 - [`Test`/](Test/) is a bunch of test cases of different C# and IL features. Please make sure they all pass before you submit a PR.

The interface is the frontend POSTS the assembly, and the back-end returns recursive JSON description of the entire assembly, in this recursive format:

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

Some guarantees about the JSON format:

- Nodes will not have both `Children` and a `LinkPath`
- A node's `Children` will have unique `Name`
- A node's `Children` will not have have overlapping `[Start, End)` ranges
- An array will be represented with names with string suffix, i.e. `{Name:Methods, Children:[ {Name: Method[0], ...}, {Name: Method[1], ...} ]}`
- Apart from array nodes with a single element, a node's `Children` will be smaller than the node.

### Scenarios for full test pass
- Run both `CloudFunction` and SPA with `CloudFunction/Client` task and with `python -m http.server 5500 -d view`; click around, upload EXE, modify EXE
  - For SPA-only changes, can just run http-server, open http://127.0.0.1:8080?Example=true, click around
- `dotnet test` passes

## Deploying

Website static file hosting uses [Cloudflare Pages](https://pages.cloudflare.com/).

C# disassembly runs as a serverless function using [Google Cloud Functions](https://cloud.google.com/functions).

Changes are tested by [AppVeyor](https://ci.appveyor.com/project/darthwalsh/dotnetbytes).

## Future work

- [ ] Convert comments like `// II.23.1.4` to something shown from the UI
  - [ ] COULD use `[EcmaAttribute("II.23.1.4") ]` but that won't work for [OpCode.cs](Lib/OpCode.cs)
  - [ ] MAYBE have a property instead?
  - [ ] Have some [link URL that shows the details](https://github.com/stakx/ecma-335/issues/10#issuecomment-1524807503)
  - [ ] Add ECMA links for all OpCode too
  - [ ] MAYBE assert that all values (all with children) have an ECMA section
- [ ] Update CloudFunction to .NET 6 [recommended by GCP](https://cloud.google.com/functions/docs/concepts/dotnet-runtime)
- [ ] Implement all [TODOs for Lib](Lib/Program.cs) and [Test](Tests/AssemblyBytesTests.cs)
- [ ] Get `dotnet test` working on linux
  - CloudFlare [build configuration](https://developers.cloudflare.com/pages/platform/build-configuration): Installed dotnet 3.1.302	 (or dotnet6)
  - CloudFlare [Blazor guide](https://developers.cloudflare.com/pages/framework-guides/deploy-a-blazor-site/) showing dotnet bootstrap
  - CloudFlare [Deploying "anything"](https://developers.cloudflare.com/pages/framework-guides/deploy-anything/)
  - CloudFlare [current build image OS](https://github.com/cloudflare/pages-build-image/discussions/1):  Ubuntu 16.04 (xenial) but might upgrade to 22.04 (jammy) and dotnet 6.0.5
- MAYBE bundle as a vscode extension?
