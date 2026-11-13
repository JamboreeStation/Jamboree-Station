// SPDX-FileCopyrightText: 2026 nao-nauno <desk.nao@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using Content.Server._Jamboree.Ntsl;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Power.Components;
using Content.Server.Radio.EntitySystems;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared._EinsteinEngines.Language.Systems;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Jamboree.Telecomms;

/// <summary>
/// Compiles NTSL scripts onto telecom servers and runs them on the radio messages those servers operate.
/// </summary>
public sealed class NtslTelecomSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GameTicker _ticker = default!;
    [Dependency] private readonly RadioSystem _radio = default!;

    public const string EntryFunction = "process_signal";
    public const int MaxBroadcastsPerRun = 10;
    public const int CommandingFontSize = 14;
    public const int MaxMemoryKeys = 500;
    public const int MaxLogEntries = 20;
    public const int MaxNameLength = 64;
    public const int MaxLoggedCodeLength = 2000;
    public static readonly TimeSpan UploadCooldown = TimeSpan.FromSeconds(2);

    private readonly Queue<PendingBroadcast> _broadcasts = new();
    private readonly List<Entity<NtslServerComponent, EncryptionKeyHolderComponent>> _servers = new();
    private int _broadcastsThisRun;
    private string? _broadcastName;
    private NtslFilters _broadcastFilters;
    private bool _broadcasting;
    private EntityUid? _speaker;
    private LanguagePrototype? _speakerLanguage;

    // Commands are the "/word" texts a script looks for, like find(sig.raw, "/loud").
    private static readonly Regex CommandRegex = new(@"[""'](/[A-Za-z0-9_]+)", RegexOptions.Compiled);

    private static readonly Dictionary<string, NtslFilters> FilterNames = new()
    {
        ["robot"] = NtslFilters.Robot,
        ["yell"] = NtslFilters.Loud,
        ["italics"] = NtslFilters.Italics,
        ["sans"] = NtslFilters.Sans,
        ["clown"] = NtslFilters.Sans,
        ["command_headset"] = NtslFilters.Commanding,
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NtslServerComponent, TransformSpeakerNameEvent>(OnTransformSpeakerName);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Broadcasts wait for the next tick to let the queue clean itself.
        while (_broadcasts.TryDequeue(out var broadcast))
        {
            if (TerminatingOrDeleted(broadcast.Server))
                continue;

            _broadcasting = true;
            _broadcastName = broadcast.Source;
            _broadcastFilters = broadcast.Filters;
            try
            {
                _radio.SendRadioMessage(broadcast.Server, broadcast.Content, broadcast.Channel, broadcast.Server, broadcast.Language);
            }
            finally
            {
                _broadcasting = false;
                _broadcastName = null;
                _broadcastFilters = NtslFilters.None;
            }
        }
    }

    private void OnTransformSpeakerName(Entity<NtslServerComponent> ent, ref TransformSpeakerNameEvent args)
    {
        if (_broadcasting && _broadcastName != null)
            args.VoiceName = _broadcastName;
    }

    public bool TryUpload(EntityUid server, string code, EntityUid user, [NotNullWhen(false)] out string? error)
    {
        var comp = EnsureComp<NtslServerComponent>(server);
        if (_timing.CurTime < comp.NextUpload)
        {
            error = Loc.GetString("ntsl-error-upload-cooldown");
            return false;
        }

        comp.NextUpload = _timing.CurTime + UploadCooldown;

        if (!TryInstall(server, code, out error))
            return false;

        comp.LastEditor = Name(user);

        var logged = code.Length > MaxLoggedCodeLength ? code[..MaxLoggedCodeLength] : code;
        _adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium, $"{ToPrettyString(user):user} uploaded an NTSL script ({code.Length} characters) to {ToPrettyString(server):server}:\n{logged}");

        return true;
    }

    /// <summary>
    /// Compiles a script and puts it on a server in place of the old one.
    /// Unlike <see cref="TryUpload"/> there is no user and no cooldown, so station events use this.
    /// </summary>
    public bool TryInstall(EntityUid server, string code, [NotNullWhen(false)] out string? error)
    {
        var comp = EnsureComp<NtslServerComponent>(server);

        NtslInterpreter interpreter;
        try
        {
            interpreter = NtslInterpreter.Compile(code);
            Bind(server, interpreter);
            _broadcastsThisRun = 0;
            interpreter.Run();
        }
        catch (NtslException e)
        {
            error = FormatError(e);
            return false;
        }

        if (comp.Code != code)
            comp.PreviousCode = comp.Code;

        comp.Code = code;
        comp.Interpreter = interpreter;
        comp.LastEditor = null;
        comp.Log.Clear();

        error = null;
        return true;
    }

    public void Clear(EntityUid server, EntityUid? user)
    {
        if (!TryComp<NtslServerComponent>(server, out var comp))
            return;

        comp.Code = string.Empty;
        comp.Interpreter = null;
        comp.Memory.Clear();
        comp.Log.Clear();

        if (user != null)
        {
            _adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium,
                $"{ToPrettyString(user.Value):user} cleared the NTSL script on {ToPrettyString(server):server}");
        }
    }

    public void SetEnabled(EntityUid server, bool enabled, EntityUid user)
    {
        var comp = EnsureComp<NtslServerComponent>(server);
        comp.Enabled = enabled;

        _adminLogger.Add(LogType.ItemConfigure, LogImpact.Medium, $"{ToPrettyString(user):user} turned NTSL scripts {(enabled ? "on" : "off")} on {ToPrettyString(server):server}");
    }

    /// <summary>
    /// Runs the scripts of every powered telecom server that operates this channel.
    /// Returns false if a script blocked the message.
    /// </summary>
    public bool ProcessRadio(EntityUid messageSource, EntityUid radioSource, LanguagePrototype language, string raw, ref string message, ref RadioChannelPrototype channel, ref string name, ref string? job, out NtslFilters filters)
    {
        filters = NtslFilters.None;
        if (_broadcasting)
        {
            filters = _broadcastFilters;
            return true;
        }

        var mapId = Transform(radioSource).MapID;
        _servers.Clear();

        var query = EntityQueryEnumerator<NtslServerComponent, EncryptionKeyHolderComponent, ApcPowerReceiverComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var script, out var keys, out var power, out var xform))
        {
            if (script.Enabled
                && script.Code.Length > 0
                && power.Powered
                && xform.MapID == mapId
                && keys.Channels.Contains(channel.ID))
            {
                _servers.Add((uid, script, keys));
            }
        }

        if (_servers.Count == 0)
            return true;

        // Several servers can carry the same channel
        _servers.Sort((a, b) => a.Owner.Id.CompareTo(b.Owner.Id));

        var contentChanged = false;
        foreach (var server in _servers)
        {
            var interpreter = GetInterpreter(server);
            if (interpreter == null || !interpreter.HasFunction(EntryFunction))
                continue;

            var signal = MakeSignal(message, channel.Frequency, name, job ?? string.Empty);
            signal.Set("channel", channel.LocalizedName);
            signal.Set("raw", raw);
            signal.Set("language", language.Name);

            object? result;
            try
            {
                _broadcastsThisRun = 0;
                _speaker = messageSource;
                _speakerLanguage = language;
                result = interpreter.Call(EntryFunction, signal);
            }
            catch (NtslException e)
            {
                AddLog(server.Comp1, FormatError(e));
                continue;
            }
            finally
            {
                _speaker = null;
                _speakerLanguage = null;
            }

            // Like on Yogstation, "return null;" blocks the message.
            if (result is not NtslList output)
            {
                if (!interpreter.Returned)
                    continue;

                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"NTSL script on {ToPrettyString(server.Owner):server} blocked a radio message from {name} via {ToPrettyString(radioSource):entity} on {channel.LocalizedName}: {message}");
                return false;
            }

            if (!NtslInterpreter.IsTrue(output.Get("pass")))
            {
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"NTSL script on {ToPrettyString(server.Owner):server} blocked a radio message from {name} via {ToPrettyString(radioSource):entity} on {channel.LocalizedName}: {message}");
                return false;
            }

            var content = NtslInterpreter.ToText(output.Get("content"));
            contentChanged |= content != message;
            message = CleanText(content, NtslInterpreter.MaxStringLength, "*beep*");
            name = CleanText(NtslInterpreter.ToText(output.Get("source")), MaxNameLength, name);
            job = CleanText(NtslInterpreter.ToText(output.Get("job")), MaxNameLength, string.Empty);
            filters = ParseFilters(output.Get("filters"));

            if (output.Get("freq") is double frequency
                && frequency != channel.Frequency
                && TryGetChannel(server.Comp2, frequency, out var rerouted))
            {
                channel = rerouted;
            }
        }

        // fix for custom script commands showing in radio chat
        if (!contentChanged && HideCommand(messageSource, raw, language) is { } rest)
        {
            if (rest.Length == 0)
                return false;

            message = rest;
        }

        return true;
    }

    /// <summary>
    /// What people nearby hear when someone says an NTSL command into their radio: only the text after it.
    /// Null when the message doesn't start with a cummand that a running script looks for.
    /// </summary>
    public string? HideCommand(EntityUid speaker, string raw, LanguagePrototype language)
    {
        raw = raw.Trim();
        if (!raw.StartsWith('/'))
            return null;

        var mapId = Transform(speaker).MapID;
        var command = string.Empty;
        var query = EntityQueryEnumerator<NtslServerComponent, ApcPowerReceiverComponent, TransformComponent>();
        while (query.MoveNext(out var script, out var power, out var xform))
        {
            if (!script.Enabled || !power.Powered || xform.MapID != mapId)
                continue;

            foreach (Match match in CommandRegex.Matches(script.Code))
            {
                var found = match.Groups[1].Value;
                if (found.Length > command.Length && raw.StartsWith(found, StringComparison.OrdinalIgnoreCase))
                    command = found;
            }
        }

        if (command.Length == 0)
            return null;

        var rest = FormattedMessage.EscapeText(raw[command.Length..].Trim());
        return rest.Length == 0 ? rest : _chat.TransformSpeech(speaker, rest, language);
    }

    public string FormatError(NtslException e)
    {
        return Loc.GetString("ntsl-error-at-line", ("line", e.Line), ("error", Loc.GetString(e.Message, e.Args)));
    }

    private NtslInterpreter? GetInterpreter(Entity<NtslServerComponent, EncryptionKeyHolderComponent> server)
    {
        var comp = server.Comp1;
        if (comp.Interpreter != null)
            return comp.Interpreter;

        try
        {
            var interpreter = NtslInterpreter.Compile(comp.Code);
            Bind(server.Owner, interpreter);
            _broadcastsThisRun = 0;
            interpreter.Run();
            comp.Interpreter = interpreter;
        }
        catch (NtslException e)
        {
            AddLog(comp, FormatError(e));
            comp.Enabled = false;
        }

        return comp.Interpreter;
    }

    private void Bind(EntityUid server, NtslInterpreter interpreter)
    {
        var channels = new NtslList();
        foreach (var proto in _prototype.EnumeratePrototypes<RadioChannelPrototype>())
        {
            channels.Set(proto.ID.ToLowerInvariant(), (double) proto.Frequency);
        }

        interpreter.Define("channels", channels);

        var filterTypes = new NtslList();
        filterTypes.Set("robot", "robot");
        filterTypes.Set("loud", "yell");
        filterTypes.Set("emphasis", "italics");
        filterTypes.Set("wacky", "sans");
        filterTypes.Set("commanding", "command_headset");
        interpreter.Define("filter_types", filterTypes);

        interpreter.Register("signal", Signal);
        interpreter.Register("broadcast", (args, line) => Broadcast(server, args, line));
        interpreter.Register("mem", (args, line) => Mem(server, args, line));
        interpreter.Register("clearmem", (_, _) => ClearMem(server));
        interpreter.Register("time", (_, _) => _ticker.RoundDuration().TotalSeconds);
        interpreter.Register("accent", (args, _) => Accent(NtslInterpreter.ToText(args.Count > 0 ? args[0] : null)));
    }

    // sig.raw has no accent, so a script that builds a message from it can put the speaker's accent back.
    private string Accent(string text)
    {
        if (_speaker is not { } speaker || _speakerLanguage == null || TerminatingOrDeleted(speaker))
            return text;

        return _chat.TransformSpeech(speaker, text, _speakerLanguage);
    }

    private double CommonFrequency => _prototype.Index(SharedChatSystem.CommonChannel).Frequency;

    private static NtslList MakeSignal(string content, double frequency, string source, string job)
    {
        var signal = new NtslList();
        signal.Set("content", content);
        signal.Set("freq", frequency);
        signal.Set("source", source);
        signal.Set("job", job);
        signal.Set("pass", 1.0);
        signal.Set("filters", new NtslList());
        return signal;
    }

    private object? Signal(List<object?> args, int line)
    {
        var frequency = args.Count > 1 && args[1] != null ? NtslInterpreter.ToNumber(args[1], line) : CommonFrequency;
        return MakeSignal(
            NtslInterpreter.ToText(args.Count > 0 ? args[0] : null),
            frequency,
            NtslInterpreter.ToText(args.Count > 2 ? args[2] : null),
            NtslInterpreter.ToText(args.Count > 3 ? args[3] : null));
    }

    private object? Broadcast(EntityUid server, List<object?> args, int line)
    {
        if (args.Count == 0)
            throw new NtslException("ntsl-error-not-a-signal", line);

        var signal = args[0] as NtslList ?? (NtslList) Signal(args, line)!;

        if (++_broadcastsThisRun > MaxBroadcastsPerRun)
            throw new NtslException("ntsl-error-too-many-broadcasts", line, ("max", MaxBroadcastsPerRun));

        if (!TryComp<EncryptionKeyHolderComponent>(server, out var keys))
            return 0.0;

        var frequency = signal.Get("freq") is double number ? number : CommonFrequency;
        if (!TryGetChannel(keys, frequency, out var channel))
            return 0.0;

        var content = CleanText(NtslInterpreter.ToText(signal.Get("content")), NtslInterpreter.MaxStringLength, "*beep*");
        var source = CleanText(NtslInterpreter.ToText(signal.Get("source")), MaxNameLength, Name(server));
        var filters = ParseFilters(signal.Get("filters"));
        // A broadcast speaks the language of the message being handled, so scripts can't translate.
        // Outside of a message it is the station's common language.
        var language = _speakerLanguage ?? _prototype.Index<LanguagePrototype>(SharedLanguageSystem.FallbackLanguagePrototype);
        _broadcasts.Enqueue(new PendingBroadcast(server, content, source, channel, filters, language));
        return 1.0;
    }

    private static NtslFilters ParseFilters(object? value)
    {
        var filters = NtslFilters.None;
        if (value is not NtslList list)
            return filters;

        foreach (var item in list.Items)
        {
            if (item is string text && FilterNames.TryGetValue(text.ToLowerInvariant(), out var filter))
                filters |= filter;
        }

        return filters;
    }

    /// <summary>
    /// Wraps already-escaped message text in the chat markup for the filters a script chose.
    /// </summary>
    public static string ApplyFilters(string content, NtslFilters filters)
    {
        if (filters == NtslFilters.None)
            return content;

        // Like SS13^^ a yell is bold.
        var bold = (filters & (NtslFilters.Loud | NtslFilters.Commanding)) != 0;
        var italic = (filters & NtslFilters.Italics) != 0;

        if ((filters & NtslFilters.Robot) != 0)
            content = $"[font=\"{(bold ? "MonospaceBold" : "Monospace")}\"]{content}[/font]";
        else if ((filters & NtslFilters.Sans) != 0)
            content = $"[font=\"{(bold ? "LDFComicSansBold" : "LDFComicSans")}\"]{content}[/font]";
        else if (bold && italic)
            content = $"[bolditalic]{content}[/bolditalic]";
        else if (bold)
            content = $"[bold]{content}[/bold]";
        else if (italic)
            content = $"[italic]{content}[/italic]";

        // Only the command voice grows, and only a little, like some species languages already do.
        if ((filters & NtslFilters.Commanding) != 0)
            content = $"[font size={CommandingFontSize}]{content}[/font]";

        return content;
    }

    private object? Mem(EntityUid server, List<object?> args, int line)
    {
        if (args.Count == 0 || args[0] is not string key)
            throw new NtslException("ntsl-error-bad-key", line);

        if (!TryComp<NtslServerComponent>(server, out var comp))
            return null;

        if (args.Count == 1)
            return comp.Memory.GetValueOrDefault(key);

        if (args[1] == null)
        {
            comp.Memory.Remove(key);
            return 1.0;
        }

        if (!comp.Memory.ContainsKey(key) && comp.Memory.Count >= MaxMemoryKeys)
            return 0.0;

        comp.Memory[key] = args[1];
        return 1.0;
    }

    private object? ClearMem(EntityUid server)
    {
        if (TryComp<NtslServerComponent>(server, out var comp))
            comp.Memory.Clear();

        return null;
    }

    private bool TryGetChannel(
        EncryptionKeyHolderComponent keys,
        double frequency,
        [NotNullWhen(true)] out RadioChannelPrototype? channel)
    {
        foreach (var id in keys.Channels)
        {
            if (_prototype.TryIndex<RadioChannelPrototype>(id, out var proto) && proto.Frequency == frequency)
            {
                channel = proto;
                return true;
            }
        }

        channel = null;
        return false;
    }

    private void AddLog(NtslServerComponent comp, string text)
    {
        comp.Log.Add($"[{_ticker.RoundDuration().ToString(@"hh\:mm\:ss")}] {text}");
        if (comp.Log.Count > MaxLogEntries)
            comp.Log.RemoveAt(0);
    }

    private static string CleanText(string text, int maxLength, string fallback)
    {
        var clean = text.Replace('\n', ' ').Replace('\r', ' ').Replace('\t', ' ').Trim();
        if (clean.Length > maxLength)
            clean = clean[..maxLength];

        return clean.Length == 0 ? fallback : clean;
    }

    internal IEnumerable<string> QueuedBroadcastLanguages => _broadcasts.Select(broadcast => broadcast.Language.ID);

    private readonly record struct PendingBroadcast(
        EntityUid Server,
        string Content,
        string Source,
        RadioChannelPrototype Channel,
        NtslFilters Filters,
        LanguagePrototype Language);
}

/// <summary>
/// Chat styles an NTSL script can put on a radio message through sig.filters.
/// </summary>
[Flags]
public enum NtslFilters : byte
{
    None = 0,
    Robot = 1 << 0,
    Loud = 1 << 1,
    Italics = 1 << 2,
    Sans = 1 << 3,
    Commanding = 1 << 4,
}
