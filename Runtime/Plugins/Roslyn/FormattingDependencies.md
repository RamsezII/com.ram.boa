# Roslyn formatting dependencies

The formatter uses the netstandard2.0 builds of Microsoft.CodeAnalysis.Workspaces.Common and Microsoft.CodeAnalysis.CSharp.Workspaces 4.13.0, matching the existing Roslyn compiler and scripting DLLs.

Additional managed dependencies:

| Package | Version |
| --- | --- |
| Humanizer.Core | 2.14.1 |
| Microsoft.Bcl.AsyncInterfaces | 8.0.0 |
| System.Composition.AttributedModel | 8.0.0 |
| System.Composition.Convention | 8.0.0 |
| System.Composition.Hosting | 8.0.0 |
| System.Composition.Runtime | 8.0.0 |
| System.Composition.TypedParts | 8.0.0 |
| System.Threading.Channels | 7.0.0 |

Microsoft.Bcl.AsyncInterfaces uses its netstandard2.1 forwarding build, compatible with Unity's BCL. Other BCL dependencies are supplied by Unity 7000.0.0a10's BCLExtensions and netstandard2.1 libraries.

Microsoft.CodeAnalysis.Workspaces.dll has reference validation disabled because it also references optional SQLite persistence assemblies that are not included in the Roslyn NuGet dependency set. Formatting uses an AdhocWorkspace without SQLite storage; no native SQLite library is required.

Official sources: [Roslyn](https://github.com/dotnet/roslyn/tree/75e79dace86b274327a1afe479228d82a06051a4), [Humanizer](https://github.com/Humanizr/Humanizer), [.NET runtime](https://github.com/dotnet/runtime). Packages were retrieved from api.nuget.org.

## MIT License

Roslyn and Microsoft dependencies: Copyright (c) Microsoft Corporation. All rights reserved.

Humanizer: Copyright (c) .NET Foundation and Contributors.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
