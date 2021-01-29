using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http.Routing;
using FluentValidation;
using Microsoft.SqlServer.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Prototype1.Foundation.Data;
using Prototype1.Foundation.Interfaces;
using Prototype1.Foundation.Web.Mvc;

namespace Prototype1.Foundation
{
    public static class ExtensionMethodsGeneral
    {
        #region Generic

        public static List<T> CopyAndAdd<T>(this ICollection<T> list, params T[] items)
        {
            var newList = new List<T>(list);
            if (items.Any())
                newList.AddRange(items);
            return newList;
        }

        public static void SafeAdd<T>(this ICollection<T> list, T item)
        {
            if (!list.Contains(item))
                list.Add(item);
        }

        public static void SafeRemove<T>(this ICollection<T> list, T item)
        {
            if (list.Contains(item))
                list.Remove(item);
        }

        public static void AddOrReplace<T>(this ICollection<T> list, T item)
            where T : EntityBase
        {
            T original = list.FindById(item.ID);
            if (original != null)
                list.Remove(original);
            list.Add(item);
        }

        public static T FindById<T>(this IEnumerable<T> list, Guid id)
            where T : IIdentifiable<Guid>
        {
            return list.FirstOrDefault(i => i != null && i.ID.Equals(id));
        }

        public static T CloneWithReflection<T>(this T obj)
        {
            return (T) CloneWithReflectionWorker(obj);
        }

        public static int RemoveAll<T>(this IList<T> list, Predicate<T> match)
        {
            var count = 0;

            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (!match(list[i])) continue;

                ++count;
                list.RemoveAt(i);
            }

            return count;
        }

        private static object CloneWithReflectionWorker(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var type = obj.GetType();

            // If the type of object is the value type, we will always get a new object when  
            // the original object is assigned to another variable. So if the type of the  
            // object is primitive or enum, we just return the object. We will process the  
            // struct type subsequently because the struct type may contain the reference  
            // fields. 
            // If the string variables contain the same chars, they always refer to the same  
            // string in the heap. So if the type of the object is string, we also return the  
            // object. 
            if (type.IsPrimitive || type.IsEnum || type == typeof (string))
            {
                return obj;
            }

            // If the type of the object is the Array, we use the CreateInstance method to get 
            // a new instance of the array. We also process recursively this method in the  
            // elements of the original array because the type of the element may be the reference  
            // type. 
            if (type.IsArray)
            {
                Type typeElement = Type.GetType(type.FullName.Replace("[]", string.Empty));
                var array = obj as Array;
                Array copiedArray = Array.CreateInstance(typeElement, array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    // Get the deep clone of the element in the original array and assign the  
                    // clone to the new array. 
                    copiedArray.SetValue(CloneWithReflection(array.GetValue(i)), i);

                }
                return copiedArray;
            }

            // If the type of the object is class or struct, it may contain the reference fields,  
            // so we use reflection and process recursively this method in the fields of the object  
            // to get the deep clone of the object.  
            // We use Type.IsValueType method here because there is no way to indicate directly whether  
            // the Type is a struct type. 
            if (type.IsClass || type.IsValueType)
            {
                object copiedObject = Activator.CreateInstance(obj.GetType());
                // Get all FieldInfo. 
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    object fieldValue = field.GetValue(obj);
                    if (fieldValue != null)
                    {
                        // Get the deep clone of the field in the original object and assign the  
                        // clone to the field in the new object. 
                        field.SetValue(copiedObject, CloneWithReflection(fieldValue));
                    }

                }
                return copiedObject;
            }

            throw new ArgumentException("The object is unknown type");
        }

        public static T Clone<T>(this T obj)
        {
            if (obj == null)
                return default(T);

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Context =
                    new StreamingContext(
                        StreamingContextStates.Clone);
                formatter.Serialize(stream, obj);
                stream.Position = 0;
                return (T) formatter.Deserialize(stream);
            }
        }

        public static string ToJson(this object obj)
        {
            if(obj is ExpandoObject)
                return JsonConvert.SerializeObject(obj, new ExpandoObjectConverter());
            return JsonConvert.SerializeObject(obj, Formatting.None,
                new JsonSerializerSettings {NullValueHandling = NullValueHandling.Ignore});
        }

        public static T FromJson<T>(this string str)
        {
            if (typeof(T) == typeof(ExpandoObject))
                return JsonConvert.DeserializeObject<T>(str, new ExpandoObjectConverter());
            return JsonConvert.DeserializeObject<T>(str);
            //return new JavaScriptSerializer().Deserialize<T>(str);
        }

        public static void AddPropertyAndValue(this ExpandoObject expando, string propName, object value)
        {
            ((IDictionary<string, object>) expando)[propName] = value;
        }

        #endregion

        #region Nullable<Generic>

        public static T IfNull<T>(this T? nullable, T replaceNullWith) where T : struct
        {
            return nullable.HasValue ? nullable.Value : replaceNullWith;
        }

        public static T? IfNull<T>(this T? nullable, T? replaceNullWith) where T : struct
        {
            return nullable.HasValue ? nullable : replaceNullWith;
        }

        public static string IfNull<T>(this T? nullable, string replaceNullWith) where T : struct
        {
            return nullable.HasValue ? nullable.Value.ToString() : replaceNullWith;
        }

        #endregion

        #region DateTime

        /// <summary>
        /// Converts a string to an integer
        /// </summary>
        /// <param name="str">str value to convert</param>
        /// <param name="CatchWithZero">Indicates if a 0 should be returned on error</param>
        /// <returns></returns>
        public static int ToInt(this string str, int CatchWithNumber)
        {
            try
            {
                return int.Parse(str);
            }
            catch
            {
                return CatchWithNumber;
            }
        }

        /// <summary>
        /// Converts a string to a datetime
        /// </summary>
        /// <param name="str">str value to convert</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string str, bool isUTC = false)
        {
            if (!isUTC)
                return DateTime.Parse(str);
            else
            {
                DateTime dtUTC = DateTime.Parse(str);
                return new DateTime(dtUTC.Year, dtUTC.Month, dtUTC.Day, dtUTC.Hour, dtUTC.Minute, dtUTC.Second,
                    DateTimeKind.Utc);
            }
        }

        public static string FormatWithTimeZone(this DateTime date, string timeZone)
        {
            return date.ToString("MM/dd/yyyy hh:mm:ss tt ") +
                   TimeZoneInfo.FindSystemTimeZoneById(timeZone).StandardName.Abbreviate();
        }

        public static string GetName(this DayOfWeek dayOfWeek)
        {
            return DateTimeFormatInfo.CurrentInfo.GetDayName(dayOfWeek);
        }
        
        public static string MaxLength(this string str, int maxLen)
        {
            if (str.IsNullOrEmpty())
                return str;

            return str.Substring(0, Math.Min(maxLen, str.Length));
        }

        public static string SplitCamelCase(this string input)
        {
            return Regex.Replace(input, "(?<=[a-z])([A-Z])", " $1", RegexOptions.Compiled);
        }

        public static DateTime ToTimeZone(this DateTime dateTime, string timeZone)
        {
            return dateTime.ToTimeZone(TimeZoneInfo.FindSystemTimeZoneById(timeZone));
        }

        public static DateTime ToTimeZone(this DateTime dateTime, TimeZoneInfo timeZone)
        {
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(dateTime, timeZone.Id);
        }

        public static DateTime ToUtc(this DateTime dateTime, string timeZone)
        {
            if (dateTime.Kind == DateTimeKind.Local)
                dateTime = new DateTime(dateTime.Ticks, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(dateTime, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
        }

        public static DateTimeOffset ToDateTimeOffset(this DateTime dateTime, TimeZoneInfo timeZone)
        {
            return new DateTimeOffset(dateTime, timeZone.GetUtcOffset(dateTime));
        }

        public static int MinutesFromMidnightSunday(this DateTime dateTime)
        {
            return
                (int)
                    new TimeSpan((int) dateTime.DayOfWeek, dateTime.TimeOfDay.Hours, dateTime.TimeOfDay.Minutes, 0)
                        .TotalMinutes;
        }

        #endregion

        #region DateTimeOffset

        public static DateTimeOffset AdjustForDST(this DateTimeOffset dateTimeOffset, TimeZoneInfo timeZone)
        {
            if (timeZone.SupportsDaylightSavingTime && timeZone.IsDaylightSavingTime(dateTimeOffset))
            {
                var rules = timeZone.GetAdjustmentRules();
                foreach (
                    var adjustmentRule in
                        rules.Where(
                            adjustmentRule =>
                                adjustmentRule.DateStart <= dateTimeOffset && adjustmentRule.DateEnd >= dateTimeOffset))
                {
                    dateTimeOffset = dateTimeOffset.Add(adjustmentRule.DaylightDelta);
                }
            }
            return dateTimeOffset;
        }

        #endregion

        #region EntityBase

        public static bool IsNew(this EntityBase entity)
        {
            if (entity is IVersioned)
                return ((IVersioned) entity).Version.Equals(-1);

            return entity.ID.Equals(Guid.Empty);
        }

        public static void SyncEntity<T>(this T into, T from, List<string> ignoreProperties = null) where T : EntityBase
        {
            ignoreProperties = ignoreProperties ?? new List<string>();

            foreach (
                var p in
                    into.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetField)
                        .Where(p => p.CanRead && p.CanWrite && p.Name != "ID" && !ignoreProperties.Contains(p.Name)))
            {
                var ownership =
                    (p.GetCustomAttributes(typeof (OwnershipAttribute), true)
                        .OfType<OwnershipAttribute>().FirstOrDefault()
                     ?? new OwnershipAttribute(Ownership.Exclusive)).Ownership;
                if (ownership != Ownership.Exclusive)
                    continue;

                var transient = p.GetCustomAttributes(typeof (TransientAttribute), true).Any();
                if (transient)
                    continue;

                var collectionType = TypeHelpers.ExtractGenericInterface(p.PropertyType, typeof (IList<>));
                if (collectionType != null)
                {
                    Type genericArgType;
                    if ((genericArgType = collectionType.GetGenericArguments().FirstOrDefault()) != null
                        && genericArgType.IsSubclassOf(typeof (EntityBase)))
                    {
                        var intoCollection = ((IList) p.GetValue(into)).OfType<EntityBase>().ToList();
                        var fromCollection = ((IList) p.GetValue(from)).OfType<EntityBase>().ToList();

                        IList<EntityBase> toAdd, toRemove;
                        intoCollection.SyncEntityList(fromCollection, out toAdd, out toRemove, ignoreProperties);
                        foreach (var a in toAdd)
                            ((IList) p.GetValue(into)).Add(a);
                        foreach (var r in toRemove)
                            ((IList) p.GetValue(into)).Remove(r);
                    }
                    continue;
                }

                p.SetValue(into, p.GetValue(from));
            }
        }

        public static void SyncEntityList<T>(this IList<T> intoCollection, IList<T> fromCollection,
            List<string> ignoreProperties = null)
            where T : EntityBase
        {
            IList<EntityBase> toAdd, toRemove;
            intoCollection.SyncEntityList(fromCollection, out toAdd, out toRemove, ignoreProperties);
        }

        public static void SyncEntityList<T>(this IList<T> intoCollection, IList<T> fromCollection,
            out IList<EntityBase> toAdd, out IList<EntityBase> toRemove, List<string> ignoreProperties = null)
            where T : EntityBase
        {
            toAdd = new List<EntityBase>();
            foreach (var f in fromCollection)
            {
                var match = intoCollection.FirstOrDefault(c => c.ID == f.ID);
                if (!f.ID.ToString().IsNullOrEmpty() && f.ID.ToString() != Guid.Empty.ToString() && match != null)
                    match.SyncEntity(f, ignoreProperties);
                else
                {
                    toAdd.Add(f);
                    intoCollection.Add(f);
                }
            }

            var fromIDs = fromCollection.Select(x => x.ID).ToArray();
            toRemove = intoCollection.Where(i => !fromIDs.Contains(i.ID)).OfType<EntityBase>().ToList();
            intoCollection.RemoveAll(i => !fromIDs.Contains(i.ID));
        }

        #endregion

        #region Enumerations

        public static bool AreAllFlagsSet(this Enum currentEnum)
        {
            var allFlagsValue = EnumHelper.GetAllFlagsValue(currentEnum.GetType());
            return (Convert.ToInt32(currentEnum) == allFlagsValue);
        }
        public static T GetEnumAttribute<T>(this Enum currentEnum)
            where T : Attribute
        {
            return (T)Attribute.GetCustomAttribute(currentEnum.GetType().GetField(currentEnum.ToString()), typeof(T));
        }

        public static string GetDescription(this Enum currentEnum)
        {
            var da = currentEnum.GetEnumAttribute<DescriptionAttribute>();
            return da != null ? da.Description : currentEnum.ToString();
        }

        public static int FlagsSet(this Enum currentEnum)
        {
            Type t = currentEnum.GetType();
            if (!t.GetCustomAttributes(typeof (FlagsAttribute), false).Any())
                throw new ArgumentException("This method is only valid on flags enums");

            int flagsSet = 0;
            foreach (var value in t.FilterEnum())
            {
                if (currentEnum.HasFlag((Enum) value))
                    flagsSet++;
            }
            return flagsSet;
        }

        /// <summary>
        /// Returns an IEnumerable of all of the Enums in the passed in Flags Enum that isn't of value '0'
        /// </summary>
        public static IEnumerable<Enum> EnumerateFlags(this Enum flagEnum)
        {
            Type t = flagEnum.GetType();
            if (!t.GetCustomAttributes(typeof(FlagsAttribute), false).Any())
                throw new ArgumentException("This method is only valid on flags enums");

            foreach (var value in t.FilterEnum())
            {
                if ((int)value != 0 && flagEnum.HasFlag((Enum)value))
                    yield return (Enum)value;
            }
        }

        public static bool HasFlags(this Enum flagEnum, Enum compareFlagEnum)
        {
            foreach (var e in compareFlagEnum.EnumerateFlags())
                if (flagEnum.HasFlag(e))
                    return true;

            return false;
        }

        public static bool HasAllFlags(this Enum flagEnum, Enum compareFlagEnum)
        {
            foreach (var e in compareFlagEnum.EnumerateFlags())
                if (!flagEnum.HasFlag(e))
                    return false;

            return true;
        }

        public static T MaxFlag<T>(this T flagEnum)
        {
            var value = (int) Convert.ChangeType(flagEnum, typeof (int));
            IEnumerable<int> setValues = typeof (T).FilterEnum().Cast<int>().Where(f => (f & value) == f);
            var intValue = setValues.Any() ? setValues.Max() : 0;
            return (T) Enum.Parse(typeof (T), intValue.ToString());
        }

        public static bool Is<T>(this Enum type, T value)
        {
            try
            {
                return (int) (object) type == (int) (object) value;
            }
            catch
            {
                return false;
            }
        }

        public static T Add<T>(this Enum type, T value)
        {
            var t = value.GetType();
            if (!t.GetCustomAttributes(typeof (FlagsAttribute), false).Any())
                throw new ArgumentException("This method is only valid on flags enums");

            try
            {
                return (T) (object) (((int) (object) type | (int) (object) value));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Could not append value from enumerated type '{typeof (T).Name}'.", ex);
            }
        }

        public static T Remove<T>(this Enum type, T value)
        {
            var t = value.GetType();
            if (!t.GetCustomAttributes(typeof (FlagsAttribute), false).Any())
                throw new ArgumentException("This method is only valid on flags enums");

            try
            {
                return (T) (object) (((int) (object) type & ~(int) (object) value));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Could not remove value from enumerated type '{typeof (T).Name}'.", ex);
            }
        }

        public static T Toggle<T>(this Enum type, T value)
        {
            var t = value.GetType();
            if (!t.GetCustomAttributes(typeof (FlagsAttribute), false).Any())
                throw new ArgumentException("This method is only valid on flags enums");

            try
            {
                return (T) (object) (((int) (object) type ^ (int) (object) value));
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Could not toggle value from enumerated type '{typeof (T).Name}'.", ex);
            }
        }

        public static Array FilterEnum(this Type type)
        {
            if (!type.IsEnum)
                throw new ArgumentException("Type must be an enum.");

            return (from field in type.GetFields(BindingFlags.Static | BindingFlags.Public)
                where !type.GetMember(field.Name)[0].GetCustomAttributes(typeof (ObsoleteAttribute), false).Any()
                select field.GetValue(null))
                .ToArray();
        }

        #endregion

        #region Exceptions

        public static string GetFullMessage(this Exception ex)
        {
            string message = ex.Message;

            if (ex.InnerException != null)
                message += " | " + ex.InnerException.GetFullMessage();

            return message;
        }

        public static List<Type> GetExceptionTypes(this Exception ex)
        {
            var types = new List<Type>();
            types.Add(ex.GetType());
            if (ex.InnerException != null)
                types.AddRange(ex.InnerException.GetExceptionTypes());

            return types;
        }

        public static Exception GetInnerMostException(this Exception ex)
        {
            return ex.InnerException == null ? ex : ex.InnerException.GetInnerMostException();
        }

        #endregion

        #region Framework Types

        /// <summary>
        /// Applies the action to each element in the list.
        /// </summary>
        /// <typeparam name="T">The enumerable item's type.</typeparam>
        /// <param name="enumerable">The elements to enumerate.</param>
        /// <param name="action">The action to apply to each item in the list.</param>
        public static IEnumerable<T> Apply<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
                action(item);

            return enumerable;
        }

        public static bool Contains(this IEnumerable<string> strings, string value, StringComparison comparison)
        {
            return strings.Any(x => x.Equals(value, comparison));
        }

        public static bool Contains(this string source, string value, StringComparison comparison)
        {
            if (source.IsNullOrEmpty() || value.IsNullOrEmpty()) return false;
            return source.IndexOf(value, 0, comparison) != -1;
        }

        public static IEnumerable<T> Execute<Target, T>(this IEnumerable<Target> targets, Func<Target, T> func)
        {
            foreach (var item in targets)
                yield return func(item);
        }

        public static List<TimeSpan> To(this TimeSpan start, TimeSpan end, int incrementMinutes)
        {
            List<TimeSpan> list = new List<TimeSpan>();
            TimeSpan current = end;

            if (end < start)
                current = end.Add(new TimeSpan(24, 0, 0));

            list.Add(current);
            while (current > start)
            {
                current = current.Subtract(new TimeSpan(0, incrementMinutes, 0));
                if (current >= start)
                    list.Insert(0, current);
            }
            return list;
        }

        public static string ToTimeString(this TimeSpan t, string Timezone)
        {
            return
                $"{(t.Hours > 12 ? t.Hours - 12 : t.Hours)}:{t.Minutes:00} {(t.Hours > 12 ? "PM" : "AM")} {Timezone}"
                    .Trim();
        }

        public static DayOfWeek Next(this DayOfWeek day)
        {
            if (day == DayOfWeek.Saturday)
                return DayOfWeek.Sunday;

            return (DayOfWeek) ((int) day + 1);
        }

        public static DayOfWeek Previous(this DayOfWeek day)
        {
            if (day == DayOfWeek.Sunday)
                return DayOfWeek.Saturday;

            return (DayOfWeek) ((int) day - 1);
        }

        public static DateTime Next(this DateTime date, DayOfWeek dayOfWeek)
        {
            date = date.AddDays(1);
            while (date.DayOfWeek != dayOfWeek)
            {
                date = date.AddDays(1);
            }
            return date;
        }

        public static string ToShortTimeString(this TimeSpan timeSpan)
        {
            return new DateTime(timeSpan.Ticks, DateTimeKind.Local).ToShortTimeString();
        }

        public static string ToCssClassString(this IEnumerable<string> strings)
        {
            var sb = new StringBuilder();
            if (strings != null)
                foreach (var s in strings)
                {
                    sb.Append(s);
                    sb.Append(" ");
                }
            return sb.ToString();
        }

        #endregion

        #region IGeolocatable

        public static SqlGeography GetGeocode(this IGeolocatable place)
        {
            var builder = new SqlGeographyBuilder();
            builder.SetSrid(4326);
            builder.BeginGeography(OpenGisGeographyType.Point);
            builder.BeginFigure(place.Latitude, place.Longitude);
            builder.EndFigure();
            builder.EndGeography();
            return builder.ConstructedGeography;
        }

        #endregion

        #region Reflection

        public static T As<T>(this object obj)
        {
            if (obj == null || !(obj is T))
                return default(T);

            return (T) obj;
        }

        public static string PropertyName<T>(this object obj, Expression<Func<T, object>> expression)
        {
            return expression.GetMemberInfo().Name;
        }

        public static PropertyInfo Property<T>(this T obj, Expression<Func<T, object>> expression)
        {
            return expression.GetMemberInfo() as PropertyInfo;
        }

        public static PropertyInfo GetPropertyForMethod(this MethodInfo methodInfo)
        {
            var prop = methodInfo.DeclaringType.GetProperties()
                .FirstOrDefault(
                    p =>
                        p.GetSetMethod() == methodInfo || p.GetSetMethod() == methodInfo ||
                        p.Name == methodInfo.Name.Substring(4));

            return prop;
        }

        public static IEnumerable<T> GetAttributes<T>(this MemberInfo member, bool inherit)
        {
            return Attribute.GetCustomAttributes(member, inherit).OfType<T>();
        }

        public static bool IsTransient(this MemberInfo member)
        {
            return member.GetAttributes<TransientAttribute>(false).Any();
        }

        public static PropertyInfo GetPropertyCaseInsensitive(this Type type, string propertyName)
        {
            var typeList = new List<Type> {type};

            if (type.IsInterface)
                typeList.AddRange(type.GetInterfaces());

            var flags = BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance;

            return typeList
                .Select(interfaceType => interfaceType.GetProperty(propertyName, flags))
                .FirstOrDefault(property => property != null);
        }

        public static object GetPropertyValue(this object obj, string propertyName)
        {
            if (obj == null)
                return null;
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null)
                throw new Exception($"No property Named {propertyName} found on Type {obj.GetType().Name}");
            return prop.GetValue(obj, null);
        }

        public static MemberInfo GetMemberInfo(this Expression expression)
        {
            var lambda = (LambdaExpression) expression;

            MemberExpression memberExpression;
            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = (UnaryExpression) lambda.Body;
                memberExpression = (MemberExpression) unaryExpression.Operand;
            }
            else
                memberExpression = (MemberExpression) lambda.Body;

            return memberExpression.Member;
        }

        #endregion

        #region GUID

        /// <summary>
        /// Returns the integer representation of the GUID's byte array
        /// </summary>
        public static int ToInt(this Guid guid)
        {
            return BitConverter.ToInt32(guid.ToByteArray(), 0);
        }

        public static bool IsNullOrEmpty(this Guid guid)
        {
            return (guid == Guid.Empty);
        }

        public static bool IsNullOrEmpty(this Guid? guid)
        {
            return (guid == null || guid == Guid.Empty);
        }

        public static Guid IfNullOrEmpty(this Guid guid, Guid replace)
        {
            return guid.IsNullOrEmpty() ? replace : guid;
        }

        #endregion

        #region Cookie helpers

        public static string GetValue(this HttpCookie cookie)
        {
            return cookie == null
                ? string.Empty
                : cookie.Value;
        }

        public static void SetValue(this HttpCookie cookie, string value)
        {
            if (cookie != null)
            {
                cookie.Value = value;
            }
        }

        #endregion

        #region IEnumerable.InSetOf

        public static IEnumerable<IList<T>> Partition<T>(this IEnumerable<T> src, int num)
        {
            IEnumerator<T> enu = src.GetEnumerator();
            while (true)
            {
                List<T> result = new List<T>(num);
                for (int i = 0; i < num; i++)
                {
                    if (!enu.MoveNext())
                    {
                        if (i > 0) yield return result;
                        yield break;
                    }
                    result.Add(enu.Current);
                }
                yield return result;
            }
        }

        #endregion

        #region HttpRequest

        /// <summary>
        /// <para>Converts Request.UserHostAddress to System.NetIPAddress</para>
        /// Returns null if parsing fails
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public static IPAddress UserHostAddresstoIPAddress(this HttpRequestBase request)
        {
            IPAddress ip;

            string ip_header = request.UserHostAddress;

            if (!IPAddress.TryParse(ip_header, out ip))
            {
                return null;
            }

            return ip;
        }

        #endregion

        #region FluentValidator

        public static IRuleBuilderOptions<T, TProperty> WithMessage<T, TProperty>(
            this IRuleBuilderOptions<T, TProperty> rule,
            Func<T, string> messageBuilder)
        {
            return rule.Configure(cfg => { cfg.MessageBuilder = context => messageBuilder((T) context.Instance); });
        }

        #endregion

        #region Strings

        public static string MD5Hash(this string str)
        {
            //Check wether data was passed
            if (string.IsNullOrEmpty(str))
                return String.Empty;

            //Calculate MD5 hash. This requires that the string is splitted into a byte[].
            using (var md5 = new MD5CryptoServiceProvider())
            {
                var textToHash = Encoding.Default.GetBytes(str);
                var result = md5.ComputeHash(textToHash);

                //Convert result back to string.
                return BitConverter.ToString(result);
            }
        }

        public static bool IsNumeric(this string str)
        {
            decimal output;
            return decimal.TryParse(str, out output);
        }

        public static bool IsAlpha(this string str)
        {
            return str.ToUpper().All(c => char.IsLetter(c));
        }

        public static bool IsValidEmailAddress(this string email)
        {
            return
                new Regex(
                    "^((([a-z]|\\d|[!#\\$%&'\\*\\+\\-\\/=\\?\\^_`{\\|}~]|[\\u00A0-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFEF])+(\\.([a-z]|\\d|[!#\\$%&'\\*\\+\\-\\/=\\?\\^_`{\\|}~]|[\\u00A0-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFEF])+)*)|((\\x22)((((\\x20|\\x09)*(\\x0d\\x0a))?(\\x20|\\x09)+)?(([\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x7f]|\\x21|[\\x23-\\x5b]|[\\x5d-\\x7e]|[\\u00A0-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFEF])|(\\\\([\\x01-\\x09\\x0b\\x0c\\x0d-\\x7f]|[\\u00A0-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFEF]))))*(((\\x20|\\x09)*(\\x0d\\x0a))?(\\x20|\\x09)+)?(\\x22)))@((([a-z]|\\d|[\\u00A0-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFEF])|(([a-z]|\\d|[\\u00A0-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFEF])([a-z]|\\d|-||_|~|[\\u00A0-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFEF])*([a-z]|\\d|[\\u00A0-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFEF])))\\.)+(([a-z]|[\\u00A0-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFEF])+|(([a-z]|[\\u00A0-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFEF])+([a-z]+|\\d|-|\\.{0,1}|_|~|[\\u00A0-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFEF])?([a-z]|[\\u00A0-\\uD7FF\\uF900-\\uFDCF\\uFDF0-\\uFFEF])))$",
                    RegexOptions.IgnoreCase)
                    .IsMatch(email);
        }

        /// <summary>
        /// 1 -> A<br/>
        /// 2 -> B<br/>
        /// ...
        /// 27 -> AA<br/>
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public static string ToExcelColumn(this int column)
        {
            string columnString = "";
            decimal columnNumber = column;
            while (columnNumber > 0)
            {
                decimal currentLetterNumber = (columnNumber - 1) % 26;
                char currentLetter = (char)(currentLetterNumber + 65);
                columnString = currentLetter + columnString;
                columnNumber = (columnNumber - (currentLetterNumber + 1)) / 26;
            }
            return columnString;
        }

        /// <summary>
        /// A -> 1<br/>
        /// B -> 2<br/>
        /// ...
        /// AA -> 27<br/>
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public static int FromExcelColumn(this string column)
        {
            int retVal = 0;
            string col = column.ToUpper();
            for (int iChar = col.Length - 1; iChar >= 0; iChar--)
            {
                char colPiece = col[iChar];
                int colNum = colPiece - 64;
                retVal = retVal + colNum * (int)Math.Pow(26, col.Length - (iChar + 1));
            }
            return retVal;
        }

        /// <summary>
        /// Will Strip html tags from a string. It is useful when using strings with html in areas that don't 
        /// support html like inside an html tag where it expects text only.
        /// **!!Faster than regex version StripTags()!!**
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string StripTagsCharArray(this string source)
        {
            if (source != null)
            {
                char[] array = new char[source.Length];
                int arrayIndex = 0;
                bool inside = false;

                for (int i = 0; i < source.Length; i++)
                {
                    char let = source[i];
                    if (let == '<')
                    {
                        inside = true;
                        continue;
                    }
                    if (let == '>')
                    {
                        inside = false;
                        continue;
                    }
                    if (!inside)
                    {
                        array[arrayIndex] = let;
                        arrayIndex++;
                    }
                }
                return new string(array, 0, arrayIndex);
            }
            return source;
        }

        public static bool Contains(this string str, params string[] args)
        {
            return args.Any(str.Contains);
        }

        public static string CleanString(this string str, bool removeAccents = true, char[] preserve = null)
        {
            try
            {
                str = str.IfNullOrEmpty("");

                if (removeAccents)
                    str = str.RemoveAccents();

                byte[] utf8 = Encoding.UTF8.GetBytes(str);

                string ret = "";
                foreach (char ch in Encoding.ASCII.GetString(utf8))
                    if (char.IsLetterOrDigit(ch) || ch == ' ' || ch == '.' || ch == ':' || ch == '-' ||
                        (preserve != null && preserve.Contains(ch)))
                        ret += ch;
                    else
                        ret += " ";

                return ret;
            }
            catch
            {
                return str;
            }
        }

        public static string RemoveAccents(this string str)
        {
            String normalizedString = str.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < normalizedString.Length; i++)
            {
                Char c = normalizedString[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    stringBuilder.Append(c);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Formats a digit-only phone number to a phone number with dashes
        /// </summary>
        /// <param name="strPhone">Unformatted phone number</param>
        /// <returns>Formatted phone number (1-###-###-###)</returns>
        public static string FormatFaxNumber(this string Phone)
        {
            try
            {
                string s = "";

                //strip the phone number first to make sure that the formatting is done correctly
                Phone = Phone.StripPhoneNumber();

                if (Phone.Length == 10)
                {
                    s = "1-" + Phone.Substring(0, 3) + "-";
                    s += Phone.Substring(3, 3) + "-";
                    s += Phone.Substring(6);
                }
                else if (Phone.Length == 11)
                {
                    s = Phone.Substring(0, 1) + "-" + Phone.Substring(1, 3) + "-";
                    s += Phone.Substring(4, 3) + "-";
                    s += Phone.Substring(7);
                }
                else
                {
                    s = Phone;
                }
                return s;
            }
            catch
            {
                return Phone;
            }
        }

        /// <summary>
        /// Formats a digit-only phone number to a phone number with dashes
        /// </summary>
        /// <param name="strPhone">Unformatted phone number</param>
        /// <returns>Formatted phone number ((###) ###-###)</returns>
        public static string FormatPhoneNumber(this string Phone)
        {
            try
            {
                string s = "";

                //strip the phone number first to make sure that the formatting is done correctly
                Phone = Phone.StripPhoneNumber();

                if (Phone.Length == 10)
                {
                    s = "(" + Phone.Substring(0, 3) + ") ";
                    s += Phone.Substring(3, 3) + "-";
                    s += Phone.Substring(6);
                }
                else
                {
                    s = Phone;
                }
                return s;
            }
            catch
            {
                return Phone;
            }
        }

        public static string FormatPhoneNumber(this string Phone, string CountryAbbr)
        {
            if (string.IsNullOrEmpty(Phone))
                return Phone;

            try
            {
                string s = "";

                //strip the phone number first to make sure that the formatting is done correctly
                Phone = Phone.StripPhoneNumber();

                if (Phone.Length == 10 && (CountryAbbr == "US" || CountryAbbr == "CA"))
                {
                    s = "(" + Phone.Substring(0, 3) + ") ";
                    s += Phone.Substring(3, 3) + "-";
                    s += Phone.Substring(6);
                }
                else if (Phone.Length == 10 && CountryAbbr == "MX")
                {
                    s = "(" + Phone.Substring(0, 2) + ") ";
                    s += Phone.Substring(2, 4) + " ";
                    s += Phone.Substring(6, 4) + " ";
                }
                else
                {
                    s = Phone;
                }
                return s;
            }
            catch
            {
                return Phone;
            }
        }

        /// <summary>
        /// Formats a digit-only CVV and replaces it with *'s
        /// </summary>
        /// <param name="CVV">Unformatted CVV</param>
        /// <returns>Formatted CVV</returns>
        public static string FormatCVV(this string CVV)
        {
            string ret = "";
            for (int i = 0; i < CVV.Length; i++)
            {
                ret += "*";
            }
            return ret;
        }

        public static string FormatCCNumber(this string CCN, bool Dashes)
        {
            return CCN.FormatCCNumber(Dashes, false);
        }

        public static string FormatCCNumber(this string CCN, bool Dashes, bool Partial)
        {
            if (CCN == null)
                return null;

            if (CCN.Length == 16)
            {
                if (Partial)
                    if (Dashes)
                        return "****-****-****-" + CCN.Substring(12);
                    else
                        return "************" + CCN.Substring(12);
                else if (Dashes)
                    return CCN.Substring(0, 4) + "-" + CCN.Substring(4, 4) + "-" + CCN.Substring(8, 4) + "-" +
                           CCN.Substring(12, 4);
                else
                    return CCN;
            }
            else if (CCN.Length == 15)
            {
                if (Partial)
                    if (Dashes)
                        return "****-******-*" + CCN.Substring(11);
                    else
                        return "***********" + CCN.Substring(11);
                else if (Dashes)
                    return CCN.Substring(0, 4) + "-" + CCN.Substring(4, 6) + "-" + CCN.Substring(10, 5);
                else
                    return CCN;
            }
            else if (CCN.Length > 4)
            {
                string ret = "";
                for (int i = 0; i < CCN.Length - 4; i++)
                {
                    ret += "*";
                }
                return ret + CCN.Substring(CCN.Length - 4);
            }
            else
            {
                return CCN.FormatCVV();
            }
        }

        public static string FormatGCNumber(this string GCN, bool Partial)
        {
            char[] ret = GCN.ToCharArray();

            if (Partial)
                for (int i = (GCN.Length > 1 ? 1 : 0); i < GCN.Length - 4; i++)
                    ret[i] = '*';

            return new string(ret);
        }

        /// <summary>
        /// Strips out all phone number characters other than the digits.
        /// </summary>
        /// <param name="strPhone">Formatted phone number</param>
        /// <returns>Unformatted phone number (##########)</returns>
        public static string StripPhoneNumber(this string Phone)
        {
            if (string.IsNullOrEmpty(Phone))
                return Phone;
            return Phone.Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "").Replace(".", "");
        }

        public static double GetDoubleFromCurrency(this string Currency)
        {
            string output = "";
            foreach (char c in Currency.ToCharArray())
                if (char.IsDigit(c) || c == '.')
                    output += c;
            return ((Currency.StartsWith("-") ? "-" : "") + output).ToDouble();
        }


        /// <summary>
        /// Converts a string to a boolean
        /// </summary>
        /// <param name="str">str value to convert</param>
        /// <returns></returns>
        public static bool ToBool(this string str)
        {
            if (str.IsNullOrEmpty())
                return false;
            if (str == "1")
                return true;
            else if (str == "0")
                return false;
            else if (str.ToLower() == "yes")
                return true;
            else if (str.ToLower() == "no")
                return false;
            else
                return bool.Parse(str);
        }

        public static bool ToBool(this string str, bool catchWithValue)
        {
            if (str.IsNullOrEmpty())
                return catchWithValue;

            try
            {
                return str.ToBool();
            }
            catch
            {
                return catchWithValue;
            }
        }

        /// <summary>
        /// Converts a string to a GUID
        /// </summary>
        /// <param name="str">str value to convert</param>
        /// <returns></returns>
        public static Guid ToGuid(this string str)
        {
            return str.ToGuid(true);
        }

        /// <summary>
        /// Converts a string to a GUID
        /// </summary>
        /// <param name="str">str value to convert</param>
        /// <param name="CatchWithEmpty">If true, Guid.Empty will be returned on error</param>
        /// <returns></returns>
        public static Guid ToGuid(this string str, bool CatchWithEmpty)
        {
            if (CatchWithEmpty)
                try
                {
                    return new Guid(str);
                }
                catch
                {
                    try
                    {
                        return str.FormatGuid();
                    }
                    catch
                    {
                        return Guid.Empty;
                    }
                }
            else
                try
                {
                    return new Guid(str);
                }
                catch
                {
                    return str.FormatGuid();
                }
        }

        public static Guid FormatGuid(this string str)
        {
            return new Guid(str.Substring(0, 8) + "-" +
                            str.Substring(8, 4) + "-" +
                            str.Substring(12, 4) + "-" +
                            str.Substring(16, 4) + "-" +
                            str.Substring(20));
        }

        /// <summary>
        /// Converts a string to a double
        /// </summary>
        /// <param name="str">str value to convert</param>
        /// <returns></returns>
        public static double ToDouble(this string str)
        {
            return str.ToDouble(false);
        }

        /// <summary>
        /// Converts a string to a double
        /// </summary>
        /// <param name="str">str value to convert</param>
        /// <returns></returns>
        public static double ToDouble(this string str, bool catchWithMinValue)
        {
            if (catchWithMinValue)
                try
                {
                    return double.Parse(str);
                }
                catch
                {
                    return double.MinValue;
                }
            else
            {
                double n;
                double.TryParse(str, out n);
                return n;
            }
        }

        /// <summary>
        /// Converts a string to a double
        /// </summary>
        /// <param name="str">str value to convert</param>
        /// <returns></returns>
        public static decimal ToDecimal(this string str)
        {
            return str.ToDecimal(false);
        }

        /// <summary>
        /// Converts a string to a double
        /// </summary>
        /// <param name="str">str value to convert</param>
        /// <returns></returns>
        public static decimal ToDecimal(this string str, bool catchWithMinValue)
        {
            if (catchWithMinValue)
                try
                {
                    return decimal.Parse(str);
                }
                catch
                {
                    return decimal.MinValue;
                }
            else
                return decimal.Parse(str);
        }

        /// <summary>
        /// Converts a string to a double
        /// </summary>
        /// <param name="str">str value to convert</param>
        /// <returns></returns>
        public static decimal ToDecimal(this string str, decimal catchWithValue)
        {
            try
            {
                return decimal.Parse(str);
            }
            catch
            {
                return catchWithValue;
            }
        }

        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        public static string IfNullOrEmpty(this string str, string replaceNullWith)
        {
            return string.IsNullOrEmpty(str) ? replaceNullWith : str;
        }

        /// <summary>
        /// Converts a string to a float
        /// </summary>
        /// <param name="str">str value to convert</param>
        /// <returns></returns>
        public static float ToFloat(this string str)
        {
            return float.Parse(str);
        }

        /// <summary>
        /// Converts a string to an integer
        /// </summary>
        /// <param name="str">str value to convert</param>
        /// <returns></returns>
        public static int ToInt(this string str)
        {
            return int.Parse(str);
        }

        /// <summary>
        /// Returns a string consisting of the first letter of every word
        /// </summary>
        public static string Abbreviate(this string str)
        {
            string[] words = str.Split(" ".ToCharArray());
            string strabbr = "";
            foreach (string word in words)
                strabbr += word[0];

            return strabbr;
        }

        /// <summary>
        /// Returns an enum of type T whoes string value matches the passed in string
        /// </summary>
        public static T ToEnum<T>(this string str, bool ignoreCase = true)
        {
            return (T) Enum.Parse(typeof (T), str, ignoreCase);
        }

        public static string ToAbsoluteUrl(this string relativeUrl, bool useBaseUrl = false, bool useCurrentPort = false)
        {
            //VALIDATE INPUT
            if (String.IsNullOrEmpty(relativeUrl))
                return String.Empty;

            //VALIDATE INPUT FOR ALREADY ABSOLUTE URL
            if (relativeUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                relativeUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return relativeUrl;

            //VALIDATE CONTEXT
            if (HttpContext.Current == null && OperationContext.Current == null)
                return relativeUrl;

            //GET CONTEXT OF CURRENT USER
            HttpContext context = HttpContext.Current;

            //FIX ROOT PATH TO APP ROOT PATH
            if (relativeUrl.StartsWith("/"))
            {
                relativeUrl = relativeUrl.Insert(0, "~");
            }

            //PREPARE TO USE IN VIRTUAL PATH UTILITY
            if (!relativeUrl.StartsWith("~/"))
            {
                relativeUrl = relativeUrl.Insert(0, "~/");
            }

            relativeUrl = VirtualPathUtility.ToAbsolute(relativeUrl);

            Uri url;
            string host, protocol = "http";
            if (useBaseUrl)
            {
                var baseUrl = ConfigurationManager.AppSettings["BaseUrl"];
                var isSsl = ConfigurationManager.AppSettings["EnableSSL"].ToBool(false);

                url = new Uri(baseUrl);

                if (useCurrentPort)
                {
                    if (HttpContext.Current != null && HttpContext.Current.Request != null)
                        host = url.Host + ":" + HttpContext.Current.Request.Url.Port;
                    else
                        host = url.Host + ":" + OperationContext.Current.EndpointDispatcher.EndpointAddress.Uri.Port;
                }
                else
                    host = url.Host + (url.Port.In(80, 443) ? string.Empty : (":" + url.Port));

                if (isSsl)
                    protocol = "https";

            }
            else
            {
                if (HttpContext.Current != null && HttpContext.Current.Request != null)
                {
                    url = HttpContext.Current.Request.Url;
                    host = HttpContext.Current.Request.Headers["Host"];
                }
                else
                {
                    url = OperationContext.Current.EndpointDispatcher.EndpointAddress.Uri;
                    host = url.Host + (url.Port.In(80, 81) ? String.Empty : ":" + url.Port);
                }
                protocol = url.Port == 81 ? "https" : url.Scheme;
            }

            //BUILD AND RETURN ABSOLUTE URL
            return $"{protocol}://{host}{relativeUrl}";
        }

        public static string Base64StringDecode(this string value)
        {
            byte[] decbuff = Convert.FromBase64String(value);
            return Encoding.UTF8.GetString(decbuff);
        }

        public static bool TryBase64StringDecode(this string value, out string decoded)
        {
            decoded = string.Empty;
            try
            {
                byte[] decbuff = Convert.FromBase64String(value);
                decoded = Encoding.UTF8.GetString(decbuff);
                return true;
            }
            catch { return false; }
        }

        public static string Base64StringEncode(this string value)
        {
            var encbuff = Encoding.UTF8.GetBytes(value);
            return Convert.ToBase64String(encbuff);
        }

        public static string Right(this string value, int length)
        {
            return value.IsNullOrEmpty() ? "" : value.Substring(Math.Max(value.Length - length, 1));
        }

        public static string OrDefault(this string original, string val)
        {
            return string.IsNullOrWhiteSpace(original) ? val : original;
        }

        public static string MaskCreditCardNumber(this string number)
        {
            return (number == null || number.Length < 14)
                ? number
                : new string('*', number.Length - 4) + number.Substring(number.Length - 4, 4);
        }

        public static string MaskGiftCardNumber(this string number)
        {
            return (number == null || number.Length < 5)
                ? number
                : new string('*', number.Length - 4) + number.Substring(number.Length - 4, 4);
        }

        public static string TrimEnd(this string str, string trim)
        {
            return trim.Reverse().Aggregate(str, (current, c) => current.TrimEnd(c));
        }
        
        public static bool StartsWith(this string str, params string[] args)
        {
            return str.StartsWith(StringComparison.CurrentCultureIgnoreCase, args);
        }

        public static bool StartsWith(this string str, StringComparison comparison, params string[] args)
        {
            return args.Any(a => str.StartsWith(a, comparison));
        }

        public static bool EndsWith(this string str, params string[] args)
        {
            return args.Any(str.EndsWith);
        }
        
        public static IEnumerable<string> SplitByLineLength(this string str, int lineLength)
        {
            if (str.IsNullOrEmpty() || str.Trim().IsNullOrEmpty())
                yield break;

            var strToSplit = str;

            while (true)
            {
                yield return strToSplit.MaxLength(lineLength).TrimEnd();
                if (strToSplit.Length <= lineLength)
                    break;
                strToSplit = strToSplit.Substring(lineLength);
            }
        }

        /// <summary>
        /// Remove any HTML tags but not their content
        /// </summary>
        public static string RemoveHtmlTags(this string input)
        {
            return Regex.Replace(input, @"<(.|\n)*?>", string.Empty);
        }

        public static bool IsHtmlEncoded(this string text)
        {
            return (HttpUtility.HtmlDecode(text) != text);
        }

        #endregion

        #region Numbers

        public static string FormatCurrency(this float Money)
        {
            return Convert.ToDouble(Money).FormatCurrency();
        }

        public static string FormatCurrency(this double Money)
        {
            if (Money >= 0)
                return $"{Money:c}";
            else
                return "-" + $"{-1*Money:c}";
        }

        public static bool ToBool(this int Integer)
        {
            return Integer.ToString().ToBool();
        }

        public static string FormatOptionItemPrice(this decimal amount, bool insideSpan = false,
            string spanClassName = "",
            bool showPlus = true)
        {
            string resp = "";
            if (amount > 0)
            {
                resp = amount.FormatCurrency();
                if (insideSpan)
                    resp = $@" <span class=""{spanClassName}"">({(showPlus ? "+ " : "")}{resp})</span>";
            }
            return resp;
        }

        public static string FormatCurrency(this decimal money)
        {
            if (money >= 0)
                return $"{money:c}";
            else
                return "-" + $"{-1*money:c}";
        }

        public static double Round(this double amount, bool roundUp = false, int decimalPlaces = 2)
        {
            if (roundUp)
                return Math.Ceiling(amount * Math.Pow(10, decimalPlaces)) / Math.Pow(10, decimalPlaces);

            return Math.Round(amount, decimalPlaces, MidpointRounding.AwayFromZero);
        }

        public static decimal Round(this decimal amount, bool roundUp = false, int decimalPlaces = 2)
        {
            if (roundUp)
                return
                    Convert.ToDecimal(Math.Ceiling(amount * Convert.ToDecimal(Math.Pow(10, decimalPlaces))) /
                                      Convert.ToDecimal(Math.Pow(10, decimalPlaces)));

            return Math.Round(amount, decimalPlaces, MidpointRounding.AwayFromZero);
        }

        #endregion

        #region Objects

        public static bool In<T>(this T obj, params T[] args)
        {
            return args.Contains(obj);
        }

        #endregion

        #region Bools

        public static int ToInt(this bool b)
        {
            return (b ? 1 : 0);
        }

        #endregion

        #region Type

        public static bool HasConcreteImplementation(this Type type, Type @interface)
        {
            if (!@interface.IsInterface)
                throw new ArgumentException($"{@interface.FullName} is not an interface.");

            if (!@interface.IsAssignableFrom(type))
                throw new ArgumentException($"{type.FullName} does not implement {@interface.FullName}");

            var interfaceProperties = @interface.GetProperties();
            foreach (var interfaceProperty in interfaceProperties)
            {
                var property = type.GetProperty(interfaceProperty.Name,
                    BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
                if (property == null || property.GetGetMethod().IsAbstract)
                    return false;
            }
            return true;
        }

        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
        }

        #endregion

        #region IEnumerable

        public static IEnumerable<T> Flatten<T>(this T root, Func<T, IEnumerable<T>> getChildernFunc)
        {
            yield return root;

            var descendants = getChildernFunc(root);
            if (descendants != null)
            {
                foreach (T element in descendants)
                {
                    foreach (T elementinner in element.Flatten(getChildernFunc))
                    {
                        yield return elementinner;
                    }
                }
            }
        }

        #endregion

        #region SqlGeography

        public static double[][][] Polygon(this SqlGeography zone)
        {
            var ring = zone.STGeometryN(1);
            var points = new List<double[]>();

            for (int i = 1; i <= ring.STNumPoints(); i++)
            {
                var point = new double[2];
                var sp = ring.STPointN(i);
                //we can safely round the lat/long to 5 decimal places as thats 1.11m at equator, reduces data transfered to client
                point[0] = Math.Round((double) sp.Lat, 5);
                point[1] = Math.Round((double) sp.Long, 5);
                points.Add(point);
            }

            var coordinates = new double[1][][];
            coordinates[0] = points.ToArray();
            return coordinates;
        }

        #endregion

        #region HttpRequest

        /// <summary>
        /// <para>Will compare HttpRequestBase[key] == value and also HttpRequestBase.Headers[key] == value</para>
        /// case insensitive
        /// </summary>
        /// <param name="request"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Contains(this HttpRequestBase request, string key, string value)
        {
            if (request[key] != null)
            {
                return string.Equals(request[key], value, StringComparison.OrdinalIgnoreCase);
            }
            else if (request.Headers[key] != null)
            {
                return string.Equals(request.Headers[key], value, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        #endregion

        public static string GetHeaderValue(this HttpRequestHeaders headers, string key)
        {
            IEnumerable<string> headerValues;
            headers.TryGetValues(key, out headerValues);
            return headerValues != null ? headerValues.FirstOrDefault() : null;
        }

        public static IHttpRouteData GetSubRouteData(this HttpRequestMessage request)
        {
            const string subRoutesKey = "MS_SubRoutes";

            var routeData = request.GetRouteData();
            object value;
            return !routeData.Values.TryGetValue(subRoutesKey, out value)
                ? null
                : value.As<IEnumerable<IHttpRouteData>>()
                    .FirstOrDefault()
                    .As<IHttpRouteData>();
        }

        public static T[] SafeToArray<T>(this IEnumerable<T> source)
        {
            return source == null ? new T[0] : source.ToArray();
        }
    }
}