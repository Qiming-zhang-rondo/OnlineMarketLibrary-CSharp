using System;
using System.Globalization;
using System.Text;

namespace OnlineMarket.Core.Utils;

public static class InvoiceHelper
{
    private static readonly CultureInfo enUS = CultureInfo.CreateSpecificCulture("en-US");

    static InvoiceHelper()
    {
        enUS.DateTimeFormat.ShortDatePattern = "yyyyMMdd";
    }

    public static string GetInvoiceNumber(int customerId, DateTime timestamp, int orderId)
    {
        return $"{customerId}-{timestamp.ToString("d", enUS)}-{orderId}";
    }
}