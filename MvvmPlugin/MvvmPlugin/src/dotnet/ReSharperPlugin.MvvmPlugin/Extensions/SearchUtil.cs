using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ReSharper.Feature.Services.CSharp.ContextActions;
using JetBrains.ReSharper.Feature.Services.Navigation.Requests;
using JetBrains.ReSharper.Feature.Services.Occurrences;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Search;
using JetBrains.ReSharper.Psi.Tree;

namespace ReSharperPlugin.MvvmPlugin.Extensions;

public static  class SearchUtil
{
    public static IEnumerable<ReferenceOccurrence> FindUsagesInFile<T>(this ICSharpContextActionDataProvider provider,
        T treeNode, Func<T, IDeclaredElement> declaredElementRetriever) where T : ITreeNode
    {   
        var consumer = new SearchResultsConsumer();
        
        // A failed search is not an empty result: callers may be changing source code.
        // Let the context-action/SDK boundary report the failure and roll back the transaction.
        provider.PsiServices.SingleThreadedFinder.FindReferences(declaredElementRetriever(treeNode), domain: SearchDomainFactory.Instance.CreateSearchDomain(treeNode.GetSourceFile()), consumer: consumer, NullProgressIndicator.Create());
        
        foreach (var occurrence in consumer.GetOccurrences().OfType<ReferenceOccurrence>())
        {
            yield return occurrence;
        }
    }
    
    public static IEnumerable<ReferenceOccurrence> FindUsagesInFile(this ICSharpContextActionDataProvider provider,
        ITreeNode treeNode,  params IDeclaredElement[] declaredElements)
    {   
        var consumer = new SearchResultsConsumer();
        
        provider.PsiServices.SingleThreadedFinder.FindReferences(declaredElements, domain: SearchDomainFactory.Instance.CreateSearchDomain(treeNode.GetSourceFile()), consumer: consumer, NullProgressIndicator.Create());
        
        foreach (var occurrence in consumer.GetOccurrences().OfType<ReferenceOccurrence>())
        {
            yield return occurrence;
        }
    }
}
