// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Threading;
using System.Threading;

ThreadPool.SetMinThreads(1, 1);
ThreadPool.SetMaxThreads(2, 2);

Console.WriteLine("Hello, World!");

int runCounter = 0;

async Task<T> RunAsyncMethodSafely<T>(Func<Task<T>> func, string caller = null)
{
    var jtf = new JoinableTaskFactory(new JoinableTaskContext());
    return jtf.Run(async () => await func().ConfigureAwait(false));
}

async Task<T> CallResult<T>(Func<Task<T>> func) {
    return func().ConfigureAwait(false).GetAwaiter().GetResult();
}

async Task<T> CallTaskRun<T>(Func<Task<T>> func) {
    return Task.Run(func).ConfigureAwait(false).GetAwaiter().GetResult();
}

async Task<T> CallAwaitTaskRun<T>(Func<Task<T>> func)
{
    return await Task.Run(func);
}

async Task<T> CallAwait<T>(Func<Task<T>> func)
{
    return await func();
}

async Task AsyncSleep(List<int> threadsList)
{
    Console.WriteLine("  AsyncSleep on " + Environment.CurrentManagedThreadId);
    threadsList.Add(System.Environment.CurrentManagedThreadId);
    await Task.Delay(50);
    threadsList.Add(System.Environment.CurrentManagedThreadId);
    Console.WriteLine("  AsyncSleep end on " + Environment.CurrentManagedThreadId);
}

async Task BlockingWorkSleep(List<int> threadsList)
{
    Console.WriteLine("  BlockingWorkSleep on " + Environment.CurrentManagedThreadId);
    threadsList.Add(System.Environment.CurrentManagedThreadId);
    Thread.Sleep(50);
    threadsList.Add(System.Environment.CurrentManagedThreadId);
    Console.WriteLine("  BlockingWorkSleep end on " + Environment.CurrentManagedThreadId);
}

async Task<List<int>> DoAsyncWork(Func<Func<Task<int>>, Task> asyncRunnerToTest)
{
    Console.WriteLine("Started on " + Environment.CurrentManagedThreadId);
    List<int> threadsList = new List<int>();
    threadsList.Add(System.Environment.CurrentManagedThreadId);

    await asyncRunnerToTest(async () =>
    {
        await AsyncSleep(threadsList);
        return default(int);
    });

    await asyncRunnerToTest(async () =>
    {
        await BlockingWorkSleep(threadsList);
        return default(int);
    });

    threadsList.Add(System.Environment.CurrentManagedThreadId);

    Console.WriteLine("Completed on " + Environment.CurrentManagedThreadId);
    Console.WriteLine(Interlocked.Increment(ref runCounter));

    return threadsList;
}


var testCases = new[]
    {
        new TestCase() { AsyncRunnerToTest = x => RunAsyncMethodSafely(x), Name = nameof(RunAsyncMethodSafely) },
        new TestCase() { AsyncRunnerToTest = x => RunAsyncMethodSafely(x), Name = nameof(RunAsyncMethodSafely) },
        new TestCase() { AsyncRunnerToTest = x => RunAsyncMethodSafely(x), Name = nameof(RunAsyncMethodSafely) },
        new TestCase() { AsyncRunnerToTest = x => CallResult(x), Name = nameof(CallResult) },
        new TestCase() { AsyncRunnerToTest = x => CallResult(x), Name = nameof(CallResult) },
        new TestCase() { AsyncRunnerToTest = x => CallResult(x), Name = nameof(CallResult) },
        new TestCase() { AsyncRunnerToTest = x => CallTaskRun(x), Name = nameof(CallTaskRun) },
        new TestCase() { AsyncRunnerToTest = x => CallTaskRun(x), Name = nameof(CallTaskRun) },
        new TestCase() { AsyncRunnerToTest = x => CallTaskRun(x), Name = nameof(CallTaskRun) },
        new TestCase() { AsyncRunnerToTest = x => CallAwaitTaskRun(x), Name = nameof(CallAwaitTaskRun) },
        new TestCase() { AsyncRunnerToTest = x => CallAwaitTaskRun(x), Name = nameof(CallAwaitTaskRun) },
        new TestCase() { AsyncRunnerToTest = x => CallAwaitTaskRun(x), Name = nameof(CallAwaitTaskRun) },
        new TestCase() { AsyncRunnerToTest = x => CallAwait(x), Name = nameof(CallAwait) },
        new TestCase() { AsyncRunnerToTest = x => CallAwait(x), Name = nameof(CallAwait) },
        new TestCase() { AsyncRunnerToTest = x => CallAwait(x), Name = nameof(CallAwait) },
    }
    .ToList();

var results = new List<string>();

foreach (var testCase in testCases)
{
    var timer = Stopwatch.StartNew();
    var tasks = Enumerable.Range(0, 10).Select(_ => DoAsyncWork(testCase.AsyncRunnerToTest)).ToArray();
    var threadsUsed = (await Task.WhenAll(tasks)).SelectMany(x => x).ToHashSet();

    Console.WriteLine($"Threads used: {threadsUsed.Count}. " + string.Join(",", threadsUsed));
    results.Add($"{testCase.Name}: Finished in {timer.Elapsed}");
}

foreach (var result in results)
{
    Console.WriteLine(result);
}

record TestCase
{
    public Func<Func<Task<int>>, Task> AsyncRunnerToTest { get; set; }
    public string Name { get; set; }
}
