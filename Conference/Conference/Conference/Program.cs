using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Conference
{
    /// <summary>
    /// Contract for printing scheduled events
    /// </summary>
    public interface IPrinter
    {
        /// <summary>
        /// Responsible for printing data on a device
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        void Print(string format, params object[] arg);
    }
    /// <summary>
    /// Default concret implementation that prints scheduled events on Console
    /// </summary>
    public class ConsolePrinter : IPrinter
    {
        /// <summary>
        /// Prints scheduled events data on console
        /// </summary>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        public void Print(string format, params object[] arg)
        {
            Console.WriteLine(format, arg);
        }
    }

    /// <summary>
    /// Contract that collects proposal from various sources
    /// </summary>
    public interface IProposalCollector
    {
        /// <summary>
        /// Holds description and duration part
        /// </summary>
        List<Tuple<string, string>> Items { get; set; }
    }
    /// <summary>
    /// Collects proposals from constructor - hardcoded into constructors
    /// </summary>
    public class ProposalCollector : IProposalCollector
    {
        public List<Tuple<string, string>> Items { get; set; }
        public ProposalCollector()
        {
            Items = new List<Tuple<string, string>>();
            Items.Add(Tuple.Create<string, string>("Writing Fast Tests Against Enterprise Rails", "60min"));
            Items.Add(Tuple.Create<string, string>("Overdoing it in Python", "45min"));
            Items.Add(Tuple.Create<string, string>("Lua for the Masses", "30min"));
            Items.Add(Tuple.Create<string, string>("Ruby Errors from Mismatched Gem Versions", "45min"));
            Items.Add(Tuple.Create<string, string>("Common Ruby Errors", "45min"));
            Items.Add(Tuple.Create<string, string>("Rails for Python Developers", "lightning"));
            Items.Add(Tuple.Create<string, string>("Communicating Over Distance", "60min"));
            Items.Add(Tuple.Create<string, string>("Accounting-Driven Development", "45min"));
            Items.Add(Tuple.Create<string, string>("Woah", "30min"));
            Items.Add(Tuple.Create<string, string>("Sit Down and Write", "30min"));
            Items.Add(Tuple.Create<string, string>("Pair Programming vs Noise", "45min"));
            Items.Add(Tuple.Create<string, string>("Rails Magic", "60min"));
            Items.Add(Tuple.Create<string, string>("Ruby on Rails: Why We Should Move On", "60min"));
            Items.Add(Tuple.Create<string, string>("Clojure Ate Scala (on my project)", "45min"));
            Items.Add(Tuple.Create<string, string>("Programming in the Boondocks of Seattle", "30min"));
            Items.Add(Tuple.Create<string, string>("Ruby vs. Clojure for Back-End Development", "30min"));
            Items.Add(Tuple.Create<string, string>("Ruby on Rails Legacy App Maintenance", "60min"));
            Items.Add(Tuple.Create<string, string>("A World Without HackerNews", "30min"));
            Items.Add(Tuple.Create<string, string>("User Interface CSS in Rails Apps", "30min"));
        }
    }

    /// <summary>
    /// Contract that is responsible for scheduling proposals
    /// </summary>
    public interface IConferenceEventScheduler
    {
        /// <summary>
        /// schedule the events
        /// </summary>
        void Schedule();
    }
    /// <summary>
    /// Default implementation that schedules the given proposals
    /// </summary>
    public class ConferenceEvent : IConferenceEventScheduler
    {
        /// <summary>
        /// Holds value 12:00 as per spec
        /// </summary>
        public static readonly TimeSpan LUNCH_START_TIME;
        /// <summary>
        /// Holds value 60mins as per spec
        /// </summary>
        public static readonly int LUNCH_DURATION;
        /// <summary>
        /// Possible networking event start date
        /// </summary>
        public static readonly TimeSpan NETWORKING_TIME_RANGE_START;
        /// <summary>
        /// Possible networking event end date
        /// </summary>
        public static readonly TimeSpan NETWORKING_TIME_RANGE_END;
        /// <summary>
        /// Lighting duration: 5mins as per spec
        /// </summary>
        public static readonly int LIGHTING_DURATION;
        /// <summary>
        /// Session minimum value: 1min
        /// </summary>
        public static readonly int SESSION_DURATION_RANGE_MIN;
        /// <summary>
        /// Session maximum value: 60min (Extra implemented)
        /// </summary>
        public static readonly int SESSION_DURATION_RANGE_MAX;
        static ConferenceEvent()
        {
            LUNCH_START_TIME = new TimeSpan(12, 0, 0);
            LUNCH_DURATION = 60;
            NETWORKING_TIME_RANGE_START = new TimeSpan(16, 0, 0);
            NETWORKING_TIME_RANGE_END = new TimeSpan(17, 0, 0);
            LIGHTING_DURATION = 5;
            SESSION_DURATION_RANGE_MIN = 1;
            SESSION_DURATION_RANGE_MAX = 60;

        }
        string EventName { get; set; }
        IPrinter Printer;
        IProposalCollector Proposal { get; set; }
        public ConferenceEvent(string eventName, IProposalCollector proposal, IPrinter printer)
        {
            EventName = eventName;
            Printer = printer;
            Proposal = proposal;
        }

        /// <summary>
        /// holds track information for each day (morning and evening schedules)
        /// </summary>
        List<EventTrack> Tracks { get; set; }
        /// <summary>
        /// Parses proposals into generic event - NonScheduledSession:ISessionItem
        /// </summary>
        /// <returns></returns>
        List<ISessionItem> ParseProposals()
        {
            var proposals = new List<ISessionItem>();
            foreach (var i in Proposal.Items)
            {
                var p = new NonScheduledSession(i.Item1, 0);
                //sets an event as lightning that will be used later on to create actual object of lighting
                if (i.Item2.Contains("lightning"))
                {
                    p.Duration = ConferenceEvent.LIGHTING_DURATION;
                    p.Session = typeof(Lightning);
                }
                else
                {
                    //all other events are talk but they are not yet scheduled
                    p.Duration = Convert.ToInt32(i.Item2.Replace("min", ""));
                    p.Session = typeof(Talk);
                }
                //validates proposal as per parsed item
                IValidate checker = p as IValidate;
                checker.Valid();
                proposals.Add(p);
            }
            //ordering events, considering max duration is first candidate
            proposals = proposals.OrderByDescending(o => o.Duration).ToList();
            return proposals;
        }
        /// <summary>
        /// Build the tracks that schedule events
        /// </summary>
        public void Schedule()
        {
            var proposals = ParseProposals();
            Tracks = new List<EventTrack>();
            //are there any events which are not scheduled?
            while (proposals.Where(w => w.GetType() == typeof(NonScheduledSession)).Count() > 0)
            {
                //if yes then try to schedule it
                var track = new EventTrack();
                track.DaySequence = Tracks.Count() + 1;

                //arranges events into sessions as per given constraints like start time and end time
                track.MorningSession.Arrange(proposals);
                track.EveningSession.Arrange(proposals);

                Tracks.Add(track);
            }
        }
        /// <summary>
        /// prints track information on specified device
        /// </summary>
        public void Print()
        {
            foreach (var track in Tracks)
            {
                track.Print(Printer);
            }
        }
    }
    /// <summary>
    /// holds the values of different session
    /// </summary>
    public class EventTrack
    {
        public int DaySequence { get; set; }
        public Morning MorningSession { get; set; }
        public Evening EveningSession { get; set; }
        public EventTrack()
        {
            MorningSession = new Morning();
            EveningSession = new Evening();
        }
        internal void Print(IPrinter printer)
        {
            printer.Print("Track {0}", this.DaySequence);
            this.MorningSession.Print(printer);
            this.EveningSession.Print(printer);
        }

    }
    //contract for validating the event that will be implemented in all kind of events
    public interface IValidate
    {
        void Valid();
    }
    //data contract that has required data that is useful while scheduling events
    public interface ISessionItem
    {
        /// <summary>
        /// holds description of an event
        /// </summary>
        string Description { get; set; }
        /// <summary>
        /// holds the start time of an event
        /// </summary>
        TimeSpan StartTime { get; set; }
        /// <summary>
        /// duration of an event
        /// </summary>
        int Duration { get; set; }
        /// <summary>
        /// type of an event
        /// </summary>
        Type Session { get; set; }
    }

    /// <summary>
    /// Events those are not scheduled
    /// </summary>
    public class NonScheduledSession : ISessionItem, IValidate
    {
        public string Description { get; set; }
        /// <summary>
        /// At the time parsing proposal it does not SET
        /// </summary>
        public TimeSpan StartTime { get; set; }
        public int Duration { get; set; }
        public Type Session
        {
            get;
            set;
        }
        public NonScheduledSession(string description, int duration)
        {
            Description = description;
            Duration = duration;
        }

        public void Valid()
        {
            if (Duration <= ConferenceEvent.SESSION_DURATION_RANGE_MIN) throw new NotFiniteNumberException("Invalid data.", Duration);
            if (Duration > ConferenceEvent.SESSION_DURATION_RANGE_MAX) throw new NotFiniteNumberException("Invalid data.", Duration);
        }
    }
    /// <summary>
    /// Lunch event that is occurred in each track
    /// </summary>
    public class Lunch : ISessionItem, IValidate
    {
        /// <summary>
        /// Assigned default value to lunch event
        /// </summary>
        public Lunch()
        {
            Description = "Lunch";
            Duration = ConferenceEvent.LUNCH_DURATION;
            StartTime = ConferenceEvent.LUNCH_START_TIME;

        }
        public string Description { get; set; }
        public TimeSpan StartTime { get; set; }
        public int Duration { get; set; }
        public Type Session { get { return this.GetType(); } set { throw new NotImplementedException(); } }
        public void Valid()
        {
        }
    }
    /// <summary>
    /// Events those are scheduled and to be performed
    /// </summary>
    public class Talk : ISessionItem, IValidate
    {
        /// <summary>
        /// It resues NonScheduledSession instance
        /// </summary>
        ISessionItem Data { get; set; }
        public Talk(ISessionItem session)
        {
            Data = session;
        }
        public string Description
        {
            get
            {
                return Data.Description;
            }
            set
            {
                Data.Description = value;
            }
        }
        public TimeSpan StartTime
        {
            get
            {
                return Data.StartTime;
            }
            set
            {
                Data.StartTime = value;
            }
        }
        public int Duration
        {
            get
            {
                return Data.Duration;
            }
            set
            {
                Data.Duration = value;
            }
        }
        public Type Session
        {
            get
            {
                return this.GetType();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
        public virtual void Valid()
        {
            var lunchEndTime = ConferenceEvent.LUNCH_START_TIME.Add(new TimeSpan(ConferenceEvent.LUNCH_DURATION, 0, 0));
            var endTime = StartTime.Add(new TimeSpan(0, Duration, 0));

            if (Duration <= ConferenceEvent.SESSION_DURATION_RANGE_MIN) throw new NotFiniteNumberException("Invalid data.", Duration);
            if (Duration > ConferenceEvent.SESSION_DURATION_RANGE_MAX) throw new NotFiniteNumberException("Invalid data.", Duration);
            if (StartTime.Hours > ConferenceEvent.LUNCH_START_TIME.Hours || StartTime.Hours < lunchEndTime.Hours) throw new NotFiniteNumberException("Invalid data.", StartTime.Hours);
            if (StartTime.Hours > ConferenceEvent.NETWORKING_TIME_RANGE_END.Hours) throw new NotFiniteNumberException("Invalid data.", StartTime.Hours);
            if (endTime.Hours > ConferenceEvent.NETWORKING_TIME_RANGE_END.Hours) throw new NotFiniteNumberException("Invalid data.", endTime.Hours);

        }
    }
    /// <summary>
    /// Lightning is also one kind of Talk so inherited from Talk class
    /// </summary>
    public class Lightning : Talk
    {
        ISessionItem Data { get; set; }
        public Lightning(ISessionItem session)
            : base(session)
        {
            Data = session;
            Data.StartTime = new TimeSpan(0, ConferenceEvent.LIGHTING_DURATION, 0);
        }
        public Type SessionType
        {
            get
            {
                return this.GetType();
            }
        }
    }
    /// <summary>
    /// Networking event that is occurred in each track after 4.00 PM
    /// </summary>
    public class NetworkingSession : ISessionItem, IValidate
    {
        ISessionItem Data { get; set; }
        public NetworkingSession()
        {
            Description = "Networking Session";
        }
        public string Description { get; set; }
        public TimeSpan StartTime { get; set; }
        public int Duration { get; set; }
        public Type Session
        {
            get
            {
                return this.GetType();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void Valid()
        {
            if (StartTime.Hours <= ConferenceEvent.NETWORKING_TIME_RANGE_START.Hours || StartTime.Hours >= ConferenceEvent.NETWORKING_TIME_RANGE_END.Hours) throw new NotFiniteNumberException("Invalid data.", StartTime.Hours);
        }
    }

    /// <summary>
    /// Base class of different sessions - Morning & Evenning
    /// </summary>
    public abstract class SessionPart
    {
        /// <summary>
        /// Holds the scheduled events
        /// </summary>
        public List<ISessionItem> SessionItems { get; set; }
        /// <summary>
        /// Start time of a session
        /// </summary>
        public TimeSpan StartTime { get; set; }
        /// <summary>
        /// End time of a session
        /// </summary>
        public TimeSpan EndTime { get; set; }
        /// <summary>
        /// Arrange events in different session parts
        /// </summary>
        /// <param name="proposals"></param>
        public abstract void Arrange(List<ISessionItem> proposals);
        /// <summary>
        /// prints the data
        /// </summary>
        /// <param name="printer"></param>
        public void Print(IPrinter printer)
        {
            foreach (var p in SessionItems)
            {
                var time = new DateTime(2013, 05, 06, p.StartTime.Hours, p.StartTime.Minutes, 0).ToString("hh:mm tt", System.Globalization.CultureInfo.GetCultureInfo("en-US"));
                printer.Print("{0} => {1}", time, p.Description);
            }
        }
        /// <summary>
        /// selects an event based on available room on session part
        /// </summary>
        /// <param name="proposals"></param>
        /// <param name="availableSessionDuration"></param>
        /// <returns>just returns an index of selected proposal</returns>
        public int GetNextSession(List<ISessionItem> proposals, int availableSessionDuration)
        {

            for (int i = 0; i < proposals.Count; i++)
            {
                if (proposals[i] is NonScheduledSession && proposals[i].Duration <= availableSessionDuration)
                {
                    return i;
                }
            }
            return -1;
        }
    }
    /// <summary>
    /// Morning session
    /// </summary>
    public class Morning : SessionPart
    {
        /// <summary>
        /// Morning session start on 9.00 AM and Ends at 12.00 PM
        /// </summary>
        public Morning()
        {
            StartTime = new TimeSpan(9, 0, 0);
            EndTime = new TimeSpan(12, 0, 0);

        }
        /// <summary>
        /// Arranges events from proposals
        /// </summary>
        /// <param name="proposals"></param>
        public override void Arrange(List<ISessionItem> proposals)
        {
            SessionItems = new List<ISessionItem>();
            //schedule an event till enough room is available
            do
            {
                //calculate the available room for an event
                var availableSessionDuration = (int)(EndTime.TotalMinutes - StartTime.TotalMinutes);
                //if there are no room for an event then exit the loop and start evening part
                if (availableSessionDuration == 0) break;
                //finds suitable next session based on available room
                var proposalIndex = GetNextSession(proposals, availableSessionDuration);
                //for invalid index, exit room
                if (proposalIndex < 0) break;
                //Based on session type it initiates event and NonScheduledSession becomes Scheduled session
                proposals[proposalIndex] = proposals[proposalIndex].Session == typeof(Lightning) ? new Lightning(proposals[proposalIndex]) : new Talk(proposals[proposalIndex]);
                proposals[proposalIndex].StartTime = StartTime;
                SessionItems.Add(proposals[proposalIndex]);
                //increases the start time that helps to calculate available room for an event
                StartTime = StartTime.Add(new TimeSpan(0, proposals[proposalIndex].Duration, 0));

            } while (true);
            //adds lunch
            if (SessionItems.Where(w => w.GetType() == typeof(Lunch)).Count() == 0)
            {
                var lunch = new Lunch();
                lunch.StartTime = StartTime;
                SessionItems.Add(lunch);
            }
        }

    }
    /// <summary>
    /// Evening session
    /// </summary>
    public class Evening : SessionPart
    {
        /// <summary>
        /// evening session starts on 1.00 PM
        /// </summary>
        public Evening()
        {
            StartTime = new TimeSpan(13, 0, 0);
        }
        public override void Arrange(List<ISessionItem> proposals)
        {
            SessionItems = new List<ISessionItem>();
            do
            {
                //calculate the available room for an event as there are two possible start time of networking event we first select lower start time
                var availableSessionDuration = (int)(ConferenceEvent.NETWORKING_TIME_RANGE_START.TotalMinutes - StartTime.TotalMinutes);
                var proposalIndex = GetNextSession(proposals, availableSessionDuration);
                //based on lower networking start time, if proposal is not found then try with latter networking start time 
                if (proposalIndex < 0)
                {
                    availableSessionDuration = (int)(ConferenceEvent.NETWORKING_TIME_RANGE_END.TotalMinutes - StartTime.TotalMinutes);
                    proposalIndex = GetNextSession(proposals, availableSessionDuration);
                    if (proposalIndex < 0) break;
                }
                //Based on session type it initiates event and NonScheduledSession becomes Scheduled session
                proposals[proposalIndex] = proposals[proposalIndex].Session == typeof(Lightning) ? new Lightning(proposals[proposalIndex]) : new Talk(proposals[proposalIndex]);
                proposals[proposalIndex].StartTime = StartTime;
                SessionItems.Add(proposals[proposalIndex]);
                //increases the start time that helps to calculate available room for an event
                StartTime = StartTime.Add(new TimeSpan(0, proposals[proposalIndex].Duration, 0));
            } while (true);
            //adds networking event at last
            if (SessionItems.Where(w => w.GetType() == typeof(NetworkingSession)).Count() == 0 && StartTime.TotalMinutes >= EndTime.TotalMinutes)
            {
                var ns = new NetworkingSession();
                ns.StartTime = StartTime;
                SessionItems.Add(ns);
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            IProposalCollector proposal = new ProposalCollector();
            var conference = new ConferenceEvent("Vrluckyin Events", proposal, new ConsolePrinter());
            conference.Schedule();
            conference.Print();
            Console.WriteLine("Please enter key to continue...");
            Console.ReadKey();
        }
    }
}
