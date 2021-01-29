using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Metadata;
using System.Web.Http.Validation;
using FluentValidation;

namespace Prototype1.Foundation.Validation
{
    public class BodyModelValidator : IBodyModelValidator
    {
        private static readonly Type _validatorBase = typeof(AbstractValidator<>);
        private readonly IDependencyResolver _dependencyResolver;
        private readonly ConcurrentDictionary<Type, Type> _validatorTypeMap = new ConcurrentDictionary<Type, Type>();
        private readonly HashSet<Type> _typesWithoutValidators = new HashSet<Type>();

        public BodyModelValidator(IDependencyResolver dependencyResolver)
        {
            _dependencyResolver = dependencyResolver;
            ValidatorOptions.ResourceProviderType = typeof(ValidationResourceProvider);
        }

        public bool Validate(object model, Type type, ModelMetadataProvider metadataProvider, HttpActionContext actionContext,
            string keyPrefix)
        {
            if (type == null)
                return true;

            if (_typesWithoutValidators.Contains(type))
                return true;

            var validatorType = _validatorTypeMap.GetOrAdd(type, t => _validatorBase.MakeGenericType(t));
            var validator = _dependencyResolver.GetService(validatorType) as IValidator;

            if (validator == null)
            {
                _typesWithoutValidators.Add(type);
                return true;
            }

            var result = validator.Validate(model);
            if (result.IsValid)
                return true;

            foreach (var validationResult in result.Errors)
            {
                actionContext.ModelState.AddModelError(validationResult.PropertyName, validationResult.ErrorMessage);
            }
            return false;
        }
    }
}