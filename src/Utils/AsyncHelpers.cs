using System.Threading;
using System.Threading.Tasks;

namespace MSPaint.Utils
{
    public static class AsyncHelpers
    {
        public static async Task Delay(int ms, CancellationToken ct = default)
        {
            await Task.Delay(ms, ct).ConfigureAwait(false);
        }
    }
}