namespace NewVivaApi.Authentication;

public class Response
{
	public string Type { get; set; } = "";
	public string Message { get; set; } = "";
}

public class DataResponse<T> : Response
{
	public T? Data { get; set; } = default!;
}

public class PaginatedDataResponse<T> : DataResponse<List<T>>
{
	public int Total { get; set; }
	public int Skip { get; set; }
	public int Take { get; set; }
}
