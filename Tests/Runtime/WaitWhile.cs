using System;
using System.Diagnostics;
using UnityEngine;

namespace BananaParty.WebSocketClient.Tests
{
    public class WaitWhile : CustomYieldInstruction
    {
        private readonly Func<bool> _conditionFunc;
        private readonly float _timeoutDuration;

        private readonly Stopwatch _stopwatch = new();

        public WaitWhile(Func<bool> condition, float timeoutDuration = float.PositiveInfinity)
        {
            _conditionFunc = condition;
            _timeoutDuration = timeoutDuration;
        }

        public override bool keepWaiting
        {
            get
            {
                if (!_stopwatch.IsRunning)
                    _stopwatch.Start();

                return _conditionFunc.Invoke() && _stopwatch.Elapsed.Seconds < _timeoutDuration;
            }
        }

        public override void Reset()
        {
            base.Reset();

            _stopwatch.Reset();
        }
    }
}
