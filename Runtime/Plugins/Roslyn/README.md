# Roslyn pour BOA

Bibliothèques de scripting C# Roslyn **4.13.0**, extraites des paquets NuGet Microsoft officiels, variante `lib/netstandard2.0`.

Les quatre DLL sont disponibles dans l'Editor et les players. Leur référencement automatique est désactivé ; `ram._BOA_.asmdef` les référence explicitement. Une autre assembly qui appelle directement Roslyn doit également déclarer ses références aux DLL.

## Dépendances fournies par Unity

Cette installation vise **Unity 7000.0.0a7 avec CoreCLR**, dont les bibliothèques système fournissent les dépendances de Roslyn. Les assemblies `System.Collections.Immutable`, `System.Reflection.Metadata`, `System.Text.Encoding.CodePages`, `Microsoft.CSharp`, `System.Memory`, `System.Buffers`, `System.Numerics.Vectors`, `System.Runtime.CompilerServices.Unsafe` et `System.Threading.Tasks.Extensions` ne sont pas dupliquées dans BOA.

`Microsoft.CodeAnalysis.Analyzers` est une dépendance d'outillage des paquets NuGet, pas une bibliothèque à importer pour exécuter les scripts. Les DLL de complétion et de Workspaces ne sont pas incluses à cette étape.

La compatibilité avec d'autres versions de Unity doit être vérifiée séparément. La compilation de code au runtime demande un backend qui permet de charger le code généré ; cette configuration ne valide pas les players IL2CPP.

## Provenance et licence

`versions.json` indique les paquets, les liens de téléchargement et les sommes SHA-256 des archives et DLL. Les fichiers `.nuspec`, la licence MIT de Roslyn et les mentions tierces originales sont conservés dans `Licenses/`.

Source Roslyn : https://github.com/dotnet/roslyn/tree/75e79dace86b274327a1afe479228d82a06051a4

Paquet principal : https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting/4.13.0
