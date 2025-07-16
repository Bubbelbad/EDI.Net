using indice.Edi.FormatSpec;
using indice.Edi.Serialization;
using System.Globalization;
using System.Text;

namespace indice.Edi.Utilities;

/// <summary>
/// Helper Extension methods.
/// </summary>
public static class EdiExtensions
{
    /// <summary>
    /// Check to see if the given <see cref="EdiToken"/> represents the begining of a structural container.
    /// </summary>
    /// <param name="token">The token to check</param>
    /// <returns></returns>
    public static bool IsStartToken(this EdiToken token) {
        switch (token) {
            case EdiToken.SegmentStart:
            case EdiToken.ElementStart:
            case EdiToken.ComponentStart:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Check to see if the given <see cref="EdiToken"/> represents primitive value type.
    /// </summary>
    /// <param name="token">The token to check</param>
    /// <returns></returns>
    public static bool IsPrimitiveToken(this EdiToken token) {
        switch (token) {
            case EdiToken.Integer:
            case EdiToken.Float:
            case EdiToken.String:
            case EdiToken.Boolean:
            case EdiToken.Null:
            case EdiToken.Date:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Allows the filtering of tokens based on the <see cref="EdiStructureType"/> they represent 
    /// instead of the build in way using the corresponding CLR <seealso cref="Type"/>. 
    /// </summary>
    /// <param name="attributes">The list of available attributes</param>
    /// <param name="container">This is the type of container we are searchig attributes for.</param>
    /// <returns>The attributes matching the <see cref="EdiStructureType"/></returns>
    public static IEnumerable<EdiAttribute> OfType(this IEnumerable<EdiAttribute> attributes, EdiStructureType container) {
        var typesToSearch = new Type[0];
        switch (container) {
            case EdiStructureType.None:
            case EdiStructureType.Interchange:
                break;
            case EdiStructureType.Group:
                typesToSearch = [typeof(EdiGroupAttribute)];
                break;
            case EdiStructureType.Message:
                typesToSearch = [typeof(EdiMessageAttribute)];
                break;
            case EdiStructureType.SegmentGroup:
                typesToSearch = [typeof(EdiSegmentGroupAttribute)];
                break;
            case EdiStructureType.Segment:
                typesToSearch = [typeof(EdiSegmentAttribute), typeof(EdiSegmentGroupAttribute)];
                break;
            case EdiStructureType.Element:
                typesToSearch = [typeof(EdiElementAttribute)];
                break;
            default:
                break;
        }

        return null == typesToSearch ? Enumerable.Empty<EdiAttribute>() : attributes.Where(a => typesToSearch.Contains(a.GetType()));
    }

    /// <summary>
    /// Figures out the container (<see cref="EdiStructureType"/>) based upon the current pool of <seealso cref="EdiStructureAttribute"/>.
    /// </summary>
    /// <param name="attributes"></param>
    /// <returns></returns>
    public static EdiStructureType InferStructure(this IEnumerable<EdiAttribute> attributes) {
        var structureType = EdiStructureType.None;
        foreach (var attribute in attributes) {
            if (!(attribute is EdiStructureAttribute)) {
                continue;
            }
            structureType = attribute is EdiSegmentGroupAttribute ? EdiStructureType.SegmentGroup :
                            attribute is EdiSegmentAttribute ? EdiStructureType.Segment :
                            attribute is EdiElementAttribute ? EdiStructureType.Element :
                            attribute is EdiMessageAttribute ? EdiStructureType.Message :
                            attribute is EdiGroupAttribute ? EdiStructureType.Group : EdiStructureType.None;
            if (structureType != EdiStructureType.None) {
                return structureType;
            }
        }
        return structureType;
    }

    /// <summary>
    /// Numberic Parse helper from string to <see cref="decimal"/>. 
    /// </summary>
    /// <param name="value">The decimal string representation</param>
    /// <param name="picture">The format spec</param>
    /// <param name="decimalMark">The character used to represent a decimal point</param>
    /// <param name="number">The outcome</param>
    /// <returns></returns>
    public static bool TryParse(this string value, IFormatSpec formatSpec, char? decimalMark, out decimal number) {
        number = 0.0M;
        try {
            var result = Parse(value, formatSpec, decimalMark);
            if (result.HasValue)
                number = result.Value;
            return true;
        } catch {
            return false;
        }
    }

    /// <summary>
    /// Parses a string representation of a date into a clr <see cref="DateTime"/> struct
    /// </summary>
    /// <param name="value">The string date value</param>
    /// <param name="format">The dotnet style format string</param>
    /// <param name="culture">The culture info</param>
    /// <returns></returns>
    public static DateTime ParseEdiDate(this string value, string format, CultureInfo culture = null) {
        var date = ParseEdiDateInternal(value, format, culture);
        return date.Value;
    }

    /// <summary>
    /// Parses a string representation of a date into a clr <see cref="DateTime"/> struct
    /// </summary>
    /// <param name="value">The string date value</param>
    /// <param name="format">The dotnet style format string</param>
    /// <param name="culture">The culture info</param>
    /// <param name="date">The outcome</param>
    /// <returns></returns>
    public static bool TryParseEdiDate(this string value, string format, CultureInfo culture, out DateTime date) {
        date = default(DateTime);
        var dateNullable = ParseEdiDateInternal(value, format, culture);
        if (dateNullable.HasValue)
            date = dateNullable.Value;
        return dateNullable.HasValue;
    }

    private static DateTime? ParseEdiDateInternal(string value, string format, CultureInfo culture = null) {
        if (string.IsNullOrWhiteSpace(format))
            throw new ArgumentOutOfRangeException(nameof(format));

        var wrapped = new StringBuilder(value);
        var dayShift = false;
        if (format.Contains("HH")) {
            var startIndex = format.IndexOf('H');
            if (wrapped[startIndex] == '2' && wrapped[startIndex + 1] == '4') {
                wrapped[startIndex] = wrapped[startIndex + 1] = '0';
                dayShift = true;
            }
        }
        if (format.Contains("ss") && format.Length > wrapped.Length) { // forgive the lack of the secconds.
            var startIndex = format.IndexOf('s');
            if (startIndex > wrapped.Length - 1) {
                wrapped.Append("00");
            }
        }
        if (format.Contains("f") && format.Length > wrapped.Length) { // forgive the lack of a fraction of a seccond.
            var startIndex = format.IndexOf('f');
            var endIndex = format.LastIndexOf('f');
            if (endIndex > wrapped.Length - 1) {
                for (var i = startIndex; i <= endIndex; i++) {
                    if (wrapped.Length > i)
                        continue;
                    wrapped.Append('0');
                }
            }
        }
        DateTime dt;
        if (DateTime.TryParseExact(wrapped.ToString(), format, culture ?? CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out dt)) {
            return dayShift
            ? dt.AddDays(1)
            : dt;
        } 
        return null;
    }

    /// <summary>
    /// Parse the <see cref="string"/> value into a decimal using any formatting hints available from the grammar itself as well as value spec.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="formatSpec"></param>
    /// <param name="decimalMark"></param>
    /// <returns></returns>
    public static decimal? Parse(this string value, IFormatSpec formatSpec, char? decimalMark) {
        if (value != null)
            value = value.TrimStart('Z'); // Z suppresses leading zeros
        if (string.IsNullOrEmpty(value))
            return null;
        decimal d;
        var provider = NumberFormatInfo.InvariantInfo;
        if (decimalMark.HasValue && formatSpec.Kind == FormatKind.Numeric && decimal.TryParse(value, NumberStyles.Integer, provider, out d)) {
            d = d * (decimal)Math.Pow(0.1, formatSpec.Precision);
            return d;
        } else if (decimalMark.HasValue) {
            if (provider.NumberDecimalSeparator != decimalMark.ToString()) {
                provider = provider.Clone() as NumberFormatInfo;
                provider.NumberDecimalSeparator = decimalMark.Value.ToString();
            }
            if (decimal.TryParse(value, NumberStyles.Number, provider, out d)) {
                return d;
            }
        }
        throw new EdiException("Could not convert string to decimal: {0}.".FormatWith(CultureInfo.InvariantCulture, value));
    }

    /// <summary>
    /// Converts the given value into a string representation according to the <see cref="IEdiGrammar"/> and the value spec.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="formatSpec"></param>
    /// <param name="decimalMark"></param>
    /// <returns></returns>
    public static string ToEdiString(this float value, IFormatSpec formatSpec, char? decimalMark) =>
        ToEdiString((decimal?)value, formatSpec, decimalMark);

    /// <summary>
    /// Converts the given value into a string representation according to the <see cref="IEdiGrammar"/> and the value spec.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="formatSpec"></param>
    /// <param name="decimalMark"></param>
    /// <returns></returns>
    public static string ToEdiString(this double value, IFormatSpec formatSpec, char? decimalMark) =>
        ToEdiString((decimal?)value, formatSpec, decimalMark);
    
    /// <summary>
    /// Converts the given value into a string representation according to the <see cref="IEdiGrammar"/> and the value spec.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="formatSpec"></param>
    /// <param name="decimalMark"></param>
    /// <returns></returns>
    public static string ToEdiString(this decimal value, IFormatSpec formatSpec, char? decimalMark) =>
        ToEdiString((decimal?)value, formatSpec, decimalMark);

    /// <summary>
    /// Converts the given value into a string representation according to the <see cref="IEdiGrammar"/> and the value spec.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="formatSpec"></param>
    /// <param name="decimalMark"></param>
    /// <returns></returns>
    public static string ToEdiString(this float? value, IFormatSpec formatSpec, char? decimalMark) =>
        ToEdiString((decimal?)value, formatSpec, decimalMark);

    /// <summary>
    /// Converts the given value into a string representation according to the <see cref="IEdiGrammar"/> and the value spec.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="formatSpec"></param>
    /// <param name="decimalMark"></param>
    /// <returns></returns>
    public static string ToEdiString(this double? value, IFormatSpec formatSpec, char? decimalMark) =>
        ToEdiString((decimal?)value, formatSpec, decimalMark);

    /// <summary>
    /// Converts the given value into a string representation according to the <see cref="IEdiGrammar"/> and the value spec.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="formatSpec"></param>
    /// <returns></returns>
    public static string ToEdiString(this int? value, IFormatSpec formatSpec) =>
        ToEdiString((long?)value, formatSpec);

    /// <summary>
    /// Converts the given value into a string representation according to the <see cref="IEdiGrammar"/> and the value spec.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="formatSpec"></param>
    /// <returns></returns>
    public static string ToEdiString(this int value, IFormatSpec formatSpec) =>
        ToEdiString((long?)value, formatSpec);
    
    /// <summary>
    /// Converts the given value into a string representation according to the <see cref="IEdiGrammar"/> and the value spec.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="formatSpec"></param>
    /// <returns></returns>
    public static string ToEdiString(this long value, IFormatSpec formatSpec) =>
        ToEdiString((long?)value, formatSpec);

    /// <summary>
    /// Converts the given value into a string representation according to the <see cref="IEdiGrammar"/> and the value spec.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="formatSpec"></param>
    /// <param name="decimalMark"></param>
    /// <returns></returns>
    public static string ToEdiString(this decimal? value, IFormatSpec formatSpec, char? decimalMark) {
        if (!value.HasValue)
            return null;
        var provider = NumberFormatInfo.InvariantInfo;
        if (decimalMark.HasValue) {
            if (provider.NumberDecimalSeparator != decimalMark.ToString()) {
                provider = provider.Clone() as NumberFormatInfo;
                provider.NumberDecimalSeparator = decimalMark.Value.ToString();
            }
            if (formatSpec.Kind == FormatKind.Numeric && formatSpec.HasPrecision) {
                var integerMask = new string(Enumerable.Range(0, formatSpec.Scale - formatSpec.Precision).Select(i => '0').ToArray());
                if (integerMask.Length == 0) {
                    integerMask = "#";
                }
                var precisionMask = new string(Enumerable.Range(0, formatSpec.Precision).Select(i => '0').ToArray());
                if (precisionMask.Length == 0) {
                    precisionMask = "#";
                }
                return value.Value.ToString($"{integerMask}.{precisionMask}", provider);
            }
            return value.Value.ToString(provider);
        } else if (formatSpec.Kind == FormatKind.Numeric && formatSpec.HasPrecision) {
            var number = value.Value;
            var integer = (int)(number * (decimal)Math.Pow(10.0, formatSpec.Precision));
            var padding = new string(Enumerable.Range(0, formatSpec.Scale).Select(i => '0').ToArray());
            var result = integer.ToString(padding);
            return result;
        } else if (formatSpec.Kind == FormatKind.Numeric) {
            var number = value.Value;
            var integer = (int)(number * (decimal)Math.Pow(10.0, formatSpec.Precision));
            var padding = new string(Enumerable.Range(0, formatSpec.Scale).Select(i => '0').ToArray());
            var result = integer.ToString(padding);
            return result;
        } else
            return string.Format(NumberFormatInfo.InvariantInfo, "{0}", value);
    }

    /// <summary>
    /// Converts the given value into a string representation according to the <see cref="IEdiGrammar"/> and the value spec.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="formatSpec"></param>
    /// <returns></returns>
    public static string ToEdiString(this long? value, IFormatSpec formatSpec) 
    {
        if (!value.HasValue && (formatSpec == null)) {
            return null;
        }
        if (value != null) {
            var pic = formatSpec;
            if (pic.Kind == FormatKind.Alphanumeric) {
                if (pic.HasPrecision) {
                    return value.HasValue ? value.Value.ToString() : String.Empty;
                } else {
                    var padding = new string(Enumerable.Range(0, pic.Scale).Select(i => ' ').ToArray());

                    var result = value.HasValue ? (padding + value.Value) : padding;
                    if (result.Length > pic.Scale * 2) {
                        return value.Value.ToString();
                    }

                    return result.Substring(result.Length - pic.Scale, pic.Scale);
                }
            } else if (pic.Kind == FormatKind.Numeric) {

                var padding = new string(Enumerable.Range(0, pic.Scale).Select(i => '0').ToArray());
                return string.Format(CultureInfo.InvariantCulture, "{0:" + padding + "}", value ?? 0);
            }
        }
        return string.Format(NumberFormatInfo.InvariantInfo, "{0}", value);
    }
}
