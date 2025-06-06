﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace OutbreakTracker2.Application.Services.FileLocators;

public interface IPcsx2Locator
{
    public ValueTask<string?> FindExeAsync(TimeSpan timeout = default, CancellationToken ct = default);

    public ValueTask<string?> FindOutbreakFile1Async(CancellationToken ct = default);

    public ValueTask<string?> FindOutbreakFile2Async(CancellationToken ct = default);
}