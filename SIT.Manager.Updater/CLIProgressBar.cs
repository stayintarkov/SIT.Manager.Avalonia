using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SIT.Manager.Updater
{
    public class CLIProgressBar : IDisposable, IProgress<double>
    {
        //TODO; Expose these for overwriting
        private static readonly char[] SpinnerChars = ['|', '/', '─', '\\', '|', '/', '─', '\\'];
        private static readonly char[] BarChars = ['[', '#', '-', ']'];
        private readonly object consoleLocker = new object();
        private readonly System.Timers.Timer _timer;
        private readonly short _cursorPos;
        private readonly int _barLength;
        private DateTime _lastSpinnerChange = DateTime.Now;
        private byte _counter = 0;
        private double _progress;
        private int _lastProgressLength = 0;

        public CLIProgressBar(int barLength, int updateRate = 10)
        {
            _timer = new System.Timers.Timer()
            {
                AutoReset = true,
                Enabled = !Console.IsOutputRedirected,
                Interval = 1000 / updateRate,
            };
            _timer.Elapsed += DrawProgress;
            //Microsoft in their infinite wisdom made this return an int despite the fact it can only be as large as BufferWidth which is a short. AAAAAAAAAAAAAAAA
            _cursorPos = (short)Console.CursorLeft;
            Console.CursorVisible = false;

            ArgumentOutOfRangeException.ThrowIfLessThan(barLength, 5);
            _barLength = barLength;
        }

        private void DrawProgress(object? state, ElapsedEventArgs? e)
        {
            lock(consoleLocker)
            {
                //Bar
                int progressLength = _barLength - 2;
                int progressPosition = (int)Math.Round(progressLength * _progress);
                StringBuilder progressBarBuilder = new();
                progressBarBuilder.Append(BarChars[0]);
                progressBarBuilder.Append(new string(BarChars[1], progressPosition));
                progressBarBuilder.Append(new string(BarChars[2], progressLength - progressPosition));
                progressBarBuilder.Append(BarChars[3]);
                progressBarBuilder.Append(new string(' ', 1));

                //Percentage
                progressBarBuilder.Append(string.Format("{0:P2} ", _progress));

                //Progress spinner
                if (DateTime.Now - _lastSpinnerChange > TimeSpan.FromMilliseconds(100))
                {
                    _counter = ++_counter == SpinnerChars.Length ? (byte)0 : _counter;
                    _lastSpinnerChange = DateTime.Now;
                }
                char spinnerChar = SpinnerChars[_counter];
                if (_progress != 1)
                    progressBarBuilder.Append(spinnerChar);

                //Drawing
                Console.SetCursorPosition(_cursorPos, Console.CursorTop);
                int lengthDiff = (progressBarBuilder.Length - _lastProgressLength) * -1;
                _lastProgressLength = progressBarBuilder.Length;
                progressBarBuilder.Append(new string(' ', lengthDiff < 0 ? 0 : lengthDiff));
                Console.Write(progressBarBuilder);
            }
        }

        public void Report(double value)
        {
            Interlocked.Exchange(ref _progress, Math.Clamp(value, 0, 1));
            DrawProgress(null, null);
        }

        public void Dispose()
        {
            Console.CursorVisible = true;
            _timer.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
