using System.Text.RegularExpressions;

namespace indice.Edi.FormatSpec;

/// <summary>
/// Picture Kind is used to specify the pattern of an Edi value.
/// </summary>
public enum PictureKind
{
    /// <summary>
    /// Characters and numbers are allowed
    /// </summary>
    Alphanumeric,

    /// <summary>
    /// Only numbers are allowed.
    /// </summary>
    Numeric
}

/// <summary>
/// Indicates the number of numeric (9) digits or alphanumeric (X) characters allowed in the data field.  
/// If the field is numeric, this excludes any minus sign or the decimal point.  
/// The decimal point is implied and its position within the data field is indicate by V.
/// </summary>
public struct PictureSpec : IFormatSpec
{
    private const string PARSE_PATTERN = @"([9X]{1})\s?\((\d+?)\)\s?(V9\((\d+?)\))?";
    private readonly byte _Precision;
    private readonly ushort _Scale;
    private readonly FormatKind _Kind;


    /// <summary>
    /// This is the total size of the string in digits
    /// </summary>
    public int Scale {
        get { return _Scale; }
    }

    /// <summary>
    /// In case of floating point number this holds the number of decimal places. Its length.
    /// </summary>
    public int Precision {
        get { return _Precision; }
    }

    /// <summary>
    /// indicates the <see cref="Kind" /> of the value represented. (ie <see cref="FormatKind.Alphanumeric"/>)
    /// </summary>
    public FormatKind Kind {
        get { return _Kind; }
    }

    /// <summary>
    /// This indicated if the value is a floating point number with by checking whether <see cref="Precision"/> is positive.
    /// </summary>
    public bool HasPrecision {
        get {
            return _Precision > 0;
        }
    }
    public bool VariableLength => throw new NotImplementedException();

    /// <summary>
    /// Checks of the scale is a positive integer
    /// </summary>
    public bool IsValid {
        get {
            return _Scale > 0;
        }
    }

    public PictureSpec(string spec) // Change byte to ulong? 
    {
        var match = Regex.Match(spec, PARSE_PATTERN);

        if (match != null) 
        {
            var kind = match.Groups[1].Value == "X" ? FormatKind.Alphanumeric : FormatKind.Numeric;
            var length = byte.Parse(match.Groups[2].Value);
            byte decimalLength = 0;

            if (kind == FormatKind.Numeric && !string.IsNullOrWhiteSpace(match.Groups[3].Value)) 
            {
                decimalLength = byte.Parse(match.Groups[4].Value);
            }

            _Scale = (byte)(length + decimalLength);
            _Precision = decimalLength;
            _Kind = kind;

        } else {
            throw new ArgumentException($"Specification string '{spec}' could not be parsed to PictureSpec class", nameof(spec));
        }
    }

    /// <summary>
    /// Constructs an <see cref="FormatKind.Alphanumeric"/> <see cref="PictureSpec"/> of a given <paramref name="length"/>.
    /// </summary>
    public PictureSpec(ushort length) 
    {
        _Scale = length;
        _Precision = 0;
        _Kind = FormatKind.Alphanumeric;
    }

    /// <summary>
    /// Constructs a <see cref="PictureSpec"/> of a given <paramref name="length"/>. Used to instantiate integer formats and alphanumerics.
    /// </summary>
    /// <param name="length"></param>
    /// <param name="kind"></param>
    public PictureSpec(ushort length, FormatKind kind) 
    {
        _Scale = length;
        _Precision = 0;
        _Kind = kind;
    }

    /// <summary>
    /// Constructs a <see cref="PictureKind.Numeric"/> <seealso cref="PictureSpec"/>.
    /// </summary>
    /// <param name="integerLength"></param>
    /// <param name="decimalLength"></param>
    public PictureSpec(ushort integerLength, byte decimalLength) 
    {
        _Scale = (ushort)(integerLength + decimalLength);
        _Precision = decimalLength;
        _Kind = FormatKind.Numeric;
    }

    /// <summary>
    /// Constructs a <see cref="PictureSpec"/>.
    /// </summary>
    /// <param name="integerLength"></param>
    /// <param name="decimalLength"></param>
    /// <param name="kind"></param>
    public PictureSpec(ushort integerLength, byte decimalLength, FormatKind kind) 
    {
        _Scale = (ushort)(integerLength + decimalLength);
        _Precision = decimalLength;
        _Kind = kind;
    }

    /// <summary>
    /// Returns a hash code for the value of this instance.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() => _Kind.GetHashCode() ^ _Scale.GetHashCode() ^ _Precision.GetHashCode();

    /// <summary>
    /// Indictes wheather this instance and the given object are equal.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj) 
    {
        if (obj != null && obj is PictureSpec) 
        {
            var other = (PictureSpec)obj;
            return other.Precision == Precision && other.Scale == Scale;
        }
        return base.Equals(obj);
    }

    /// <summary>
    /// String representation of a <see cref="PictureSpec"/> clause.
    /// </summary>
    /// <returns></returns>
    public override string ToString() 
    {
        switch (Kind) 
        {
            case FormatKind.Alphanumeric:
                return string.Format("X({0})", Scale);

            case FormatKind.Numeric:
                return HasPrecision ? string.Format("9({0}) V9({1})", Scale - Precision, Precision) : string.Format("9({0})", Scale);

            default:
                return string.Format("{0}({1},{2})", Kind, Scale, Precision);
        }
    }

    /// <summary>
    /// Parse a text representation of a <see cref="PictureSpec"/> into the struct.
    /// </summary>
    /// <param name="format"></param>
    /// <returns></returns>
    public static PictureSpec Parse(string format) 
    {
        return new PictureSpec(format); 
    }

    /// <summary>
    /// Implicit cast operator from <see cref="PictureSpec"/> to <seealso cref="string"/>
    /// </summary>
    /// <param name="value"></param>
    public static implicit operator string(PictureSpec value) 
    {
        return value.ToString();
    }

    /// Explicit cast operator from <see cref="string"/> to <seealso cref="PictureSpec"/>
    public static explicit operator PictureSpec(string value) 
    {
        return Parse(value);
    }

}
