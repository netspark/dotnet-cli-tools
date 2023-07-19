namespace NamespacePlaceholder;

QueriesNsPlaceholderCommandsNsPlaceholder
using Swashbuckle.AspNetCore.Filters;

public class ResponsePlaceholderExample : IMultipleExamplesProvider<ResponsePlaceholder>
{
    public IEnumerable<SwaggerExample<ResponsePlaceholder>> GetExamples()
    {
        yield return SwaggerExample.Create(
            "TitlePlaceholder",
            new ResponsePlaceholder()
            {
                // TODO: place sample attributes here
            }
        );
    }
}