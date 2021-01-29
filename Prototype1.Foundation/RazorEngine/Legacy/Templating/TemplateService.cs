﻿namespace RazorEngine.Templating
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using Compilation;
    using Configuration;
    using Parallel;
    using Text;

#if RAZOR4
    using System.Runtime.ExceptionServices;
#endif

    /// <summary>
    /// Defines a template service.
    /// </summary>
    [Obsolete("Use RazorEngineService instead")]
    public class TemplateService : CrossAppDomainObject, ITemplateService
    {
        #region Fields
        private readonly RazorEngineService _service;
        private bool disposed;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialises a new instance of <see cref="TemplateService"/>
        /// </summary>
        /// <param name="config">The template service configuration.</param>
        public TemplateService(ITemplateServiceConfiguration config)
        {
            Contract.Requires(config != null);
            _service = new RazorEngineService(config);
        }

        internal TemplateService(RazorEngineService service)
        {
            Contract.Requires(service != null);
            _service = service;
        }

        /// <summary>
        /// Initialises a new instance of <see cref="TemplateService"/>.
        /// </summary>
        public TemplateService()
            : this(new TemplateServiceConfiguration()) { }

        /// <summary>
        /// Initialises a new instance of <see cref="TemplateService"/>
        /// </summary>
        /// <param name="language">The code language.</param>
        /// <param name="encoding">the encoding.</param>
        internal TemplateService(Language language, Encoding encoding)
            : this(new TemplateServiceConfiguration() { Language = language, EncodedStringFactory = GetEncodedStringFactory(encoding) }) { }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the template service configuration.
        /// </summary>
        public ITemplateServiceConfiguration Configuration { get { return _service.Configuration; } }

        /// <summary>
        /// Gets the encoded string factory.
        /// </summary>
        public IEncodedStringFactory EncodedStringFactory { get { return _service.Configuration.EncodedStringFactory; } }
        #endregion

        #region Methods
        /// <summary>
        /// Adds a namespace that will be imported into the template.
        /// </summary>
        /// <param name="ns">The namespace to be imported.</param>
        public void AddNamespace(string ns)
        {
            Configuration.Namespaces.Add(ns);
        }
        private ITemplateKey GetKeyAndAdd(string template, string name = null)
        {
            if (name == null)
            {
                name = "dynamic_" + Guid.NewGuid().ToString();
            }
            var key = _service.GetKey(name);
            var source = new LoadedTemplateSource(template);
            _service.Configuration.TemplateManager.AddDynamic(key, source);
            return key;
        }

        /// <summary>
        /// Compiles the specified template.
        /// </summary>
        /// <param name="razorTemplate">The string template.</param>
        /// <param name="modelType">The model type.</param>
        /// <param name="cacheName">The name of the template type in the cache.</param>
        public void Compile(string razorTemplate, Type modelType, string cacheName)
        {
            Contract.Requires(razorTemplate != null);
            Contract.Requires(cacheName != null);
            _service.Compile(GetKeyAndAdd(razorTemplate, cacheName), CheckModelType(modelType));
        }

        /// <summary>
        /// Creates a new <see cref="ExecuteContext"/> for tracking templates.
        /// </summary>
        /// <param name="viewBag">This parameter is ignored, set the Viewbag with template.SetData(null, viewBag)</param>
        /// <returns>The execute context.</returns>
        public virtual ExecuteContext CreateExecuteContext(DynamicViewBag viewBag = null)
        {
            if (viewBag != null) throw new NotSupportedException("This kind of usage is no longer supported, please switch to the non-obsolete API.");
            return _service.Core.CreateExecuteContext();
        }

        /// <summary>
        /// Creates a new <see cref="InstanceContext"/> for creating template instances.
        /// </summary>
        /// <param name="templateType">The template type.</param>
        /// <returns>An instance of <see cref="InstanceContext"/>.</returns>
        [Pure]
        protected virtual InstanceContext CreateInstanceContext(Type templateType)
        {
            return _service.Core.CreateInstanceContext(templateType);
        }

        private Type CheckModelType(Type modelType)
        {
            if (modelType == null)
            {
                return null;
            }
            if (CompilerServicesUtility.IsAnonymousTypeRecursive(modelType))
            {
                //throw new ArgumentException("Cannot use anonymous type as model type.");
                modelType = null;
            }
            if (modelType != null && CompilerServicesUtility.IsDynamicType(modelType))
            {
                modelType = null;
            }
            return modelType;
        }

        private Tuple<object, Type> CheckModel(object model)
        {
            if (model == null)
            {
                return Tuple.Create((object)null, (Type)null);
            }
            Type modelType = (model == null) ? typeof(object) : model.GetType();

            bool isAnon = CompilerServicesUtility.IsAnonymousTypeRecursive(modelType);
            if (isAnon ||
                CompilerServicesUtility.IsDynamicType(modelType))
            {
                modelType = null;
                if (isAnon || Configuration.AllowMissingPropertiesOnDynamic)
                {
                    model = RazorDynamicObject.Create(model, Configuration.AllowMissingPropertiesOnDynamic);
                }
            }
            return Tuple.Create(model, modelType);
        }

        /// <summary>
        /// Creates an instance of <see cref="ITemplate"/> from the specified string template.
        /// </summary>
        /// <param name="razorTemplate">
        /// The string template.
        /// If templateType is not NULL, this parameter may be NULL (unused).
        /// </param>
        /// <param name="templateType">
        /// The template type or NULL if the template type should be dynamically created.
        /// If razorTemplate is not NULL, this parameter may be NULL (unused).
        /// </param>
        /// <param name="model">The model instance or NULL if no model exists.</param>
        /// <returns>An instance of <see cref="ITemplate"/>.</returns>
        [Pure]
        public virtual ITemplate CreateTemplate(string razorTemplate, Type templateType, object model)
        {
            var key = GetKeyAndAdd(razorTemplate);
            ICompiledTemplate compiledTemplate;
            var check = CheckModel(model);
            Type modelType = check.Item2;
            model = check.Item1;
            
            if (templateType == null) {
                compiledTemplate = _service.CompileAndCacheInternal(key, modelType);
            }
            else
            {
                var source = _service.Core.Resolve(key);
                compiledTemplate = new CompiledTemplate(new CompilationData(null, null), key, source, templateType, modelType);
	        }
            return _service.Core.CreateTemplate(compiledTemplate, model, null);
        }

        /// <summary>
        /// Creates a set of templates from the specified string templates and models.
        /// </summary>
        /// <param name="razorTemplates">
        /// The set of templates to create or NULL if all template types are already created (see templateTypes).
        /// If this parameter is NULL, the the templateTypes parameter may not be NULL. 
        /// Individual elements in this set may be NULL if the corresponding templateTypes[i] is not NULL (precompiled template).
        /// </param>
        /// <param name="models">
        /// The set of models or NULL if no models exist for all templates.
        /// Individual elements in this set may be NULL if no model exists for a specific template.
        /// </param>
        /// <param name="templateTypes">
        /// The set of template types or NULL to dynamically create template types for each template.
        /// If this parameter is NULL, the the razorTemplates parameter may not be NULL. 
        /// Individual elements in this set may be NULL to dynamically create the template if the corresponding razorTemplates[i] is not NULL (dynamically compile template).
        /// </param>
        /// <param name="parallel">Flag to determine whether to create templates in parallel.</param>
        /// <returns>The enumerable set of template instances.</returns>
        [Pure]
        public virtual IEnumerable<ITemplate> CreateTemplates(IEnumerable<string> razorTemplates, IEnumerable<Type> templateTypes, IEnumerable<object> models, bool parallel = false)
        {
            if ((razorTemplates == null) && (templateTypes == null))
                throw new ArgumentException("The razorTemplates and templateTypes parameters may not both be null.");

            List<string> razorTemplateList = (razorTemplates == null) ? null : razorTemplates.ToList();
            List<object> modelList = (models == null) ? null : models.ToList();
            List<Type> templateTypeList = (templateTypes == null) ? null : templateTypes.ToList();

            int templateCount = (razorTemplateList != null) ? razorTemplateList.Count() : templateTypeList.Count();

            if ((razorTemplateList != null) && (razorTemplateList.Count != templateTypeList.Count))
                throw new ArgumentException("Expected the same number of templateTypes as razorTemplates to be processed.");

            if ((templateTypeList != null) && (templateTypeList.Count != templateCount))
                throw new ArgumentException("Expected the same number of templateTypes as the number of templates to be processed.");

            if ((modelList != null) && (modelList.Count != templateCount))
                throw new ArgumentException("Expected the same number of models as the number of templates to be processed.");

            if ((razorTemplateList != null) && (templateTypeList != null))
            {
                for (int i = 0; (i < templateCount); i++)
                {
                    if (razorTemplateList == null)
                    {
                        if (templateTypeList[i] == null)
                            throw new ArgumentException("Expected non-NULL value in templateTypes[" + i.ToString() + "].");
                    }
                    else if (templateTypeList == null)
                    {
                        if (razorTemplateList[i] == null)
                            throw new ArgumentException("Expected non-NULL value in either razorTemplates[" + i.ToString() + "].");
                    }
                    else
                    {
                        if ((razorTemplateList[i] == null) && (templateTypeList[i] == null))
                            throw new ArgumentException("Expected non-NULL value in either razorTemplates[" + i.ToString() + "] or templateTypes[" + i.ToString() + "].");
                    }
                }
            }

            if (parallel)
            {
                if (razorTemplateList != null)
                    return GetParallelQueryPlan<string>()
                        .CreateQuery(razorTemplates)
                        .Select((rt, i) => CreateTemplate(
                            rt,
                            (templateTypeList == null) ? null : templateTypeList[i],
                            (modelList == null) ? null : modelList[i]));
                else
                    return GetParallelQueryPlan<Type>()
                        .CreateQuery(templateTypes)
                        .Select((tt, i) => CreateTemplate(
                            (razorTemplateList == null) ? null : razorTemplateList[i],
                            tt,
                            (modelList == null) ? null : modelList[i]));
            }

            if (razorTemplateList != null)
                return razorTemplates.Select((rt, i) => CreateTemplate(
                    rt,
                    (templateTypeList == null) ? null : templateTypeList[i],
                    (modelList == null) ? null : modelList[i]));
            else
                return templateTypeList.Select((tt, i) => CreateTemplate(
                    (razorTemplateList == null) ? null : razorTemplateList[i],
                    tt,
                    (modelList == null) ? null : modelList[i]));
        }

        /// <summary>
        /// Creates a <see cref="Type"/> that can be used to instantiate an instance of a template.
        /// </summary>
        /// <param name="razorTemplate">The string template.</param>
        /// <param name="modelType">The model type or NULL if no model exists.</param>
        /// <returns>An instance of <see cref="Type"/>.</returns>
        [Pure]
        public virtual Type CreateTemplateType(string razorTemplate, Type modelType)
        {
            var result = _service.Core.CreateTemplateType(new LoadedTemplateSource(razorTemplate), CheckModelType(modelType));
            result.Item2.DeleteAll();
            return result.Item1;
        }

        /// <summary>
        /// Creates a set of template types from the specfied string templates.
        /// </summary>
        /// <param name="razorTemplates">The set of templates to create <see cref="Type"/> instances for.</param>
        /// <param name="modelTypes">
        /// The set of model types or NULL if no models exist for all templates.
        /// Individual elements in this set may be NULL if no model exists for a specific template.
        /// </param>
        /// <param name="parallel">Flag to determine whether to create template types in parallel.</param>
        /// <returns>The set of <see cref="Type"/> instances.</returns>
        [Pure]
        public virtual IEnumerable<Type> CreateTemplateTypes(IEnumerable<string> razorTemplates, IEnumerable<Type> modelTypes, bool parallel = false)
        {
            Contract.Requires(razorTemplates != null);

            List<Type> modelTypeList = (modelTypes == null) ? null : modelTypes.ToList();

            if (parallel)
                return GetParallelQueryPlan<string>()
                    .CreateQuery(razorTemplates)
                    .Select((t, i) => CreateTemplateType(t,
                        (modelTypeList == null) ? null : modelTypeList[i]));

            return razorTemplates.Select((t, i) => CreateTemplateType(t,
                (modelTypeList == null) ? null : modelTypeList[i]));
        }

        /// <summary>
        /// Releases managed resources used by this instance.
        /// </summary>
        /// <param name="disposing">Are we explicitly disposing of this instance?</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                _service.Dispose();
                disposed = true;
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Gets an instance of a <see cref="IEncodedStringFactory"/> for a known encoding.
        /// </summary>
        /// <param name="encoding">The encoding to get a factory for.</param>
        /// <returns>An instance of <see cref="IEncodedStringFactory"/></returns>
        internal static IEncodedStringFactory GetEncodedStringFactory(Encoding encoding)
        {
            switch (encoding)
            {
                case Encoding.Html:
                    return new HtmlEncodedStringFactory();

                case Encoding.Raw:
                    return new RawStringFactory();

                default:
                    throw new ArgumentException("Unsupported encoding: " + encoding);
            }
        }

        /// <summary>
        /// Gets a parellel query plan used to configure a parallel query.
        /// </summary>
        /// <typeparam name="T">The query item type.</typeparam>
        /// <returns>An instance of <see cref="IParallelQueryPlan{T}"/>.</returns>
        protected virtual IParallelQueryPlan<T> GetParallelQueryPlan<T>()
        {
            return new DefaultParallelQueryPlan<T>();
        }

        /// <summary>
        /// Gets an instance of the template using the cached compiled type, or compiles the template type
        /// if it does not exist in the cache.
        /// </summary>
        /// <param name="razorTemplate">The string template.</param>
        /// <param name="model">The model instance or NULL if there is no model for this template.</param>
        /// <param name="cacheName">The name of the template type in the cache.</param>
        /// <returns>An instance of <see cref="ITemplate{T}"/>.</returns>
        public virtual ITemplate GetTemplate(string razorTemplate, object model, string cacheName)
        {
            if (razorTemplate == null)
                throw new ArgumentNullException("razorTemplate");
            var key = GetKeyAndAdd(razorTemplate, cacheName);

            var check = CheckModel(model);
            Type modelType = check.Item2;
            model = check.Item1;

            return _service.GetTemplate(key, modelType, model, null);
            // return this.GetTemplate<object>(razorTemplate, model, cacheName);
        }

        /// <summary>
        /// Gets an instance of the template using the cached compiled type, or compiles the template type
        /// if it does not exist in the cache.
        /// </summary>
        /// <typeparam name="T">Type of the model</typeparam>
        /// <param name="razorTemplate">The string template.</param>
        /// <param name="model">The model instance or NULL if there is no model for this template.</param>
        /// <param name="cacheName">The name of the template type in the cache.</param>
        /// <returns>An instance of <see cref="ITemplate{T}"/>.</returns>
        private ITemplate GetTemplate<T>(string razorTemplate, object model, string cacheName)
        {
            if (razorTemplate == null)
                throw new ArgumentNullException("razorTemplate");
            var key = GetKeyAndAdd(razorTemplate, cacheName);
            var check = CheckModel(model);
            model = check.Item1;
            return _service.GetTemplate(key, typeof(T), model, null);
        }

        /// <summary>
        /// Gets the set of template instances for the specified string templates. Cached templates will be considered
        /// and if they do not exist, new types will be created and instantiated.
        /// </summary>
        /// <param name="razorTemplates">The set of templates to create.</param>
        /// <param name="models">
        /// The set of models or NULL if no models exist for all templates.
        /// Individual elements in this set may be NULL if no model exists for a specific template.
        /// </param>
        /// <param name="cacheNames">The set of cache names.</param>
        /// <param name="parallel">Flag to determine whether to get the templates in parallel.</param>
        /// <returns>The set of <see cref="ITemplate"/> instances.</returns>
        public virtual IEnumerable<ITemplate> GetTemplates(IEnumerable<string> razorTemplates, IEnumerable<object> models, IEnumerable<string> cacheNames, bool parallel = false)
        {
            Contract.Requires(razorTemplates != null);
            Contract.Requires(cacheNames != null);
            Contract.Requires(razorTemplates.Count() == models.Count(),
                              "Expected same number of models as string templates to be processed.");
            Contract.Requires(razorTemplates.Count() == cacheNames.Count(),
                              "Expected same number of cache names as string templates to be processed.");

            var modelList = (models == null) ? null : models.ToList();
            var cacheNameList = cacheNames.ToList();

            if (parallel)
                return GetParallelQueryPlan<string>()
                    .CreateQuery(razorTemplates)
                    .Select((t, i) => GetTemplate(t,
                        (modelList == null) ? null : modelList[i],
                        cacheNameList[i]));

            return razorTemplates.Select((t, i) => GetTemplate(t,
                (modelList == null) ? null : modelList[i],
                cacheNameList[i]));
        }


        /// <summary>
        /// Parses and returns the result of the specified string template.
        /// </summary>
        /// <param name="razorTemplate">The string template.</param>
        /// <param name="model">The model instance or NULL if no model exists.</param>
        /// <param name="viewBag">The ViewBag contents or NULL for an initially empty ViewBag.</param>
        /// <param name="cacheName">The name of the template type in the cache or NULL if no caching is desired.</param>
        /// <returns>The string result of the template.</returns>
        [Pure]
        public virtual string Parse(string razorTemplate, object model, DynamicViewBag viewBag, string cacheName)
        {
            ITemplate instance;

            if (cacheName == null)
                instance = CreateTemplate(razorTemplate, null, model);
            else
                instance = GetTemplate(razorTemplate, model, cacheName);

            return Run(instance, viewBag);
        }


        /// <summary>
        /// Parses and returns the result of the specified string template.
        /// </summary>
        /// <param name="razorTemplate">The string template.</param>
        /// <param name="model">The model instance or NULL if no model exists.</param>
        /// <param name="viewBag">The ViewBag contents or NULL for an initially empty ViewBag.</param>
        /// <param name="cacheName">The name of the template type in the cache or NULL if no caching is desired.</param>
        /// <returns>The string result of the template.</returns>
        [Pure]
        public virtual string Parse<T>(string razorTemplate, object model, DynamicViewBag viewBag, string cacheName)
        {
            using(var writer = new System.IO.StringWriter())
            {
                var key = GetKeyAndAdd(razorTemplate, cacheName);
                var check = CheckModel(model);
                model = check.Item1;
                _service.RunCompile(GetKeyAndAdd(razorTemplate, cacheName), writer, typeof(T), model, viewBag);
                return writer.ToString();
	        }
        }

        /// <summary>
        /// Parses the specified set of templates.
        /// </summary>
        /// <param name="razorTemplates">The set of string templates to parse.</param>
        /// <param name="models">
        /// The set of models or NULL if no models exist for all templates.
        /// Individual elements in this set may be NULL if no model exists for a specific template.
        /// </param>
        /// <param name="viewBags">
        /// The set of initial ViewBag contents or NULL for an initially empty ViewBag for all templates.
        /// Individual elements in this set may be NULL if an initially empty ViewBag is desired for a specific template.
        /// </param>
        /// <param name="cacheNames">
        /// The set of cache names or NULL if no caching is desired for all templates.
        /// Individual elements in this set may be NULL if caching is not desired for specific template.
        /// </param>
        /// <param name="parallel">Flag to determine whether parsing in parallel.</param>
        /// <returns>The set of parsed template results.</returns>
        public virtual IEnumerable<string> ParseMany(IEnumerable<string> razorTemplates, IEnumerable<object> models, IEnumerable<DynamicViewBag> viewBags, IEnumerable<string> cacheNames, bool parallel)
        {
            if (razorTemplates == null)
                throw new ArgumentException("Expected at least one entry in razorTemplates collection.");

            if ((models != null) && (razorTemplates.Count() != models.Count()))
                throw new ArgumentException("Expected same number of models as string templates to be processed.");

            if ((viewBags != null) && (razorTemplates.Count() != viewBags.Count()))
                throw new ArgumentException("Expected same number of viewBags as string templates to be processed.");

            if ((cacheNames != null) && (razorTemplates.Count() != cacheNames.Count()))
                throw new ArgumentException("Expected same number of cacheNames as string templates to be processed.");

            var modelList = (models == null) ? null : models.ToList();
            var viewBagList = (viewBags == null) ? null : viewBags.ToList();
            var cacheNameList = (cacheNames == null) ? null : cacheNames.ToList();

            //
            //  :Matt:
            //      What is up with the GetParallelQueryPlan() here?
            //          o   Everywhere else in the code we simply return the ParallelQueryPlan
            //              (this implements IEnumerablt<T>
            //          o   However, when I remove the ToArray() in the following code,
            //              the unit tests that call ParseMany() start failing, complaining
            //              about serialization (give it a try)
            //          o   Should all GetParallelQueryPlan() results call ToList()
            //              to force Linq to execute the results?
            //          o   If we do not call ToArray() below, then the Parse() 
            //              method never gets called.
            //          o   Something is wrong or inconsistent here.  :)
            //
            if (parallel)
            {
                return GetParallelQueryPlan<string>()
                    .CreateQuery(razorTemplates)
                    .Select((t, i) => Parse(t,
                        (modelList == null) ? null : modelList[i],
                        (viewBagList == null) ? null : viewBagList[i],
                        (cacheNameList == null) ? null : cacheNameList[i]))
                    .ToArray();
            }
            return razorTemplates.Select((t, i) => Parse(t,
                        (modelList == null) ? null : modelList[i],
                        (viewBagList == null) ? null : viewBagList[i],
                        (cacheNameList == null) ? null : cacheNameList[i]))
                    .ToArray();
        }

        /// <summary>
        /// Returns whether or not a template by the specified name has been created already.
        /// </summary>
        /// <param name="cacheName">The name of the template type in cache.</param>
        /// <returns>Whether or not the template has been created.</returns>
        public bool HasTemplate(string cacheName)
        {
            throw new InvalidOperationException("This member is no longer supported!");
            //ICompiledTemplate source;
            //return _service.Configuration.CachingProvider.TryRetrieveTemplate(
            //    _service.Core.GetKey(cacheName), typeof(object), out source);
        }

        /// <summary>
        /// Remove a template by the specified name from the cache.
        /// </summary>
        /// <param name="cacheName">The name of the template type in cache.</param>
        /// <returns>Whether or not the template has been removed.</returns>
        public bool RemoveTemplate(string cacheName)
        {
            throw new InvalidOperationException("This member is no longer supported!");
            //CachedTemplateItem item;
            //return _cache.TryRemove(cacheName, out item);
        }
        
        /// <summary>
        /// Resolves the template with the specified name.
        /// </summary>
        /// <param name="cacheName">The name of the template type in cache.</param>
        /// <param name="model">The model or NULL if there is no model for the template.</param>
        /// <returns>The resolved template.</returns>
        public virtual ITemplate Resolve(string cacheName, object model)
        {
            var check = CheckModel(model);
            var modelType = check.Item2;
            model = check.Item1;
            return _service.GetTemplate(
                _service.GetKey(cacheName), modelType, model, null);
        }

        /// <summary>
        /// Runs the template with the specified cacheName.
        /// </summary>
        /// <param name="cacheName">The name of the template in cache.  The template must be in cache.</param>
        /// <param name="model">The model for the template or NULL if there is no model.</param>
        /// <param name="viewBag">The initial ViewBag contents NULL for an empty ViewBag.</param>
        /// <returns>The string result of the template.</returns>
        public string Run(string cacheName, object model, DynamicViewBag viewBag)
        {
            if (string.IsNullOrWhiteSpace(cacheName))
                throw new ArgumentException("'cacheName' is a required parameter.");
            using(var writer = new System.IO.StringWriter())
            {
                var check = CheckModel(model);
                var modelType = check.Item2;
                model = check.Item1;
                _service.Run(_service.GetKey(cacheName), writer, modelType, model, viewBag);
                return writer.ToString();
	        }
        }

        /// <summary>
        /// Runs the specified template and returns the result.
        /// </summary>
        /// <param name="template">The template to run.</param>
        /// <param name="viewBag">The ViewBag contents or NULL for an initially empty ViewBag.</param>
        /// <returns>The string result of the template.</returns>
        public string Run(ITemplate template, DynamicViewBag viewBag)
        {
            if (template == null)
                throw new ArgumentNullException("template");

            using (var writer = new System.IO.StringWriter())
            {
                template.SetData(null, viewBag);
#if RAZOR4
                try
                {
                    template.Run(CreateExecuteContext(), writer).Wait();
                }
                catch (AggregateException ex)
                {
                    ExceptionDispatchInfo.Capture(ex.Flatten().InnerExceptions.First()).Throw();
                }
#else
                template.Run(CreateExecuteContext(), writer);
#endif
                return writer.ToString();
            }
        }

        #endregion
    }
}