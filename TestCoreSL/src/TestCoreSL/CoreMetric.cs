using System.Reflection;

namespace TestCoreSL
{
    public class CoreMetric
    {
        public int Id { get; set; }
        public string? MetricName { get; set; }
        public string? MetricType { get; set; }
        public string? MetricValue { get; set; }
        public string? MetricHost { get; set; }
        public string? MetricTime { get; set; }
    }

    /*
    public class SearchCriteria
    {
        public string? MetricHost { get; set; }

        public static ValueTask<SearchCriteria?> BindAsync(HttpContext context, ParameterInfo parameter)
        {
            string hostname = context.Request.Query["MetricHost"];
            // int.TryParse(context.Request.Query["Id"], out var id);
            var result = new SearchCriteria
            {
                MetricHost = hostname
                // Id = id
            };
            return ValueTask.FromResult<SearchCriteria?>(result);
        }
    }
    */
}

