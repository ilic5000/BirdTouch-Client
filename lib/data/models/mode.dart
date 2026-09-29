/// The two ways a user can be visible to others. Used as a path segment by the API.
enum Mode {
  private('private'),
  business('business');

  const Mode(this.apiName);

  final String apiName;

  static Mode fromApi(String value) => values.firstWhere(
    (m) => m.apiName == value,
    orElse: () => throw FormatException('Unknown mode $value'),
  );
}
