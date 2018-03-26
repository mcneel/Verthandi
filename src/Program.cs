using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;

namespace Verthandi
{
  public class Program
  {
    private static readonly object _lock = new object();
    public static void Main(string[] args)
    {
      Console.ForegroundColor = ConsoleColor.Black;
      Console.BackgroundColor = ConsoleColor.White;

      Assembly assembly = Assembly.GetAssembly(typeof(Program));
      if (assembly == null)
      {
        WriteLine(ConsoleColor.DarkRed, "Go get David, the console assembly couldn't be found.");
        WriteLine(ConsoleColor.DarkRed, "Press any key to stop this application...");
        Console.ReadKey(true);
        return;
      }

      string name = System.IO.Path.GetFileNameWithoutExtension(assembly.Location);
      HistoryFile = System.IO.Path.ChangeExtension(assembly.Location, "txt");
      History = History.LoadFromFile(HistoryFile);
      CurrentTrack = null;
      Recording = false;

      while (true)
      {
        Console.Clear();
        Write("This application is tracking time for category: ");
        WriteLine(ConsoleColor.DarkGreen, name);
        WriteLine();

        WriteLine(ConsoleColor.DarkGreen, History.ToString());
        WriteLine();

        ConsoleGetter getter = new ConsoleGetter();
        getter.AddOption("S", "Show summary of tracked time.", () => History.ShowSummary(), ConsoleColor.Blue);
        getter.AddOption("P", "Pause tracking, but do not close application.", () => Recording = false, ConsoleColor.Blue);
        getter.AddOption("Q", "Quit tracking, and close application.", ShutDown, ConsoleColor.Blue);
        getter.AddOption("R", "Resume or begin tracking.", () => Recording = true, ConsoleColor.Blue);
        getter.AddOption("+", "Add custom time span.", AddCustomSpan);
        getter.AddOption("-", "Subtract a custom time span.", SubtractCustomSpan);

        getter.GetOption(true);
      }
    }
    private static void ShutDown()
    {
      Recording = false;
      Write(ConsoleColor.DarkMagenta, "Shutting down");
      for (int i = 0; i < 10; i++)
      {
        Thread.Sleep(50);
        Write(ConsoleColor.Magenta, ".");
      }
      Environment.Exit(0);
    }
    private static void AddCustomSpan()
    {
      if (GetSpan("n incremental", out TimeSpan span))
      {
        Track addition = new Track(DateTime.Now, span);
        lock (_lock)
        {
          History = History.AppendTrack(addition);
          History.SaveToFile(HistoryFile);
        }
      }
    }
    private static void SubtractCustomSpan()
    {
      if (GetSpan(" decremental", out TimeSpan span))
      {
        Track addition = new Track(DateTime.Now, new TimeSpan(-span.Ticks));
        lock (_lock)
        {
          History = History.AppendTrack(addition);
          History.SaveToFile(HistoryFile);
        }
      }
    }

    public static void Write(string text, params object[] data)
    {
      Console.Write(text, data);
    }
    public static void Write(ConsoleColor colour, string text, params object[] data)
    {
      ConsoleColor colour0 = Console.ForegroundColor;
      Console.ForegroundColor = colour;
      Console.Write(text, data);
      Console.ForegroundColor = colour0;
    }
    public static void WriteLine(string text, params object[] data)
    {
      Console.WriteLine(text, data);
    }
    public static void WriteLine(ConsoleColor colour, string text, params object[] data)
    {
      ConsoleColor colour0 = Console.ForegroundColor;
      Console.ForegroundColor = colour;
      Console.WriteLine(text, data);
      Console.ForegroundColor = colour0;
    }
    public static void WriteLine()
    {
      Console.WriteLine();
    }

    public static bool GetDate(string description, out DateTime date)
    {
      date = DateTime.MinValue;

      Console.WriteLine("Specify a{0} date in the form 'day/month/year':", description);
      var result = Console.ReadLine();
      if (result == null)
        return false;

      result = result.Replace(".", "/");
      result = result.Replace("\\", "/");
      string[] parts = result.Split('/');

      if (parts.Length < 2 || parts.Length > 3)
      {
        WriteLine(ConsoleColor.Red, "That is not a valid date format.");
        WriteLine(ConsoleColor.Red, "Press Enter to try again, anything else to abort.");
        if (Console.ReadKey(true).Key == ConsoleKey.Enter)
          return GetDate(description, out date);
        return false;
      }

      if (!int.TryParse(parts[0], out int day))
      {
        WriteLine(ConsoleColor.Red, "The day isn't a number.");
        WriteLine(ConsoleColor.Red, "Press Enter to try again, anything else to abort.");
        if (Console.ReadKey(true).Key == ConsoleKey.Enter)
          return GetDate(description, out date);
        return false;
      }

      if (!int.TryParse(parts[1], out int month))
      {
        WriteLine(ConsoleColor.Red, "The month isn't a number.");
        WriteLine(ConsoleColor.Red, "Press Enter to try again, anything else to abort.");
        if (Console.ReadKey(true).Key == ConsoleKey.Enter)
          return GetDate(description, out date);
        return false;
      }

      int year = DateTime.Now.Year;
      if (parts.Length > 2)
      {
        if (!int.TryParse(parts[2], out year))
        {
          WriteLine(ConsoleColor.Red, "The year isn't a number.");
          WriteLine(ConsoleColor.Red, "Press Enter to try again, anything else to abort.");
          if (Console.ReadKey(true).Key == ConsoleKey.Enter)
            return GetDate(description, out date);
          return false;
        }
      }

      try
      {
        date = new DateTime(year, month, day);
        return true;
      }
      catch
      {
        WriteLine(ConsoleColor.Red, "This is not a valid date.");
        WriteLine(ConsoleColor.Red, "Press Enter to try again, anything else to abort.");
        if (Console.ReadKey(true).Key == ConsoleKey.Enter)
          return GetDate(description, out date);
        return false;
      }
    }
    public static bool GetSpan(string description, out TimeSpan span)
    {
      span = TimeSpan.Zero;

      Console.WriteLine("Specify a{0} duration in minutes, or type \"more\" for detailed input:", description);
      var result = Console.ReadLine();
      if (result == null)
        return false;

      if (string.Equals(result, "more", StringComparison.OrdinalIgnoreCase) ||
          string.Equals(result, "m", StringComparison.OrdinalIgnoreCase))
      {
        Console.WriteLine("Specify a{0} duration in the format hours:minutes:seconds", description);
        result = Console.ReadLine();
        if (result == null)
          return false;

        string[] segments = result.Split(':');
        if (segments.Length < 2)
        {
          WriteLine(ConsoleColor.Red, "Too few numbers specified.");
          WriteLine(ConsoleColor.Red, "Press Enter to try again, anything else to abort.");
          if (Console.ReadKey(true).Key == ConsoleKey.Enter)
            return GetSpan(description, out span);
          return false;
        }
        if (segments.Length > 3)
        {
          WriteLine(ConsoleColor.Red, "Too many numbers specified.");
          WriteLine(ConsoleColor.Red, "Press Enter to try again, anything else to abort.");
          if (Console.ReadKey(true).Key == ConsoleKey.Enter)
            return GetSpan(description, out span);
          return false;
        }

        if (!int.TryParse(segments[0], out int hours))
        {
          WriteLine(ConsoleColor.Red, "The hours portion isn't a number.");
          WriteLine(ConsoleColor.Red, "Press Enter to try again, anything else to abort.");
          if (Console.ReadKey(true).Key == ConsoleKey.Enter)
            return GetSpan(description, out span);
          return false;
        }
        if (!int.TryParse(segments[1], out int minutes))
        {
          WriteLine(ConsoleColor.Red, "The minutes portion isn't a number.");
          WriteLine(ConsoleColor.Red, "Press Enter to try again, anything else to abort.");
          if (Console.ReadKey(true).Key == ConsoleKey.Enter)
            return GetSpan(description, out span);
          return false;
        }
        int seconds = 0;
        if (segments.Length > 2)
          if (!int.TryParse(segments[2], out seconds))
          {
            WriteLine(ConsoleColor.Red, "The seconds portion isn't a number.");
            WriteLine(ConsoleColor.Red, "Press Enter to try again, anything else to abort.");
            if (Console.ReadKey(true).Key == ConsoleKey.Enter)
              return GetSpan(description, out span);
            return false;
          }

        span = new TimeSpan(hours, minutes, seconds);
        return true;
      }
      else
      {
        if (int.TryParse(result, out int minutes))
        {
          span = TimeSpan.FromMinutes(minutes);
          return true;
        }
        WriteLine(ConsoleColor.Red, "I don't know what you call this, but it's not a number.");
        WriteLine(ConsoleColor.Red, "Press Enter to try again, anything else to abort.");
        if (Console.ReadKey(true).Key == ConsoleKey.Enter)
          return GetSpan(description, out span);
        return false;
      }
    }

    public static History History { get; set; }
    public static string HistoryFile { get; private set; }
    public static Track CurrentTrack { get; set; }
    public static bool Recording
    {
      get { return _timer != null; }
      set
      {
        if (value) // Start recording.
        {
          if (_timer != null)
            return;

          lock (_lock)
          {
            DateTime t0 = DateTime.Now;
            CurrentTrack = new Track(t0, t0);

            _timer = new Timer(TimerCallback);
            _timer.Change(1000, 1000);

            Write("Time tracking is now ");
            Write(ConsoleColor.DarkGreen, "enabled");
            WriteLine(".");

            Console.Title = "Recording";
          }
        }
        else // Stop recording.
        {
          Console.Title = "Paused";
          if (_timer == null)
            return;

          lock (_lock)
          {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
            _timer.Dispose();
            _timer = null;

            History = History.AppendTrack(CurrentTrack);
            History.SaveToFile(HistoryFile);

            Write("Time tracking is now ");
            Write(ConsoleColor.DarkGreen, "disabled");
            WriteLine(".");
          }
        }
      }
    }

    private static Timer _timer;
    private static void TimerCallback(object arg1)
    {
      if (!Recording)
        Console.Title = "Paused";
      else
      {
        lock (_lock)
        {
          Track track = CurrentTrack;
          if (track == null)
          {
            DateTime now = DateTime.Now;
            track = new Track(now, now);
          }
          else
            track = track.Update();

          if (track.Duration.TotalSeconds > 60)
            // After one minute switch to a 5 second update interval.
            _timer.Change(5 * 1000, 5 * 1000);
          else if (track.Duration.TotalSeconds > 60 * 60)
            // After one hour switch to a 30 second update interval.
            _timer.Change(30 * 1000, 30 * 1000);

          CurrentTrack = track;
          History localHistory = History.AppendTrack(track);
          localHistory.SaveToFile(HistoryFile);

          Console.Title = string.Format(
            "Currently tracking: {0:hh\\:mm\\:ss}, total time today: {1}", 
            track.Duration, 
            History.FormatDuration(localHistory.TimeToday()));
        }
      }
    }
  }

  /// <summary>
  /// Utility for getting console options.
  /// </summary>
  public sealed class ConsoleGetter
  {
    private readonly List<string> _names = new List<string>();
    private readonly List<string> _desc = new List<string>();
    private readonly List<Action> _actions = new List<Action>();
    private readonly List<ConsoleColor?> _colours = new List<ConsoleColor?>();
    private bool _singleCharacterOptions = true;

    /// <summary>
    /// Add an option to the getter.
    /// </summary>
    /// <param name="name">Name of option.</param>
    /// <param name="description">Description of option.</param>
    /// <param name="action">Action.</param>
    /// <param name="colour">Optional colour of option.</param>
    /// <returns>True if option was added.</returns>
    public bool AddOption(string name, string description, Action action, ConsoleColor? colour = null)
    {
      if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
      if (string.IsNullOrWhiteSpace(description)) throw new ArgumentNullException(nameof(description));
      if (action == null) throw new ArgumentNullException(nameof(action));

      if (_names.Contains(name))
        return false;

      if (name.Length > 1)
        _singleCharacterOptions = false;

      _names.Add(name);
      _desc.Add(description);
      _actions.Add(action);
      _colours.Add(colour);
      return true;
    }

    /// <summary>
    /// Show the getter prompt.
    /// </summary>
    public bool GetOption(bool includeExplanation)
    {
      if (_names.Count == 0)
        return false;

      if (includeExplanation)
      {
        Program.WriteLine("The following options are available:");
        for (int i = 0; i < _names.Count; i++)
        {
          Program.Write("  ");
          if (_colours[i].HasValue)
            Program.Write(_colours[i].Value, _names[i]);
          else
            Program.Write(_names[i]);
          Program.Write(" = ");
          Program.WriteLine(_desc[i]);
        }
      }

      Program.WriteLine();
      Program.Write("Pick an option [");
      for (int i = 0; i < _names.Count; i++)
      {
        if (i > 0) Program.Write(" ");
        if (_colours[i].HasValue)
          Program.Write(_colours[i].Value, _names[i]);
        else
          Program.Write(_names[i]);
      }
      Program.Write("]: ");

      string result;
      if (_singleCharacterOptions)
      {
        result = Console.ReadKey(true).KeyChar.ToString();
        Console.WriteLine();
      }
      else
        result = Console.ReadLine();

      for (int i = 0; i < _names.Count; i++)
        if (string.Equals(result, _names[i], StringComparison.OrdinalIgnoreCase))
        {
          _actions[i].Invoke();
          return true;
        }

      return false;
    }
  }

  /// <summary>
  /// Represents a collection of recorded time tracks.
  /// </summary>
  public sealed class History
  {
    private readonly Track[] _tracks;

    /// <summary>
    /// Creates an empty history.
    /// </summary>
    public History()
    {
      _tracks = new Track[0];
    }
    /// <summary>
    /// Creates a history based on a set of tracks.
    /// </summary>
    /// <param name="tracks">Tracks.</param>
    public History(IEnumerable<Track> tracks)
    {
      _tracks = tracks.ToArray();
      Array.Sort(_tracks);
    }

    /// <summary>
    /// Try and load the history record from a file.
    /// </summary>
    /// <param name="file">File location.</param>
    /// <returns>History record, or empty record if no file.</returns>
    public static History LoadFromFile(string file)
    {
      if (!System.IO.File.Exists(file))
        return new History();

      var lines = System.IO.File.ReadAllLines(file);
      if (lines.Length == 0)
        return new History();

      List<Track> tracks = new List<Track>();
      foreach (var line in lines)
      {
        if (string.IsNullOrWhiteSpace(line))
          continue;

        string record = line.Trim();
        if (record.StartsWith("#"))
          continue;

        if (Track.TryParseFormat(record, out Track track))
          tracks.Add(track);
        else
          Program.WriteLine(ConsoleColor.Red, "Couldn't parse track line: " + record);
      }

      return new History(tracks);
    }
    /// <summary>
    /// Save this history to a file.
    /// </summary>
    /// <param name="file"></param>
    public void SaveToFile(string file)
    {
      List<string> lines = new List<string>(_tracks.Length);
      foreach (var track in _tracks)
        lines.Add(track.ToString());

      System.IO.File.WriteAllLines(file, lines);
    }

    /// <summary>
    /// Add a track to this history.
    /// </summary>
    /// <param name="track">Track to add.</param>
    /// <returns>Modified history.</returns>
    public History AppendTrack(Track track)
    {
      if (track == null) return this;
      if (track.Duration.Ticks == 0) return this;

      Track[] tracks = new Track[_tracks.Length + 1];
      Array.Copy(_tracks, tracks, _tracks.Length);
      tracks[tracks.Length - 1] = track;

      return new History(tracks);
    }

    /// <summary>
    /// Enumerate over all recorded tracks in a period.
    /// </summary>
    /// <param name="first">Beginning of period.</param>
    /// <param name="last">End of period.</param>
    public IEnumerable<Track> TracksBetween(DateTime first, DateTime last)
    {
      for (int i = 0; i < _tracks.Length; i++)
        if (_tracks[i].Start >= first && _tracks[i].Start < last)
          yield return _tracks[i];
    }
    /// <summary>
    /// Measure all recorded time between two dates.
    /// </summary>
    /// <param name="first">Beginning of period.</param>
    /// <param name="last">End of period.</param>
    public TimeSpan TimeBetween(DateTime first, DateTime last)
    {
      TimeSpan total = TimeSpan.Zero;
      foreach (var track in TracksBetween(first, last))
        total += track.Duration;
      return total;
    }
    /// <summary>
    /// Measure the total time tracked today.
    /// </summary>
    public TimeSpan TimeToday()
    {
      DateTime now = DateTime.Now;
      DateTime today = new DateTime(now.Year, now.Month, now.Day);

      TimeSpan total = TimeSpan.Zero;
      for (int i = _tracks.Length - 1; i >= 0; i--)
        if (_tracks[i].Start < today)
          break;
        else
          total += _tracks[i].Duration;
      return total;
    }

    /// <summary>
    /// Show the summary.
    /// </summary>
    public void ShowSummary()
    {
      DateTime now = DateTime.Now;
      DateTime today = new DateTime(now.Year, now.Month, now.Day);
      DateTime yesterday = today.AddDays(-1);
      DateTime week = today.AddDays(-1 * (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7);
      DateTime month = new DateTime(now.Year, now.Month, 1);

      Program.Write("  Hours logged today:       "); Program.WriteLine(ConsoleColor.DarkGreen, FormatDuration(TimeBetween(today, now)));
      Program.Write("  Hours logged yesterday:   "); Program.WriteLine(ConsoleColor.DarkGreen, FormatDuration(TimeBetween(yesterday, today)));
      Program.Write("  Hours logged this week:   "); Program.WriteLine(ConsoleColor.DarkGreen, FormatDuration(TimeBetween(week, now)));
      Program.Write("  Hours logged this month:  "); Program.WriteLine(ConsoleColor.DarkGreen, FormatDuration(TimeBetween(month, now)));

      Console.WriteLine();
      Program.WriteLine("The following command are available:");
      Program.Write(ConsoleColor.Blue, "  C"); Program.WriteLine(" = Compose a summary over a custom period.");
      Program.Write(ConsoleColor.Blue, "  A"); Program.WriteLine(" = Compose a total summary for *all* records.");
      Program.WriteLine();
      Program.WriteLine("Type a command: [C A]");

      var key = Console.ReadKey(true);

      while (true)
        switch (char.ToUpperInvariant(key.KeyChar))
        {
          case 'C':
            if (Program.GetDate(" starting", out DateTime date0))
              if (Program.GetDate("n ending", out DateTime date1))
              {
                if (date1 < date0)
                {
                  Program.WriteLine(ConsoleColor.Red, "Ending date may not be earlier than starting date.");
                  continue;
                }

                Program.Write("Hours logged from {0} to {1}:");
                Program.WriteLine(ConsoleColor.DarkGreen, "  " + FormatDuration(TimeBetween(date0, date1)));
              }
            break;

          case 'A':
            Console.WriteLine("TODO: read all txt files in this folder and combine their sum-totals.");
            continue;

          default:
            return;
        }
    }
    /// <summary>
    /// Format a duration into hours and minutes.
    /// </summary>
    public static string FormatDuration(TimeSpan span)
    {
      double hours = Math.Floor(span.TotalHours);
      double minutes = span.Minutes;

      if (hours < 1)
        return string.Format("{0} minutes", minutes);

      if (hours.Equals(1))
        return string.Format("1 hour, {0} minutes", minutes);

      return string.Format("{0:0} hours, {1} minutes", hours, minutes);
    }

    /// <summary>
    /// Some very general statistics.
    /// </summary>
    public override string ToString()
    {
      if (_tracks.Length == 0)
        return "No time tracking so far...";

      TimeSpan total = TimeSpan.Zero;
      TimeSpan monthly = TimeSpan.Zero;

      DateTime now = DateTime.Now;
      DateTime thisMonth = new DateTime(now.Year, now.Month, 1);
      foreach (var track in _tracks)
      {
        total += track.Duration;
        if (track.Start >= thisMonth)
          monthly += track.Duration;
      }

      return string.Format("A total of {0:0.00} days recorded, with {1:0.00} hours this month.", total.TotalDays, monthly.TotalHours);
    }
  }
  /// <summary>
  /// A contiguous tracking recording.
  /// </summary>
  public class Track : IComparable<Track>
  {
    /// <summary>
    /// Create a new track from a time frame.
    /// </summary>
    /// <param name="start">Beginning of track.</param>
    /// <param name="end">End of track.</param>
    public Track(DateTime start, DateTime end)
    {
      if (end < start)
        throw new ArgumentException("Track end must happen *after* the track start.");

      Start = start;
      End = end;
      Duration = end - start;
      CustomTrack = false;
    }
    /// <summary>
    /// Create a new custom track.
    /// </summary>
    /// <param name="start">Beginning of track.</param>
    /// <param name="duration">Track duration.</param>
    public Track(DateTime start, TimeSpan duration)
    {
      Start = start;
      End = start;
      Duration = duration;
      CustomTrack = true;
    }

    /// <summary>
    /// Beginning of track.
    /// </summary>
    public DateTime Start { get; }
    /// <summary>
    /// End of track.
    /// </summary>
    public DateTime End { get; }
    /// <summary>
    /// Duration of track.
    /// </summary>
    public TimeSpan Duration { get; }
    /// <summary>
    /// Gets whether this track was specified by the user, rather than recorded over time.
    /// Custom tracks can have negative durations.
    /// </summary>
    public bool CustomTrack { get; }

    /// <summary>
    /// Format this track.
    /// </summary>
    public override string ToString()
    {
      string t0 = Start.Ticks.ToString();
      string t1 = End.Ticks.ToString();
      string td = Duration.Ticks.ToString("D16");

      string textStart = string.Format("{0:F}", Start);
      string textDuration = string.Format("{0:hh\\:mm\\:ss}", Duration);

      return string.Format("{0}|{1}|{2} # on {3} (duration: {4})", t0, t1, td, textStart, textDuration);
    }
    /// <summary>
    /// Parse a string into a track.
    /// </summary>
    public static bool TryParseFormat(string format, out Track track)
    {
      track = default(Track);

      if (string.IsNullOrWhiteSpace(format))
        return false;

      int hashIndex = format.IndexOf('#');
      if (hashIndex > 0)
        format = format.Substring(0, hashIndex);

      string[] parts = format.Split('|');
      if (parts.Length != 3)
      {
        Program.WriteLine(ConsoleColor.Red, "Track line was not formatted correctly.");
        return false;
      }

      if (!long.TryParse(parts[0], out long t0))
      {
        Program.WriteLine(ConsoleColor.Red, "Start time of track was not a valid integer.");
        return false;
      }

      if (!long.TryParse(parts[1], out long t1))
      {
        Program.WriteLine(ConsoleColor.Red, "End time of track was not a valid integer.");
        return false;
      }

      if (!long.TryParse(parts[2], out long td))
      {
        Program.WriteLine(ConsoleColor.Red, "Duration of track was not a valid integer.");
        return false;
      }

      if (t0 >= t1)
        track = new Track(new DateTime(t0), new TimeSpan(td));
      else
        track = new Track(new DateTime(t0), new DateTime(t1));

      return true;
    }

    /// <summary>
    /// Extend the track end-time to Now.
    /// This does not work for custom tracks.
    /// </summary>
    /// <returns>Extended track.</returns>
    public Track Update()
    {
      return Update(DateTime.Now);
    }
    /// <summary>
    /// Extend the track end-time to a new end time.
    /// This does not work for custom tracks.
    /// </summary>
    /// <returns>Extended track.</returns>
    public Track Update(DateTime newEndTime)
    {
      if (CustomTrack)
        throw new InvalidOperationException("Custom tracks cannot be extended.");

      return new Track(Start, newEndTime);
    }

    /// <summary>
    /// Compare two tracks.
    /// </summary>
    public int CompareTo(Track other)
    {
      if (other == null)
        return +1;

      int rc = Start.CompareTo(other.Start);
      if (rc != 0) return rc;

      rc = Duration.CompareTo(other.Duration);
      if (rc != 0) return rc;

      return 0;
    }
  }
}