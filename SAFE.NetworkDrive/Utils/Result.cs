namespace SAFE.NetworkDrive
{
    public static class Result
    {
        public static Result<T> OK<T>(T value)
        {
            return new Result<T>(value, true);
        }

        public static Result<T> Fail<T>(string errorMsg)
        {
            return new Result<T>(default, false, errorMsg);
        }
    }

    public class Result<T>
    {
        public bool HasValue { get; private set; }

        public string ErrorMsg { get; }

        public T Value { get; private set; }

        public Result(T value, bool hasValue, string errorMsg = "")
        {
            Value = value;
            HasValue = hasValue;
            ErrorMsg = errorMsg ?? string.Empty;
        }
    }
}
