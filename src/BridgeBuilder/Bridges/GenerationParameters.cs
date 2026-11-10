using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

// Decoded immutable inputs belong to one construction, never to the loaded mod.
internal static class GenerationParameters
{
    private static readonly System.Threading.AsyncLocal<Session?> Current = new();
    private static Session? _current { get => Current.Value; set => Current.Value=value; }
    internal sealed class Session
    {
        internal readonly Dictionary<object, object> Values = new();
        internal int Leases;
        internal bool Closed;
    }
    internal sealed class Lease : IDisposable
    {
        private Session? _session;
        private readonly Session? _previous;
        internal Lease(Session session)
        {
            lock(session.Values)
            {
                if(session.Closed)throw new ObjectDisposedException(nameof(GenerationParameters));
                session.Leases++;
            }
            _previous=_current;_current=session;_session=session;
        }
        public void Dispose()
        {
            var session=_session;if(session==null)return;
            if(!ReferenceEquals(_current,session))throw new InvalidOperationException("Parameter scopes must end in stack order.");
            _current=_previous;_session=null;
            lock(session.Values)
            {
                if(--session.Leases==0){session.Values.Clear();session.Closed=true;}
            }
        }
    }
    internal static Lease Begin()=>new(new Session());
    internal static Session? Capture()=>_current;
    internal static Lease? Attach(Session? session)=>session==null?null:new Lease(session);
    internal static T Get<T>(object key,Func<T> create) where T:notnull
    {
        // Catalogue/metadata consumers have a bounded temporary lifetime too.
        if(_current==null){using var scope=Begin();return Get(key,create);}
        var session=_current;
        lock(session.Values)
        {
            if(session.Closed)throw new ObjectDisposedException(nameof(GenerationParameters));
            if(session.Values.TryGetValue(key,out var value))return (T)value;
            T result=create();session.Values.Add(key,result);return result;
        }
    }
}
