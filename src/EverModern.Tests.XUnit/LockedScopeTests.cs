using EverModern.Threading.Locks;

namespace EverModern.Tests.XUnit;

public class LockedScopeTests
{
    [Fact]
    public void Enter_Lock_AcquiresAndExits()
    {
        var lockObj = new Lock();

        using (var scope = LockedScope.Enter(lockObj))
        {
            Assert.NotNull(scope);
        }

        using var scope2 = LockedScope.Enter(lockObj);
    }

    [Fact]
    public async Task EnterAsync_Semaphore_AcquiresAndReleases()
    {
        var semaphore = new SemaphoreSlim(1, 1);

        var scope = await LockedScope.EnterAsync(semaphore);
        Assert.NotNull(scope);

        var acquiredAfterEnter = semaphore.Wait(0);
        Assert.False(acquiredAfterEnter);

        scope.Exit();

        var acquiredAfterExit = semaphore.Wait(0);
        Assert.True(acquiredAfterExit);
        semaphore.Release();
    }

    [Fact]
    public void Exit_ReleasesLock()
    {
        var lockObj = new Lock();

        var scope = LockedScope.Enter(lockObj);
        scope.Exit();

        using var scope2 = LockedScope.Enter(lockObj);
    }

    [Fact]
    public async Task DoubleDispose_DoesNotThrow()
    {
        var semaphore = new SemaphoreSlim(1, 1);

        var scope = await LockedScope.EnterAsync(semaphore);

        ((IDisposable)scope).Dispose();
        ((IDisposable)scope).Dispose();
    }
}
