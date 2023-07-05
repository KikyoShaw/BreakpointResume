using System.Threading;
using System.Threading.Tasks;

namespace WpfApp1
{
    public struct PauseToken
    {
        private readonly PauseTokenSource _source;

        internal PauseToken(PauseTokenSource source)
        {
            _source = source;
        }

        public bool IsPaused => _source is { IsPaused: true };

        public CancellationToken Token => _source.Token;

        public async Task WaitWhilePausedAsync()
        {
            TaskCompletionSource<bool> cur = _source?._paused;
            if (cur != null)
                await cur.Task.ConfigureAwait(false);
        }
    }

    public class PauseTokenSource
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        public volatile TaskCompletionSource<bool> _paused;

        public CancellationToken Token => _cts.Token;

        public PauseToken PauseToken => new PauseToken(this);

        public bool IsPaused
        {
            get { return _paused != null; }
            set
            {
                if (value)
                {
                    Interlocked.CompareExchange(ref _paused, new TaskCompletionSource<bool>(), null);
                }
                else
                {
                    while (true)
                    {
                        TaskCompletionSource<bool> tcs = _paused;
                        if (tcs == null) return;
                        if (Interlocked.CompareExchange(ref _paused, null, tcs) == tcs)
                        {
                            tcs.SetResult(true);
                            break;
                        }
                    }
                }
            }
        }

        public void Cancel()
        {
            _cts.Cancel();
        }
    }

}
