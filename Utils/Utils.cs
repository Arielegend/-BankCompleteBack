namespace back.Utils
{
    public static class Utils
    {
        public static List<DateTime> GetListOfDatesFromString(string listOfDatesAsString)
        {
            var listOfDates = new List<DateTime>();
            foreach (var date in listOfDatesAsString.Split(";"))
            {
                if (date.Length > 0)
                    listOfDates.Add(DateTime.Parse(date));
            }
            return listOfDates;
        }
        public static string GetStringFromListOfDates(List<DateTime> listOfDates)
        {
            if (listOfDates.Count == 0)
            {
                return String.Empty;
            }
            var listOfDatesString = string.Empty;
            foreach (var date in listOfDates)
            {
                listOfDatesString += ";" + date.ToString();
            }
            return listOfDatesString.Remove(0, 1);
        }
    }
}
