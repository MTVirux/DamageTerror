namespace DamageTerror.Helpers;

public static class JobNameHelper
{
    public static string GetFullName(string abbreviation) => JobDataTable.GetFullName(abbreviation);
}
