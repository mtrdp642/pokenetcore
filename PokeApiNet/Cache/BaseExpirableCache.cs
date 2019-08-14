﻿using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using System;
using System.Threading;

namespace PokeApiNet.Cache
{
    internal abstract class BaseExpirableCache : IDisposable
    {
        private readonly IDisposable expirationOptionsSub;
        private CancellationTokenSource clearToken = new CancellationTokenSource();
        private DateTimeOffset? absoluteExpiration;
        private TimeSpan? absoluteExpirationRelativeToNow;
        private TimeSpan? slidingExpiration;

        public BaseExpirableCache(IObservable<CacheExpirationOptions> expirationOptionsProvider)
        {
            this.expirationOptionsSub = expirationOptionsProvider.Subscribe(new CacheExpirationOptionsObserver(this));
        }

        protected void ExpireAll()
        {
            // TODO add lock?
            if (clearToken != null && !clearToken.IsCancellationRequested && clearToken.Token.CanBeCanceled)
            {
                clearToken.Cancel();
                clearToken.Dispose();
            }

            clearToken = new CancellationTokenSource();
        }

        /// <summary>
        /// Gets a <see cref="MemoryCacheEntryOptions"/> instance.
        /// </summary>
        /// <remarks>
        /// New options instance has to be constantly instantiated instead of shared
        /// as a consequence of <see cref="clearToken"/> being mutable
        /// </remarks>
        protected MemoryCacheEntryOptions CacheEntryOptions {
            get
            {
                var opts = new MemoryCacheEntryOptions().AddExpirationToken(new CancellationChangeToken(clearToken.Token));
                opts.AbsoluteExpiration = this.absoluteExpiration;
                opts.AbsoluteExpirationRelativeToNow = this.absoluteExpirationRelativeToNow;
                opts.SlidingExpiration = this.slidingExpiration;
                return opts;
            }
        }

        public virtual void Dispose()
        {
            this.ExpireAll();
            this.expirationOptionsSub.Dispose();
        }

        private sealed class CacheExpirationOptionsObserver : IObserver<CacheExpirationOptions>
        {
            private readonly BaseExpirableCache cache;

            public CacheExpirationOptionsObserver(BaseExpirableCache cache)
            {
                this.cache = cache;
            }

            public void OnCompleted()
            {
                // NOOP
            }

            public void OnError(Exception error)
            {
                // NOOP
            }

            public void OnNext(CacheExpirationOptions value)
            {
                this.cache.absoluteExpiration = value.AbsoluteExpiration;
                this.cache.absoluteExpirationRelativeToNow = value.AbsoluteExpirationRelativeToNow;
                this.cache.slidingExpiration = value.SlidingExpiration;
            }
        }
    }
}
