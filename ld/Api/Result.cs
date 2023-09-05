
namespace Language.Api;

public class Result<T, E>
{
    private readonly T? _t;
    private readonly E? _e;

    public Result(T ok)
    {
        _t = ok;
    }
    public Result(E err)
    {
        _e = err;
    }

    public bool IsOkay()
        => _t != null;
    public bool IsErr()
        => _e != null;

    public T Unwrap()
        => _t!;
    public E UnwrapErr()
        => _e!;
    public T UnwrapOr(T value)
        => IsOkay() ? Unwrap() : value; 
}
