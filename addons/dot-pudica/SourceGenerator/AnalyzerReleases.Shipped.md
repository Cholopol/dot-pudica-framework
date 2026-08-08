; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DOTPUDICA001 | DotPudicaBinding | Error | PathNotFound
DOTPUDICA005 | DotPudicaBinding | Error | CommandNotICommand
DOTPUDICA010 | DotPudicaBinding | Error | CollectionNotObservable
DOTPUDICA030 | DotPudicaBinding | Error | StructIntermediatePath
DOTPUDICA031 | DotPudicaBinding | Error | TargetPropertyInvalid
DOTPUDICA032 | DotPudicaBinding | Error | TypeMismatchWithoutConverter
DOTPUDICA033 | DotPudicaBinding | Error | ConverterNotTyped
DOTPUDICA034 | DotPudicaBinding | Error | TwoWayReferenceUpcastRequiresConverter
DOTPUDICA035 | DotPudicaBinding | Error | BoxingConversionNotAllowed
DOTPUDICA036 | DotPudicaBinding | Error | ItemCommandParameterMismatch
