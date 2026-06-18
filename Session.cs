using Microsoft.AspNetCore.Http;
using System.Dynamic;
using System.Reflection;
using UISupportGeneric.Resources;
using UISupportGeneric.UI;

namespace UISupportBlazor
{
    /// <summary>
    /// Represents a user session with associated values and timeout functionality
    /// </summary>
    public class Session
    {

        private static IHttpContextAccessor? _httpContextAccessor;
        /// <summary>
        /// Configures the session with the provided IHttpContextAccessor
        /// </summary>
        /// <param name="httpContextAccessor"></param>
        public static void Configure(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        /// <summary>
        /// Checks if the session is configured
        /// </summary>
        public static bool IsConfigured => _httpContextAccessor != null;

        /// <summary>
        /// Gets the current HttpContext
        /// </summary>
        /// <returns>Current HttpContext</returns>
        public static HttpContext? GetCurrentHttpContext()
        {
            return _httpContextAccessor?.HttpContext;
        }

        /// <summary>
        /// Static class managing all active sessions and providing session-related operations
        /// </summary>
        static public class Sessions
        {
            /// <summary>
            /// Event triggered when a new session is created
            /// </summary>
            static public event Action<Session>? OnNewSession;

            /// <summary>
            /// Event triggered when a session expires
            /// </summary>
            static public event Action<Session>? OnSessionExpired;

            /// <summary>
            /// Obtain session object of current user
            /// </summary>
            /// <param name="context">Current HttpContext</param>
            /// <returns>Session object of current user</returns>
            static public Session? GetSession(HttpContext? context = null)
            {
                context ??= GetCurrentHttpContext();
                if (context == null)
                {
                    return null;
                }
                return RefreshSessionTimeOut(context);
            }

            // Internal method to trigger session expiration event
            internal static void Expired(Session session) => OnSessionExpired?.Invoke(session);

            // Default session timeout duration (30 minutes)
            public static TimeSpan TimeOut = TimeSpan.FromMinutes(30);

            // Dictionary storing all active sessions by their GUID
            internal static Dictionary<Guid, Session> ActiveSessions = [];

            /// <summary>
            /// Refreshes the timeout for a session identified by GUID
            /// Creates new session if it doesn't exist
            /// </summary>
            /// <param name="id">Session GUID</param>
            /// <returns>The existing or newly created session</returns>
            internal static Session RefreshSessionTimeOut(Guid id)
            {
                if (ActiveSessions.TryGetValue(id, out var session))
                {
                    session.Refresh();
                }
                else
                {
                    session = new Session(id);
                    OnNewSession?.Invoke(session);
                }
                return session;
            }

            /// <summary>
            /// Refreshes the timeout for a session identified by HttpContext
            /// Creates new session if it doesn't exist
            /// </summary>
            /// <param name="context">Current HttpContext</param>
            /// <returns>The existing or newly created session</returns>
            static internal Session RefreshSessionTimeOut(HttpContext context)
            {
                Guid sessionId;
                var id = (string)context.Items["s"];
                id ??= GetCookie(context, "s");
                if (id != null)
                {
                    sessionId = new Guid(id);
                }
                else
                {
                    sessionId = Guid.NewGuid();
                    context.Items.Add("s", sessionId.ToString());
                    SetCookie(context, "s", sessionId.ToString());
                }
                return RefreshSessionTimeOut(sessionId);
            }

            /// <summary>
            /// Sets a cookie with secure options
            /// </summary>
            /// <param name="context">Current HttpContext</param>
            /// <param name="key">Cookie name</param>
            /// <param name="value">Cookie value</param>
            static public bool SetCookie(HttpContext context, string key, string value)
            {
                if (!context.Response.HasStarted)
                {
                    var options = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        MaxAge = TimeOut,
                    };
                    context.Response.Cookies.Delete(key);
                    context.Response.Cookies.Append(key, value, options);
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Retrieves a cookie value by key
            /// </summary>
            /// <param name="context">Current HttpContext</param>
            /// <param name="key">Cookie name</param>
            /// <returns>Cookie value if found, null otherwise</returns>
            static public string? GetCookie(HttpContext context, string key)
            {
                if (context.Request.Cookies.TryGetValue(key, out var value))
                {
                    return value;
                }
                return null;
            }

            /// <summary>
            /// Sets a value in the session's dictionary
            /// </summary>
            /// <param name="id">Session GUID</param>
            /// <param name="name">Value name</param>
            /// <param name="value">Value to store</param>
            public static void SetValue(Guid id, string name, object value)
            {
                if (ActiveSessions.TryGetValue(id, out var session))
                {
                    lock (session.Values)
                    {
                        session.Values[name] = value;
                    }
                }
            }

            /// <summary>
            /// Retrieves a value from the session's dictionary
            /// </summary>
            /// <param name="id">Session GUID</param>
            /// <param name="name">Value name</param>
            /// <returns>Stored value if found, null otherwise</returns>
            public static object? GetValue(Guid id, string name)
            {
                if (ActiveSessions.TryGetValue(id, out var session))
                {
                    lock (session.Values)
                    {
                        if (session.Values.TryGetValue(name, out var value))
                            return value;
                    }
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the current session for the provided HttpContext
        /// </summary>
        /// <param name="httpContext">Current HttpContext</param>
        /// <returns>Session object or null if the session cannot be obtained</returns>
        static public Session? Current(HttpContext? httpContext = null)
        {
            if (httpContext == null)
            {
                httpContext = GetCurrentHttpContext();
                if (httpContext == null)
                {
                    return null;
                }
            }
            var session = Sessions.GetSession(httpContext);
            return session;
        }

        /// <summary>
        /// Gets panel-related values from the session
        /// Can return panels collection, single panel, or panel element value
        /// </summary>
        /// <param name="className">The name of the associated class that creates an instance of a panel</param>
        /// <param name="elementName">The name of a panel element</param>
        /// <returns>Set of panels / a panel / the value of an element in a panel</returns>
        public object? GetPanelValue(string? className = null, string? elementName = null)
        {
            object panels;
            if (Values.TryGetValue(nameof(panels), out panels))
            {
                if (panels is Dictionary<string, object> panelsDictionary)
                {
                    if (className == null)
                        return panels;
                    if (panelsDictionary.TryGetValue(className, out object? panel))
                    {
                        if (elementName == null)
                            return panel;
                        GetElementValue(panel, elementName);
                    }
                }
            }
            else if (panels != null)
            {
                if (className == null)
                    return panels;
                var panel = GetElementValue(panels, className);
                if (elementName == null)
                    return panel;
                GetElementValue(panel, elementName);
            }
            return null;
        }

        /// <summary>
        /// Gets a panel of a specific type from the session
        /// </summary>
        /// <param name="type">Specific type</param>
        /// <returns>Instance of the class specified with type, for the current session</returns>
        public object? GetPanel(Type type)
        {
            object panels;
            if (Values.TryGetValue(nameof(panels), out panels))
            {
                if (panels is Dictionary<string, object> panelsDictionary)
                {
                    foreach (var panel in panelsDictionary)
                    {
                        if (panel.Value.GetType() == type)
                            return panel.Value;
                    }
                }
            }
            return null;
        }

        private void OnSessionExpired()
        {
            object? classInfoList;
            if (Values.TryGetValue(nameof(classInfoList), out classInfoList))
            {
                if (classInfoList is List<ClassInfo> panelsList)
                {
                    foreach (var panel in panelsList)
                    {
                        if (!panel.IsStatic)
                            panel.IsExpired = true; // Mark the panel as expired
                    }
                }
            }
        }

        /// <summary>
        /// Gets ClassInfo by its ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns>ClassInfo object</returns>
        public ClassInfo? GetClassInfoById(Guid id)
        {
            object? classInfoList;
            if (Values.TryGetValue(nameof(classInfoList), out classInfoList))
            {
                if (classInfoList is List<ClassInfo> panelsList)
                {
                    foreach (var panel in panelsList)
                    {
                        if (panel.Id == id)
                            return panel;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Gets a property or field value from an object by name using reflection
        /// </summary>
        /// <param name="obj">Object to inspect</param>
        /// <param name="elementName">Property or field name</param>
        /// <returns>Value of the property or field if found, null otherwise</returns>
        private static object? GetElementValue(object obj, string elementName)
        {
            PropertyInfo proprieta = obj.GetType().GetProperty(elementName);
            if (proprieta != null)
            {
                return proprieta.GetValue(obj);
            }
            FieldInfo campo = obj.GetType().GetField(elementName);
            if (campo != null)
            {
                return campo.GetValue(obj);
            }
            return null;
        }

        /// <summary>
        /// Creates a new session with the specified GUID
        /// </summary>
        /// <param name="id">Session GUID</param>
        public Session(Guid id)
        {
            Id = id;
            ExpireSessionTimer = new Timer(OnExpired, null, Sessions.TimeOut, Sessions.TimeOut);
            lock (Sessions.ActiveSessions)
            {
                Sessions.ActiveSessions[Id] = this;
            }
        }

        private readonly Guid Id;

        /// <summary>
        /// Refreshes the session timeout
        /// </summary>
        public void Refresh()
        {
            ExpireSessionTimer.Change(Sessions.TimeOut, Sessions.TimeOut);
        }

        Timer ExpireSessionTimer;

        /// <summary>
        /// Handles session expiration by cleaning up resources and removing from active sessions
        /// </summary>
        private void OnExpired(object? obj)
        {
            Sessions.Expired(this);
            lock (Sessions.ActiveSessions)
            {
                if (Sessions.ActiveSessions.TryGetValue(Id, out Session? value))
                {
                    value.OnSessionExpired();
                    Sessions.ActiveSessions.Remove(Id);
                }
            }
            ExpireSessionTimer.Change(Timeout.Infinite, Timeout.Infinite);
            ExpireSessionTimer.Dispose();
        }

        /// <summary>
        /// Dictionary storing session values
        /// </summary>
        public Dictionary<string, object> Values = [];


        /// <summary>
        /// Used to get and set values associated with the current session.
        /// </summary>
        public dynamic Val
        {
            get
            {
                return new DynamicWrapper(Values);
            }
            set
            {
                if (value is ExpandoObject expando)
                {
                    var expandoDict = (IDictionary<string, object>)expando;
                    foreach (var kvp in expandoDict)
                    {
                        Values[kvp.Key] = kvp.Value;
                    }
                }
                else
                {
                    throw new InvalidOperationException("Val setter accepts only ExpandoObject.");
                }
            }
        }


        private class DynamicWrapper : DynamicObject
        {
            private readonly IDictionary<string, object> _dictionary;

            public DynamicWrapper(IDictionary<string, object> dictionary)
            {
                _dictionary = dictionary;
            }

            public override bool TryGetMember(GetMemberBinder binder, out object? result)
            {
                lock (_dictionary)
                {
                    if (_dictionary.TryGetValue(binder.Name, out result))
                    {
                        return true;
                    }
                }
                result = null;
                return true;
            }

            public override bool TrySetMember(SetMemberBinder binder, object? value)
            {
                lock (_dictionary)
                {
                    _dictionary[binder.Name] = value!;
                }
                return true;
            }
        }
    }

    /// <summary>
    /// Middleware for managing sessions in the ASP.NET Core pipeline
    /// </summary>
    public class SessionMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes the session middleware
        /// </summary>
        /// <param name="next">Next middleware in the pipeline</param>
        public SessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invokes the middleware to refresh session timeout and continue pipeline execution
        /// </summary>
        /// <param name="context">Current HttpContext</param>
        public async Task InvokeAsync(Microsoft.AspNetCore.Http.HttpContext context)
        {
            Session.Sessions.RefreshSessionTimeOut(context);
            await _next(context);
        }
    }
}