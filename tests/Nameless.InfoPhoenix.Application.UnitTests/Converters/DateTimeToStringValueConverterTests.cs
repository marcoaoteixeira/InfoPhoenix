using System.Globalization;

namespace Nameless.InfoPhoenix.Application.Converters;

public class DateTimeToStringValueConverterTests {
    [TestCase("pt-BR", "31/12/2024 14:30:15")]
    [TestCase("en-US", "12/31/2024 2:30:15 PM")]
    public void WhenConvertDateTime_ThenResultShouldBeStringDate(string cultureName, string expected) {
        var culture = new CultureInfo(cultureName);
        var date = new DateTime(year: 2024,
                                month: 12,
                                day: 31,
                                hour: 14,
                                minute: 30,
                                second: 15);
        var sut = new DateTimeToStringValueConverter();

        var actual = sut.Convert(date, typeof(string), parameter: null, culture);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [TestCase("pt-BR", "31/12/2024 14:30:15")]
    [TestCase("en-US", "12/31/2024 2:30:15 PM")]
    public void WhenConvertBackStringDateTime_ThenResultShouldBeDateTime(string cultureName, string stringDateTime) {
        var culture = new CultureInfo(cultureName);
        var expected = new DateTime(year: 2024,
                                month: 12,
                                day: 31,
                                hour: 14,
                                minute: 30,
                                second: 15);
        var sut = new DateTimeToStringValueConverter();

        var actual = sut.ConvertBack(stringDateTime, typeof(string), parameter: null, culture);

        Assert.That(actual, Is.EqualTo(expected));
    }
}