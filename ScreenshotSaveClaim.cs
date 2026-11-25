using ResoniteModLoader;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ResoniteScreenshotExtensions;

public partial class ResoniteScreenshotExtensions : ResoniteMod
{
    static readonly TimeSpan CLAIM_EXPIRATION = TimeSpan.FromMinutes(10);
    static readonly ConcurrentDictionary<DateTime, ScreenshotSaveClaim> _pendingClaims = new();

    static ScreenshotSaveClaim ClaimScreenshot(DateTime timeTaken)
    {
        var expired = DateTime.UtcNow - CLAIM_EXPIRATION;
        foreach (var pair in _pendingClaims)
        {
            if (pair.Value.CreatedAt < expired)
            {
                _pendingClaims.TryRemove(pair);
            }
        }

        var claim = new ScreenshotSaveClaim(timeTaken);
        _pendingClaims[timeTaken] = claim;
        return claim;
    }

    static bool TryTakeOverDefaultSave(DateTime timeTaken, Action defaultSave)
    {
        return _pendingClaims.TryRemove(timeTaken, out var claim) && claim.DeferDefaultSave(defaultSave);
    }

    sealed class ScreenshotSaveClaim
    {
        readonly object _lock = new();
        readonly DateTime _timeTaken;
        Action? _defaultSave;
        bool _failed;
        bool _completed;

        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public DateTime TimeTaken => _timeTaken;

        public ScreenshotSaveClaim(DateTime timeTaken)
        {
            _timeTaken = timeTaken;
        }

        public bool DeferDefaultSave(Action defaultSave)
        {
            lock (_lock)
            {
                if (_failed) return false;
                if (!_completed) _defaultSave = defaultSave;
                return true;
            }
        }

        public void Complete()
        {
            lock (_lock)
            {
                if (_failed) return;
                _completed = true;
                _defaultSave = null;
            }
        }

        public void Fail()
        {
            Action? defaultSave;
            lock (_lock)
            {
                if (_failed || _completed) return;
                _failed = true;
                defaultSave = _defaultSave;
                _defaultSave = null;
            }
            _pendingClaims.TryRemove(new KeyValuePair<DateTime, ScreenshotSaveClaim>(_timeTaken, this));

            if (defaultSave == null) return;
            try
            {
                Msg("Falling back to Resonite's default screenshot saving.");
                defaultSave();
            }
            catch (Exception ex)
            {
                Error("Failed to save screenshot with Resonite's default behavior:\n" + ex);
            }
        }
    }
}
