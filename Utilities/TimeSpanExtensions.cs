namespace Fidobot.Utilities;

public static class TimeSpanExtensions {
  public static string ToDynamicString(this TimeSpan timeSpan) {
    if (timeSpan.TotalSeconds < 1) {
      return "less than a second";
    }

    var nonZeroComponents = new List<(int value, string name)>();

    if (timeSpan.Days > 0) {
      nonZeroComponents.Add((timeSpan.Days, "day"));
    }

    if (timeSpan.Hours > 0) {
      nonZeroComponents.Add((timeSpan.Hours, "hour"));
    }

    if (timeSpan.Minutes > 0) {
      nonZeroComponents.Add((timeSpan.Minutes, "minute"));
    }

    if (timeSpan.Seconds > 0 || nonZeroComponents.Count == 0) {
      nonZeroComponents.Add((timeSpan.Seconds, "second"));
    }

    var formattedComponents = nonZeroComponents
        .Select((c) => $"{c.value} {c.name}{(c.value == 1 ? "" : "s")}")
        .ToList();

    if (formattedComponents.Count == 1) {
      return formattedComponents[0];
    }

    string lastComponent = formattedComponents.Last();
    formattedComponents.RemoveAt(formattedComponents.Count - 1);

    return $"{string.Join(", ", formattedComponents)} and {lastComponent}";
  }
}
