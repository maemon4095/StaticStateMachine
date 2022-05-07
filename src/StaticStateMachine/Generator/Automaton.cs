using Microsoft.CodeAnalysis;

namespace StaticStateMachine.Generator;

class Automaton
{
    static int Expand(ref StateEntry[] entries)
    {
        var len = entries.Length;
        Array.Resize(ref entries, Math.Max(16, len * 2));
        var span = entries.AsSpan(len);
        for (var i = 0; i < span.Length; ++i)
        {
            ref var entry = ref span[i];
            entry.Next = i + len + 1;
        }
        entries[^1].Next = -1;
        return len;
    }

    public Automaton()
    {
        this.entries = Array.Empty<StateEntry>();
        this.freeState = Expand(ref this.entries);
        this.InitialState = this.AddState();
    }
    StateEntry[] entries;
    int freeState;

    public int InitialState { get; }
    public string? InitialStateAssociated { get; set; }

    public int AddState()
    {
        var free = this.freeState;
        if (free < 0) free = Expand(ref this.entries);
        ref var entry = ref this.entries[free];
        this.freeState = entry.Next;
        entry = (-1,  new());
        return free;
    }
    public void RemoveState(int state)
    {
        ref var entry = ref this.entries[state];
        entry.Connections = null!;
        entry.Next = this.freeState;
        this.freeState = state;
    }

    public void Associate(int state, Arg chara, string? associated)
    {
        var dst = this.entries[state].Connections.TryGetValue(chara, out var tuple) ? tuple.Dst : -1;
        this.Connect(state, chara, dst, associated);
    }
    public string? GetAssociated(int state, Arg chara)
    {
        if (this.entries[state].Connections.TryGetValue(chara, out var pair)) return pair.Associated;
        return null;
    }

    public void Connect(int src, Arg chara, int dst)
    {
        var associated = this.entries[src].Connections.TryGetValue(chara, out var tuple) ? tuple.Associated : null;
        this.Connect(src, chara, dst, associated);
    }
    public void Connect(int src, Arg chara, int dst, string? associated)
    {
        var entries = this.entries;
        var connections = entries[src].Connections;

        if (connections.ContainsKey(chara))
            connections[chara] = (dst, associated);
        else
            connections.Add(chara, (dst, associated));
    }
    public void Disconnect(int src, Arg chara)
    {
        this.entries[src].Connections.Remove(chara);
    }
    public IEnumerable<(Arg Arg, int Dst, string? Associated)> GetConnections(int state) => this.entries[state].Connections.Select(pair => (pair.Key, pair.Value.Dst, pair.Value.Associated));
    public (int, string?) Transition(int state, Arg chara)
    {
        var connections = this.entries[state].Connections;
        if (connections.TryGetValue(chara, out var result)) return result;
        if (connections.TryGetValue(Arg.WildCard, out result)) return result;
        return (-1, null);
    }

    public bool IsTerminal(int state) => !this.entries[state].Connections.Any();
    public bool IsTerminal(int state, Arg chara) => !this.entries[state].Connections.ContainsKey(chara);

    public IEnumerable<(int State, IEnumerable<(Arg Arg, int Dst, string? Associated)> Connections)> EnumerateConnections()
    {
        return core(this.InitialState);

        IEnumerable<(int, IEnumerable<(Arg, int, string?)>)> core(int initial)
        {
            var entries = this.entries;
            var connections = entries[initial].Connections;
            yield return (initial, connections.Select(pair => (pair.Key, pair.Value.Dst, pair.Value.Associated)));
            foreach (var pair in connections)
            {
                var dst = pair.Value.Dst;
                if (dst < 0) continue;
                foreach (var tuple in core(dst))
                    yield return tuple;
            }
        }
    }
    public IEnumerable<(int State, Arg Arg, int Dst, string? Associated)> EnumerateFlattenConnections()
    {
        return this.EnumerateConnections().SelectMany(pair => pair.Connections.Select(c => (pair.State, c.Arg, c.Dst, c.Associated)));
    }

    record struct StateEntry(int Next, Dictionary<Arg, (int Dst, string? Associated)> Connections)
    {
        public static implicit operator StateEntry((int Next, Dictionary<Arg, (int Dst, string? Associated)> Connections) tuple) => new(tuple.Next, tuple.Connections);
    }

    public readonly struct Arg : IEquatable<Arg>
    {
        public static bool operator ==(Arg left, Arg right) => left.Equals(right);
        public static bool operator !=(Arg left, Arg right) => !(left == right);
        public static Arg WildCard => default;

        public Arg(ITypeSymbol type, string? literal)
        {
            this.Type = type;
            this.Literal = literal;
        }

        public bool IsWildCard => this.Literal is null;

        public ITypeSymbol Type { get; }
        public string? Literal { get; }

        public override bool Equals(object? obj) => obj is Arg arg && this.Equals(arg);
        public bool Equals(Arg other) => this.Literal == other.Literal;
        public override int GetHashCode() => HashCode.Combine(this.Literal);
    }
}
