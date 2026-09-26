using System;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ContextActions;
using JetBrains.TextControl;
using JetBrains.Threading;
using JetBrains.Util;
using JetBrains.Util.Logging;

namespace ReSharperPlugin.MvvmPlugin.ContextActions;

/// <summary>
/// Provides native JetBrains diagnostics at MVVM context-action boundaries.
/// </summary>
/// <remarks>
/// Execution failures must escape to the SDK's transaction executor for rollback and reporting.
/// Exception data identifies the action for diagnostics; it does not assign a Marketplace plugin ID.
/// </remarks>
public abstract class MvvmContextActionBase : ContextActionBase
{
    /// <summary>Reports failed availability checks and hides the action, allowing cancellation to propagate.</summary>
    public sealed override bool IsAvailable(IUserDataHolder cache)
    {
        try
        {
            return IsAvailableCore(cache);
        }
        catch (Exception exception) when (!exception.IsOperationCanceled())
        {
            Logger.GetLogger(GetType()).LogException(exception);
            return false;
        }
    }

    /// <summary>Checks availability without changing source code.</summary>
    protected abstract bool IsAvailableCore(IUserDataHolder cache);

    /// <summary>Enriches failures without suppressing the exception needed by the SDK to roll back.</summary>
    protected sealed override Action<ITextControl>? ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
    {
        var continuation = ExecuteWithContext(() => ExecutePsiTransactionCore(solution, progress), "ExecutePsiTransaction");
        return continuation == null ? null : textControl => ExecuteWithContext(() =>
        {
            continuation(textControl);
            return true;
        }, "PostTransaction");
    }

    /// <summary>Performs the action inside the SDK-owned PSI transaction.</summary>
    protected abstract Action<ITextControl>? ExecutePsiTransactionCore(ISolution solution, IProgressIndicator progress);

    /// <summary>
    /// Attaches diagnostic context and rethrows the original failure. Cancellation is neither enriched nor reported.
    /// Post-transaction failures cannot roll back an already committed transaction.
    /// </summary>
    private T ExecuteWithContext<T>(Func<T> action, string phase)
    {
        try
        {
            return action();
        }
        catch (Exception exception) when (!exception.IsOperationCanceled())
        {
            exception.WithDataSafe("MvvmHelper.ContextAction", GetType().FullName);
            exception.WithDataSafe("MvvmHelper.Phase", phase);
            throw;
        }
    }
}
