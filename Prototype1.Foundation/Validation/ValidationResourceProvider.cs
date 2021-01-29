using System.Linq;

namespace Prototype1.Foundation.Validation
{
    public class ValidationResourceProvider
    {
        private const string SEPARATOR = ",";
        private static string PropertyError(string error, params string[] values)
        {
            return "{PropertyName}_" + error + (values.Any() ? SEPARATOR + string.Join(SEPARATOR, values) : string.Empty);
        }

        public static string email_error
        {
            get { return PropertyError("EmailError"); }
        }

        public static string equal_error
        {
            get { return PropertyError("EqualError", "{ComparisonValue}"); }
        }

        public static string exact_length_error
        {
            get { return PropertyError("ExactLengthError", "{MaxLength}", "{TotalLength}"); }
        }

        public static string exclusivebetween_error
        {
            get { return PropertyError("ExclusiveBetweenError", "{From}", "{To}", "{Value}"); }
        }

        public static string greaterthan_error
        {
            get { return PropertyError("GreaterThanError", "{ComparisonValue}"); }
        }

        public static string greaterthanorequal_error
        {
            get { return PropertyError("GreaterThanOrEqualError", "{ComparisonValue}"); }
        }

        public static string inclusivebetween_error
        {
            get { return PropertyError(PropertyError("InclusiveBetweenError", "{From}", "{To}", "{Value}")); }
        }

        public static string length_error
        {
            get { return PropertyError(PropertyError("LengthError", "{MinLength}", "{MaxLength}", "{TotalLength}")); }
        }

        public static string lessthan_error
        {
            get { return PropertyError("LessThanError", "{ComparisonValue}"); }
        }

        public static string lessthanorequal_error
        {
            get { return PropertyError("LessThanOrEqualError", "{ComparisonValue}"); }
        }

        public static string notempty_error
        {
            get { return PropertyError("NotEmptyError"); }
        }

        public static string notequal_error
        {
            get { return PropertyError("NotEqualError", "{ComparisonValue}"); }
        }

        public static string notnull_error
        {
            get { return PropertyError("NotNullError"); }
        }

        public static string predicate_error
        {
            get { return PropertyError("PredicateError"); }
        }

        public static string regex_error
        {
            get { return PropertyError("RegexError"); }
        }

        public static string EnumTryParseError
        {
            get { return PropertyError("EnumTryParseError"); }
        }
    }
}