# dotNetBytes

[![Build status](https://ci.appveyor.com/api/projects/status/4ejfir3fhv80rhjv/branch/main?svg=true)](https://ci.appveyor.com/project/darthwalsh/dotnetbytes/branch/main)

Have you ever wondered what was inside your C# EXE or DLL? Have you ever tried to modify a binary and got some weird CLR exception? dotNetBytes can help.

There are many good .NET disassemblers out there, and there are many good visual explanations of what the pieces of a .NET assembly are. I wanted the best of both worlds, having a tool to create a custom visualization of my assemblies.

## Features
- See a structured view of the PE file header, CLI metadata, and the byte-code
  - look in the Table of Contents on the left
- See the raw hex codes and ASCII view of the bytes
  - bytes are colored based on grouping and ToC selection
- Understand little endian numbers, bit flags, and string blobs
  - look though the detail view on the right
- Errors are listed in red details
- Relative and absolute addresses are hyperlinked
  - look for the cursor to change

## Try It!

https://dotnet.carlwa.com/

## Motivation

I was working on a [C# assembler](https://github.com/darthwalsh/bootstrappingCIL) and I was frustrated when running the EXE failed with useless errors. Without this app, you might need to resort to reading through the [EMCA-335 spec](https://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf) to find the off-by-one error in your metadata tables or op codes.

## Contributing

There are many ways to help out!
- You can try out the app at the website or try cloning it yourself.
- You can file issues for any problems or questions you run into.
- You can file issues with any suggestions or features requests you have.
- You can submit pull requests for any issues or to add testing.

## Software Design

There's two main parts to the app, a C# back-end and a JavaScript frontend.

There are four major parts:
 - [`Lib/`](Lib/README.md) has the disassembly library, and can be executed as CLI to see a structured YAML view of parsed objects
 - [`view`/](view/) is the web frontend
 - [`Web`/](Web/README.md) is backend ASP.NET MVC service running `Lib`, [deployed to cloud](#deploying).
 - [`Tests`/](Tests/README.md) has various test cases of different C# and IL features. Keep them passing before you submit a PR!

The interface is the frontend POSTS the assembly, and the back-end returns recursive JSON description of the entire assembly, in this recursive format:

```json
{
  "Name": "SomeUniqueName",
  "Description": "Notes about this node based on the language spec",
  "Value": "A ToString() view of the node\n Can Be multiple lines",
  "Start": 16,
  "End": 32,
  "Ecma": "II.24.2.1",
  "LinkPath": "Path/To/Another/Node",
  "Errors": ["Any problems in the bytes that violate the language spec"],
  "Children": [
    { "Name": "FirstChild", "Description": "" },
    { "Name": "Next Child", "Description": "etc etc" }
  ]
}
```

Some guarantees about the JSON format:

- Nodes will not have both `Children` and a `LinkPath`
- A node's `Children` will have unique `Name`
- A node's `Children` will not have have overlapping `[Start, End)` ranges
- An array will be represented with names with string suffix, i.e. `{Name:Methods, Children:[ {Name: Method[0], ...}, {Name: Method[1], ...} ]}`
- Apart from container nodes (e.g. arrays) with a single element, a node's `Children` will be strictly smaller than the node.

## Debugging

To debug everything, the vscode task `Web/Client` debugs both [`Web/`](Web/README.md) and SPA.

When coding on the frontend, I normally:
1. use [Live Preview extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode.live-server)
2. run vscode debug task `Client` for a separate chrome PWA window, or open http://127.0.0.1:5500/view/?Example=true
3. use Chrome dev-tools to debug HTML, and VS Code / dev-tools for coding or debugging JS
4. click around, upload EXE, modify EXE

### Running Local Docker Server

Run for local docker:

```bash
docker build -t dotnetbytes .
docker run --rm -p 8080:8080 dotnetbytes

# With INFO logging
docker run --rm -p 8080:8080 -e "Logging__LogLevel__Microsoft.AspNetCore=Information" dotnetbytes
```

## Testing

Changes to [`Lib/`](Lib/README.md) are well-tested by [`Tests/`](Tests/README.md):

```bash
dotnet test
```

Branches in github are tested by [AppVeyor](https://ci.appveyor.com/project/darthwalsh/dotnetbytes).

### Manual Testing for Web App

1. Run SPA with `python -m http.server 5500 -d view`
2. Open http://127.0.0.1:5500?Example=true
3. Click around

## Deploying

Frontend runs [`view/`](view/): [Cloudflare Pages](https://pages.cloudflare.com/) autodeploys static files.

Backend runs [`Web/`](Web/README.md): Manually deploys [`Dockerfile`](Dockerfile) image and [`fly.toml`](fly.toml) to [Fly.io](https://fly.io/).
Run in repo root dir, on `main` branch:
```bash
# May be required: fly auth login

fly deploy
```

`fly` will build `Dockerfile` on their cloud, then host at https://dotnetbytes.fly.dev/

## Future work

- [x] Update to .NET 10 because .NET 8 is EOL
- [ ] Implement all [TODOs for Lib](Lib/Program.cs) and [Tests](Tests/AssemblyBytesTests.cs)
- [ ] Get `dotnet test` working on linux
  - CloudFlare [build configuration](https://developers.cloudflare.com/pages/platform/build-configuration)
  - CloudFlare [Blazor guide](https://developers.cloudflare.com/pages/framework-guides/deploy-a-blazor-site/) showing dotnet bootstrap
  - CloudFlare [Deploying "anything"](https://developers.cloudflare.com/pages/framework-guides/deploy-anything/)
  - CloudFlare [current build image OS](https://github.com/cloudflare/pages-build-image/discussions/1):  Ubuntu 16.04 (xenial) but might upgrade to 22.04 (jammy) and dotnet 6.0.5
- MAYBE bundle as a vscode extension?
- [ ] Add macOS to CI/CD
    - [ ] check required depenencies in https://www.appveyor.com/docs/macos-images-software/
    - [ ] check really free tier; add to https://github.com/jixserver/free-for-dev?tab=readme-ov-file#ci--cd
