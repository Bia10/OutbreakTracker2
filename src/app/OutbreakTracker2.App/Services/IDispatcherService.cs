﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.App.Services;

public interface IDispatcherService
{
    public void PostOnUI(Action action);

    public Task InvokeOnUIAsync(Action action, CancellationToken cancellationToken = default);

    public Task<TResult?> InvokeOnUIAsync<TResult>(Func<TResult> action, CancellationToken cancellationToken = default);
}
