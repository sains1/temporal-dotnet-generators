using System.Collections.Immutable;
using System.Linq;
using CodeGenHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ActivityMockGenerator;

[Generator]
public class ActivityMockCodeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        
    }
    
    private static void Execute(SourceProductionContext context, ImmutableArray<IMethodSymbol?> symbols)
    {
        
    }
}