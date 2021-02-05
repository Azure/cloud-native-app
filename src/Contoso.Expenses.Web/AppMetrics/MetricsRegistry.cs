using App.Metrics;
using App.Metrics.Counter;

namespace Contoso.Expenses.Web.AppMetrics
{
    public class MetricsRegistry
    {
        public static CounterOptions CreatedExpenseCounter => new CounterOptions
        {
            // App Metrics counter to track number of expenses submitted 
            Name = "Number of Expenses Submitted",
            Context = "Contoso Expenses Web App",
            MeasurementUnit = Unit.Calls
        };
    }
}
