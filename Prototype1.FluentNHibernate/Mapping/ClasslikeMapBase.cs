using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FluentNHibernate.Mapping.Providers;
using FluentNHibernate.Utils;

namespace FluentNHibernate.Mapping
{
    public abstract class ClasslikeMapBase<T>
    {
        readonly MappingProviderStore providers;

        protected ClasslikeMapBase(MappingProviderStore providers)
        {
            this.providers = providers;
        }

        /// <summary>
        /// Called when a member is mapped by a builder method.
        /// </summary>
        /// <param name="member">Member being mapped.</param>
        internal virtual void OnMemberMapped(Member member)
        {}

        /// <summary>
        /// Create a property mapping.
        /// </summary>
        /// <param name="memberExpression">Property to map</param>
        /// <example>
        /// Map(x => x.Name);
        /// </example>
        public PropertyPart Map(Expression<Func<T, object>> memberExpression)
        {
            return Map(memberExpression, null);
        }

        /// <summary>
        /// Create a property mapping.
        /// </summary>
        /// <param name="memberExpression">Property to map</param>
        /// <param name="columnName">Property column name</param>
        /// <example>
        /// Map(x => x.Name, "person_name");
        /// </example>
        public PropertyPart Map(Expression<Func<T, object>> memberExpression, string columnName)
        {
            return Map(memberExpression.ToMember(), columnName);
        }

        PropertyPart Map(Member member, string columnName)
        {
            //PROTOTYPE1: ADDED
            var previousMapping = providers.Properties.OfType<PropertyPart>().FirstOrDefault(p => p.Property.Name == member.Name);
            if (previousMapping != null)
                return previousMapping;
            //PROTOTYPE1: ADDED

            OnMemberMapped(member);

            var propertyMap = new PropertyPart(member, typeof(T));

            //PROTOTYPE1: ADDED
            if (this.UseBackingField(member.Name))
                propertyMap.Access.CamelCaseField(Prefix.Underscore);
            //PROTOTYPE1: ADDED

            if (!string.IsNullOrEmpty(columnName))
                propertyMap.Column(columnName);

            providers.Properties.Add(propertyMap);

            return propertyMap;
        }

        /// <summary>
        /// Create a reference to another entity. In database terms, this is a many-to-one
        /// relationship.
        /// </summary>
        /// <typeparam name="TOther">Other entity</typeparam>
        /// <param name="memberExpression">Property on the current entity</param>
        /// <example>
        /// References(x => x.Company);
        /// </example>
        public ManyToOnePart<TOther> References<TOther>(Expression<Func<T, TOther>> memberExpression)
        {
            return References(memberExpression, null);
        }

        /// <summary>
        /// Create a reference to another entity. In database terms, this is a many-to-one
        /// relationship.
        /// </summary>
        /// <typeparam name="TOther">Other entity</typeparam>
        /// <param name="memberExpression">Property on the current entity</param>
        /// <param name="columnName">Column name</param>
        /// <example>
        /// References(x => x.Company, "company_id");
        /// </example>
        public ManyToOnePart<TOther> References<TOther>(Expression<Func<T, TOther>> memberExpression, string columnName)
        {
            return References<TOther>(memberExpression.ToMember(), columnName);
        }

        /// <summary>
        /// Create a reference to another entity. In database terms, this is a many-to-one
        /// relationship.
        /// </summary>
        /// <typeparam name="TOther">Other entity</typeparam>
        /// <param name="memberExpression">Property on the current entity</param>
        /// <example>
        /// References(x => x.Company, "company_id");
        /// </example>
        public ManyToOnePart<TOther> References<TOther>(Expression<Func<T, object>> memberExpression)
        {
            return References<TOther>(memberExpression, null);
        }

        /// <summary>
        /// Create a reference to another entity. In database terms, this is a many-to-one
        /// relationship.
        /// </summary>
        /// <typeparam name="TOther">Other entity</typeparam>
        /// <param name="memberExpression">Property on the current entity</param>
        /// <param name="columnName">Column name</param>
        /// <example>
        /// References(x => x.Company, "company_id");
        /// </example>
        public ManyToOnePart<TOther> References<TOther>(Expression<Func<T, object>> memberExpression, string columnName)
        {
            return References<TOther>(memberExpression.ToMember(), columnName);
        }

        ManyToOnePart<TOther> References<TOther>(Member member, string columnName)
        {
            //PROTOTYPE1: ADDED
            var part = providers.References.OfType<ManyToOnePart<TOther>>().FirstOrDefault(m => m.Property.Name == member.Name);
            if (part == null)
            {
                //PROTOTYPE1: ADDED

                OnMemberMapped(member);

                part = new ManyToOnePart<TOther>(EntityType, member);

                //PROTOTYPE1: ADDED
                if (this.UseBackingField(member.Name))
                    part.Access.CamelCaseField(Prefix.Underscore);
                //PROTOTYPE1: ADDED

                if (columnName != null)
                    part.Column(columnName);

                providers.References.Add(part);

                //PROTOTYPE1: ADDED
            }
            //PROTOTYPE1: ADDED

            return part;
        }

        /// <summary>
        /// Create a reference to any other entity. This is an "any" polymorphic relationship.
        /// </summary>
        /// <typeparam name="TOther">Other entity to reference</typeparam>
        /// <param name="memberExpression">Property</param>
        public AnyPart<TOther> ReferencesAny<TOther>(Expression<Func<T, TOther>> memberExpression)
        {
            return ReferencesAny<TOther>(memberExpression.ToMember());
        }

        AnyPart<TOther> ReferencesAny<TOther>(Member member)
        {
            //PROTOTYPE1: ADDED
            var part = providers.Anys.OfType<AnyPart<TOther>>().FirstOrDefault(p => p.Property.Name == member.Name);
            if (part == null)
            //PROTOTYPE1: ADDED

            OnMemberMapped(member);

            part = new AnyPart<TOther>(typeof(T), member);

            //PROTOTYPE1: ADDED
            if (this.UseBackingField(member.Name))
                part.Access.CamelCaseField(Prefix.Underscore);
            //PROTOTYPE1: ADDED

            providers.Anys.Add(part);

            return part;
        }

        /// <summary>
        /// Create a reference to another entity based exclusively on the primary-key values.
        /// This is sometimes called a one-to-one relationship, in database terms. Generally
        /// you should use <see cref="References{TOther}(System.Linq.Expressions.Expression{System.Func{T,object}})"/>
        /// whenever possible.
        /// </summary>
        /// <typeparam name="TOther">Other entity</typeparam>
        /// <param name="memberExpression">Property</param>
        /// <example>
        /// HasOne(x => x.ExtendedInfo);
        /// </example>
        public OneToOnePart<TOther> HasOne<TOther>(Expression<Func<T, Object>> memberExpression)
        {
            return HasOne<TOther>(memberExpression.ToMember());
        }

        /// <summary>
        /// Create a reference to another entity based exclusively on the primary-key values.
        /// This is sometimes called a one-to-one relationship, in database terms. Generally
        /// you should use <see cref="References{TOther}(System.Linq.Expressions.Expression{System.Func{T,object}})"/>
        /// whenever possible.
        /// </summary>
        /// <typeparam name="TOther">Other entity</typeparam>
        /// <param name="memberExpression">Property</param>
        /// <example>
        /// HasOne(x => x.ExtendedInfo);
        /// </example>
        public OneToOnePart<TOther> HasOne<TOther>(Expression<Func<T, TOther>> memberExpression)
        {
            return HasOne<TOther>(memberExpression.ToMember());
        }

        OneToOnePart<TOther> HasOne<TOther>(Member member)
        {
            //PROTOTYPE1: ADDED
            var part = providers.OneToOnes.OfType<OneToOnePart<TOther>>().FirstOrDefault(p => p.Property.Name == member.Name);
            if (part == null)
            {
                //PROTOTYPE1: ADDED

                OnMemberMapped(member);

                part = new OneToOnePart<TOther>(EntityType, member);

                //PROTOTYPE1: ADDED
                if (this.UseBackingField(member.Name))
                    part.Access.CamelCaseField(Prefix.Underscore);
                //PROTOTYPE1: ADDED

                providers.OneToOnes.Add(part);

                //PROTOTYPE1: ADDED
            }
            //PROTOTYPE1: ADDED
            return part;
        }

        /// <summary>
        /// Create a dynamic component mapping. This is a dictionary that represents
        /// a limited number of columns in the database.
        /// </summary>
        /// <param name="memberExpression">Property containing component</param>
        /// <param name="dynamicComponentAction">Component setup action</param>
        /// <example>
        /// DynamicComponent(x => x.Data, comp =>
        /// {
        ///   comp.Map(x => (int)x["age"]);
        /// });
        /// </example>
        public DynamicComponentPart<IDictionary> DynamicComponent(Expression<Func<T, IDictionary>> memberExpression, Action<DynamicComponentPart<IDictionary>> dynamicComponentAction)
        {
            return DynamicComponent(memberExpression.ToMember(), dynamicComponentAction);
        }

        DynamicComponentPart<IDictionary> DynamicComponent(Member member, Action<DynamicComponentPart<IDictionary>> dynamicComponentAction)
        {
            //PROTOTYPE1: ADDED
            var part = providers.Components.OfType<DynamicComponentPart<IDictionary>>().FirstOrDefault(p => p.Property.Name == member.Name);
            if (part == null)
            {
                //PROTOTYPE1: ADDED

                OnMemberMapped(member);

                part = new DynamicComponentPart<IDictionary>(typeof (T), member);

                //PROTOTYPE1: ADDED
                if (this.UseBackingField(member.Name))
                    part.Access.CamelCaseField(Prefix.Underscore);
                //PROTOTYPE1: ADDED

                dynamicComponentAction(part);

                //PROTOTYPE1: ADDED
            }
            //PROTOTYPE1: ADDED

            providers.Components.Add(part);

            return part;
        }

        /// <summary>
        /// Creates a component reference. This is a place-holder for a component that is defined externally with a
        /// <see cref="ComponentMap{T}"/>; the mapping defined in said <see cref="ComponentMap{T}"/> will be merged
        /// with any options you specify from this call.
        /// </summary>
        /// <typeparam name="TComponent">Component type</typeparam>
        /// <param name="member">Property exposing the component</param>
        /// <returns>Component reference builder</returns>
        public virtual ReferenceComponentPart<TComponent> Component<TComponent>(Expression<Func<T, TComponent>> member)
        {
            return Component<TComponent>(member.ToMember());
        }

        ReferenceComponentPart<TComponent> Component<TComponent>(Member member)
        {
            OnMemberMapped(member);

            var part = new ReferenceComponentPart<TComponent>(member, typeof(T));

            providers.Components.Add(part);

            return part;
        }

        /// <summary>
        /// Maps a component
        /// </summary>
        /// <typeparam name="TComponent">Type of component</typeparam>
        /// <param name="expression">Component property</param>
        /// <param name="action">Component mapping</param>
        /// <example>
        /// Component(x => x.Address, comp =>
        /// {
        ///   comp.Map(x => x.Street);
        ///   comp.Map(x => x.City);
        /// });
        /// </example>
        public ComponentPart<TComponent> Component<TComponent>(Expression<Func<T, TComponent>> expression, Action<ComponentPart<TComponent>> action)
        {
            return Component(expression.ToMember(), action);
        }

        /// <summary>
        /// Maps a component
        /// </summary>
        /// <typeparam name="TComponent">Type of component</typeparam>
        /// <param name="expression">Component property</param>
        /// <param name="action">Component mapping</param>
        /// <example>
        /// Component(x => x.Address, comp =>
        /// {
        ///   comp.Map(x => x.Street);
        ///   comp.Map(x => x.City);
        /// });
        /// </example>
        public ComponentPart<TComponent> Component<TComponent>(Expression<Func<T, object>> expression, Action<ComponentPart<TComponent>> action)
        {
            return Component(expression.ToMember(), action);
        }

        ComponentPart<TComponent> Component<TComponent>(Member member, Action<ComponentPart<TComponent>> action)
        {
            //PROTOTYPE1: ADDED
            var part = providers.Components.OfType<ComponentPart<TComponent>>().FirstOrDefault(p => p.Property.Name == member.Name);
            if (part == null)
            {
                //PROTOTYPE1: ADDED

                OnMemberMapped(member);

                part = new ComponentPart<TComponent>(typeof (T), member);

                //PROTOTYPE1: ADDED
                if (this.UseBackingField(member.Name))
                    part.Access.CamelCaseField(Prefix.Underscore);
                //PROTOTYPE1: ADDED

                if (action != null) action(part);

                providers.Components.Add(part);

                //PROTOTYPE1: ADDED
            }
            //PROTOTYPE1: ADDED

            return part;
        }

        /// <summary>
        /// Allows the user to add a custom component mapping to the class mapping.        
        /// Note: not a fluent method.
        /// </summary>
        /// <remarks>
        /// In some cases, our users need a way to add an instance of their own implementation of IComponentMappingProvider.
        /// For an example of where this might be necessary, see: http://codebetter.com/blogs/jeremy.miller/archive/2010/02/16/our-extension-properties-story.aspx
        /// </remarks>        
        public void Component(IComponentMappingProvider componentProvider)
        {
            providers.Components.Add(componentProvider);
        }

        private OneToManyPart<TChild> MapHasMany<TChild, TReturn>(Expression<Func<T, TReturn>> expression)
        {
            return HasMany<TChild>(expression.ToMember());
        }

        OneToManyPart<TChild> HasMany<TChild>(Member member)
        {
            //PROTOTYPE1: ADDED
            var part = providers.Collections.OfType<OneToManyPart<TChild>>().FirstOrDefault(p => p.Property.Name == member.Name);
            if (part == null)
            {
                //PROTOTYPE1: ADDED

                OnMemberMapped(member);

                part = new OneToManyPart<TChild>(EntityType, member);

                //PROTOTYPE1: ADDED
                if (this.UseBackingField(member.Name))
                    part.Access.CamelCaseField(Prefix.Underscore);
                //PROTOTYPE1: ADDED

                providers.Collections.Add(part);

                //PROTOTYPE1: ADDED
            }
            //PROTOTYPE1: ADDED

            return part;
        }

        /// <summary>
        /// Maps a collection of entities as a one-to-many
        /// </summary>
        /// <typeparam name="TChild">Child entity type</typeparam>
        /// <param name="memberExpression">Collection property</param>
        /// <example>
        /// HasMany(x => x.Locations);
        /// </example>
        public OneToManyPart<TChild> HasMany<TChild>(Expression<Func<T, IEnumerable<TChild>>> memberExpression)
        {
            return MapHasMany<TChild, IEnumerable<TChild>>(memberExpression);
        }

        public OneToManyPart<TChild> HasMany<TKey, TChild>(Expression<Func<T, IDictionary<TKey, TChild>>> memberExpression)
        {
            return MapHasMany<TChild, IDictionary<TKey, TChild>>(memberExpression);
        }

        /// <summary>
        /// Maps a collection of entities as a one-to-many
        /// </summary>
        /// <typeparam name="TChild">Child entity type</typeparam>
        /// <param name="memberExpression">Collection property</param>
        /// <example>
        /// HasMany(x => x.Locations);
        /// </example>
        public OneToManyPart<TChild> HasMany<TChild>(Expression<Func<T, object>> memberExpression)
        {
            return MapHasMany<TChild, object>(memberExpression);
        }

        private ManyToManyPart<TChild> MapHasManyToMany<TChild, TReturn>(Expression<Func<T, TReturn>> expression)
        {
            return HasManyToMany<TChild>(expression.ToMember());
        }

        ManyToManyPart<TChild> HasManyToMany<TChild>(Member member)
        {
            //PROTOTYPE1: ADDED
            var part = providers.Collections.OfType<ManyToManyPart<TChild>>().FirstOrDefault(p => p.Property.Name == member.Name);
            if (part == null)
            {
                //PROTOTYPE1: ADDED

                OnMemberMapped(member);

                part = new ManyToManyPart<TChild>(EntityType, member);

                //PROTOTYPE1: ADDED
                if (this.UseBackingField(member.Name))
                    part.Access.CamelCaseField(Prefix.Underscore);
                //PROTOTYPE1: ADDED

                providers.Collections.Add(part);

                //PROTOTYPE1: ADDED
            }
            //PROTOTYPE1: ADDED

            return part;
        }

        /// <summary>
        /// Maps a collection of entities as a many-to-many
        /// </summary>
        /// <typeparam name="TChild">Child entity type</typeparam>
        /// <param name="memberExpression">Collection property</param>
        /// <example>
        /// HasManyToMany(x => x.Locations);
        /// </example>
        public ManyToManyPart<TChild> HasManyToMany<TChild>(Expression<Func<T, IEnumerable<TChild>>> memberExpression)
        {
            return MapHasManyToMany<TChild, IEnumerable<TChild>>(memberExpression);
        }

        /// <summary>
        /// Maps a collection of entities as a many-to-many
        /// </summary>
        /// <typeparam name="TChild">Child entity type</typeparam>
        /// <param name="memberExpression">Collection property</param>
        /// <example>
        /// HasManyToMany(x => x.Locations);
        /// </example>
        public ManyToManyPart<TChild> HasManyToMany<TChild>(Expression<Func<T, object>> memberExpression)
        {
            return MapHasManyToMany<TChild, object>(memberExpression);
        }

        /// <summary>
        /// Specify an insert stored procedure
        /// </summary>
        /// <param name="innerText">Stored procedure call</param>
        public StoredProcedurePart SqlInsert(string innerText)
        {
            return StoredProcedure("sql-insert", innerText);
        }

        /// <summary>
        /// Specify an update stored procedure
        /// </summary>
        /// <param name="innerText">Stored procedure call</param>
        public StoredProcedurePart SqlUpdate(string innerText)
        {
            return StoredProcedure("sql-update", innerText);
        }

        /// <summary>
        /// Specify an delete stored procedure
        /// </summary>
        /// <param name="innerText">Stored procedure call</param>
        public StoredProcedurePart SqlDelete(string innerText)
        {
            return StoredProcedure("sql-delete", innerText);
        }

        /// <summary>
        /// Specify an delete all stored procedure
        /// </summary>
        /// <param name="innerText">Stored procedure call</param>
        public StoredProcedurePart SqlDeleteAll(string innerText)
        {
            return StoredProcedure("sql-delete-all", innerText);
        }

        protected StoredProcedurePart StoredProcedure(string element, string innerText)
        {
            var part = new StoredProcedurePart(element, innerText);
            providers.StoredProcedures.Add(part);
            return part;
        }

        internal IEnumerable<IPropertyMappingProvider> Properties
		{
            get { return providers.Properties; }
		}

        internal IEnumerable<IComponentMappingProvider> Components
		{
            get { return providers.Components; }
		}

        internal Type EntityType
        {
            get { return typeof(T); }
        }

        //PROTOTYPE1: ADDED
        protected bool UseBackingField(string propertyName)
        {
            var fieldName = "_" + Char.ToLower(propertyName[0]) + propertyName.Substring(1);
            return (typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance) != null);
        }
        //PROTOTYPE1: ADDED
    }
}
