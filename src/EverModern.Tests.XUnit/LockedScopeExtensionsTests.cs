using EverModern.Threading.Locks;

namespace EverModern.Tests.XUnit;

public class LockedScopeExtensionsTests
{
    [Fact]
    public void LockScope_Extension_Lock()
    {
        var lockObj = new Lock();

        using var scope = lockObj.LockScope();
        Assert.NotNull(scope);
        Assert.IsType<LockedScope>(scope);
    }

    [Fact]
    public async Task LockScopeAsync_Extension_Semaphore()
    {
        var semaphore = new SemaphoreSlim(1, 1);

        using var scope = await semaphore.LockScopeAsync();
        Assert.NotNull(scope);
        Assert.IsType<LockedScope>(scope);
    }
}
