﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BMS {
    public delegate void OnBMSEvent(BMSEvent bmsEvent);

    public abstract class Chart {
        protected string title;
        protected string subTitle;
        protected string artist;
        protected string subArtist;
        protected string comments;
        protected string genre;
        protected int playerCount;
        protected float initialBPM;
        protected int playLevel;
        protected int rank;
        protected float volume;
        protected int maxCombos;
        protected readonly List<BMSEvent> bmsEvents = new List<BMSEvent>();
        protected readonly Dictionary<ResourceId, BMSResourceData> resourceDatas = new Dictionary<ResourceId, BMSResourceData>();
        protected readonly HashSet<int> allChannels = new HashSet<int>();
        
        private Action onBmsRefresh;

        public string Title { get { return title; } }
        public string SubTitle { get { return subTitle; } }
        public string Artist { get { return artist; } }
        public string SubArtist { get { return subArtist; } }
        public string Comments { get { return comments; } }
        public string Genre { get { return genre; } }
        public int PlayerCount { get { return playerCount; } }
        public float BPM { get { return initialBPM; } }
        public float PlayLevel { get { return playLevel; } }
        public int Rank { get { return rank; } }
        public float Volume { get { return volume; } }
        public int MaxCombos { get { return maxCombos; } }
        public virtual string RawContent { get { return string.Empty; } }

        public IList<BMSEvent> Events {
            get { return new ReadOnlyCollection<BMSEvent>(bmsEvents); }
        }

        public ICollection<int> AllChannels {
            get { return allChannels; }
        }

        public virtual void Parse(ParseType parseType) {
            if((parseType & ParseType.Content) == ParseType.Content)
                OnDataRefresh();
        }

        private void OnDataRefresh() {
            if(onBmsRefresh != null)
                onBmsRefresh.Invoke();
        }

        protected void ResetAllData(ParseType parseType) {
            if((parseType & ParseType.Header) == ParseType.Header) {
                title = "";
                artist = "";
                subArtist = "";
                comments = "";
                genre = "";
                playerCount = 1;
                initialBPM = 130;
                playLevel = 0;
                rank = 0;
                volume = 1;
            }
            if((parseType & ParseType.Resources) == ParseType.Resources) {
                resourceDatas.Clear();
            }
            if((parseType & ParseType.Content) == ParseType.Content) {
                maxCombos = 0;
                bmsEvents.Clear();
                allChannels.Clear();
                OnDataRefresh();
            }
        }

        public EventDispatcher GetEventDispatcher() {
            return new EventDispatcher(this);
        }

        public IEnumerable<BMSResourceData> IterateResourceData(ResourceType type = ResourceType.Unknown) {
            if(type == ResourceType.Unknown)
                return resourceDatas.Values;
            return resourceDatas.Where(kv => kv.Key.type == type).Select(kv => kv.Value);
        }

        public BMSResourceData GetResourceData(ResourceType type, long id) {
            BMSResourceData result;
            resourceDatas.TryGetValue(new ResourceId(type, id), out result);
            return result;
        }

        public class EventDispatcher {
            readonly Chart chart;
            TimeSpan currentTime, endTime;
            int currentIndex;
            int length;
            public bool debug;

            public event OnBMSEvent BMSEvent;

            internal EventDispatcher(Chart chart) {
                this.chart = chart;
                chart.onBmsRefresh += OnBMSRefresh;
                OnBMSRefresh();
            }

            ~EventDispatcher() {
                chart.onBmsRefresh -= OnBMSRefresh;
            }

            public TimeSpan CurrentTime {
                get { return currentTime; }
            }

            public bool IsStart {
                get { return currentIndex <= 0; }
            }

            public bool IsEnd {
                get { return currentIndex >= length - 1; }
            }

            public TimeSpan EndTime {
                get { return endTime; }
            }

            public long Index {
                get { return currentIndex; }
            }

            public void Seek(TimeSpan newTime, bool dispatchEvents = true) {
                List<BMSEvent> bmsEvents = chart.bmsEvents;
                if(newTime > currentTime) {
                    currentTime = newTime;
                    while(currentIndex < length - 1 && bmsEvents[currentIndex + 1].time <= currentTime) {
                        currentIndex++;
                        if(dispatchEvents && BMSEvent != null)
                            BMSEvent.Invoke(bmsEvents[currentIndex]);
                    }
                } else if(newTime < currentTime) {
                    currentTime = newTime;
                    while(currentIndex > 0 && bmsEvents[currentIndex - 1].time >= currentTime) {
                        currentIndex--;
                        if(dispatchEvents && BMSEvent != null)
                            BMSEvent.Invoke(bmsEvents[currentIndex]);
                    }
                }
            }

            internal void OnBMSRefresh() {
                length = chart.bmsEvents.Count;
                if(length > 0)
                    endTime = length > 0 ? chart.bmsEvents[length - 1].time : TimeSpan.Zero;
                currentTime = TimeSpan.Zero;
                currentIndex = 0;
            }
        }
    }

    [Flags]
    public enum ParseType {
        None = 0,
        Header = 0x1,
        Resources = 0x2,
        Content = 0x4,
    }

    public enum BMSEventType {
        Unknown,
        BMP,
        WAV,
        BPM,
        STOP,
        Note,
        LongNoteStart,
        LongNoteEnd,
        BeatReset
    }

    public struct BMSEvent : IComparable<BMSEvent> {
        public BMSEventType type;
        public int ticks;
        public int measure;
        public float beat;
        public TimeSpan time, time2;
        public int data1;
        public long data2;

        public int CompareTo(BMSEvent other) {
            int comparison;
            if((comparison = time.CompareTo(other.time)) != 0) return comparison;
            if((comparison = ticks.CompareTo(other.ticks)) != 0) return comparison;
            if((comparison = measure.CompareTo(other.measure)) != 0) return comparison;
            return beat.CompareTo(other.beat);
        }
    }

    public struct BMSResourceData {
        public ResourceType type;
        public long resourceId;
        public string dataPath;
        public object additionalData;
    }

    public struct ResourceId: IEquatable<ResourceId> {
        public ResourceType type;
        public long resourceId;

        public ResourceId(ResourceType type, long resourceId) {
            this.type = type;
            this.resourceId = resourceId;
        }

        public bool Equals(ResourceId other) {
            return type == other.type && resourceId == other.resourceId;
        }

        public override bool Equals(object obj) {
            return obj is ResourceId && Equals((ResourceId)obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = 17;
                hashCode = hashCode * 23 + type.GetHashCode();
                hashCode = hashCode * 23 + resourceId.GetHashCode();
                return hashCode;
            }
        }
    }
}