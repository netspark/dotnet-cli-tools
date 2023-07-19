namespace NamespacePlaceholder;

QueriesNsPlaceholderCommandsNsPlaceholder
using Swashbuckle.AspNetCore.Filters;

public class RequestPlaceholderExample : IMultipleExamplesProvider<RequestPlaceholder>
{
    public IEnumerable<SwaggerExample<RequestPlaceholder>> GetExamples()
    {
        yield return SwaggerExample.Create(
            "TitlePlaceholder",
            new RequestPlaceholder()
            {
                // TODO: place sample attributes here
            }
        );
    }
}