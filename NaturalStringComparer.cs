namespace DJI_Mission_Installer
{
  public class NaturalStringComparer : IComparer<string>
  {
    #region Constants & Statics

    public static NaturalStringComparer Ordinal           { get; } = new NaturalStringComparer(StringComparison.Ordinal);
    public static NaturalStringComparer OrdinalIgnoreCase { get; } = new NaturalStringComparer(StringComparison.OrdinalIgnoreCase);
    public static NaturalStringComparer CurrentCulture    { get; } = new NaturalStringComparer(StringComparison.CurrentCulture);
    public static NaturalStringComparer CurrentCultureIgnoreCase { get; } =
      new NaturalStringComparer(StringComparison.CurrentCultureIgnoreCase);
    public static NaturalStringComparer InvariantCulture { get; } = new NaturalStringComparer(StringComparison.InvariantCulture);
    public static NaturalStringComparer InvariantCultureIgnoreCase { get; } =
      new NaturalStringComparer(StringComparison.InvariantCultureIgnoreCase);

    #endregion

    #region Properties & Fields - Non-Public

    private readonly StringComparison _comparison;

    #endregion

    #region Constructors

    public NaturalStringComparer(StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
      _comparison = comparison;
    }

    #endregion

    #region Methods Impl

    public int Compare(string? x, string? y)
    {
      // Let string.Compare handle the case where x or y is null
      if (x is null || y is null)
        return string.Compare(x, y, _comparison);

      var xSegments = GetSegments(x);
      var ySegments = GetSegments(y);

      while (xSegments.MoveNext() && ySegments.MoveNext())
      {
        int cmp;

        // If they're both numbers, compare the value
        if (xSegments.CurrentIsNumber && ySegments.CurrentIsNumber)
        {
          var xValue = long.Parse(xSegments.Current);
          var yValue = long.Parse(ySegments.Current);
          cmp = xValue.CompareTo(yValue);
          if (cmp != 0)
            return cmp;
        }
        // If x is a number and y is not, x is "lesser than" y
        else if (xSegments.CurrentIsNumber)
        {
          return -1;
        }
        // If y is a number and x is not, x is "greater than" y
        else if (ySegments.CurrentIsNumber)
        {
          return 1;
        }

        // OK, neither are number, compare the segments as text
        cmp = xSegments.Current.CompareTo(ySegments.Current, _comparison);
        if (cmp != 0)
          return cmp;
      }

      // At this point, either all segments are equal, or one string is shorter than the other

      // If x is shorter, it's "lesser than" y
      if (x.Length < y.Length)
        return -1;

      // If x is longer, it's "greater than" y
      if (x.Length > y.Length)
        return 1;

      // If they have the same length, they're equal
      return 0;
    }

    #endregion

    #region Methods

    private static StringSegmentEnumerator GetSegments(string s) => new StringSegmentEnumerator(s);

    #endregion

    private struct StringSegmentEnumerator
    {
      private readonly string _s;
      private          int    _start;
      private          int    _length;

      public StringSegmentEnumerator(string s)
      {
        _s              = s;
        _start          = -1;
        _length         = 0;
        CurrentIsNumber = false;
      }

      public ReadOnlySpan<char> Current => _s.AsSpan(_start, _length);

      public bool CurrentIsNumber { get; private set; }

      public bool MoveNext()
      {
        var currentPosition = _start >= 0
          ? _start + _length
          : 0;

        if (currentPosition >= _s.Length)
          return false;

        int  start            = currentPosition;
        bool isFirstCharDigit = Char.IsDigit(_s[currentPosition]);

        while (++currentPosition < _s.Length && Char.IsDigit(_s[currentPosition]) == isFirstCharDigit) { }

        _start          = start;
        _length         = currentPosition - start;
        CurrentIsNumber = isFirstCharDigit;

        return true;
      }
    }
  }
}
