using Microsoft.CodeAnalysis;

namespace StaticStateMachine.Generator;

class Automaton
{
    static int Expand(ref Entry[] entries)
    {
        var len = entries.Length;
        Array.Resize(ref entries, Math.Max(16, len * 2));
        var span = entries.AsSpan(len);
        for (var i = 0; i < span.Length; ++i)
        {
            ref var entry = ref span[i];
            entry.Next = i + len + 1;
        }
        entries[entries.Length - 1].Next = -1;
        return len;
    }

    public Automaton()
    {
        this.entries = Array.Empty<Entry>();
        this.freeState = Expand(ref this.entries);
        this.InitialState = this.AddState();
    }

    Entry[] entries;
    int freeState;

    public int InitialState { get;  }

    public int AddState()
    {
        var free = this.freeState;
        if (free < 0) free = Expand(ref this.entries);
        ref var entry = ref this.entries[free];
        this.freeState = entry.Next;
        entry = (-1, new());
        return free;
    }
    public void RemoveState(int state)
    {
        ref var entry = ref this.entries[state];
        entry.Connections = null!;
        entry.Next = this.freeState;
        this.freeState = state;
    }

    public void Associate(int state, string? chara, string associated)
    {
        var dst = this.entries[state].Connections.TryGetValue(chara, out var tuple) ? tuple.Dst : -1;
        this.Connect(state, chara, dst, associated);
    }
    public void Connect(int src, string? chara, int dst)
    {
        var associated = this.entries[src].Connections.TryGetValue(chara, out var tuple) ? tuple.Associated : null;
        this.Connect(src, chara, dst, associated);
    }
    public void Connect(int src, string? chara, int dst, string? associated)
    {
        var entries = this.entries;
        var connections = entries[src].Connections;

        if (connections.ContainsKey(chara))
            connections[chara] = (dst, associated);
        else
            connections.Add(chara, (dst, associated));
    }
    public void Disconnect(int src, string? chara)
    {
        this.entries[src].Connections.Remove(chara);
    }
    public (int, string?) Transition(int state, string? chara)
    {
        var connections = this.entries[state].Connections;
        if (connections.TryGetValue(chara, out var result)) return result;
        if (connections.TryGetValue(Arg.WildCard, out result)) return result;
        return (-1, null);
    }

    public IEnumerable<(int, IEnumerable<(string? Chara, int Dst, string? Associated)>)> EnumerateConnections()
    {
        return Body(this.InitialState);

        IEnumerable<(int, IEnumerable<(string? Chara, int Dst, string? Associated)>)> Body(int initial)
        {
            var connections = this.entries[initial].Connections;
            yield return (initial, connections.Select(p => (p.Key.Chara, p.Value.Dst, p.Value.Associated)));
            foreach (var pair in connections)
            {
                var dst = pair.Value.Dst;
                if (dst < 0) continue;
                foreach (var tuple in Body(dst))
                    yield return tuple;
            }
        }
    }

    record struct Entry(int Next, Dictionary<Arg, (int Dst, string? Associated)> Connections)
    {
        public static implicit operator (int Next, Dictionary<Arg, (int Dst, string? Associated)>)(Entry value) => (value.Next, value.Connections);
        public static implicit operator Entry((int Next, Dictionary<Arg, (int Dst, string? Associated)> Connections) value) => new(value.Next, value.Connections);
    }

    readonly record struct Arg(string? Chara) : IEquatable<Arg>
    {
        public static Arg WildCard => new(null);
        public static implicit operator Arg(string? chara) => new Arg(chara);

        public bool IsWildCard => this.Chara is null;
    }
}
