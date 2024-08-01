﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using ScriptPlayer.Shared.Controls;

namespace ScriptPlayer.Shared.Beats
{
    public class Bar : INotifyPropertyChanged, ITickSource
    {
        public event EventHandler TactChanged; 

        private Tact _tact;
        private int _start;
        private Rythm _rythm;
        private int _subdivisions = 1;
        private int _length;

        public int Subdivisions
        {
            get => _subdivisions;
            set
            {
                if (value == _subdivisions) return;
                _subdivisions = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BeatDuration));
            }
        }

        public Tact Tact
        {
            get => _tact;
            set
            {
                if (Equals(value, _tact)) return;

                if(_tact != null)
                    _tact.PropertyChanged -= TactOnPropertyChanged;

                _tact = value;

                if (_tact != null)
                    _tact.PropertyChanged += TactOnPropertyChanged;

                OnPropertyChanged();
            }
        }

        private void TactOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            OnTactChanged();
        }

        public int Start
        {
            get => _start;
            set
            {
                if (value == _start) return;
                _start = value;
                OnPropertyChanged();
            }
        }

        public int Length
        {
            get => _length;
            set
            {
                if (value == _length) return;
                _length = value;
                OnPropertyChanged();
            }
        }

        public Rythm Rythm
        {
            get => _rythm;
            set
            {
                if (Equals(value, _rythm)) return;
                _rythm = value;
                OnPropertyChanged();
            }
        }

        public TimeFrame TimeFrame => new TimeFrame(GetStartTime(), GetEndTime());

        public TimeSpan BeatDuration => Tact.BeatDuration.Divide(Subdivisions);

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void OnTactChanged()
        {
            TactChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool Intersects(TimeSpan start, TimeSpan end)
        {
            return Intersects(new TimeFrame(start, end));
        }

        public bool Intersects(TimeFrame timeframe)
        {
            return TimeFrame.Intersects(timeframe);
        }

        public TickType DetermineTick(TimeFrame within)
        {
            var indices = GetBeatIndices(within);

            foreach (int index in indices)
            {
                //if(index < Start || index > End)
                //    continue;

                int localIndex = index % Rythm.Length;

                if (!Rythm[localIndex])
                    continue;

                TimeSpan time = TranslateIndex(index);
                if (within.Contains(time))
                    return localIndex == 0 ? TickType.Major : TickType.Minor;
            }

            return TickType.None;
        }

        public TimeSpan GetStartTime()
        {
            return Tact.Start + BeatDuration.Multiply(Start);
        }

        public TimeSpan GetEndTime()
        {
            return Tact.Start + BeatDuration.Multiply(Start + Length);
        }


        public IEnumerable<TimeSpan> GetBeats()
        {
            return GetIndices().Select(TranslateIndex);
        }

        public List<TimeSpan> GetBeats(TimeSpan tBegin, TimeSpan tEnd)
        {
            //TODO optimize (divide instead of enumerating)
            return GetIndices().Select(TranslateIndex).Where(t => t >= tBegin && t <= tEnd).ToList();
        }

        private IEnumerable<int> GetIndices()
        {
            return Enumerable.Range(0, Length +1)
                .Where(i => Rythm[i % Rythm.Length]);
        }

        public IEnumerable<int> GetBeatIndices(TimeFrame timeframe)
        {
            return GetBeatIndices(timeframe.From, timeframe.To);
        }

        public IEnumerable<int> GetBeatIndices(TimeSpan start, TimeSpan end)
        {
            int tactTotalBeats = Tact.Beats * Subdivisions;

            TimeSpan barStart = GetStartTime();

            int firstBeat = (int)Math.Ceiling((start - barStart).Divide(BeatDuration));
            int lastBeat = (int)Math.Floor((end - barStart).Divide(BeatDuration));

            firstBeat = Math.Min(tactTotalBeats - 1, Math.Max(0, firstBeat));
            lastBeat = Math.Min(tactTotalBeats - 1, Math.Max(0, lastBeat));

            return Enumerable.Range(firstBeat, lastBeat - firstBeat + 1);
        }

        public TimeSpan TranslateIndex(int indexRelativeToBar)
        {
            return GetStartTime() + BeatDuration.Multiply(indexRelativeToBar);
        }

        public int GetClosestIndex(TimeSpan time)
        {
            double position = (time - Tact.Start).Divide(BeatDuration);
            int index = (int)Math.Round(position, MidpointRounding.AwayFromZero);

            int maxIndex = (Tact.Beats - 1) * Subdivisions;

            if (index < 0)
                index = 0;
            else if (index > maxIndex)
                index = maxIndex;

            return index;
        }

        public void ChangeSubdivisions(int divisions)
        {
            int newLength = (int)Math.Ceiling((divisions * Length) / (double)Subdivisions);
            int newStart = (int)Math.Round((divisions * Start) / (double)Subdivisions);

            Start = newStart;
            Length = newLength;
            Subdivisions = divisions;
        }
    }
}