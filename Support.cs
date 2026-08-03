using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using UISupportGeneric.UI;

namespace UISupportBlazor
{
    /// <summary>
    /// GUI support features
    /// </summary>
    public static class Support
    {
        /// <summary>
        /// Maximum size, in bytes, accepted for a single attachment uploaded through the
        /// multi-file picker (InputType.file). Defaults to 100 MB per file.
        /// Larger files are rejected by the browser upload and reported as an error.
        /// </summary>
        public static long MaxAttachmentSizeBytes = 100L * 1024 * 1024;

        /// <summary>
        /// Get all ClassInfos instantiated as members of the type passed as a parameter.
        /// This function also creates a browsing session for the current user(if it hasn't already been created), and creates an object of the type passed as a parameter and assigns it to the user session.
        /// </summary>
        /// <param name="httpContext">Current httpContext</param>
        /// <param name="panelsType">The type of object that represents the navigation menu, that is, an object with properties, each of which represents a panel associated with a menu item.</param>
        /// <returns>The list of ClassInfos that represent the panels that the application must have. This list corresponds to the properties of the type passed as a parameter.</returns>
        public static List<ClassInfo>? GetAllClassInfo(HttpContext httpContext, Type panelsType)
        {
            List<ClassInfo>? classInfoList = null;
            if (httpContext != null)
            {
                lock (httpContext)
                {

                    var session = Session.Sessions.GetSession(httpContext);
                    object panels;
                    if (session.Values.TryGetValue(nameof(panels), out panels))
                    {
                        if (session.Values.TryGetValue(nameof(classInfoList), out object? classInfoListObj))
                        {
                            return (List<ClassInfo>)classInfoListObj;
                        }
                    }
                    else
                    {
                        try
                        {
                            panels = Activator.CreateInstance(panelsType);
                        }
                        catch (Exception)
                        {
                            throw new Exception("Cannot process classes without parameterless constructor (invalid type value)");
                        }
                        session.Values.Add(nameof(panels), panels);
                        classInfoList = [];
                        session.Values.Add(nameof(classInfoList), classInfoList);
                        var classInfo = UISupportGeneric.Util.GetClassInfo(panels);
                        foreach (var element in classInfo.Elements)
                        {
                            if (element is ClassInfo info)
                                classInfoList.Add(info);
                        }
                    }
                }
            }
            return classInfoList;
        }

        /// <summary>
        /// Get all ClassInfos instantiated as members of the type passed as a parameter.
        /// This function also creates a browsing session for the current user(if it hasn't already been created), and creates an object of the type passed as a parameter and assigns it to the user session.
        /// </summary>
        /// <param name="httpContextAccessor">http Context Accessor object</param>
        /// <param name="atNamespace">The namespace where all the classes representing the panels are defined. The default value indicates the namespace associated with the Panels directory</param>
        /// <returns></returns>
        public static List<ClassInfo>? GetAllClassInfo(IHttpContextAccessor httpContextAccessor, string? atNamespace = default)
        {
            if (!Session.IsConfigured)
                Session.Configure(httpContextAccessor);
            return GetAllClassInfo(httpContext: null, atNamespace);
        }

        /// <summary>
        /// Get all ClassInfos instantiated as members of the type passed as a parameter.
        /// This function also creates a browsing session for the current user(if it hasn't already been created), and creates an object of the type passed as a parameter and assigns it to the user session.
        /// </summary>
        /// <param name="httpContext">Current httpContext</param>
        /// <param name="atNamespace">The namespace where all the classes representing the panels are defined. The default value indicates the namespace associated with the Panels directory</param>
        /// <returns>The list of ClassInfos that represent the panels that the application must have. This list corresponds to the properties of the type passed as a parameter.</returns>
        public static List<ClassInfo>? GetAllClassInfo(HttpContext? httpContext = null, string? atNamespace = default)
        {
            httpContext ??= Session.GetCurrentHttpContext();
            List<ClassInfo>? classInfoList = null;
            if (httpContext != null)
            {
                Assembly? assembly = null;
                if (atNamespace != null)
                    assembly = UISupportGeneric.Util.NamespaceToAssembly(atNamespace);
                if (assembly == null)
                {
                    var currentAssembly = MethodBase.GetCurrentMethod().DeclaringType.Assembly;
                    var stackTrace = new StackTrace();
                    for (int i = 0; i < stackTrace.FrameCount; i++)
                    {
                        var method = stackTrace.GetFrame(i)?.GetMethod();
                        var declaringType = method?.DeclaringType;
                        assembly = declaringType?.Assembly;
                        if (assembly != null && assembly != currentAssembly)
                        {
                            break;
                        }
                    }
                }
                if (atNamespace == null)
                {
                    var fullNamespace = UISupportGeneric.Util.GetNamespace(assembly);
                    if (fullNamespace == null)
                        return null;
                    var rootNamespace = fullNamespace.Split('.')[0];
                    atNamespace = rootNamespace + ".Panels";
                }
                lock (httpContext)
                {
                    var classes = assembly.GetTypes().Where(t => t.Namespace == atNamespace && t.IsClass && t.IsPublic);
                    var session = Session.Sessions.GetSession(httpContext);
                    object panels;
                    if (session != null)
                    {
                        lock (session)
                        {
                            if (!session.Values.TryGetValue(nameof(panels), out panels))
                            {

                                var panelsDictionary = new Dictionary<string, object>();
                                classInfoList = [];

                                foreach (var type in classes)
                                {
                                    if (UISupportGeneric.Util.IsStaticClass(type))
                                    {
                                        panelsDictionary.Add(type.Name, type);
                                        var classInfo = UISupportGeneric.Util.GetClassInfo(type);
                                        classInfoList.Add(classInfo);
                                    }
                                    else if (type.GetConstructor(Type.EmptyTypes) != null)
                                    {
                                        var panel = Activator.CreateInstance(type);
                                        if (panel != null)
                                        {
                                            panelsDictionary.Add(type.Name, panel);
                                            var classInfo = UISupportGeneric.Util.GetClassInfo(panel);
                                            classInfoList.Add(classInfo);
                                        }
                                    }
                                }
                                panels = panelsDictionary;
                                session.Values.Add(nameof(panels), panelsDictionary); // The panel dictionary contains the type, for static classes, and the class instance for non-static classes.
                                session.Values.Add(nameof(classInfoList), classInfoList);
                            }
                            else if (session.Values.TryGetValue(nameof(classInfoList), out object? classInfoListObj))
                            {
                                return (List<ClassInfo>)classInfoListObj;
                            }
                        }
                    }
                }
            }
            return classInfoList;
        }
    }
}
